using NUnit.Framework;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class WhenBookmarkCannotBeCreated : HttpLogShipperBaseTestBase
    {
        [Test]
        public void ThenGracefullyComplete()
        {
            GivenPersistedBookmarkIsLocked();

            WhenLogShipperIsCalled();
        }
    }
}