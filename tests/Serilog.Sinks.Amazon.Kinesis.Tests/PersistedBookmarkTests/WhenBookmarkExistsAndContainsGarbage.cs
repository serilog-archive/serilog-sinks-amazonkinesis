using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenBookmarkExistsAndContainsGarbage : PersistedBookmarkTestBase
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(10)]
        [TestCase(64)]
        [TestCase(100)]
        public void ThenFileNameAndPositionAreEmpty(int garbageLength)
        {
            GivenFileExist();
            GivenFileContainsGarbage(garbageLength);
            WhenBookmarkIsCreated();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(0),
                () => Target.FileName.ShouldBeNull()
                );
        }
    }
}
