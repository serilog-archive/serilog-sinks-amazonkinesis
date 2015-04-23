// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Amazon.Kinesis;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AmazonKinesis
{
    /// <summary>
    /// Provides KinesisSink with configurable options
    /// </summary>
    public class KinesisSinkOptions
    {
        /// <summary>
        /// The default time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The default interval between checking the buffer files. Defaults to 2 seconds 
        /// </summary>
        public static TimeSpan DefaultBufferLogShippingInterval = TimeSpan.FromSeconds(2);

        /// <summary>
        /// The default maximum number of events to post in a single batch. Defaults to 500.
        /// </summary>
        public static int DefaultBatchPostingLimit = 500;
        

        /// <summary>
        /// The default stream name to use for the log events.
        /// </summary>
        public string StreamName { get; set; }

        /// <summary>
        /// The number of shards for this stream.
        /// A stream is composed of multiple shards, each of which provides a fixed unit of capacity. 
        /// The total capacity of the stream is the sum of the capacities of its shards. 
        /// Each shard corresponds to 1 MB/s of write capacity and 2 MB/s of read capacity.  
        /// </summary>
        public int ShardCount { get; set; }

        /// <summary>
        /// The Amazon Kinesis client.
        /// </summary>
        public IAmazonKinesis KinesisClient { get; set; }

        /// <summary>
        /// The maximum number of events to post in a single batch. Defaults to 500.
        /// </summary>
        public int BatchPostingLimit { get; set; }

        /// <summary>
        /// The time to wait between checking for event batches. Defaults to 2 seconds.
        /// </summary>
        public TimeSpan Period { get; set; }

        /// <summary>
        /// Supplies culture-specific formatting information, or null.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        /// <summary>
        /// The minimum log event level required in order to write an event to the sink.
        /// </summary>
        public LogEventLevel? MinimumLogEventLevel { get; set; }

        /// <summary>
        /// Optional path to directory that can be used as a log shipping buffer for increasing the reliabilty of the log forwarding.
        /// </summary>
        public string BufferBaseFilename { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which the buffer log file for a specific date will be allowed to grow. 
        /// By default no limit will be applied.
        /// </summary>
        public long? BufferFileSizeLimitBytes { get; set; }

        /// <summary>
        /// The interval between checking the buffer files. Defaults to 2 seconds 
        /// </summary>
        public TimeSpan? BufferLogShippingInterval { get; set; }

        /// <summary>
        /// Customizes the formatter used when converting the log events into Amazon Kinesis documents. 
        /// Please note that the formatter output must be valid JSON.
        /// </summary>
        public ITextFormatter CustomFormatter { get; set; }

        /// <summary>
        /// Customizes the formatter used when converting the log events into the durable sink. 
        /// Please note that the formatter output must be valid JSON.
        /// </summary>
        public ITextFormatter CustomDurableFormatter { get; set; }

        
        /// <summary>
        /// Configures the Amazon Kinesis sink defaults.
        /// </summary>
        protected KinesisSinkOptions()
        {
            Period = DefaultPeriod;
            BatchPostingLimit = DefaultBatchPostingLimit;
        }

        /// <summary>
        /// Configures the Amazon Kinesis sink.
        /// </summary>
        /// <param name="kinesisClient">The Amazon Kinesis client.</param>
        /// <param name="streamName">The name of the Kinesis stream.</param>
        /// <param name="shardCount"></param>
        public KinesisSinkOptions(IAmazonKinesis kinesisClient, string streamName, int? shardCount = null) 
            : this()
        {
            if (kinesisClient == null) throw new ArgumentNullException("kinesisClient");
            if (streamName == null) throw new ArgumentNullException("streamName");

            KinesisClient = kinesisClient;
            StreamName = streamName;
            ShardCount = shardCount ?? 1;
        }
    }
}