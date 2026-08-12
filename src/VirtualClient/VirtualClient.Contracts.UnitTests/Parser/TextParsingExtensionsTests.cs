// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Parser
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class TextParsingExtensionsTests
    {
        [Test]
        [TestCase("100kb", "102400")]
        [TestCase("1mb", "1048576")]
        [TestCase("1gb", "1073741824")]
        [TestCase("1tb", "1099511627776")]
        [TestCase("1pb", "1125899906842624")]
        public void TextParsingExtensionsTranslateByteUnitAsExpected(string originalText, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateByteUnit(originalText), expectedOutput));
        }

        [Test]
        [TestCase("1.5kb", "1536")]
        [TestCase("1.5mb", "1572864")]
        [TestCase("1.5gb", "1610612736")]
        [TestCase("0.5tb", "549755813888")]
        [TestCase("2.5pb", "2814749767106560")]
        [TestCase("100.75gb", "108179488768")]
        [TestCase("2.5 gb", "2684354560")]
        public void TextParsingExtensionsTranslateByteUnitSupportsDecimalValues(string originalText, string expectedOutput)
        {
            Assert.AreEqual(expectedOutput, TextParsingExtensions.TranslateByteUnit(originalText));
        }

        [Test]
        [TestCase("3.7tb", "4068193022771.2")]
        [TestCase("3.7gb", "3972844748.8")]
        [TestCase("1.1kb", "1126.4")]
        public void TextParsingExtensionsTranslateByteUnitSupportsFractionalByteCounts(string originalText, string expectedOutput)
        {
            // The units are powers of 1024, so something like 3.7GB does not land on a whole byte. Keep the
            // fraction instead of rounding or throwing.
            Assert.AreEqual(expectedOutput, TextParsingExtensions.TranslateByteUnit(originalText));
        }

        [Test]
        [TestCase("8pb", 9007199254740992)]
        [TestCase("64pb", 72057594037927936)]
        [TestCase("8191pb", 9222246136947933184)]
        public void TextParsingExtensionsTranslateByteUnitRemainsExactAcrossTheInt64Range(string originalText, long expectedBytes)
        {
            // double only holds whole numbers exactly up to 2^53 (~8PB). decimal covers the full Int64 range.
            Assert.AreEqual(expectedBytes, TextParsingExtensions.TranslateByteUnitToBytes(originalText));
            Assert.AreEqual(expectedBytes.ToString(CultureInfo.InvariantCulture), TextParsingExtensions.TranslateByteUnit(originalText));
        }

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("fr-FR")]
        public void TextParsingExtensionsTranslateByteUnitIsNotAffectedByTheCurrentCulture(string culture)
        {
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

                // Where ',' is the decimal separator, culture-sensitive parsing would read '1.5' as 15 or just fail.
                Assert.AreEqual("1610612736", TextParsingExtensions.TranslateByteUnit("1.5gb"));
                Assert.AreEqual("4068193022771.2", TextParsingExtensions.TranslateByteUnit("3.7tb"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        [Test]
        [TestCase("1024", "1024")]
        [TestCase("1.5", "1.5")]
        [TestCase("100kb", "102400")]
        [TestCase("3.7tb", "4068193022771.2")]
        public void TextParsingExtensionsTryTranslateByteUnitAsExpected(string originalText, string expectedBytes)
        {
            Assert.IsTrue(TextParsingExtensions.TryTranslateByteUnit(originalText, out decimal bytes));
            Assert.AreEqual(decimal.Parse(expectedBytes, CultureInfo.InvariantCulture), bytes);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("abc")]
        [TestCase("gb")]
        [TestCase("not-a-size")]
        public void TextParsingExtensionsTryTranslateByteUnitHandlesInvalidValues(string originalText)
        {
            Assert.IsFalse(TextParsingExtensions.TryTranslateByteUnit(originalText, out decimal bytes));
            Assert.AreEqual(0m, bytes);
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
        public void TextParsingExtensionsTranslateStorageByUnitAsExpected(string originalText, string metricUnit, string expectedOutput)
        {
            Assert.IsTrue(string.Equals(TextParsingExtensions.TranslateStorageByUnit(originalText, metricUnit), expectedOutput));
        }

        [Test]
        [TestCase("100k", "100000")]
        [TestCase("100m", "100000000")]
        public void TextParsingExtensionsTranslateNumericUnitAsExpected(string originalText, string expectedOutput)
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
        public void TextParsingExtensionsTranslateTimeUnitToSecondAsExpected(string originalText, string expectedOutput)
        {
            string result = TextParsingExtensions.TranslateToSecondUnit(originalText);
            Assert.AreEqual(result, expectedOutput);
        }

        [Test]
        [TestCase(" -2seconds ")]
        [TestCase("-1minute")]
        public void TextParsingExtensionsDoesNotSupportNegativeTime(string originalText)
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
        public void TextParsingExtensionsTranslateTimeUnitAsExpected(string originalText, string metricUnit, string expectedOutput)
        {
            Assert.AreEqual(TextParsingExtensions.TranslateTimeByUnit(originalText, metricUnit), expectedOutput);
        }

        [Test]
        [TestCase("\"key1=value1,,,key2=value2,,,key3=value3\"")]
        [TestCase("\'key1=value1,,,key2=value2,,,key3=value3\'")]
        [Ignore("This is not the right behavior. When a user includes escaped quotation marks, this is purposeful and not intended to be stripped out.")]
        public void TextParsingExtensionsHandlesQuotationsSurroundingDelimitedStrings(string delimitedString)
        {
            var result = TextParsingExtensions.ParseDelimitedValues(delimitedString);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesKeyValuePairsDelimitedWithTripleCommas()
        {
            string example = "key1=value1,,,key2=value2,,,key3=value3";
            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesKeyValuePairsDelimitedWithSemiColons()
        {
            string example = "key1=value1;key2=value2;key3=value3";
            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesKeyValuePairsDelimitedWithCommas()
        {
            string example = "key1=value1,key2=value2,key3=value3";
            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesKeyValuePairsThatHaveValuesContainingDelimiters()
        {
            string example = "key1=v1a,v1b,v1c;key2=value2;key3=v3a,v3b";
            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "v1a,v1b,v1c" },
                { "key2", "value2" },
                { "key3", "v3a,v3b" }
            }, result);

            example = "key1=v1a,v1b,v1c,,,key2=value2,,,key3=v3a,v3b";
            result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "v1a,v1b,v1c" },
                { "key2", "value2" },
                { "key3", "v3a,v3b" }
            }, result);

            example = "key1=v1a;v1b;v1c,,,key2=value2,,,key3=v3a;v3b";
            result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "key1", "v1a;v1b;v1c" },
                { "key2", "value2" },
                { "key3", "v3a;v3b" }
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesConnectionStringsWithCertificateThumbprint()
        {
            string example =
                "CertificateThumbprint=a7b126e40c1f80b40c1d8b2e1d27ac47da6a456f;ClientId=924796e9-a608-483f-9a9c-4f96dB865123;" +
                "TenantId=fd456aa1-af19-48d2-8dbf-caeea9111712;EndpointUrl=https://any.blob.core.windows.net";

            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "CertificateThumbprint", "a7b126e40c1f80b40c1d8b2e1d27ac47da6a456f" },
                { "ClientId", "924796e9-a608-483f-9a9c-4f96dB865123" },
                { "TenantId", "fd456aa1-af19-48d2-8dbf-caeea9111712" },
                { "EndpointUrl", "https://any.blob.core.windows.net"}
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesConnectionStringsWithCertificateIssuerAndSubject()
        {
            string example = 
                "CertificateIssuer=Any Infra CA 01;CertificateSubject=any.service.azure.com;" +
                "ClientId=924796e9-a608-483f-9a9c-4f96dB865123;TenantId=fd456aa1-af19-48d2-8dbf-caeea9111712;EndpointUrl=https://any.blob.core.windows.net";

            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "CertificateIssuer", "Any Infra CA 01" },
                { "CertificateSubject", "any.service.azure.com" },
                { "ClientId", "924796e9-a608-483f-9a9c-4f96dB865123" },
                { "TenantId", "fd456aa1-af19-48d2-8dbf-caeea9111712" },
                { "EndpointUrl", "https://any.blob.core.windows.net"}
            }, result);
        }

        [Test]
        public void TextParsingExtensionsParseDelimitedValuesHandlesConnectionStringsWithCertificateIssuerAndSubjectDistinguishedNames()
        {
            string example =
                "CertificateIssuer=CN=Any Infra CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.service.azure.com;" +
                "ClientId=924796e9-a608-483f-9a9c-4f96dB865123;TenantId=fd456aa1-af19-48d2-8dbf-caeea9111712;EndpointUrl=https://any.blob.core.windows.net";

            var result = TextParsingExtensions.ParseDelimitedValues(example);

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "CertificateIssuer", "CN=Any Infra CA 01, DC=ABC, DC=COM" },
                { "CertificateSubject", "CN=any.service.azure.com" },
                { "ClientId", "924796e9-a608-483f-9a9c-4f96dB865123" },
                { "TenantId", "fd456aa1-af19-48d2-8dbf-caeea9111712" },
                { "EndpointUrl", "https://any.blob.core.windows.net"}
            }, result);
        }

        [Test]
        public void TextParsingExtensionsHandlesSinglePairStrings()
        {
            var result = TextParsingExtensions.ParseDelimitedValues("single=pair");

            CollectionAssert.AreEqual(new Dictionary<string, string>
            {
                { "single", "pair" }
            }, result);
        }
    }
}