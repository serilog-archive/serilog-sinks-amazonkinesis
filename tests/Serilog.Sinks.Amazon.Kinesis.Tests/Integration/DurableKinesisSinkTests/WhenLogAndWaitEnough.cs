using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.Integration.DurableKinesisSinkTests
{
    class WhenLogAndWaitEnough : DurableKinesisSinkTestBase
    {
        [Test]
        public void ThenAllDataWillBeSent()
        {
            GivenKinesisClient();
            WhenLoggerCreated();

            var messages = Fixture.CreateMany<string>(100).ToList();
            foreach (var message in messages)
            {
                Logger.Information(message);
                Thread.Sleep(TimeSpan.FromMilliseconds(ThrottleTime.TotalMilliseconds / 30));
            }

            Thread.Sleep(ThrottleTime.Add(ThrottleTime));

            DataSent.Position = 0;
            var data = new StreamReader(DataSent).ReadToEnd();

            messages.ShouldAllBe(msg => data.Contains(msg));
        }
    }
}
