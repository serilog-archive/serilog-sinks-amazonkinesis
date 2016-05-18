using System.IO;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    class LogShipperFileManager : ILogShipperFileManager
    {
        public long GetFileLengthExclusiveAccess(string filePath)
        {
            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return fileStream.Length;
            }
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.GetFiles(path, searchPattern);
        }

        public void LockAndDeleteFile(string filePath)
        {
            using (new FileStream(filePath, FileMode.Open, FileAccess.Read,
                FileShare.None, 128, FileOptions.DeleteOnClose))
            {
            }
        }
    }
}