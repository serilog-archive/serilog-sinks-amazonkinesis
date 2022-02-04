using System.IO;
using NUnit.Framework;
using Shouldly;
using Ploeh.AutoFixture;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    class WhenGetFileLengthExclusiveAccess : FileTestBase
    {
        [Test]
        public void GivenFileDoesNotExist_ThenIOException()
        {
            Should.Throw<IOException>(
                () => Target.GetFileLengthExclusiveAccess(FileName)
                );
        }

        [Test]
        public void GivenFileExistsAndNotOpened_ThenCorrectLengthIsReturned()
        {
            var length = Fixture.Create<int>();
            File.WriteAllBytes(FileName, new byte[length]);
            Target.GetFileLengthExclusiveAccess(FileName).ShouldBe(length);
        }

        [Test]
        public void GivenFileExistsAndIsOpenedForWriting_ThenIOException()
        {
            File.WriteAllBytes(FileName, new byte[Fixture.Create<int>()]);
            using (File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                Should.Throw<IOException>(
                    () => Target.GetFileLengthExclusiveAccess(FileName)
                    );
            }
        }

        [Test]
        public void GivenFileExistsAndOpenedForReading_ThenCorrectLengthIsReturned()
        {
            var length = Fixture.Create<int>();
            File.WriteAllBytes(FileName, new byte[length]);
            using (File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Target.GetFileLengthExclusiveAccess(FileName).ShouldBe(length);
            }
        }
    }
}
