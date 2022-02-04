using System.IO;
using NUnit.Framework;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogReaderTests
{
    class WhenNoLogFileExists : LogReaderTestBase
    {
        [Test]
        public void ThenExceptionIsThrown()
        {
            GivenLogFileDoesNotExist();
            GivenInitialPosition(0);

            Should.Throw<IOException>(
                () => WhenLogReaderIsCreated()
                );
        }
    }
}
