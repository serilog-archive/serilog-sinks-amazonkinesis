namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    interface ILogReaderFactory
    {
        ILogReader Create(string fileName, long position);
    }
}