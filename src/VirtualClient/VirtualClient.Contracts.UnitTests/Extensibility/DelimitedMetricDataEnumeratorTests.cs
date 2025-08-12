// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using YamlDotNet.Core;

    [TestFixture]
    [Category("Unit")]
    public class DelimitedMetricDataEnumeratorTests : MockFixture
    {
        private static readonly string Examples = MockFixture.GetDirectory(typeof(DelimitedMetricDataEnumeratorTests), "Examples", "Extensibility");

        public void SetupTest(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);
        }

        [Test]
        public void DelimitedMetricDataEnumeratorParsesExpectedMetricsFromFilesInCsvFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited CSV events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string csvContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "csv.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(csvContent, DataFormat.Csv))
            {
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorParsesExpectedMetricsFromFilesInJsonFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited JSON events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "json.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorParsesExpectedMetricsFromFilesInYamlFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited YAML events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string yamlContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "yaml.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(yamlContent, DataFormat.Yaml))
            {
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorHandlesRepeatedCallsCorrectly()
        {
            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "json.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                // The enumerator should not advance or hit any issues.
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                Assert.IsTrue(dataPoints.Count == 2);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorHandlesResetsCorrectly()
        {
            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "json.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                Assert.IsTrue(dataPoints.Count == 2);

                dataPoints.Clear();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                Assert.IsTrue(dataPoints.Count == 2);

                dataPoints.Clear();
                enumerator.Reset();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                Assert.IsTrue(dataPoints.Count == 2);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualCsvItems()
        {
            // Scenario:
            // CSV has rows with unexpected numbers of field values. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string csvContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "csv_with_errors.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(csvContent, DataFormat.Csv))
            {
                int errorsHandled = 0;
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();

                enumerator.ParsingError += (sender, args) =>
                {
                    errorsHandled++;
                    Assert.IsNotNull(args.Error);
                    Assert.IsInstanceOf<SchemaException>(args.Error);
                    Assert.IsTrue(args.Error.Message.StartsWith("Invalid CSV content."));
                };

                // The enumerator should not allow an exception to surface caused by 
                // and individual item parsing error.
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                    {
                        dataPoints.Add(enumerator.Current);
                    }
                }

                // 2 invalid CSV data points expected.
                Assert.IsTrue(errorsHandled == 2);

                // 2 valid CSV data points expected
                Assert.IsTrue(dataPoints.Count == 2);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualJsonItems()
        {
            // Scenario:
            // JSON has items with invalid JSON formatting. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "json_with_errors.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(jsonContent, DataFormat.Json))
            {
                int errorsHandled = 0;
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();

                enumerator.ParsingError += (sender, args) =>
                {
                    errorsHandled++;
                    Assert.IsNotNull(args.Error);
                    Assert.IsInstanceOf<JsonException>(args.Error);
                };

                // The enumerator should not allow an exception to surface caused by 
                // and individual item parsing error.
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                    {
                        dataPoints.Add(enumerator.Current);
                    }
                }

                // 2 invalid CSV data points expected.
                Assert.IsTrue(errorsHandled == 2);

                // 2 valid CSV data points expected
                Assert.IsTrue(dataPoints.Count == 2);
            }
        }

        [Test]
        public void DelimitedMetricDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualYamlItems()
        {
            // Scenario:
            // JSON has items with invalid JSON formatting. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string yamlContent = System.IO.File.ReadAllText(this.Combine(DelimitedMetricDataEnumeratorTests.Examples, "yaml_with_errors.metrics"));

            using (var enumerator = new DelimitedMetricDataEnumerator(yamlContent, DataFormat.Yaml))
            {
                int errorsHandled = 0;
                List<MetricDataPoint> dataPoints = new List<MetricDataPoint>();

                enumerator.ParsingError += (sender, args) =>
                {
                    errorsHandled++;
                    Assert.IsNotNull(args.Error);
                    Assert.IsInstanceOf<YamlException>(args.Error);
                };

                // The enumerator should not allow an exception to surface caused by 
                // and individual item parsing error.
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current != null)
                    {
                        dataPoints.Add(enumerator.Current);
                    }
                }

                // 2 invalid CSV data points expected.
                Assert.IsTrue(errorsHandled == 2);

                // 2 valid CSV data points expected
                Assert.IsTrue(dataPoints.Count == 2);
            }
        }

        private static void AssertExpectedMetricsParsed(IEnumerable<MetricDataPoint> dataPoints)
        {
            Assert.IsNotNull(dataPoints);
            Assert.IsTrue(dataPoints.Count() == 2);

            foreach (var dataPoint in dataPoints)
            {
                Assert.AreEqual((int)LogLevel.Debug, dataPoint.SeverityLevel);

                // Part A properties
                Assert.AreEqual("linux-demo01", dataPoint.AppHost);
                Assert.AreEqual("PerfCheck", dataPoint.AppName);
                Assert.AreEqual("1.5.0", dataPoint.AppVersion);
                Assert.AreEqual("98733b6a-9a19-4c50-85ce-9ef95f74b79c", dataPoint.OperationId.ToString());
                Assert.AreEqual("8c3956be-54cd-456c-b784-06f41730f8fc", dataPoint.OperationParentId.ToString());
                Assert.AreEqual("2025-07-23T20:34:48.6439102Z", dataPoint.Timestamp?.ToString("o"));

                // Part B + C properties
                Assert.AreEqual("linux-demo01-client-01", dataPoint.ClientId);
                Assert.AreEqual("6c83d269-2dff-4fb5-9924-375d84602c5b", dataPoint.ExperimentId);
                Assert.AreEqual("METIS-CPU-CRYPTOGRAPHIC", dataPoint.ExecutionProfile);
                Assert.AreEqual("Cryptographic Operations", dataPoint.MetricCategorization);
                Assert.AreEqual("SHA256 algorithm operation rate", dataPoint.MetricDescription);
                Assert.AreEqual("HigherIsBetter", dataPoint.MetricRelativity.ToString());
                Assert.AreEqual("kilobytes/sec", dataPoint.MetricUnit);
                Assert.AreEqual(0, dataPoint.MetricVerbosity);
                Assert.AreEqual("Unix", dataPoint.OperatingSystemPlatform);
                Assert.AreEqual("linux-x64", dataPoint.PlatformArchitecture);
                Assert.AreEqual("sha256", dataPoint.Scenario);
                Assert.AreEqual("2025-07-23T20:34:48.6298225Z", dataPoint.ScenarioEndTime.Value.ToString("o"));
                Assert.AreEqual("2025-07-23T20:24:48.6170306Z", dataPoint.ScenarioStartTime.Value.ToString("o"));
                Assert.AreEqual("CPU;OpenSSL;Cryptography", dataPoint.Tags);
                Assert.AreEqual("OpenSSL", dataPoint.Toolset);
                Assert.AreEqual("3.0.0", dataPoint.ToolsetVersion);
                Assert.IsTrue(dataPoint.ToolsetResults.StartsWith("version: 3.0.0-beta3-dev"));

                Assert.IsNotNull(dataPoint.Metadata);
                Assert.IsTrue(dataPoint.Metadata.Count == 4);
                Assert.AreEqual("Group A", dataPoint.Metadata["groupId"]);
                Assert.AreEqual("System Pre-Check", dataPoint.Metadata["intent"]);
                Assert.AreEqual("metis-support@company.com", dataPoint.Metadata["owner"]);
                Assert.AreEqual("2.6", dataPoint.Metadata["revision"]);

                Assert.IsNotNull(dataPoint.HostMetadata);
                Assert.IsTrue(dataPoint.HostMetadata.Count == 3);
                Assert.AreEqual("Unix 6.11.0.1013", dataPoint.HostMetadata["osDescription"]);
                Assert.AreEqual("Unix", dataPoint.HostMetadata["osFamily"]);
                Assert.AreEqual("Ubuntu 24.04.2 LTS", dataPoint.HostMetadata["osName"]);
            }

            MetricDataPoint dataPoint1 = dataPoints.ElementAt(0);
            Assert.AreEqual("sha256 16-byte", dataPoint1.MetricName);
            Assert.AreEqual(4530442.74, dataPoint1.MetricValue);

            MetricDataPoint dataPoint2 = dataPoints.ElementAt(1);
            Assert.AreEqual("sha256 64-byte", dataPoint2.MetricName);
            Assert.AreEqual(15234880.42, dataPoint2.MetricValue);
        }
    }
}
