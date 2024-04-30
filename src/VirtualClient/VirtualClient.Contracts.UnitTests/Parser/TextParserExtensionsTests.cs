// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
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
        [TestCase("0.0h", "0")]
        [TestCase("1h", "3600")]
        [TestCase("1hrs", "3600")]
        [TestCase("1hr", "3600")]
        [TestCase("1.0hours", "3600")]
        [TestCase("0.01HOUR", "36")]
        //
        [TestCase("1m", "60")]
        [TestCase("1minutes", "60")]
        [TestCase("1MINUTE", "60")]
        [TestCase("0.5MINUTE", "30")]
        //
        [TestCase("1000ms", "1")]
        [TestCase("1000milliseconds", "1")]
        [TestCase("1000millisecond", "1")]
        [TestCase("0.99MILLISECONDS", "0.00099")]
        [TestCase("0.99MiLLiseconDS", "0.00099")]
        //
        [TestCase("1000000us", "1")]
        [TestCase("1000000microsecond", "1")]
        [TestCase("1000000microseconds", "1")]
        //
        [TestCase("1000000000.00ns", "1")]
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
        [TestCase(" -2seconds ")]
        [TestCase("-1minute")]
        public void TextParserExtensionsDoesNotSupportNegativeTime(string originalText)
        {
            Assert.Throws<NotSupportedException>(() => 
            {
                TextParsingExtensions.TranslateToSecondUnit(originalText);
            });
        }

        [Test]
        [TestCase("0", MetricUnit.Nanoseconds, "0")]
        [TestCase("60M", MetricUnit.Minutes, "60")]
        [TestCase("60s", MetricUnit.Minutes, "1")]
        [TestCase("60.0seCONDs", MetricUnit.Minutes, "1")]
        [TestCase("0.01MinuteS", MetricUnit.Seconds, "0.6")]
        [TestCase("24hour", MetricUnit.Minutes, "1440")]
        [TestCase("24HOURs", MetricUnit.Minutes, "1440")]
        [TestCase("2.04HOURs", MetricUnit.Minutes, "122.4")]
        [TestCase("0.02hr", MetricUnit.Minutes, "1.2")]
        [TestCase("24hrs", MetricUnit.Seconds, "86400")]
        [TestCase("1000ms", MetricUnit.Milliseconds, "1000")]
        [TestCase("1000ms", MetricUnit.Seconds, "1")]
        [TestCase("1000000000nanoseconds", MetricUnit.Microseconds, "1000000")]
        [TestCase("1000000.00us", MetricUnit.Seconds, "1")]
        public void TextParserExtensionsTranslateTimeUnitAsExpected(string originalText, string metricUnit, string expectedOutput)
        {
            Assert.AreEqual(TextParsingExtensions.TranslateTimeByUnit(originalText, metricUnit), expectedOutput);
        }

        [Test]
        public void TextParserExtensionsParseVcDelimeteredParameters()
        {
            string example = "key1=value1;key2=value2;key3=value3";
            var result = TextParsingExtensions.ParseVcDelimiteredParameters(example);
            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }, result);

            string exampleWithSemiColon = "key1=v1a;v1b,v1c;key2=value2;key3=v3a;v3b";
            result = TextParsingExtensions.ParseVcDelimiteredParameters(exampleWithSemiColon);
            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "v1a;v1b,v1c" },
                { "key2", "value2" },
                { "key3", "v3a;v3b" }
            }, result);

            string complexExample = "key1=v1a;v1b,v1c;key2=value2;key3=v3a;;v3b,,,key4=v4a,,,v4b;v4c;;;v4d";
            result = TextParsingExtensions.ParseVcDelimiteredParameters(complexExample);
            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "v1a;v1b,v1c" },
                { "key2", "value2" },
                { "key3", "v3a;;v3b" },
                { "key4", "v4a,,,v4b;v4c;;;v4d" }
            }, result);
        }
    }
}