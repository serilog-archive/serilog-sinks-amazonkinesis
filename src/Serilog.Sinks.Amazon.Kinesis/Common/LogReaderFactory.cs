namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class LogReaderFactory : ILogReaderFactory
    {
        public ILogReader Create(string fileName, long position)
        {
            return LogReader.Create(fileName, position);
        }
    }
}