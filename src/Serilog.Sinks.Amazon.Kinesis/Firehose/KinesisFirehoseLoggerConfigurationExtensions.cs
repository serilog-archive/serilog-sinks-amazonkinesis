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
using Amazon.KinesisFirehose;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Amazon.Kinesis;
using Serilog.Sinks.Amazon.Kinesis.Firehose.Sinks;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.AmazonKinesisFirehose() extension method to <see cref="LoggerConfiguration"/>.
    /// </summary>
    public static class KinesisFirehoseLoggerConfigurationExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events as documents to Amazon Kinesis Firehose.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="options"></param>
        /// <param name="kinesisFirehoseClient"></param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AmazonKinesisFirehose(
            this LoggerSinkConfiguration loggerConfiguration,
            KinesisFirehoseSinkOptions options,
            IAmazonKinesisFirehose kinesisFirehoseClient)
        {
            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
            if (options == null) throw new ArgumentNullException("options");

            ILogEventSink sink;
            if (options.BufferBaseFilename == null)
            {
                sink = new KinesisFirehoseSink(options, kinesisFirehoseClient);
            }
            else
            {
                sink = new DurableKinesisFirehoseSink(options, kinesisFirehoseClient);
            }

            return loggerConfiguration.Sink(sink, options.MinimumLogEventLevel ?? LevelAlias.Minimum);
        }


        /// <summary>
        /// Adds a sink that writes log events as documents to Amazon Kinesis.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="kinesisFirehoseClient"></param>
        /// <param name="streamName"></param>
        /// <param name="bufferBaseFilename"></param>
        /// <param name="bufferFileSizeLimitBytes"></param>
        /// <param name="batchPostingLimit"></param>
        /// <param name="period"></param>
        /// <param name="minimumLogEventLevel"></param>
        /// <param name="onLogSendError"></param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static LoggerConfiguration AmazonKinesisFirehose(
            this LoggerSinkConfiguration loggerConfiguration,
            IAmazonKinesisFirehose kinesisFirehoseClient,
            string streamName,
            string bufferBaseFilename = null,
            int? bufferFileSizeLimitBytes = null,
            int? batchPostingLimit = null,
            TimeSpan? period = null,
            LogEventLevel? minimumLogEventLevel = null,
            EventHandler<LogSendErrorEventArgs> onLogSendError = null)
        {
            if (kinesisFirehoseClient == null) throw new ArgumentNullException("kinesisFirehoseClient");
            if (streamName == null) throw new ArgumentNullException("streamName");

            var options = new KinesisFirehoseSinkOptions(streamName)
            {
                BufferFileSizeLimitBytes = bufferFileSizeLimitBytes,
                BufferBaseFilename = bufferBaseFilename,
                Period = period ?? KinesisFirehoseSinkOptions.DefaultPeriod,
                BatchPostingLimit = batchPostingLimit ?? KinesisFirehoseSinkOptions.DefaultBatchPostingLimit,
                MinimumLogEventLevel = minimumLogEventLevel ?? LevelAlias.Minimum,
                OnLogSendError = onLogSendError
            };

            return AmazonKinesisFirehose(loggerConfiguration, options, kinesisFirehoseClient);
        }
    }
}
