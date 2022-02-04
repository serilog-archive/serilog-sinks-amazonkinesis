using System;

namespace Serilog.Sinks.Amazon.Kinesis.Common
{
    interface ILogReader : IDisposable
    {
        /// <summary>
        /// Read next line until CR or LF character.
        /// Ignoring leading CR/LF (no empty lines returned),
        /// </summary>
        /// <returns>Stream with line content, or empty stream in case of EOF</returns>
        System.IO.MemoryStream ReadLine();
        long Position { get; }
    }
}