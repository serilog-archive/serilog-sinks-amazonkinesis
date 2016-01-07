// Copyright 2014 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Amazon.Kinesis.Model;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis.Stream
{
    class HttpLogShipper : HttpLogShipperBase
    {
        private static readonly ILog Logger = LogProvider.For<HttpLogShipper>();
        private readonly KinesisSinkState _state;

        public HttpLogShipper(KinesisSinkState state):base(state)
        {
            _state = state;
        }

        protected override void OnTick()
        {
            try
            {
                var count = 0;

                do
                {
                    // Locking the bookmark ensures that though there may be multiple instances of this
                    // class running, only one will ship logs at a time.

                    using (var bookmark = File.Open(_bookmarkFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
                    {
                        long startingOffset;
                        long nextLineBeginsAtOffset;
                        string currentFilePath;

                        TryReadBookmark(bookmark, out nextLineBeginsAtOffset, out currentFilePath);
                        Logger.TraceFormat("Bookmark is currently at offset {0} in '{1}'", nextLineBeginsAtOffset, currentFilePath);

                        var fileSet = GetFileSet();

                        if (currentFilePath == null || !File.Exists(currentFilePath))
                        {
                            nextLineBeginsAtOffset = 0;
                            currentFilePath = fileSet.FirstOrDefault();
                            Logger.InfoFormat("Current log file is {0}",currentFilePath);
                        }

                        if (currentFilePath != null)
                        {
                            count = 0;

                            var records = new List<PutRecordsRequestEntry>();
                            using (var current = File.Open(currentFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                startingOffset = current.Position = nextLineBeginsAtOffset;

                                string nextLine;
                                while (count < _batchPostingLimit && TryReadLine(current, ref nextLineBeginsAtOffset, out nextLine))
                                {
                                    ++count;
                                    var bytes = Encoding.UTF8.GetBytes(nextLine);
                                    var record = new PutRecordsRequestEntry
                                    {
                                        PartitionKey = Guid.NewGuid().ToString(),
                                        Data = new MemoryStream(bytes)
                                    };
                                    records.Add(record);
                                }
                            }

                            if (count > 0)
                            {
                                var request = new PutRecordsRequest
                                {
                                    StreamName = _state.Options.StreamName,
                                    Records = records
                                };

                                Logger.TraceFormat("Writing {0} records to kinesis", count);
                                PutRecordsResponse response = _state.KinesisClient.PutRecords(request);

                                if (response.FailedRecordCount > 0)
                                {
                                    foreach (var record in response.Records)
                                    {
                                        Logger.TraceFormat("Kinesis failed to index record in stream '{0}'. {1} {2} ", _state.Options.StreamName, record.ErrorCode, record.ErrorMessage);
                                    }
                                    // fire event
                                    OnLogSendError(new LogSendErrorEventArgs(string.Format("Error writing records to {0} ({1} of {2} records failed)", _state.Options.StreamName, response.FailedRecordCount, count),null));
                                }
                                else
                                {
                                    // Advance the bookmark only if we successfully written to Kinesis Stream
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
                                var isProcessingFirstFile = fileSet.First().Equals(currentFilePath,StringComparison.InvariantCultureIgnoreCase);
                                var isFirstFileUnlocked = IsUnlockedAtLength(currentFilePath, nextLineBeginsAtOffset);
                                Logger.TraceFormat("BufferedFilesCount: {0}; IsProcessingFirstFile: {1}; IsFirstFileUnlocked: {2}", bufferedFilesCount, isProcessingFirstFile, isFirstFileUnlocked);

                                if (bufferedFilesCount == 2 && isProcessingFirstFile && isFirstFileUnlocked)
                                {
                                    Logger.TraceFormat("Advancing bookmark from '{0}' to '{1}'", currentFilePath, fileSet[1]);
                                    WriteBookmark(bookmark, 0, fileSet[1]);
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
                    }
                }
                while (count == _batchPostingLimit);
            }
            catch (Exception ex)
            {
                Logger.DebugException("Exception while emitting periodic batch", ex);
                OnLogSendError(new LogSendErrorEventArgs(string.Format("Error in shipping logs to '{0}' stream)", _state.Options.StreamName),ex));
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
    }
}