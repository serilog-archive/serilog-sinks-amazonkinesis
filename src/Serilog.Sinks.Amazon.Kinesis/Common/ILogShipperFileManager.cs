namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    interface ILogShipperFileManager
    {
        long GetFileLengthExclusiveAccess(string filePath);
        string[] GetFiles(string path, string searchPattern);
        void LockAndDeleteFile(string filePath);
    }
}
