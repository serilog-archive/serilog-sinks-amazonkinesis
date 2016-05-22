using System;

namespace Serilog.Sinks.Amazon.Kinesis
{
    interface IPersistedBookmark : IDisposable
    {
        long Position { get; }
        string FileName { get; }
        void UpdatePosition(long position);
        void UpdateFileNameAndPosition(string fileName, long position);
    }
}