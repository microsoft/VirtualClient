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
    public class DelimitedEventDataEnumeratorTests : MockFixture
    {
        private static readonly string Examples = MockFixture.GetDirectory(typeof(DelimitedEventDataEnumeratorTests), "Examples", "Extensibility");

        public void SetupTest(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);
        }

        [Test]
        public void DelimitedEventDataEnumeratorParsesExpectedEventsFromFilesInCsvFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited CSV events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string csvContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "csv.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(csvContent, DataFormat.Csv))
            {
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedEventDataEnumeratorParsesExpectedEventsFromFilesInJsonFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited JSON events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "json.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedEventDataEnumeratorParsesExpectedEventsFromFilesInYamlFormat()
        {
            this.SetupTest(PlatformID.Unix);

            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited YAML events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string yamlContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "yaml.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(yamlContent, DataFormat.Yaml))
            {
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();
                while (enumerator.MoveNext())
                {
                    dataPoints.Add(enumerator.Current);
                }

                AssertExpectedMetricsParsed(dataPoints);
            }
        }

        [Test]
        public void DelimitedEventDataEnumeratorHandlesRepeatedCallsCorrectly()
        {
            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "json.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();
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
        public void DelimitedEventDataEnumeratorHandlesResetsCorrectly()
        {
            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "json.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(jsonContent, DataFormat.Json))
            {
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();
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
        public void DelimitedEventDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualCsvItems()
        {
            // Scenario:
            // CSV has rows with unexpected numbers of field values. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string csvContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "csv_with_errors.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(csvContent, DataFormat.Csv))
            {
                int errorsHandled = 0;
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();

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
        public void DelimitedEventDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualJsonItems()
        {
            // Scenario:
            // JSON has items with invalid JSON formatting. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string jsonContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "json_with_errors.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(jsonContent, DataFormat.Json))
            {
                int errorsHandled = 0;
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();

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
        public void DelimitedEventDataEnumeratorHandlesErrorsWhileAttemptingToParseIndividualYamlItems()
        {
            // Scenario:
            // JSON has items with invalid JSON formatting. The enumerator is expected to throw
            // an exception but to continue moving forward to the next row of field values provided the
            // exception is handled during enumeration.

            this.SetupTest(PlatformID.Unix);
            string yamlContent = System.IO.File.ReadAllText(this.Combine(DelimitedEventDataEnumeratorTests.Examples, "yaml_with_errors.events"));

            using (var enumerator = new DelimitedEventDataEnumerator(yamlContent, DataFormat.Yaml))
            {
                int errorsHandled = 0;
                List<EventDataPoint> dataPoints = new List<EventDataPoint>();

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

        private static void AssertExpectedMetricsParsed(IEnumerable<EventDataPoint> dataPoints)
        {
            Assert.IsNotNull(dataPoints);
            Assert.IsTrue(dataPoints.Count() == 2);

            foreach (var dataPoint in dataPoints)
            {
                Assert.AreEqual((int)LogLevel.Critical, dataPoint.SeverityLevel);

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
                Assert.AreEqual(500, dataPoint.EventCode);
                Assert.AreEqual("Critical system event", dataPoint.EventDescription);
                Assert.AreEqual("eventlog.journalctl", dataPoint.EventId);
                Assert.AreEqual("journalctl", dataPoint.EventSource);
                Assert.AreEqual("EventLog", dataPoint.EventType);

                Assert.AreEqual("Unix", dataPoint.OperatingSystemPlatform);
                Assert.AreEqual("linux-x64", dataPoint.PlatformArchitecture);
                Assert.AreEqual("CPU;OpenSSL;Cryptography", dataPoint.Tags);

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

            EventDataPoint dataPoint1 = dataPoints.ElementAt(0);
            Assert.IsNotNull(dataPoint1.EventInfo);
            Assert.IsTrue(dataPoint1.EventInfo.Count == 6);
            object lastCheckPoint = dataPoint1.EventInfo["lastCheckPoint"];
            Assert.AreEqual("2025-07-23T20:30:48.6439102Z", lastCheckPoint is DateTime ? ((DateTime)lastCheckPoint).ToString("o") : lastCheckPoint);
            Assert.AreEqual("CRITICAL: Unexpected termination due to segmentation fault", dataPoint1.EventInfo["message"]);
            Assert.AreEqual("2", dataPoint1.EventInfo["priority"]);
            Assert.AreEqual("a1b2c3d4e5f6g7h8i9j0", dataPoint1.EventInfo["bootId"]);
            Assert.AreEqual("/usr/bin/myapp1", dataPoint1.EventInfo["exe"]);
            Assert.AreEqual("myapp1.service", dataPoint1.EventInfo["unit"]);

            EventDataPoint dataPoint2 = dataPoints.ElementAt(1);
            Assert.IsNotNull(dataPoint2.EventInfo);
            Assert.IsTrue(dataPoint2.EventInfo.Count == 6);
            lastCheckPoint = dataPoint2.EventInfo["lastCheckPoint"];
            Assert.AreEqual("2025-07-23T20:30:48.6439102Z", lastCheckPoint is DateTime ? ((DateTime)lastCheckPoint).ToString("o") : lastCheckPoint);
            Assert.AreEqual("CRITICAL: Power supply unit failure detected on PSU1", dataPoint2.EventInfo["message"]);
            Assert.AreEqual("2", dataPoint2.EventInfo["priority"]);
            Assert.AreEqual("a1b2c3d4e5f6g7h8i9j0", dataPoint2.EventInfo["bootId"]);
            Assert.AreEqual("/usr/lib/systemd/systemd", dataPoint2.EventInfo["exe"]);
            Assert.AreEqual("power-monitor.service", dataPoint2.EventInfo["unit"]);
        }
    }
}
