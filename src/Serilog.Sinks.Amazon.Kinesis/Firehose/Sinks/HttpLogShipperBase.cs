using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose
{
    internal abstract class HttpLogShipperBase : IDisposable
    {
        static readonly ILog Logger = LogProvider.GetCurrentClassLogger();

        protected volatile bool _unloading;
        protected readonly object _stateLock = new object();
        readonly Timer _timer;
        public event EventHandler<LogSendErrorEventArgs> LogSendError;
        readonly TimeSpan _period;
        protected readonly int _batchPostingLimit;
        protected readonly string _bookmarkFilename;
        protected readonly string _logFolder;
        protected readonly string _candidateSearchPath;
        protected readonly string _streamName;

        protected HttpLogShipperBase(KinesisSinkStateBase state)
        {
            _period = state.SinkOptions.Period;
            _timer = new Timer(s => OnTick());
            _batchPostingLimit = state.SinkOptions.BatchPostingLimit;
            _streamName = state.SinkOptions.StreamName;
            _bookmarkFilename = Path.GetFullPath(state.SinkOptions.BufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(state.SinkOptions.BufferBaseFilename) + "*.json";

            Logger.InfoFormat("Candidate search path is {0}",_candidateSearchPath);
            Logger.InfoFormat("Log folder is {0}",_logFolder);

            AppDomain.CurrentDomain.DomainUnload += OnAppDomainUnloading;
            AppDomain.CurrentDomain.ProcessExit += OnAppDomainUnloading;

            SetTimer();

        }

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
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
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

        protected abstract void OnTick();

        protected static bool IsUnlockedAtLength(string file, long maxLen)
        {
            try
            {
                using (var fileStream = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                {
                    return fileStream.Length <= maxLen;
                }
            }
            catch (IOException ex)
            {
                var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
                if (errorCode != 32 && errorCode != 33)
                {
                    Logger.TraceException("Unexpected I/O exception while testing locked status of {0}", ex, file);
                }
            }
            catch (Exception ex)
            {
                Logger.TraceException("Unexpected exception while testing locked status of {0}", ex, file);
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
                    var parts = current.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
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
            Logger.InfoFormat("FileSet contains: {0}", fileSetDesc);
            return fileSet;
        }
    }
}