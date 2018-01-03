using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Serilog.Core;
using Serilog.Sinks.Amazon.Kinesis.Common;
using Serilog.Sinks.Amazon.Kinesis.Stream;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.Integration.DurableKinesisSinkTests
{
    [TestFixture(Category = TestFixtureCategory.Integration)]
    abstract class DurableKinesisSinkTestBase
    {
        protected Fixture Fixture { get; private set; }
        protected Logger Logger { get; private set; }
        protected IAmazonKinesis Client { get { return ClientMock.Object; } }
        protected Mock<IAmazonKinesis> ClientMock { get; private set; }
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
            ClientMock = new Mock<IAmazonKinesis>(MockBehavior.Loose);
            ClientMock.Setup(
                    x => x.PutRecordsAsync(It.IsAny<PutRecordsRequest>(), It.IsAny<CancellationToken>())
                )
                .Callback((PutRecordsRequest request, CancellationToken token) =>
                {
                    request.Records.ForEach(r => r.Data.WriteTo(DataSent));
                })
                .Returns(Task.FromResult(new PutRecordsResponse() { FailedRecordCount = 0 }));

        }

        protected void WhenLoggerCreated()
        {
            var loggerConfig = new LoggerConfiguration()
                .WriteTo.Trace()
                .MinimumLevel.Verbose();

            loggerConfig.WriteTo.AmazonKinesis(
                kinesisClient: Client,
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
            Directory.Delete(LogPath, true);
            DataSent?.Dispose();
        }
    }
}
