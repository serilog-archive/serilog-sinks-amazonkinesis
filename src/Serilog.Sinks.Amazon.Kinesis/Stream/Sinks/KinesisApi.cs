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
using System.Threading;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Amazon.Runtime;
using Serilog.Debugging;

namespace Serilog.Sinks.Amazon.Kinesis.Stream.Sinks
{
    /// <summary>
    /// Utilities to create and delete Amazon Kinesis streams.
    /// </summary>
    public class KinesisApi
    {
        /// <summary>
        /// Creates the Amazon Kinesis stream specified and waits for it to become active.
        /// </summary>
        /// <param name="kinesisClient">The Amazon Kinesis client.</param>
        /// <param name="streamName">The name of the steam to be created.</param>
        /// <param name="shardCount">The number of shards the stream should be created with.</param>
        public static bool CreateAndWaitForStreamToBecomeAvailable(IAmazonKinesis kinesisClient, string streamName, int shardCount)
        {
            SelfLog.WriteLine(string.Format("Checking stream '{0}' status.", streamName));

            var stream = DescribeStream(kinesisClient, streamName);
            if (stream != null)
            {
                string state = stream.StreamDescription.StreamStatus;
                switch (state)
                {
                    case "DELETING":
                    {
                        SelfLog.WriteLine(string.Format("Stream '{0}' is {1}", streamName, state));

                        var startTime = DateTime.UtcNow;
                        var endTime = startTime + TimeSpan.FromSeconds(120);

                        while (DateTime.UtcNow < endTime && StreamExists(kinesisClient, streamName))
                        {
                            SelfLog.WriteLine(string.Format("... waiting for stream '{0}' to delete ...", streamName));
                            Thread.Sleep(1000 * 5);
                        }

                        if (StreamExists(kinesisClient, streamName))
                        {
                            var error = string.Format("Timed out waiting for stream '{0}' to delete", streamName);
                            SelfLog.WriteLine(error);
                            return false;
                        }

                        SelfLog.WriteLine(string.Format("Creating stream '{0}'.", streamName));
                        var response = CreateStream(kinesisClient, streamName, shardCount);
                        if (response == null)
                        {
                            return false;
                        }

                        break;
                    }
                    case "CREATING":
                    {
                        break;
                    }
                    case "ACTIVE":
                    case "UPDATING":
                    {
                        SelfLog.WriteLine(string.Format("Stream '{0}' is {1}", streamName, state));
                        return true;
                    }
                    default:
                    {
                        SelfLog.WriteLine(string.Format("Unknown stream state: {0}", state));
                        return false;
                    }
                }
            }
            else
            {
                SelfLog.WriteLine(string.Format("Creating stream '{0}'.", streamName));
                var response = CreateStream(kinesisClient, streamName, shardCount);
                if (response == null)
                {
                    return false;
                }
            }

            {
                // Wait for the stream status to become ACTIVE, timeout after 2 minutes
                var startTime = DateTime.UtcNow;
                var endTime = startTime + TimeSpan.FromSeconds(120);

                while (DateTime.UtcNow < endTime)
                {
                    Thread.Sleep(1000 * 5);

                    var response = DescribeStream(kinesisClient, streamName);
                    if (response != null)
                    {
                        string state = response.StreamDescription.StreamStatus;
                        if (state == "ACTIVE")
                        {
                            SelfLog.WriteLine(string.Format("Stream '{0}' is {1}", streamName, state));
                            return true;
                        }
                    }

                    SelfLog.WriteLine(string.Format("... waiting for stream {0} to become active ....", streamName));
                }

                SelfLog.WriteLine(string.Format("Stream '{0}' never went active.", streamName));
                return false;
            }
        }

        static bool StreamExists(IAmazonKinesis kinesisClient, string streamName)
        {
            var stream = DescribeStream(kinesisClient, streamName);
            
            return stream != null;
        }

        static CreateStreamResponse CreateStream(IAmazonKinesis kinesisClient, string streamName, int shardCount)
        {
            var request = new CreateStreamRequest { StreamName = streamName, ShardCount = shardCount };
            try
            {
                CreateStreamResponse response = kinesisClient.CreateStream(request);
                return response;
            }
            catch (AmazonServiceException e)
            {
                var error = string.Format("Failed to create stream '{0}'. Reason: {1}", streamName, e.Message);
                SelfLog.WriteLine(error);

                return null;
            }
        }

        static DescribeStreamResponse DescribeStream(IAmazonKinesis kinesisClient, string streamName)
        {
            var request = new DescribeStreamRequest { StreamName = streamName };
            try
            {
                var response = kinesisClient.DescribeStream(request);
                return response;
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }
        }
    }
}