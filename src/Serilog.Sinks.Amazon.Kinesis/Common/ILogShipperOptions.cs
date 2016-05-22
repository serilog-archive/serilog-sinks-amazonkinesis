namespace Serilog.Sinks.Amazon.Kinesis
{
    /// <summary>
    /// Options usd by LogShipperBase
    /// </summary>
    public interface ILogShipperOptions
    {
        /// <summary>
        /// The maximum number of events to post in a single batch.
        /// </summary>
        int BatchPostingLimit { get; }

        /// <summary>
        /// Path and base file name of log files.
        /// </summary>
        string BufferBaseFilename { get; }

        /// <summary>
        /// Name of output stream/sink
        /// </summary>
        string StreamName { get; }
    }
}