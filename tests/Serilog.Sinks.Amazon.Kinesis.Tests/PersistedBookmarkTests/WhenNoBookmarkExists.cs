using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenNoBookmarkExists : PersistedBookmarkTestBase
    {
        [Test]
        public void ThenFileIsCreated()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();

            File.Exists(BookmarkFileName).ShouldBeTrue();
        }

        [Test]
        public void ThenFileNameAndPositionAreEmpty()
        {
            GivenFileDoesNotExist();
            WhenBookmarkIsCreated();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(0),
                () => Target.FileName.ShouldBeNull()
                );
        }
    }
}
