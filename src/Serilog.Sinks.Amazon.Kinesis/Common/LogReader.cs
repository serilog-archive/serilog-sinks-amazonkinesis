using System.IO;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    sealed class LogReader : ILogReader
    {
        private readonly System.IO.Stream _logStream;

        private LogReader(System.IO.Stream logStream)
        {
            _logStream = logStream;
        }

        public static LogReader Create(string fileName, long position)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 128, FileOptions.SequentialScan);
            var length = stream.Length;
            if (position > length)
            {
                position = length;
            }
            stream.Seek(position, SeekOrigin.Begin);
            return new LogReader(stream);
        }

        public System.IO.MemoryStream ReadLine()
        {
            // check and skip BOM in the beginning of file
            if (_logStream.Position == 0)
            {
                var bom = Encoding.UTF8.GetPreamble();
                var bomBuffer = new byte[bom.Length];
                if (bomBuffer.Length != _logStream.Read(bomBuffer, 0, bomBuffer.Length)
                    || !bomBuffer.SequenceEqual(bom))
                {
                    _logStream.Position = 0;
                }
            }

            var result = new MemoryStream(256);
            int thisByte;
            while (0 <= (thisByte = _logStream.ReadByte()))
            {
                if (result.Length == 0
                    && thisByte < 128 // main ASCII set control or whitespace
                    && (char.IsControl((char)thisByte) || char.IsWhiteSpace((char)thisByte))
                    )
                {
                    continue; // Ignore CR/LF and spaces in the beginning of the line
                }
                else if (thisByte == 10 || thisByte == 13)
                {
                    break; // EOL found
                }

                result.WriteByte((byte)thisByte);
            }

            return result;
        }

        public long Position { get { return _logStream.Position; } }

        public void Dispose()
        {
            _logStream.Dispose();
        }
    }

}
