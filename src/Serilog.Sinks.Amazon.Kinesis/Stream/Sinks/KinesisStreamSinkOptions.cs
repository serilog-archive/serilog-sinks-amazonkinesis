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

using Amazon.Kinesis;

namespace Serilog.Sinks.Amazon.Kinesis.Stream.Sinks
{
    /// <summary>
    ///     Provides KinesisSink with configurable options
    /// </summary>
    public class KinesisStreamSinkOptions : KinesisSinkOptionsBase
    {
        /// <summary>
        ///     Configures the Amazon Kinesis sink.
        /// </summary>
        /// <param name="streamName">The name of the Kinesis stream.</param>
        /// <param name="shardCount"></param>
        public KinesisStreamSinkOptions(string streamName, int? shardCount = null) : base(streamName)
        {
            ShardCount = shardCount ?? 1;
        }

        /// <summary>
        ///     Will be appended to buffer base filenames.
        /// </summary>
        public override string BufferBaseFilenameAppend
        {
            get { return ".stream"; }
        }

        /// <summary>
        ///     The number of shards for this stream.
        ///     A stream is composed of multiple shards, each of which provides a fixed unit of capacity.
        ///     The total capacity of the stream is the sum of the capacities of its shards.
        ///     Each shard corresponds to 1 MB/s of write capacity and 2 MB/s of read capacity.
        /// </summary>
        public int ShardCount { get; set; }

        /// <summary>
        ///     The Amazon Kinesis client.
        /// </summary>
        public IAmazonKinesis KinesisClient { get; set; }
    }
}