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
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Serilog.Sinks.Amazon.Kinesis.Logging;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose.Sinks
{
    internal class HttpLogShipper : HttpLogShipperBase<Record, PutRecordBatchResponse>
    {
        readonly IAmazonKinesisFirehose _kinesisFirehoseClient;

        public HttpLogShipper(KinesisSinkState state) : base(state)
        {
            _kinesisFirehoseClient = state.KinesisFirehoseClient;
        }

        protected override Record PrepareRecord(byte[] bytes)
        {
            return new Record
            {
                Data = new MemoryStream(bytes)
            };
        }

        protected override PutRecordBatchResponse SendRecords(List<Record> records, out bool successful)
        {
            var request = new PutRecordBatchRequest
            {
                DeliveryStreamName = _streamName,
                Records = records
            };

            Logger.TraceFormat("Writing {0} records to firehose", records.Count);
            var response = _kinesisFirehoseClient.PutRecordBatch(request);

            successful = response.FailedPutCount == 0;
            return response;
        }

        protected override void HandleError(PutRecordBatchResponse response, int originalRecordCount)
        {
            foreach (var record in response.RequestResponses)
            {
                Logger.TraceFormat("Firehose failed to index record in stream '{0}'. {1} {2} ", _streamName, record.ErrorCode, record.ErrorMessage);
            }
            // fire event
            OnLogSendError(new LogSendErrorEventArgs(string.Format("Error writing records to {0} ({1} of {2} records failed)", _streamName, response.FailedPutCount, originalRecordCount), null));
        }
    }
}