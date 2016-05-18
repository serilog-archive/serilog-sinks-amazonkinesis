using System;
using System.IO;
using System.Linq;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Serilog.Sinks.Amazon.Kinesis.Common;
using System.Collections.Generic;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    [TestFixture]
    abstract class HttpLogShipperBaseTestBase
    {
        private MockRepository _mockRepository;

        private LogShipperSUT Target { get; set; }

        private Mock<ILogShipperOptions> Options { get; set; }
        private Mock<ILogReaderFactory> LogReaderFactory { get; set; }
        private Mock<IPersistedBookmarkFactory> PersistedBookmarkFactory { get; set; }
        private Mock<IPersistedBookmark> PersistedBookmark { get; set; }
        private Mock<ILogShipperFileManager> LogShipperFileManager { get; set; }
        private Mock<ILogShipperProtectedDelegator> LogShipperDelegator { get; set; }
        private EventHandler<LogSendErrorEventArgs> TargetOnLogSendError { get; set; }

        protected string LogFileNamePrefix { get; private set; }
        protected string LogFolder { get; private set; }
        protected int BatchPostingLimit { get; private set; }

        protected string[] LogFiles { get; private set; }
        protected int SentBatches { get; private set; }
        protected int SentRecords { get; private set; }
        protected int FailedBatches { get; private set; }
        protected int FailedRecords { get; private set; }

        protected string CurrentLogFileName { get; private set; }
        protected long CurrentLogFilePosition { get; private set; }

        protected IFixture Fixture { get; private set; }

        [SetUp]
        public void SetUp()
        {
            CurrentLogFileName = null;
            CurrentLogFilePosition = 0;

            TargetOnLogSendError = DefaultTargetOnLogSendError;

            _mockRepository = new MockRepository(MockBehavior.Strict);

            Fixture = new Fixture().Customize(
                new AutoMoqCustomization()
                );

            BatchPostingLimit = Fixture.Create<int>();
            LogFolder = Path.GetDirectoryName(Path.GetTempPath());
            LogFileNamePrefix = Guid.NewGuid().ToString("N");


            LogReaderFactory = _mockRepository.Create<ILogReaderFactory>();
            Fixture.Inject(LogReaderFactory.Object);

            PersistedBookmarkFactory = _mockRepository.Create<IPersistedBookmarkFactory>();
            Fixture.Inject(PersistedBookmarkFactory.Object);

            LogShipperFileManager = _mockRepository.Create<ILogShipperFileManager>();
            Fixture.Inject(LogShipperFileManager.Object);

            SentBatches = SentRecords = 0;
            FailedBatches = FailedRecords = 0;
            LogShipperDelegator = _mockRepository.Create<ILogShipperProtectedDelegator>();
            Fixture.Inject(LogShipperDelegator.Object);

            LogShipperDelegator.Setup(x => x.PrepareRecord(It.Is<MemoryStream>(s => s.Length > 0)))
                .Returns((MemoryStream s) => new string('a', (int)s.Length));

            SetUpSinkOptions();
        }

        private void SetUpSinkOptions()
        {
            Options = _mockRepository.Create<ILogShipperOptions>();
            Fixture.Inject(Options.Object);

            Options.SetupGet(x => x.BufferBaseFilename)
                .Returns(Path.Combine(LogFolder, LogFileNamePrefix));
            Options.SetupGet(x => x.StreamName)
                .Returns(Fixture.Create<string>());
            Options.SetupGet(x => x.BatchPostingLimit)
                .Returns(BatchPostingLimit);
        }

        protected void GivenSendIsSuccessful()
        {
            var success = true;
            LogShipperDelegator.Setup(
                x => x.SendRecords(It.Is<List<string>>(s => s.Count > 0 && s.Count <= Options.Object.BatchPostingLimit), out success)
                )
                .Returns(Fixture.Create<string>())
                .Callback((List<string> batch, bool b) =>
                {
                    SentBatches++;
                    SentRecords += batch.Count;
                });
        }

        protected void GivenSendIsFailed()
        {
            var success = false;
            var response = Fixture.Create<string>();
            LogShipperDelegator.Setup(
                x => x.SendRecords(It.Is<List<string>>(s => s.Count > 0 && s.Count <= Options.Object.BatchPostingLimit), out success)
                )
                .Returns(response);

            LogShipperDelegator.Setup(
                x => x.HandleError(response, It.IsAny<int>())
                )
                .Callback((string resp, int recs) =>
                {
                    FailedBatches++;
                    FailedRecords += recs;
                });

        }

        protected void GivenLogFilesInDirectory(int files = 5)
        {
            LogFiles = Fixture.CreateMany<string>(files)
                .Select(x => Path.Combine(LogFolder, LogFileNamePrefix + x + ".json"))
                .OrderBy(x => x)
                .ToArray();

            LogShipperFileManager
                .Setup(x => x.GetFiles(
                    It.Is<string>(s => s == LogFolder),
                    It.Is<string>(s => s == LogFileNamePrefix + "*.json")
                    )
                )
                .Returns(() => LogFiles);
        }

        protected void GivenFileDeleteSucceeds(string filePath)
        {
            LogShipperFileManager.Setup(x => x.LockAndDeleteFile(filePath)).Callback((string file) =>
            {
                LogFiles = LogFiles.Where(x => !string.Equals(x, file, StringComparison.OrdinalIgnoreCase)).ToArray();
            });
        }

        protected void GivenFileCannotBeLocked(string logFileName)
        {
            LogShipperFileManager
                .Setup(x => x.GetFileLengthExclusiveAccess(logFileName))
                .Throws<IOException>();
        }

        protected void GivenLockedFileLength(string logFileName, long length)
        {
            LogShipperFileManager
                .Setup(x => x.GetFileLengthExclusiveAccess(logFileName))
                .Returns(length);
        }

        protected void GivenPersistedBookmarkIsLocked()
        {
            PersistedBookmarkFactory
                .Setup(x => x.Create(It.Is<string>(s => s == Options.Object.BufferBaseFilename + ".bookmark")))
                .Throws<IOException>();
        }

        protected void GivenPersistedBookmarkFilePermissionsError()
        {
            PersistedBookmarkFactory
                .Setup(x => x.Create(It.Is<string>(s => s == Options.Object.BufferBaseFilename + ".bookmark")))
                .Throws<UnauthorizedAccessException>();
        }

        protected void GivenPersistedBookmark(string logFileName = null, long position = 0)
        {
            CurrentLogFileName = logFileName;
            CurrentLogFilePosition = position;

            PersistedBookmark = _mockRepository.Create<IPersistedBookmark>();
            PersistedBookmark.Setup(x => x.Dispose()).Callback(() => PersistedBookmark.Reset());
            PersistedBookmark.SetupGet(x => x.FileName).Returns(() => CurrentLogFileName);
            PersistedBookmark.SetupGet(x => x.Position).Returns(() => CurrentLogFilePosition);
            PersistedBookmark.Setup(x => x.UpdatePosition(It.IsAny<long>()))
                .Callback((long pos) => { CurrentLogFilePosition = pos; });
            PersistedBookmark.Setup(x => x.UpdateFileNameAndPosition(It.IsAny<string>(), It.IsAny<long>()))
                .Callback((string fileName, long pos) =>
                {
                    CurrentLogFileName = fileName;
                    CurrentLogFilePosition = pos;
                });

            PersistedBookmarkFactory
                .Setup(
                    x => x.Create(It.Is<string>(s => s == Options.Object.BufferBaseFilename + ".bookmark"))
                )
                .Returns(PersistedBookmark.Object);
        }

        protected void GivenLogReaderCreateIOError(string fileName, long position)
        {
            LogReaderFactory.Setup(x => x.Create(fileName, position)).Throws<IOException>();
        }

        protected void GivenLogReader(string logFileName, long length, int maxStreams)
        {
            LogReaderFactory.Setup(x => x.Create(logFileName, It.IsInRange(0L, Math.Max(CurrentLogFilePosition, length), Range.Inclusive)))
                .Returns((string fileName, long position) =>
                {
                    var internalPosition = position > length ? length : position;
                    var streamsLeft = maxStreams;
                    var reader = _mockRepository.Create<ILogReader>();
                    reader.SetupGet(x => x.Position).Returns(() => internalPosition);
                    reader.Setup(x => x.ReadLine()).Returns(() =>
                    {
                        internalPosition++;
                        streamsLeft--;
                        if (internalPosition > length || streamsLeft < 0)
                        {
                            internalPosition = length;
                            return new MemoryStream();
                        }
                        return new MemoryStream(new byte[1]);
                    });
                    reader.Setup(x => x.Dispose()).Callback(() => { reader.Reset(); });
                    return reader.Object;
                });
        }

        protected void GivenOnLogSendErrorHandler(EventHandler<LogSendErrorEventArgs> handler)
        {
            TargetOnLogSendError = handler;
        }

        protected void WhenLogShipperIsCalled()
        {
            Target = Fixture.Create<LogShipperSUT>();
            Target.LogSendError += TargetOnLogSendError;

            Target.ShipIt();
        }

        private void DefaultTargetOnLogSendError(object sender, LogSendErrorEventArgs logSendErrorEventArgs)
        {
            throw logSendErrorEventArgs.Exception;
        }
    }
}
