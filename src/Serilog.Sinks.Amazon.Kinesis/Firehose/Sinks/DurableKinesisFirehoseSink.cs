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
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.RollingFile;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose.Sinks
{
    class DurableKinesisFirehoseSink : ILogEventSink, IDisposable
    {
        readonly HttpLogShipper _shipper;
        readonly RollingFileSink _sink;

        public DurableKinesisFirehoseSink(KinesisFirehoseSinkOptions options, IAmazonKinesisFirehose kinesisFirehoseClient)
        {
            var state = new KinesisSinkState(options, kinesisFirehoseClient);

            if (string.IsNullOrWhiteSpace(options.BufferBaseFilename))
            {
                throw new ArgumentException("Cannot create the durable Amazon Kinesis Firehose sink without a buffer base file name.");
            }

            _sink = new RollingFileSink(
               options.BufferBaseFilename + "-{Date}.json",
               state.DurableFormatter,
               options.BufferFileSizeLimitBytes,
               null);

            _shipper = new HttpLogShipper(state);

            if (options.OnLogSendError != null) {
                _shipper.LogSendError += options.OnLogSendError;
            }
        }

        public void Emit(LogEvent logEvent)
        {
            _sink.Emit(logEvent);
        }

        public void Dispose()
        {
            _sink.Dispose();
            _shipper.Dispose();
        }
    }
}