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
using Serilog.Formatting;

namespace Serilog.Sinks.Amazon.Kinesis.Firehose
{
    internal class KinesisSinkState
    {
        public static KinesisSinkState Create(KinesisFirehoseSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            var state = new KinesisSinkState(options);
            return state;
        }

        private readonly KinesisFirehoseSinkOptions _options;
        private readonly IAmazonKinesisFirehose _client;
        private readonly ITextFormatter _formatter;
        private readonly ITextFormatter _durableFormatter;

        public KinesisFirehoseSinkOptions Options { get { return _options; } }
        public IAmazonKinesisFirehose KinesisFirehoseClient { get { return _client; } }
        public ITextFormatter Formatter { get { return _formatter; } }
        public ITextFormatter DurableFormatter { get { return _durableFormatter; } }

        private KinesisSinkState(KinesisFirehoseSinkOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.StreamName)) throw new ArgumentException("options.StreamName");

            _client = options.KinesisFirehoseClient;
            _options = options;

            _formatter = options.CustomDurableFormatter ?? new CustomJsonFormatter(
                omitEnclosingObject: false,
                closingDelimiter: string.Empty,
                renderMessage: true,
                formatProvider: options.FormatProvider
            );

            _durableFormatter = options.CustomDurableFormatter ?? new CustomJsonFormatter(
                omitEnclosingObject: false,
                closingDelimiter: Environment.NewLine,
                renderMessage: true,
                formatProvider: options.FormatProvider
            );
        }
    }
}