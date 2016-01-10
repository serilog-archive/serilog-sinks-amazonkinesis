using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis
{
    internal abstract class HttpLogShipperBase<TRecord, TResponse> : IDisposable
    {
        const long ERROR_SHARING_VIOLATION = 0x20;
        const long ERROR_LOCK_VIOLATION = 0x21;

        ILog _logger;
        protected ILog Logger => _logger ?? (_logger = LogProvider.GetLogger(GetType()));

        protected readonly int _batchPostingLimit;
        protected readonly string _bookmarkFilename;
        protected readonly string _candidateSearchPath;
        protected readonly string _logFolder;
        readonly TimeSpan _period;
        protected readonly object _stateLock = new object();
        protected readonly string _streamName;
        readonly Timer _timer;

        protected volatile bool _unloading;

        protected HttpLogShipperBase(KinesisSinkStateBase state)
        {
            _period = state.SinkOptions.Period;
            _timer = new Timer(s => OnTick());
            _batchPostingLimit = state.SinkOptions.BatchPostingLimit;
            _streamName = state.SinkOptions.StreamName;
            _bookmarkFilename = Path.GetFullPath(state.SinkOptions.BufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(state.SinkOptions.BufferBaseFilename) + "*.json";

            Logger.InfoFormat("Candidate search path is {0}", _candidateSearchPath);
            Logger.InfoFormat("Log folder is {0}", _logFolder);

            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnloading;

            SetTimer();
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

        protected abstract TRecord PrepareRecord(byte[] bytes);
        protected abstract TResponse SendRecords(List<TRecord> records, out bool successful);
        protected abstract void HandleError(TResponse response, int originalRecordCount);
        public event EventHandler<LogSendErrorEventArgs> LogSendError;

        void OnAppDomainUnloading(object sender, EventArgs e)
        {
            CloseAndFlush();
        }

        protected void OnLogSendError(LogSendErrorEventArgs e)
        {
            var handler = LogSendError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        void CloseAndFlush()
        {
            lock (_stateLock)
            {
                if (_unloading)
                    return;

                _unloading = true;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit -= OnAppDomainUnloading;

            var wh = new ManualResetEvent(false);
            if (_timer.Dispose(wh))
                wh.WaitOne();

            OnTick();
        }

        /// <summary>
        ///     Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">
        ///     If true, called because the object is being disposed; if false,
        ///     the object is being disposed from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            CloseAndFlush();
        }

        protected void SetTimer()
        {
            // Note, called under _stateLock

#if NET40
           _timer.Change(_period, TimeSpan.FromDays(30));
#else
            _timer.Change(_period, Timeout.InfiniteTimeSpan);
#endif
        }

        void OnTick()
        {
            try
            {
                int count;

                do
                {
                    count = 0;

                    // Locking the bookmark ensures that though there may be multiple instances of this
                    // class running, only one will ship logs at a time.

                    using (var bookmark = File.Open(_bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        long nextLineBeginsAtOffset;
                        string currentFilePath;

                        TryReadBookmark(bookmark, out nextLineBeginsAtOffset, out currentFilePath);
                        Logger.TraceFormat("Bookmark is currently at offset {0} in '{1}'", nextLineBeginsAtOffset, currentFilePath);

                        var fileSet = GetFileSet();

                        if (currentFilePath == null || !File.Exists(currentFilePath))
                        {
                            nextLineBeginsAtOffset = 0;
                            currentFilePath = fileSet.FirstOrDefault();
                            Logger.InfoFormat("Current log file is {0}", currentFilePath);

                            if (currentFilePath == null) continue;
                        }

                        var records = new List<TRecord>();
                        long startingOffset;
                        using (var current = File.Open(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            startingOffset = current.Position = nextLineBeginsAtOffset;

                            string nextLine;
                            while (count < _batchPostingLimit && TryReadLine(current, ref nextLineBeginsAtOffset, out nextLine))
                            {
                                ++count;
                                var bytes = Encoding.UTF8.GetBytes(nextLine);
                                var record = PrepareRecord(bytes);
                                records.Add(record);
                            }
                        }

                        if (count > 0)
                        {
                            bool successful;
                            var response = SendRecords(records, out successful);

                            if (!successful)
                            {
                                HandleError(response, records.Count);
                            }
                            else
                            {
                                // Advance the bookmark only if we successfully wrote
                                Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", startingOffset, nextLineBeginsAtOffset);
                                WriteBookmark(bookmark, nextLineBeginsAtOffset, currentFilePath);
                            }
                        }
                        else
                        {
                            Logger.TraceFormat("Found no records to process");

                            // Only advance the bookmark if no other process has the
                            // current file locked, and its length is as we found it.

                            var bufferedFilesCount = fileSet.Length;
                            var isProcessingFirstFile = fileSet.First().Equals(currentFilePath, StringComparison.InvariantCultureIgnoreCase);

                            if (bufferedFilesCount == 2 && isProcessingFirstFile)
                            {
                              Logger.TraceFormat("BufferedFilesCount: {0}; AreProcessingFirstFile: true", bufferedFilesCount);
                              var weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread = WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(currentFilePath, nextLineBeginsAtOffset);
                              if (weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread)
                              {
                                Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", currentFilePath, fileSet[1]);
                                WriteBookmark(bookmark, 0, fileSet[1]);                                
                              }
                            }

                            if (bufferedFilesCount > 2)
                            {
                                // Once there's a third file waiting to ship, we do our
                                // best to move on, though a lock on the current file
                                // will delay this.
                                Logger.InfoFormat("Deleting '{0}'", fileSet[0]);

                                File.Delete(fileSet[0]);
                            }
                        }
                    }
                } while (count == _batchPostingLimit);
            }
            catch (IOException ex)
            {
                long win32ErrorCode = GetWin32ErrorCode(ex);

                if (win32ErrorCode == ERROR_SHARING_VIOLATION || win32ErrorCode == ERROR_LOCK_VIOLATION)
                {
                    Logger.TraceException("Swallowed I/O exception", ex);
                }
                else
                {
                    Logger.ErrorException("Unexpected I/O exception", ex);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Exception while emitting periodic batch", ex);
                OnLogSendError(new LogSendErrorEventArgs(string.Format("Error in shipping logs to '{0}' stream)", _streamName), ex));
            }
            finally
            {
                lock (_stateLock)
                {
                    if (!_unloading)
                        SetTimer();
                }
            }
        }

        private long GetWin32ErrorCode(IOException ex)
        {
#if NET40
            long win32ErrorCode = System.Runtime.InteropServices.Marshal.GetHRForException(ex) & 0xFFFF;
#else
            long win32ErrorCode = ex.HResult & 0xFFFF;
#endif
            return win32ErrorCode;
        }

        protected bool WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(string file, long nextLineBeginsAtOffset)
        {
            try
            {
                using (var fileStream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    return fileStream.Length <= nextLineBeginsAtOffset;
                }
            }
            catch (IOException ex)
            {
                long win32ErrorCode = GetWin32ErrorCode(ex);

                if (win32ErrorCode == ERROR_SHARING_VIOLATION || win32ErrorCode == ERROR_LOCK_VIOLATION)
                {
                    Logger.TraceException("Swallowed I/O exception while testing locked status of {0}", ex, file);
                }
                else
                {
                    Logger.ErrorException("Unexpected I/O exception while testing locked status of {0}", ex, file);
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Unexpected exception while testing locked status of {0}", ex, file);
            }

            return false;
        }

        protected static void WriteBookmark(FileStream bookmark, long nextLineBeginsAtOffset, string currentFile)
        {
#if NET40
    // Important not to dispose this StreamReader as the stream must remain open.
            var writer = new StreamWriter(bookmark);
            writer.WriteLine("{0}:::{1}", nextLineBeginsAtOffset, currentFile);
            writer.Flush();
#else
            using (var writer = new StreamWriter(bookmark))
            {
                writer.WriteLine("{0}:::{1}", nextLineBeginsAtOffset, currentFile);
            }
#endif
        }

        // It would be ideal to chomp whitespace here, but not required.
        protected static bool TryReadLine(System.IO.Stream current, ref long nextStart, out string nextLine)
        {
            var includesBom = nextStart == 0;

            if (current.Length <= nextStart)
            {
                nextLine = null;
                return false;
            }

            current.Position = nextStart;

#if NET40
    // Important not to dispose this StreamReader as the stream must remain open.
            var reader = new StreamReader(current, Encoding.UTF8, false, 128);
            nextLine = reader.ReadLine();
#else
            using (var reader = new StreamReader(current, Encoding.UTF8, false, 128, true))
            {
                nextLine = reader.ReadLine();
            }
#endif

            if (nextLine == null)
                return false;

            nextStart += Encoding.UTF8.GetByteCount(nextLine) + Encoding.UTF8.GetByteCount(Environment.NewLine);
            if (includesBom)
                nextStart += 3;

            return true;
        }

        protected static void TryReadBookmark(System.IO.Stream bookmark, out long nextLineBeginsAtOffset, out string currentFile)
        {
            nextLineBeginsAtOffset = 0;
            currentFile = null;

            if (bookmark.Length != 0)
            {
                string current;
#if NET40
    // Important not to dispose this StreamReader as the stream must remain open.
                var reader = new StreamReader(bookmark, Encoding.UTF8, false, 128);
                current = reader.ReadLine();
#else
                using (var reader = new StreamReader(bookmark, Encoding.UTF8, false, 128, true))
                {
                    current = reader.ReadLine();
                }
#endif

                if (current != null)
                {
                    bookmark.Position = 0;
                    var parts = current.Split(new[] {":::"}, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        nextLineBeginsAtOffset = long.Parse(parts[0]);
                        currentFile = parts[1];
                    }
                }
            }
        }

        protected string[] GetFileSet()
        {
            var fileSet = Directory.GetFiles(_logFolder, _candidateSearchPath)
                .OrderBy(n => n)
                .ToArray();
            var fileSetDesc = string.Join(";", fileSet);
            Logger.TraceFormat("FileSet contains: {0}", fileSetDesc);
            return fileSet;
        }
    }
}