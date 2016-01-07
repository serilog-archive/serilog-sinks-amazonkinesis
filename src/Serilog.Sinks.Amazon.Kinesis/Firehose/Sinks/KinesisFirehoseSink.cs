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

using System.Collections.Generic;
using System.IO;
using System.Text;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose.Sinks
{
    /// <summary>
    /// Writes log events as documents to a Amazon KinesisFirehose.
    /// </summary>
    public class KinesisFirehoseSink : PeriodicBatchingSink
    {
        readonly KinesisSinkState _state;
        readonly LogEventLevel? _minimumAcceptedLevel;

        /// <summary>
        /// Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="kinesisFirehoseClient">The Amazon Kinesis Firehose client.</param>
        /// <param name="options">Options for configuring how the sink behaves, may NOT be null.</param>
        public KinesisFirehoseSink(KinesisFirehoseSinkOptions options, IAmazonKinesisFirehose kinesisFirehoseClient) :
            base(options.BatchPostingLimit, options.Period)
        {
            _state = new KinesisSinkState(options, kinesisFirehoseClient);

            _minimumAcceptedLevel = _state.Options.MinimumLogEventLevel;
        }

        /// <summary>
        /// Free resources held by the sink.
        /// </summary>
        /// <param name="disposing">If true, called because the object is being disposed; if false,
        /// the object is being disposed from the finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            // First flush the buffer
            base.Dispose(disposing);

            if (disposing)
            {
                _state.KinesisFirehoseClient.Dispose();
            }
        }

        /// <summary>
        /// Emit a batch of log events, running to completion asynchronously.
        /// </summary>
        /// <param name="events">The events to be logged to Kinesis Firehose</param>
        protected override void EmitBatch(IEnumerable<LogEvent> events)
        {
            var request = new PutRecordBatchRequest
            {
                DeliveryStreamName = _state.Options.StreamName
            };

            foreach (var logEvent in events)
            {
                var json = new StringWriter();
                _state.Formatter.Format(logEvent, json);

                var bytes = Encoding.UTF8.GetBytes(json.ToString());

                var entry = new Record
                {
                    Data = new MemoryStream(bytes),
                };

                request.Records.Add(entry);
            }

            _state.KinesisFirehoseClient.PutRecordBatch(request);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        protected override bool CanInclude(LogEvent evt)
        {
            return _minimumAcceptedLevel == null ||
                (int)_minimumAcceptedLevel <= (int)evt.Level;
        }
    }
}
