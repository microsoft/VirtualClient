// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using NUnit.Framework;
    using System;
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
        public void TextParserExtensionsTranslateByteUnitAsExpected(string originalText, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateByteUnit(originalText), expectedOutput));
        }

        [Test]
        [TestCase("100kb", MetricUnit.Kilobytes, "100")]
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
        public void TextParserExtensionsTranslateStorageByUnitAsExpected(string originalText, string metricUnit, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateStorageByUnit(originalText, metricUnit), expectedOutput));
        }

        [Test]
        [TestCase("100k", "100000")]
        [TestCase("100m", "100000000")]
        public void TextParserExtensionsTranslateNumericUnitAsExpected(string originalText, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateNumericUnit(originalText), expectedOutput));
        }

        [Test]
        [TestCase("1h", "3600")]
        [TestCase("1hrs", "3600")]
        [TestCase("1hr", "3600")]
        [TestCase("1hours", "3600")]
        [TestCase("1hour", "3600")]
        //
        [TestCase("1m", "60")]
        [TestCase("1minutes", "60")]
        [TestCase("1minute", "60")]
        //
        [TestCase("1000ms", "1")]
        [TestCase("1000milliseconds", "1")]
        [TestCase("1000millisecond", "1")]
        //
        [TestCase("1000000us", "1")]
        [TestCase("1000000microsecond", "1")]
        [TestCase("1000000microseconds", "1")]
        //
        [TestCase("1000000000ns", "1")]
        [TestCase("1000000000nanosecond", "1")]
        [TestCase("1000000000nanoseconds", "1")]
        //
        [TestCase("1s", "1")]
        [TestCase("1second", "1")]
        [TestCase("2seconds", "2")]
        public void TextParserExtensionsTranslateTimeUnitToSecondAsExpected(string originalText, string expectedOutput)
        {
            string result = TextParsingExtensions.TranslateToSecondUnit(originalText);
            Assert.AreEqual(result, expectedOutput);
        }

        [Test]
        [TestCase("60s", MetricUnit.Minutes, "1")]
        [TestCase("24hr", MetricUnit.Minutes, "1440")]
        [TestCase("24hrs", MetricUnit.Seconds, "86400")]
        [TestCase("1000ms", MetricUnit.Milliseconds, "1000")]
        [TestCase("1000ms", MetricUnit.Seconds, "1")]
        [TestCase("1000000000nanoseconds", MetricUnit.Microseconds, "1000000")]
        [TestCase("1000000us", MetricUnit.Seconds, "1")]
        public void TextParserExtensionsTranslateTimeUnitAsExpected(string originalText, string metricUnit, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateTimeByUnit(originalText, metricUnit), expectedOutput));
        }
    }
}