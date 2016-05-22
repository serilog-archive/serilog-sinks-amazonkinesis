using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.PersistedBookmarkTests
{
    [TestFixture]
    abstract class PersistedBookmarkTestBase
    {
        protected IPersistedBookmark Target { get; private set; }
        protected string BookmarkFileName { get; private set; }
        protected string FileName { get; private set; }
        protected long Position { get; private set; }

        [TearDown]
        public void TearDown()
        {
            if (Target != null)
            {
                Target.Dispose();
            }
            if (!string.IsNullOrEmpty(BookmarkFileName))
            {
                File.Delete(BookmarkFileName);
            }
        }

        protected void GivenFileDoesNotExist()
        {
            BookmarkFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        }

        protected void GivenFileExist()
        {
            BookmarkFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            File.WriteAllText(BookmarkFileName, "");
        }

        protected void GivenFileContainsGarbage(int dataLength)
        {
            var bytes = new byte[dataLength];
            using (var rnd = RandomNumberGenerator.Create())
            {
                rnd.GetBytes(bytes);
            }
            File.WriteAllBytes(BookmarkFileName, bytes);
        }

        protected void WhenBookmarkIsCreated()
        {
            Target = PersistedBookmark.Create(BookmarkFileName);
        }

        protected void WhenBookmarkIsClosed()
        {
            Target.Dispose();
            Target = null;
        }

        protected void WhenUpdatedWithFileNameAndPosition()
        {
            FileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Position = new Random().Next(0, int.MaxValue);
            Target.UpdateFileNameAndPosition(FileName, Position);
        }

        protected void WhenUpdatedWithPosition()
        {
            Position = Target.Position + new Random().Next(0, int.MaxValue);
            Target.UpdatePosition(Position);
        }
    }
}
