namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class PersistedBookmarkFactory : IPersistedBookmarkFactory
    {
        public IPersistedBookmark Create(string bookmarkFileName)
        {
            return PersistedBookmark.Create(bookmarkFileName);
        }
    }
}