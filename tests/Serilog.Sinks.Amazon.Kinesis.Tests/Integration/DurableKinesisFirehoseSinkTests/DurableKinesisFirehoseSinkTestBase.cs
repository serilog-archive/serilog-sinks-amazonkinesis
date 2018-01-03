using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Serilog.Core;
using Serilog.Sinks.Amazon.Kinesis.Common;
using Serilog.Sinks.Amazon.Kinesis.Firehose;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.Integration.DurableKinesisFirehoseSinkTests
{
    [TestFixture(Category = TestFixtureCategory.Integration)]
    abstract class DurableKinesisFirehoseSinkTestBase
    {
        protected Fixture Fixture { get; private set; }
        protected Logger Logger { get; private set; }
        protected IAmazonKinesisFirehose Client { get { return ClientMock.Object; } }
        protected Mock<IAmazonKinesisFirehose> ClientMock { get; private set; }
        protected EventHandler<LogSendErrorEventArgs> OnLogSendError { get; private set; }
        protected string LogPath { get; private set; }
        protected string BufferBaseFileName { get; private set; }
        protected string StreamName { get; private set; }
        protected TimeSpan ThrottleTime { get; private set; }
        protected MemoryStream DataSent { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Fixture = new Fixture();
            LogPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(LogPath);

            BufferBaseFileName = Path.Combine(LogPath, "prefix");
            StreamName = Fixture.Create<string>();
            ThrottleTime = TimeSpan.FromSeconds(2);
            DataSent = new MemoryStream();
        }

        protected void GivenKinesisClient()
        {
            ClientMock = new Mock<IAmazonKinesisFirehose>(MockBehavior.Loose);
            ClientMock.Setup(
                    x => x.PutRecordBatchAsync(It.IsAny<PutRecordBatchRequest>(), It.IsAny<CancellationToken>())
                )
                .Callback((PutRecordBatchRequest request, CancellationToken token) =>
                {
                    request.Records.ForEach(r => r.Data.WriteTo(DataSent));
                })
                .Returns(Task.FromResult(new PutRecordBatchResponse() { FailedPutCount = 0 }));
        }

        protected void WhenLoggerCreated()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Trace()
                .MinimumLevel.Verbose();

            loggerConfig.WriteTo.AmazonKinesisFirehose(
                kinesisFirehoseClient: Client,
                streamName: StreamName,
                period: ThrottleTime,
                bufferBaseFilename: BufferBaseFileName,
                onLogSendError: OnLogSendError
            );

            Logger = loggerConfig.CreateLogger();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            ((IDisposable)Logger)?.Dispose();
            Directory.Delete(LogPath, true);
            DataSent?.Dispose();
        }
    }
}
