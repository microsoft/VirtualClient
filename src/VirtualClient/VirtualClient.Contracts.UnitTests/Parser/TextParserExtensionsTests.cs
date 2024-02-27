// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class TextParserExtensionsTests
    {
        [Test]
        [TestCase("100kb", "102400")]
        [TestCase("1mb", "1048576")]
        [TestCase("1gb", "1073741824")]
        [TestCase("1tb", "1099511627776")]
        [TestCase("1pb", "1125899906842624")]
        public void TextParserExtensionsTranslateByteUnitAsExpected(string orginalText, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateByteUnit(orginalText), expectedOutput));
        }

        [Test]
        [TestCase("100kb",MetricUnit.Kilobytes,"100")]
        [TestCase("100mb", MetricUnit.Megabytes, "100")]
        [TestCase("100gb", MetricUnit.Gigabytes, "100")]
        [TestCase("100tb", MetricUnit.Terabytes, "100")]
        [TestCase("1000pb", MetricUnit.Petabytes, "1000")]
        [TestCase("100kb", MetricUnit.Bytes, "102400")]
        [TestCase("100mb", MetricUnit.Kilobytes, "102400")]
        [TestCase("100gb", MetricUnit.Megabytes, "102400")]
        [TestCase("100tb", MetricUnit.Gigabytes, "102400")]
        [TestCase("100pb", MetricUnit.Terabytes, "102400")]
        [TestCase("1tb", MetricUnit.Kilobytes, "1073741824")]
        public void TextParserExtensionsTranslateStorageByUnitAsExpected(string orginalText, string metricUnit, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateStorageByUnit(orginalText, metricUnit), expectedOutput));
        }

        [Test]
        [TestCase("100k", "100000")]
        [TestCase("100m", "100000000")]
        public void TextParserExtensionsTranslateNumericUnitAsExpected(string orginalText, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateNumericUnit(orginalText), expectedOutput));
        }
    }
}
