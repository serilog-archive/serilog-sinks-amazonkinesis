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
using System.Threading.Tasks;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Serilog.Debugging;
using Serilog.Sinks.Amazon.Kinesis.Common;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose.Sinks
{
    internal class HttpLogShipper : HttpLogShipperBase<Record, PutRecordBatchResponse>
    {
        readonly IAmazonKinesisFirehose _kinesisFirehoseClient;
        readonly Throttle _throttle;

        public HttpLogShipper(KinesisSinkState state) : base(state.Options,
            new LogReaderFactory(),
            new PersistedBookmarkFactory(),
            new LogShipperFileManager()
            )
        {
            _throttle = new Throttle(ShipLogs, state.Options.Period);
            _kinesisFirehoseClient = state.KinesisFirehoseClient;
        }

        public void Emit()
        {
            _throttle.ThrottleAction();
        }

        public void Dispose()
        {
            _throttle.Stop();
            _throttle.Dispose();
        }

        protected override Record PrepareRecord(MemoryStream stream)
        {
            return new Record
            {
                Data = stream
            };
        }

        protected override PutRecordBatchResponse SendRecords(List<Record> records, out bool successful)
        {
            var request = new PutRecordBatchRequest
            {
                DeliveryStreamName = _streamName,
                Records = records
            };

            SelfLog.WriteLine("Writing {0} records to firehose", records.Count);
            var putRecordBatchTask = Task.Run(async () => await _kinesisFirehoseClient.PutRecordBatchAsync(request));

            successful = putRecordBatchTask.Result.FailedPutCount == 0;
            return putRecordBatchTask.Result;
        }

        protected override void HandleError(PutRecordBatchResponse response, int originalRecordCount)
        {
            foreach (var record in response.RequestResponses)
            {
                SelfLog.WriteLine("Firehose failed to index record in stream '{0}'. {1} {2} ", _streamName, record.ErrorCode, record.ErrorMessage);
            }
            // fire event
            OnLogSendError(new LogSendErrorEventArgs(string.Format("Error writing records to {0} ({1} of {2} records failed)", _streamName, response.FailedPutCount, originalRecordCount), null));
        }
    }
}