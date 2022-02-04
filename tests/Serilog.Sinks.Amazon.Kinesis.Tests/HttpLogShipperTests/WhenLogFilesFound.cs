using System;
using System.Linq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenLogFilesFound : HttpLogShipperBaseTestBase
    {
        [Test]
        public void WithBookmarkIsGreaterThanAllFiles_ThenFilesAreDeleted()
        {
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles, GivenFileDeleteSucceeds);
            var bookmarkedFile = LogFiles.Max() + "z";

            GivenPersistedBookmark(bookmarkedFile, Fixture.Create<long>());

            WhenLogShipperIsCalled();

            LogFiles.ShouldBeEmpty("No one shall remain!");
        }

        [Test]
        public void WithBookmarkedLogCannotBeOpened_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);
            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();

            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);
            GivenLogReaderCreateIOError(CurrentLogFileName, CurrentLogFilePosition);

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change"),
                () => CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change"),
                () => LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!")
                );
        }

        [Test]
        public void WithBookmarkedLogAtTheEnd_ThenPreviousFilesAreDeletedButNotLast()
        {
            GivenLogFilesInDirectory();
            Array.ForEach(LogFiles.Take(LogFiles.Length - 1).ToArray(), GivenFileDeleteSucceeds);

            var bookmarkedFile = LogFiles.Last();
            var bookmarkedPosition = Fixture.Create<long>();
            GivenPersistedBookmark(bookmarkedFile, bookmarkedPosition);
            GivenLogReader(CurrentLogFileName, CurrentLogFilePosition, 0);

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => CurrentLogFileName.ShouldBe(bookmarkedFile, "Bookmarked log file name should not change"),
                () => CurrentLogFilePosition.ShouldBe(bookmarkedPosition, "Bookmarked position should not change"),
                () => LogFiles.ShouldBe(new[] { bookmarkedFile }, "Only one shall remain!")
                );
        }

        [Test]
        public void WithBookmarkedLogAtTheEndOfFirstFile_ThenAllNextFilesAreRead()
        {
            GivenLogFilesInDirectory(files: 2);
            var initialFile = LogFiles[0];
            var otherFile = LogFiles[1];
            var batchCount = Fixture.Create<int>();
            var otherFileLength = BatchPostingLimit * batchCount;

            GivenFileDeleteSucceeds(initialFile);
            GivenPersistedBookmark(initialFile, Fixture.Create<long>());

            GivenLockedFileLength(initialFile, length: CurrentLogFilePosition);
            GivenLockedFileLength(otherFile, length: otherFileLength);

            GivenLogReader(initialFile, length: CurrentLogFilePosition, maxStreams: int.MaxValue);
            GivenLogReader(otherFile, length: otherFileLength, maxStreams: int.MaxValue);

            GivenSendIsSuccessful();

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => LogFiles.ShouldBe(new[] { otherFile }, "Only one shall remain!"),
                () => CurrentLogFileName.ShouldBe(otherFile),
                () => CurrentLogFilePosition.ShouldBe(otherFileLength),
                () => SentBatches.ShouldBe(batchCount),
                () => SentRecords.ShouldBe(otherFileLength)
                );
        }

        [Test]
        public void WithFailureLockingFileAndGettingLength_ThenProcessingStopsInTheEndOfTheFile()
        {
            GivenLogFilesInDirectory(files: 2);
            var initialFile = LogFiles[0];
            var initialPosition = Fixture.Create<long>();
            var otherFile = LogFiles[1];

            GivenPersistedBookmark(initialFile, initialPosition);
            GivenFileCannotBeLocked(initialFile);
            GivenLogReader(initialFile, length: initialPosition, maxStreams: 0);

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => LogFiles.ShouldBe(new[] { initialFile, otherFile }, "No files should be removed."),
                () => CurrentLogFileName.ShouldBe(initialFile),
                () => CurrentLogFilePosition.ShouldBe(initialPosition)
                );
        }

        [Test]
        public void WithSendFailure_ThenBookmarkIsNotChanged()
        {
            GivenLogFilesInDirectory(files: 2);
            var allFiles = LogFiles.ToArray();
            var initialFile = LogFiles[0];

            GivenPersistedBookmark(initialFile, 0);
            GivenLogReader(initialFile, length: BatchPostingLimit, maxStreams: BatchPostingLimit);
            GivenSendIsFailed();

            WhenLogShipperIsCalled();

            LogFiles.ShouldBe(allFiles, "Nothing shall be deleted.");

            this.ShouldSatisfyAllConditions(
                () => CurrentLogFileName.ShouldBe(initialFile),
                () => CurrentLogFilePosition.ShouldBe(0),
                () => SentBatches.ShouldBe(0),
                () => SentRecords.ShouldBe(0),
                () => FailedBatches.ShouldBe(1),
                () => FailedRecords.ShouldBe(BatchPostingLimit)
                );
        }

        [Test]
        public void WhenLogFileSizeIsLessThanBookmarkPosition_ThenFileIsReadFromTheBeginning()
        {
            GivenLogFilesInDirectory(1);
            var initialFile = LogFiles[0];
            var batchesCount = Fixture.Create<int>();
            var realFileLength = batchesCount * BatchPostingLimit;
            var bookmarkedPosition = realFileLength + Fixture.Create<int>();

            GivenPersistedBookmark(initialFile, position: bookmarkedPosition);
            GivenLockedFileLength(initialFile, length: realFileLength);
            GivenLogReader(initialFile, length: realFileLength, maxStreams: int.MaxValue);
            GivenSendIsSuccessful();

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => CurrentLogFileName.ShouldBe(initialFile),
                () => CurrentLogFilePosition.ShouldBe(realFileLength),
                () => SentBatches.ShouldBe(batchesCount),
                () => SentRecords.ShouldBe(realFileLength)
                );
        }
    }
}
