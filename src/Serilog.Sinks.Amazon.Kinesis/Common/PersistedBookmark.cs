using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Serilog.Sinks.Amazon.Kinesis
{
    class PersistedBookmark : IPersistedBookmark
    {
        private static readonly Encoding _bookmarkEncoding = new UTF8Encoding(false, false);
        private readonly System.IO.Stream _bookmarkStream;

        private string _fileName;
        private long _position;

        private PersistedBookmark(System.IO.Stream bookmarkStream)
        {
            _bookmarkStream = bookmarkStream;
        }

        public static PersistedBookmark Create(string bookmarkFileName)
        {
            var stream = File.Open(bookmarkFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            try
            {
                var bookmark = new PersistedBookmark(stream);
                bookmark.Read();

                stream = null;
                return bookmark;
            }
            finally
            {
                if (stream != null) stream.Dispose();
            }
        }

        public long Position
        {
            get { return _position; }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public void UpdatePosition(long position)
        {
            if (string.IsNullOrEmpty(_fileName))
                throw new InvalidOperationException("Cannot update Position when FileName is not set.");

            _position = position;
            Save();
        }

        public void UpdateFileNameAndPosition(string fileName, long position)
        {
            _position = position;
            _fileName = fileName;
            Save();
        }

        private void Save()
        {
            _bookmarkStream.SetLength(0);
            string content = string.Format(CultureInfo.InvariantCulture, "{0}:::{1}", _position, _fileName);
            byte[] buffer = _bookmarkEncoding.GetBytes(content);
            _bookmarkStream.Position = 0;
            _bookmarkStream.Write(buffer, 0, buffer.Length);
            _bookmarkStream.Flush();
        }

        private void Read()
        {
            string content = ReadContentString();
            if (content == null) return;
            var parts = content.Split(new[] { ":::" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                long position;
                string fileName = parts[1];
                if (long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out position)
                    && !string.IsNullOrWhiteSpace(fileName)
                    && fileName.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                {
                    _position = position;
                    _fileName = fileName;
                }
            }
        }

        private string ReadContentString()
        {
            _bookmarkStream.Position = 0;
            byte[] buffer = new byte[_bookmarkStream.Length];

            int bufferLength = 0, advance;
            while (
                bufferLength != buffer.Length
                && 0 != (advance = _bookmarkStream.Read(buffer, bufferLength, buffer.Length - bufferLength))
                )
            {
                bufferLength += advance;
            }

            if (bufferLength > 0)
            {
                try
                {
                    return _bookmarkEncoding.GetString(buffer, 0, bufferLength);
                }
                catch (DecoderFallbackException) { }
            }
            return null;
        }

        public void Dispose()
        {
            _bookmarkStream.Dispose();
        }
    }
}