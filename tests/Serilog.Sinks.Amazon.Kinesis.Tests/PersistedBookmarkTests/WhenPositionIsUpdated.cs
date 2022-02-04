using System;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenPositionIsUpdated : PersistedBookmarkTestBase
    {
        [Test]
        public void AfterFileNameAndPositionUpdated_ThenReadSameValue()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenUpdatedWithPosition();

            Target.Position.ShouldBe(Position);
        }

        [Test]
        public void AfterOpeningNonEmptyBookmark_ThenReadTheSameValue()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenBookmarkIsClosed();
            WhenBookmarkIsCreated();

            WhenUpdatedWithPosition();

            Target.Position.ShouldBe(Position);
        }

        [Test]
        public void ForEmptyBookmark_ThenThrowException()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();

            Should.Throw<InvalidOperationException>(
                () => WhenUpdatedWithPosition()
                );
        }
    }
}
