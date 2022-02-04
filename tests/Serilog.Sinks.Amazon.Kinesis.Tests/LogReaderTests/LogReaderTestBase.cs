using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Ploeh.AutoFixture;
using Serilog.Sinks.Amazon.Kinesis.Common;

namespace Serilog.Sinks.Amazon.Kinesis.Tests.LogReaderTests
{
    [TestFixture]
    abstract class LogReaderTestBase
    {
        private Random _random = new Random();
        protected Fixture Fixture { get; } = new Fixture();
        protected ILogReader Target { get; private set; }
        protected long InitialPosition { get; private set; }
        protected string LogFileName { get; private set; }
        protected byte[] RawContent { get; private set; }
        protected string StringContent { get; private set; }
        protected string[] NormalisedContent { get; private set; }
        protected MemoryStream[] ReadContent { get; private set; }

        [TearDown]
        public void TearDown()
        {
            if (Target != null)
            {
                Target.Dispose();
            }
            if (!string.IsNullOrEmpty(LogFileName))
            {
                File.Delete(LogFileName);
            }
            if (ReadContent != null)
            {
                foreach (var memoryStream in ReadContent)
                {
                    memoryStream.Dispose();
                }
                ReadContent = null;
            }
        }

        protected void GivenLogFileDoesNotExist()
        {
            LogFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        }

        protected void GivenNoContent()
        {
            NormalisedContent = new string[0];
            StringContent = string.Empty;
            RawContent = new byte[0];
        }



        private readonly char[] WhiteSpaceChars = GetWhiteSpaceChars().ToArray();

        private static IEnumerable<char> GetWhiteSpaceChars()
        {
            for (char c = (char)0; c < 128; c++) // not all unicode characters, just ASCII subset
            {
                if ((char.IsControl(c) || char.IsWhiteSpace(c)) && c != '\r' && c != '\n')
                {
                    yield return c;
                }
            }
        }

        private string CreateRandomWhiteSpaceString(int minLength, int maxLength)
        {
            var len = _random.Next(minLength, maxLength + 1);
            return new string(
                Enumerable.Range(0, len).Select(_ => WhiteSpaceChars[_random.Next(WhiteSpaceChars.Length)]).ToArray()
                );
        }

        protected void GivenRandomTextContent(string lineBreak, int emptyLinesBetweenLines = 0, int leadingEmptyLines = 0, int trailingEmptyLines = 0)
        {
            NormalisedContent = Fixture.CreateMany<string>(100).ToArray();

            var prefix = Enumerable.Range(0, leadingEmptyLines).Select(_ => CreateRandomWhiteSpaceString(0, 5));
            var suffix = Enumerable.Range(0, trailingEmptyLines).Select(_ => CreateRandomWhiteSpaceString(0, 5));

            Func<string> createEmptyLines = () => string.Join(lineBreak,
                Enumerable.Range(0, emptyLinesBetweenLines).Select(_ => CreateRandomWhiteSpaceString(0, 5)));

            var lines = NormalisedContent.Select(s =>
            {
                var emptyLines = createEmptyLines();
                if (!string.IsNullOrEmpty(emptyLines))
                    emptyLines = lineBreak + emptyLines;

                return string.Concat(
                    CreateRandomWhiteSpaceString(0, 5),
                    s,
                    emptyLines
                    );
            });

            StringContent = string.Join(lineBreak, prefix.Concat(lines).Concat(suffix));
        }

        protected void GivenUTF8WithBOMEncoding()
        {
            var encoding = new UTF8Encoding(true);
            RawContent = encoding.GetBytes(StringContent);
        }

        protected void GivenUTF8NoBOMEcoding()
        {
            var encoding = new UTF8Encoding(false);
            RawContent = encoding.GetBytes(StringContent);
        }

        protected void GivenRandomBytesContent()
        {
            RawContent = Fixture.CreateMany<byte>(10000).ToArray();
        }

        protected void GivenLogFileExistWithContent()
        {
            LogFileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            File.WriteAllBytes(LogFileName, RawContent);
        }

        protected void GivenInitialPosition(long position)
        {
            InitialPosition = position;
        }

        protected void WhenLogReaderIsCreated()
        {
            Target = LogReader.Create(LogFileName, InitialPosition);
        }

        protected void WhenAllDataIsRead()
        {
            var content = new List<MemoryStream>();
            do
            {
                var stream = Target.ReadLine();
                if (stream.Length == 0) { stream.Dispose(); break; }
                content.Add(stream);
            } while (true);
            ReadContent = content.ToArray();
        }

    }
}
