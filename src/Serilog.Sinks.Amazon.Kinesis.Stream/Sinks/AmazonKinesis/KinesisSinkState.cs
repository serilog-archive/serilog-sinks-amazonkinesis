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
using Serilog.Formatting;

namespace Serilog.Sinks.Amazon.Kinesis.Stream
{
    internal class KinesisSinkState
    {
        public static KinesisSinkState Create(KinesisSinkOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");
            var state = new KinesisSinkState(options);
            return state;
        }

        private readonly KinesisSinkOptions _options;
        private readonly IAmazonKinesis _client;
        private readonly ITextFormatter _formatter;
        private readonly ITextFormatter _durableFormatter;

        public KinesisSinkOptions Options { get { return _options; } }
        public IAmazonKinesis KinesisClient { get { return _client; } }
        public ITextFormatter Formatter { get { return _formatter; } }
        public ITextFormatter DurableFormatter { get { return _durableFormatter; } }

        private KinesisSinkState(KinesisSinkOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.StreamName)) throw new ArgumentException("options.StreamName");

            _client = options.KinesisClient;
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