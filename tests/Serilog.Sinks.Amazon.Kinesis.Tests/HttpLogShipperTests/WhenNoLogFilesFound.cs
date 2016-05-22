using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenNoLogFilesFound : HttpLogShipperBaseTestBase
    {
        [Test]
        public void AndBookmarkHasNoData()
        {
            GivenPersistedBookmark(logFileName: null, position: 0);
            GivenLogFilesInDirectory(files: 0);

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                "Shipper does not progress",
                () => CurrentLogFileName.ShouldBeNull(),
                () => CurrentLogFilePosition.ShouldBe(0)
                );
        }

        [Test]
        public void AndBookmarkHasData()
        {
            GivenPersistedBookmark(logFileName: Fixture.Create<string>(), position: Fixture.Create<long>());
            GivenLogFilesInDirectory(files: 0);

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                "Shipper does not progress and resets bookmark data",
                () => CurrentLogFileName.ShouldBeNull(),
                () => CurrentLogFilePosition.ShouldBe(0)
                );
        }
    }
}
