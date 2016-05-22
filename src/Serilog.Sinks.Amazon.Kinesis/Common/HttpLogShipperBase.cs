using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Serilog.Sinks.Amazon.Kinesis.Common;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis
{
    abstract class HttpLogShipperBase<TRecord, TResponse>
    {
        protected ILog Logger { get; }

        private readonly ILogReaderFactory _logReaderFactory;
        private readonly IPersistedBookmarkFactory _persistedBookmarkFactory;
        private readonly ILogShipperFileManager _fileManager;

        protected readonly int _batchPostingLimit;
        protected readonly string _bookmarkFilename;
        protected readonly string _candidateSearchPath;
        protected readonly string _logFolder;
        protected readonly string _streamName;

        protected HttpLogShipperBase(
            ILogShipperOptions options,
            ILogReaderFactory logReaderFactory,
            IPersistedBookmarkFactory persistedBookmarkFactory,
            ILogShipperFileManager fileManager
            )
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (logReaderFactory == null) throw new ArgumentNullException(nameof(logReaderFactory));
            if (persistedBookmarkFactory == null) throw new ArgumentNullException(nameof(persistedBookmarkFactory));
            if (fileManager == null) throw new ArgumentNullException(nameof(fileManager));

            Logger = LogProvider.GetLogger(GetType());

            _logReaderFactory = logReaderFactory;
            _persistedBookmarkFactory = persistedBookmarkFactory;
            _fileManager = fileManager;

            _batchPostingLimit = options.BatchPostingLimit;
            _streamName = options.StreamName;
            _bookmarkFilename = Path.GetFullPath(options.BufferBaseFilename + ".bookmark");
            _logFolder = Path.GetDirectoryName(_bookmarkFilename);
            _candidateSearchPath = Path.GetFileName(options.BufferBaseFilename) + "*.json";

            Logger.InfoFormat("Candidate search path is {0}", _candidateSearchPath);
            Logger.InfoFormat("Log folder is {0}", _logFolder);
        }


        protected abstract TRecord PrepareRecord(MemoryStream stream);
        protected abstract TResponse SendRecords(List<TRecord> records, out bool successful);
        protected abstract void HandleError(TResponse response, int originalRecordCount);
        public event EventHandler<LogSendErrorEventArgs> LogSendError;

        protected void OnLogSendError(LogSendErrorEventArgs e)
        {
            var handler = LogSendError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private IPersistedBookmark TryCreateBookmark()
        {
            try
            {
                return _persistedBookmarkFactory.Create(_bookmarkFilename);
            }
            catch (IOException ex)
            {
                Logger.TraceException("Bookmark cannot be opened.", ex);
                return null;
            }
        }

        protected void ShipLogs()
        {
            try
            {
                // Locking the bookmark ensures that though there may be multiple instances of this
                // class running, only one will ship logs at a time.

                using (var bookmark = TryCreateBookmark())
                {
                    if (bookmark == null)
                        return;

                    ShipLogs(bookmark);
                }
            }
            catch (IOException ex)
            {
                Logger.WarnException("Error shipping logs", ex);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error shipping logs", ex);
                OnLogSendError(new LogSendErrorEventArgs(string.Format("Error in shipping logs to '{0}' stream", _streamName), ex));
            }
        }

        private void ShipLogs(IPersistedBookmark bookmark)
        {
            do
            {
                string currentFilePath = bookmark.FileName;

                Logger.TraceFormat("Bookmark is currently at offset {0} in '{1}'", bookmark.Position, currentFilePath);

                var fileSet = GetFileSet();

                if (currentFilePath == null)
                {
                    currentFilePath = fileSet.FirstOrDefault();
                    Logger.InfoFormat("New log file is {0}", currentFilePath);
                    bookmark.UpdateFileNameAndPosition(currentFilePath, 0L);
                }
                else if (fileSet.All(f => CompareFileNames(f, currentFilePath) != 0))
                {
                    currentFilePath = fileSet.FirstOrDefault(f => CompareFileNames(f, currentFilePath) >= 0);
                    Logger.InfoFormat("New log file is {0}", currentFilePath);
                    bookmark.UpdateFileNameAndPosition(currentFilePath, 0L);
                }

                // delete all previous files - we will not read them anyway
                if (currentFilePath == null)
                {
                    foreach (var fileToDelete in fileSet)
                    {
                        TryDeleteFile(fileToDelete);
                    }
                }
                else
                {
                    foreach (var fileToDelete in fileSet.TakeWhile(f => CompareFileNames(f, currentFilePath) < 0))
                    {
                        TryDeleteFile(fileToDelete);
                    }
                }

                if (currentFilePath == null)
                {
                    Logger.InfoFormat("No log file is found. Nothing to do.");
                    break;
                }

                // now we are interested in current file and all after it.
                fileSet =
                    fileSet.SkipWhile(f => CompareFileNames(f, currentFilePath) < 0)
                        .ToArray();

                var initialPosition = bookmark.Position;
                List<TRecord> records;
                do
                {
                    var batch = ReadRecordBatch(currentFilePath, bookmark.Position, _batchPostingLimit);
                    records = batch.Item2;
                    if (records.Count > 0)
                    {
                        bool successful;
                        var response = SendRecords(records, out successful);

                        if (!successful)
                        {
                            Logger.ErrorFormat("SendRecords failed for {0} records.", records.Count);
                            HandleError(response, records.Count);
                            return;
                        }
                    }

                    var newPosition = batch.Item1;
                    if (initialPosition < newPosition)
                    {
                        Logger.TraceFormat("Advancing bookmark from {0} to {1} on {2}", initialPosition, newPosition, currentFilePath);
                        bookmark.UpdatePosition(newPosition);
                    }
                    else if (initialPosition > newPosition)
                    {
                        newPosition = 0;
                        Logger.WarnFormat("File {2} has been truncated or re-created, bookmark reset from {0} to {1}", initialPosition, newPosition, currentFilePath);
                        bookmark.UpdatePosition(newPosition);
                    }

                } while (records.Count >= _batchPostingLimit);

                if (initialPosition == bookmark.Position)
                {
                    Logger.TraceFormat("Found no records to process");

                    // Only advance the bookmark if there is next file in the queue 
                    // and no other process has the current file locked, and its length is as we found it.

                    if (fileSet.Length > 1)
                    {
                        Logger.TraceFormat("BufferedFilesCount: {0}; checking if can advance to the next file", fileSet.Length);
                        var weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread = WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(currentFilePath, bookmark.Position);
                        if (weAreAtEndOfTheFileAndItIsNotLockedByAnotherThread)
                        {
                            Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", currentFilePath, fileSet[1]);
                            bookmark.UpdateFileNameAndPosition(fileSet[1], 0);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        Logger.TraceFormat("This is a single log file, and we are in the end of it. Nothing to do.");
                        break;
                    }
                }
            } while (true);
        }

        private Tuple<long, List<TRecord>> ReadRecordBatch(string currentFilePath, long position, int maxRecords)
        {
            var records = new List<TRecord>(maxRecords);
            long positionSent;
            using (var reader = _logReaderFactory.Create(currentFilePath, position))
            {
                do
                {
                    var stream = reader.ReadLine();
                    if (stream.Length == 0)
                    {
                        break;
                    }
                    records.Add(PrepareRecord(stream));
                } while (records.Count < maxRecords);

                positionSent = reader.Position;
            }

            return Tuple.Create(positionSent, records);
        }

        private bool TryDeleteFile(string fileToDelete)
        {
            try
            {
                _fileManager.LockAndDeleteFile(fileToDelete);
                Logger.InfoFormat("Log file deleted: {0}", fileToDelete);
                return true;
            }
            catch (Exception ex)
            {
                Logger.WarnException("Exception deleting file: {0}", ex, fileToDelete);
                return false;
            }
        }

        private bool WeAreAtEndOfTheFileAndItIsNotLockedByAnotherThread(string file, long nextLineBeginsAtOffset)
        {
            try
            {
                return _fileManager.GetFileLengthExclusiveAccess(file) <= nextLineBeginsAtOffset;
            }
            catch (IOException ex)
            {
                Logger.TraceException("Swallowed I/O exception while testing locked status of {0}", ex, file);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Unexpected exception while testing locked status of {0}", ex, file);
            }

            return false;
        }

        private string[] GetFileSet()
        {
            var fileSet = _fileManager.GetFiles(_logFolder, _candidateSearchPath)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Logger.TraceFormat("FileSet contains: {0}", string.Join(";", fileSet));
            return fileSet;
        }

        private static int CompareFileNames(string fileName1, string fileName2)
        {
            return string.Compare(fileName1, fileName2, StringComparison.OrdinalIgnoreCase);
        }
    }
}