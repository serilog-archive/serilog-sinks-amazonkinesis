namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    interface IPersistedBookmarkFactory
    {
        IPersistedBookmark Create(string bookmarkFileName);
    }
}