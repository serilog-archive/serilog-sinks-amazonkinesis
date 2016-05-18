using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    [TestFixture]
    class WhenLockAndDeleteFile : FileTestBase
    {
        [Test]
        public void GivenFileDoesNotExist_ThenIOException()
        {
            Should.Throw<IOException>(
                () => Target.LockAndDeleteFile(FileName)
                );
        }

        [TestCase(FileAccess.Read, FileShare.Read, TestName = "File is opened with Read access and Read share")]
        [TestCase(FileAccess.Write, FileShare.ReadWrite, TestName = "File is opened with Write access and Read/Write share")]
        [TestCase(FileAccess.Read, FileShare.Delete, TestName = "File is opened with Read access and Delete share")]
        [TestCase(FileAccess.Write, FileShare.Delete, TestName = "File is opened with Write access and Delete share")]
        public void GivenFileIsOpened_ThenIOException(
            FileAccess fileAccess,
            FileShare fileShare
            )
        {
            File.WriteAllBytes(FileName, new byte[42]);
            using (File.Open(FileName, FileMode.OpenOrCreate, fileAccess, fileShare))
            {
                Should.Throw<IOException>(
                    () => Target.LockAndDeleteFile(FileName)
                );
            }
        }

        [Test]
        public void GivenFileIsNotOpened_ThenDeleteSucceeds()
        {
            File.WriteAllBytes(FileName, new byte[42]);
            Target.LockAndDeleteFile(FileName);

            File.Exists(FileName).ShouldBeFalse();
        }
    }
}
