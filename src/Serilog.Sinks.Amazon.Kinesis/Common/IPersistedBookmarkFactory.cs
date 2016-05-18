namespace Serilog.Sinks.Amazon.Kinesis
{
    interface IPersistedBookmarkFactory
    {
        IPersistedBookmark Create(string bookmarkFileName);
    }
}