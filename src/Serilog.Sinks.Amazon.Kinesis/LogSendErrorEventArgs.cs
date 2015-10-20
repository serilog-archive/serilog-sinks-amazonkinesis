using System;

namespace Serilog.Sinks.Amazon.Kinesis
{
    /// <summary>
    /// Args for event raised when log sending errors.
    /// </summary>
    public class LogSendErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogSendErrorEventArgs"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public LogSendErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// A message with details of the error.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The underlying exception.
        /// </summary>
        public Exception Exception { get; set; }
    }
}