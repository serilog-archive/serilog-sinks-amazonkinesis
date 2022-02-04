using System;
using System.IO;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Serilog.Sinks.Amazon.Kinesis.Common;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogShipperFileManagerTests
{
    [TestFixture]
    abstract class FileTestBase
    {
        protected ILogShipperFileManager Target { get; private set; }
        protected string FileName { get; private set; }
        protected Fixture Fixture { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Fixture = new Fixture();
            Target = new LogShipperFileManager();
        }

        [SetUp]
        public void SetUp()
        {
            FileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            File.Delete(FileName);
        }
    }
}
