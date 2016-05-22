namespace Serilog.Sinks.Amazon.Kinesis
{
    class PersistedBookmarkFactory : IPersistedBookmarkFactory
    {
        public IPersistedBookmark Create(string bookmarkFileName)
        {
            return PersistedBookmark.Create(bookmarkFileName);
        }
    }
}