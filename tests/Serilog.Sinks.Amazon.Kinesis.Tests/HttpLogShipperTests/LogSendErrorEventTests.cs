using System;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.HttpLogShipperTests
{
    class LogSendErrorEventTests : HttpLogShipperBaseTestBase
    {
        [Test]
        public void WhenNonIOExceptionOccurs_ThenLogSendErrorEventFires()
        {
            GivenPersistedBookmark();
            GivenPersistedBookmarkFilePermissionsError();

            Exception exception = null;
            string message = null;
            GivenOnLogSendErrorHandler((sender, args) =>
            {
                exception = args.Exception;
                message = args.Message;
            });

            WhenLogShipperIsCalled();

            this.ShouldSatisfyAllConditions(
                () => exception.ShouldNotBeNull(),
                () => message.ShouldNotBeNullOrWhiteSpace()
                );
        }
    }
}
