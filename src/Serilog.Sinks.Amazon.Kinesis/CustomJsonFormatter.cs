using System;
using System.IO;
using Serilog.Formatting.Json;

namespace Serilog.Sinks.Amazon.Kinesis
{
    public class CustomJsonFormatter : JsonFormatter
    {
        public CustomJsonFormatter(bool omitEnclosingObject = false, string closingDelimiter = null, bool renderMessage = false, IFormatProvider formatProvider = null)
            : base(omitEnclosingObject, closingDelimiter, renderMessage, formatProvider)
        {
        }

        protected override void WriteTimestamp(DateTimeOffset timestamp, ref string precedingDelimiter, TextWriter output)
        {
            output.Write(precedingDelimiter);
            output.Write("\"");
            output.Write("Timestamp");
            output.Write("\":");
            WriteOffset(timestamp, output);
            precedingDelimiter = ",";
        }

        private static void WriteOffset(DateTimeOffset value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.UtcDateTime.ToString("o"));
            output.Write("\"");
        }
    }
}