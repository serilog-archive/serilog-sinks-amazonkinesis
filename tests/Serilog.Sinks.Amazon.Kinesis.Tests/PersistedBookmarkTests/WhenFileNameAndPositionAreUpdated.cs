using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    class WhenFileNameAndPositionAreUpdated : PersistedBookmarkTestBase
    {
        [TestCase(true, false, TestName = "Given empty file exists")]
        [TestCase(true, true, TestName = "Given bookmark with garbage exists")]
        [TestCase(false, false, TestName = "Given bookmark does not exist")]
        public void AfterOpening_ThenCanReadSameFileNameAndPosition(
            bool fileExists,
            bool containsGarbage
            )
        {
            if (fileExists) { GivenFileExist(); } else { GivenFileDoesNotExist(); }
            if (containsGarbage) GivenFileContainsGarbage(1000);

            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }

        [TestCase(true, false, TestName = "Given empty file exists")]
        [TestCase(true, true, TestName = "Given bookmark with garbage exists")]
        [TestCase(false, false, TestName = "Given bookmark does not exist")]
        public void AfterClosedAndReOpened_ThenCanReadSameFileNameAndPosition(
            bool fileExists,
            bool containsGarbage
            )
        {
            if (fileExists) { GivenFileExist(); } else { GivenFileDoesNotExist(); }
            if (containsGarbage) GivenFileContainsGarbage(1000);

            WhenBookmarkIsCreated();
            WhenUpdatedWithFileNameAndPosition();
            WhenBookmarkIsClosed();
            WhenBookmarkIsCreated();

            Target.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(Position),
                () => Target.FileName.ShouldBe(FileName)
                );
        }
    }
}
