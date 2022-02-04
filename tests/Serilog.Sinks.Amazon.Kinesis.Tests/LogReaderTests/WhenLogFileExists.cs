using System.Linq;
using System.Text;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Shouldly;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogReaderTests
{
    class WhenLogFileExists : LogReaderTestBase
    {
        [Test]
        public void WithNoContent_ThenNothingIsRead()
        {
            GivenNoContent();
            GivenUTF8NoBOMEcoding();
            GivenLogFileExistWithContent();
            GivenInitialPosition(0);

            WhenLogReaderIsCreated();
            WhenAllDataIsRead();

            this.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(0),
                () => ReadContent.ShouldBeEmpty()
                );
        }

        [Test]
        public void WithInitialPositionBeyondFileEnd_ThenPositionIsSetToLogLength()
        {
            GivenRandomTextContent("\n");
            GivenUTF8WithBOMEncoding();
            GivenLogFileExistWithContent();
            GivenInitialPosition(RawContent.Length + Fixture.Create<int>());

            WhenLogReaderIsCreated();

            Target.Position.ShouldBe(RawContent.Length);
        }

        [Test]
        public void WithInitialPositionAtFileEnd_ThenNothingIsRead()
        {
            GivenRandomTextContent("\n");
            GivenUTF8WithBOMEncoding();
            GivenLogFileExistWithContent();
            GivenInitialPosition(RawContent.Length + Fixture.Create<int>());

            WhenLogReaderIsCreated();
            WhenAllDataIsRead();

            this.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(RawContent.Length),
                () => ReadContent.ShouldBeEmpty()
                );
        }

        [TestCase("\n", 0, 0, 0)]
        [TestCase("\r\n", 0, 0, 0)]
        [TestCase("\n\r", 0, 0, 0)]
        [TestCase("\r", 0, 0, 0)]
        [TestCase("\r\n", 3, 1, 1)]
        [TestCase("\n", 5, 3, 3)]
        public void WithRandomTextContentNoBOM_ThenItCanBeRead(
            string lineBreak,
            int emptyLinesBetweenLines,
            int leadingEmptyLines,
            int trailingEmptyLines
            )
        {
            GivenRandomTextContent(lineBreak, emptyLinesBetweenLines, leadingEmptyLines, trailingEmptyLines);
            GivenUTF8NoBOMEcoding();
            GivenLogFileExistWithContent();
            GivenInitialPosition(0);

            WhenLogReaderIsCreated();
            WhenAllDataIsRead();

            Target.Position.ShouldBe(RawContent.Length);
            ReadContent.Length.ShouldBe(NormalisedContent.Length);
            ReadContent.Select(s => Encoding.UTF8.GetString(s.ToArray())).ShouldBe(NormalisedContent);
        }

        [TestCase("\n", 0, 0, 0)]
        [TestCase("\r", 0, 1, 1)]
        [TestCase("\r\n", 2, 10, 100)]
        [TestCase("\n\r", 3, 1, 0)]
        public void WithRandomTextContentAndBOM_ThenItCanBeRead(
            string lineBreak,
            int emptyLinesBetweenLines,
            int leadingEmptyLines,
            int trailingEmptyLines
            )
        {
            GivenRandomTextContent(lineBreak, emptyLinesBetweenLines, leadingEmptyLines, trailingEmptyLines);
            GivenUTF8WithBOMEncoding();
            GivenLogFileExistWithContent();
            GivenInitialPosition(0);

            WhenLogReaderIsCreated();
            WhenAllDataIsRead();

            this.ShouldSatisfyAllConditions(
                () => Target.Position.ShouldBe(RawContent.Length),
                () => ReadContent.Select(s => Encoding.UTF8.GetString(s.ToArray())).ShouldBe(NormalisedContent)
                );
        }

        [Test]
        public void WithRandomBinaryContent_ThenStillCanRead()
        {
            GivenRandomBytesContent();
            GivenLogFileExistWithContent();
            GivenInitialPosition(0);

            WhenLogReaderIsCreated();
            WhenAllDataIsRead();

            ReadContent.ShouldNotBeEmpty();
        }
    }
}