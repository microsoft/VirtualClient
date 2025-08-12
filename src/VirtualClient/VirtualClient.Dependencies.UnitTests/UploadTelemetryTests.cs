// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Extensibility;
    using VirtualClient.Contracts.Metadata;

    [TestFixture]
    [Category("Unit")]
    public class UploadTelemetryTests : MockFixture
    {
        private static readonly string Examples = MockFixture.GetDirectory(typeof(UploadTelemetryTests), "Examples", "Extensibility");

        public void SetupTest(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);

            this.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(UploadTelemetry.TargetFiles), this.GetLogsPath("csv.metrics") },
                { nameof(UploadTelemetry.Format), DataFormat.Json.ToString() },
                { nameof(UploadTelemetry.Schema), DataSchema.Metrics.ToString() }
            };
        }

        [Test]
        [TestCase(DataSchema.Events, DataFormat.Csv)]
        [TestCase(DataSchema.Events, DataFormat.Json)]
        [TestCase(DataSchema.Events, DataFormat.Yaml)]
        [TestCase(DataSchema.Metrics, DataFormat.Csv)]
        [TestCase(DataSchema.Metrics, DataFormat.Json)]
        [TestCase(DataSchema.Metrics, DataFormat.Yaml)]
        public void ConstructorSetsPropertiesToExpectedValues_1(DataSchema expectedSchema, DataFormat expectedFormat)
        {
            this.SetupTest(PlatformID.Unix);

            this.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(UploadTelemetry.TargetFiles), "/home/user/logs/csv.metrics" },
                { nameof(UploadTelemetry.Format), expectedFormat.ToString() },
                { nameof(UploadTelemetry.Schema), expectedSchema.ToString() }
            };

            using (var component = new TestUploadTelemetry(this))
            {
                CollectionAssert.AreEqual(new string[] { "/home/user/logs/csv.metrics" }, component.TargetFiles);
                Assert.AreEqual(false, component.Intrinsic);
                Assert.AreEqual(expectedFormat, component.Format);
                Assert.AreEqual(expectedSchema, component.Schema);
            }
        }

        [Test]
        public void ConstructorSetsPropertiesToExpectedValues_2()
        {
            this.SetupTest(PlatformID.Unix);

            this.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(UploadTelemetry.TargetFiles), "/home/user/logs/any1.metrics;/home/user/logs/any2.metrics" },
                { nameof(UploadTelemetry.Intrinsic), true },
                { nameof(UploadTelemetry.Format), DataFormat.Json.ToString() },
                { nameof(UploadTelemetry.Schema), DataSchema.Metrics.ToString() }
            };

            using (var component = new TestUploadTelemetry(this))
            {
                CollectionAssert.AreEqual(new string[] { "/home/user/logs/any1.metrics", "/home/user/logs/any2.metrics" }, component.TargetFiles);
                Assert.AreEqual(true, component.Intrinsic);
                Assert.AreEqual(DataFormat.Json, component.Format);
                Assert.AreEqual(DataSchema.Metrics, component.Schema);
            }
        }

        [Test]
        public void UploadTelemetryCreatesTheExpectedContextForIntrinsicMetricTelemetryScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Intrinsic)] = true;

                // Intrinsic metadata exists. This includes metadata passed in on the command line
                // as well as host metadata captured on startup. This information ALL becomes part of
                // the default VC "metadata contract".
                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["groupId"] = "Group A",
                        ["experimentName"] = "CPU_Performance_Check_1"
                    },
                    MetadataContract.DefaultCategory);

                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["cpuArchitecture"] = "X64",
                        ["cpuSockets"] = 1,
                        ["cpuLogicalProcessors"] = 4
                    },
                    MetadataContract.HostCategory);

                MetricDataPoint dataPoint = this.CreateMetric();
                EventContext metricContext = component.CreateContext(dataPoint, DateTime.UtcNow);

                Assert.AreEqual(dataPoint.AppHost, metricContext.Properties["appHost"]);
                Assert.AreEqual(dataPoint.AppName, metricContext.Properties["appName"]);
                Assert.AreEqual(dataPoint.AppVersion, metricContext.Properties["appVersion"]);
                Assert.AreEqual(dataPoint.ClientId, metricContext.Properties["clientId"]);
                Assert.AreEqual($"{dataPoint.ExecutionProfile} ({dataPoint.PlatformArchitecture})", metricContext.Properties["executionProfile"]);
                Assert.AreEqual(dataPoint.ExecutionProfile, metricContext.Properties["executionProfileName"]);
                Assert.AreEqual(dataPoint.ExperimentId, metricContext.Properties["experimentId"]);
                Assert.AreEqual(dataPoint.OperatingSystemPlatform, metricContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual(dataPoint.OperationId, metricContext.ActivityId);
                Assert.AreEqual(dataPoint.OperationParentId, metricContext.ParentActivityId);
                Assert.AreEqual(dataPoint.PlatformArchitecture, metricContext.Properties["platformArchitecture"]);
                Assert.AreEqual(dataPoint.Timestamp, metricContext.Properties["timestamp"]);

                // Metadata
                IDictionary<string, object> metadata = metricContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.IsNotEmpty(metadata);
                foreach (var entry in dataPoint.Metadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                IDictionary<string, object> intrinsicMetadata = component.MetadataContract.Get(MetadataContract.DefaultCategory);
                foreach (var entry in intrinsicMetadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                // Host Metadata
                IDictionary<string, object> hostMetadata = metricContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(hostMetadata);
                Assert.IsNotEmpty(hostMetadata);
                foreach (var entry in dataPoint.HostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }

                IDictionary<string, object> intrinsicHostMetadata = component.MetadataContract.Get(MetadataContract.HostCategory);
                foreach (var entry in intrinsicHostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }
            }
        }

        [Test]
        public void UploadTelemetryCreatesTheExpectedContextForNonIntrinsicMetricTelemetryScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                MetricDataPoint dataPoint = this.CreateMetric();
                EventContext metricContext = component.CreateContext(dataPoint, DateTime.UtcNow);

                Assert.AreEqual(dataPoint.AppHost, metricContext.Properties["appHost"]);
                Assert.AreEqual(dataPoint.AppName, metricContext.Properties["appName"]);
                Assert.AreEqual(dataPoint.AppVersion, metricContext.Properties["appVersion"]);
                Assert.AreEqual(dataPoint.ClientId, metricContext.Properties["clientId"]);
                Assert.AreEqual($"{dataPoint.ExecutionProfile} ({dataPoint.PlatformArchitecture})", metricContext.Properties["executionProfile"]);
                Assert.AreEqual(dataPoint.ExecutionProfile, metricContext.Properties["executionProfileName"]);
                Assert.AreEqual(dataPoint.ExperimentId, metricContext.Properties["experimentId"]);
                Assert.AreEqual(dataPoint.OperatingSystemPlatform, metricContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual(dataPoint.OperationId, metricContext.ActivityId);
                Assert.AreEqual(dataPoint.OperationParentId, metricContext.ParentActivityId);
                Assert.AreEqual(dataPoint.PlatformArchitecture, metricContext.Properties["platformArchitecture"]);
                Assert.AreEqual(dataPoint.Timestamp, metricContext.Properties["timestamp"]);

                // Metadata
                IDictionary<string, object> metadata = metricContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.IsNotEmpty(metadata);
                foreach (var entry in dataPoint.Metadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                // Host Metadata
                IDictionary<string, object> hostMetadata = metricContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(hostMetadata);
                Assert.IsNotEmpty(hostMetadata);
                foreach (var entry in dataPoint.HostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }
            }
        }

        [Test]
        public void UploadTelemetryCreatesTheExpectedContextForIntrinsicEventsTelemetryScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Intrinsic)] = true;

                // Intrinsic metadata exists. This includes metadata passed in on the command line
                // as well as host metadata captured on startup. This information ALL becomes part of
                // the default VC "metadata contract".
                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["groupId"] = "Group A",
                        ["experimentName"] = "CPU_Performance_Check_1"
                    },
                    MetadataContract.DefaultCategory);

                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["cpuArchitecture"] = "X64",
                        ["cpuSockets"] = 1,
                        ["cpuLogicalProcessors"] = 4
                    },
                    MetadataContract.HostCategory);

                EventDataPoint dataPoint = this.CreateEvent();
                EventContext metricContext = component.CreateContext(dataPoint, DateTime.UtcNow);

                Assert.AreEqual(dataPoint.AppHost, metricContext.Properties["appHost"]);
                Assert.AreEqual(dataPoint.AppName, metricContext.Properties["appName"]);
                Assert.AreEqual(dataPoint.AppVersion, metricContext.Properties["appVersion"]);
                Assert.AreEqual(dataPoint.ClientId, metricContext.Properties["clientId"]);
                Assert.AreEqual($"{dataPoint.ExecutionProfile} ({dataPoint.PlatformArchitecture})", metricContext.Properties["executionProfile"]);
                Assert.AreEqual(dataPoint.ExecutionProfile, metricContext.Properties["executionProfileName"]);
                Assert.AreEqual(dataPoint.ExperimentId, metricContext.Properties["experimentId"]);
                Assert.AreEqual(dataPoint.OperatingSystemPlatform, metricContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual(dataPoint.OperationId, metricContext.ActivityId);
                Assert.AreEqual(dataPoint.OperationParentId, metricContext.ParentActivityId);
                Assert.AreEqual(dataPoint.PlatformArchitecture, metricContext.Properties["platformArchitecture"]);
                Assert.AreEqual(dataPoint.Timestamp, metricContext.Properties["timestamp"]);

                // Metadata
                IDictionary<string, object> metadata = metricContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.IsNotEmpty(metadata);
                foreach (var entry in dataPoint.Metadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                IDictionary<string, object> intrinsicMetadata = component.MetadataContract.Get(MetadataContract.DefaultCategory);
                foreach (var entry in intrinsicMetadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                // Host Metadata
                IDictionary<string, object> hostMetadata = metricContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(hostMetadata);
                Assert.IsNotEmpty(hostMetadata);
                foreach (var entry in dataPoint.HostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }

                IDictionary<string, object> intrinsicHostMetadata = component.MetadataContract.Get(MetadataContract.HostCategory);
                foreach (var entry in intrinsicHostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }
            }
        }

        [Test]
        public void UploadTelemetryCreatesTheExpectedContextForNonIntrinsicEventsTelemetryScenarios()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                EventDataPoint dataPoint = this.CreateEvent();
                EventContext metricContext = component.CreateContext(dataPoint, DateTime.UtcNow);

                Assert.AreEqual(dataPoint.AppHost, metricContext.Properties["appHost"]);
                Assert.AreEqual(dataPoint.AppName, metricContext.Properties["appName"]);
                Assert.AreEqual(dataPoint.AppVersion, metricContext.Properties["appVersion"]);
                Assert.AreEqual(dataPoint.ClientId, metricContext.Properties["clientId"]);
                Assert.AreEqual($"{dataPoint.ExecutionProfile} ({dataPoint.PlatformArchitecture})", metricContext.Properties["executionProfile"]);
                Assert.AreEqual(dataPoint.ExecutionProfile, metricContext.Properties["executionProfileName"]);
                Assert.AreEqual(dataPoint.ExperimentId, metricContext.Properties["experimentId"]);
                Assert.AreEqual(dataPoint.OperatingSystemPlatform, metricContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual(dataPoint.OperationId, metricContext.ActivityId);
                Assert.AreEqual(dataPoint.OperationParentId, metricContext.ParentActivityId);
                Assert.AreEqual(dataPoint.PlatformArchitecture, metricContext.Properties["platformArchitecture"]);
                Assert.AreEqual(dataPoint.Timestamp, metricContext.Properties["timestamp"]);

                // Metadata
                IDictionary<string, object> metadata = metricContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.IsNotEmpty(metadata);
                foreach (var entry in dataPoint.Metadata)
                {
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                // Host Metadata
                IDictionary<string, object> hostMetadata = metricContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(hostMetadata);
                Assert.IsNotEmpty(hostMetadata);
                foreach (var entry in dataPoint.HostMetadata)
                {
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }
            }
        }

        [Test]
        public void UploadTelemetryPrioritizesDataPointMetadataOverIntrinsicMetadata()
        {
            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Intrinsic)] = true;

                // Intrinsic metadata SHOULD NOT override metadata defined in the
                // actual data point.
                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["groupId"] = "Intrinsic",
                        ["intent"] = "Intrinsic",
                        ["owner"] = "Intrinsic",
                        ["revision"] = "Intrinsic"
                    },
                    MetadataContract.DefaultCategory);

                component.MetadataContract.Add(
                    new Dictionary<string, object>
                    {
                        ["osDescription"] = "Intrinsic",
                        ["osFamily"] = "Intrinsic",
                        ["osName"] = "Intrinsic"
                    },
                    MetadataContract.HostCategory);

                MetricDataPoint dataPoint = this.CreateMetric();
                EventContext metricContext = component.CreateContext(dataPoint, DateTime.UtcNow);

                // Metadata from the data point should take precedence.
                IDictionary<string, object> metadata = metricContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.IsNotEmpty(metadata);
                foreach (var entry in dataPoint.Metadata)
                {
                    Assert.IsFalse(entry.Value.ToString() == "Intrinsic");
                    Assert.AreEqual(entry.Value, metadata[entry.Key]);
                }

                // Host Metadata from the data point should take precedence.
                IDictionary<string, object> hostMetadata = metricContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(hostMetadata);
                Assert.IsNotEmpty(hostMetadata);
                foreach (var entry in dataPoint.HostMetadata)
                {
                    Assert.IsFalse(entry.Value.ToString() == "Intrinsic");
                    Assert.AreEqual(entry.Value, hostMetadata[entry.Key]);
                }
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedEventsFromFilesInCsvFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // CSV events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string csvContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "csv.events"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Events.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Csv.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(csvContent);

                await component.ProcessEventDataAsync("/home/user/any/path/csv.events", DateTime.UtcNow, EventContext.None);
                AssertEventsFromFileLogged(this.Logger);
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedEventsFromFilesInJsonFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited JSON events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string jsonContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "json.events"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Events.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Json.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonContent);

                await component.ProcessEventDataAsync("/home/user/any/path/json.events", DateTime.UtcNow, EventContext.None);
                AssertEventsFromFileLogged(this.Logger);
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedEventsFromFilesInYamlFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited YAML events.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string yamlContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "yaml.events"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Events.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Yaml.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(yamlContent);

                await component.ProcessEventDataAsync("/home/user/any/path/yaml.events", DateTime.UtcNow, EventContext.None);
                AssertEventsFromFileLogged(this.Logger);
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedMetricsFromFilesInCsvFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited CSV metrics.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string csvContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "csv.metrics"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Metrics.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Csv.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(csvContent);

                await component.ProcessMetricsDataAsync("/home/user/any/path/csv.metrics", DateTime.UtcNow, EventContext.None);
                AssertMetricsFromFileLogged(this.Logger);
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedMetricsFromFilesInJsonFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited JSON metrics.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string jsonContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "json.metrics"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Metrics.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Json.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(jsonContent);

                await component.ProcessMetricsDataAsync("/home/user/any/path/json.metrics", DateTime.UtcNow, EventContext.None);
                AssertMetricsFromFileLogged(this.Logger);
            }
        }

        [Test]
        public async Task UploadTelemetryUploadsExpectedMetricsFromFilesInYamlFormat()
        {
            // Scenario:
            // This test is designed to evaluate parsing logic from an actual file containing
            // delimited YAML metrics.
            //
            // Note that the results being verified below are defined in the example file below.
            // Any changes to this file can invalidate the test.
            string yamlContent = System.IO.File.ReadAllText(this.Combine(UploadTelemetryTests.Examples, "yaml.metrics"));

            this.SetupTest(PlatformID.Unix);

            using (var component = new TestUploadTelemetry(this))
            {
                component.Parameters[nameof(UploadTelemetry.Schema)] = DataSchema.Metrics.ToString();
                component.Parameters[nameof(UploadTelemetry.Format)] = DataFormat.Yaml.ToString();

                this.FileSystem
                    .Setup(fs => fs.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(yamlContent);

                await component.ProcessMetricsDataAsync("/home/user/any/path/yaml.metrics", DateTime.UtcNow, EventContext.None);
                AssertMetricsFromFileLogged(this.Logger);
            }
        }

        private static void AssertEventsFromFileLogged(InMemoryLogger logger)
        {
            var messagesLogged = logger.MessagesLogged("EventLog.EventResult");
            Assert.IsNotNull(messagesLogged);
            Assert.IsTrue(messagesLogged.Count() == 2);

            foreach (var message in messagesLogged)
            {
                LogLevel logSeverityLevel = message.Item1;
                Assert.AreEqual(LogLevel.Critical, logSeverityLevel);

                EventContext eventContext = message.Item3 as EventContext;
                Assert.IsNotNull(eventContext);

                // Part A properties
                Assert.AreEqual("linux-demo01", eventContext.Properties["appHost"]);
                Assert.AreEqual("PerfCheck", eventContext.Properties["appName"]);
                Assert.AreEqual("1.5.0", eventContext.Properties["appVersion"]);
                Assert.AreEqual("98733b6a-9a19-4c50-85ce-9ef95f74b79c", eventContext.ActivityId.ToString());
                Assert.AreEqual("8c3956be-54cd-456c-b784-06f41730f8fc", eventContext.ParentActivityId.ToString());
                Assert.AreEqual("2025-07-23T20:34:48.6439102Z", ((DateTime)eventContext.Properties["timestamp"]).ToString("o"));

                // Part B + C properties
                Assert.AreEqual("linux-demo01-client-01", eventContext.Properties["clientId"]);
                Assert.AreEqual("6c83d269-2dff-4fb5-9924-375d84602c5b", eventContext.Properties["experimentId"]);
                Assert.AreEqual("METIS-CPU-CRYPTOGRAPHIC (linux-x64)", eventContext.Properties["executionProfile"]);
                Assert.AreEqual("METIS-CPU-CRYPTOGRAPHIC", eventContext.Properties["executionProfileName"]);
                Assert.AreEqual(500, eventContext.Properties["eventCode"]);
                Assert.AreEqual("Critical system event", eventContext.Properties["eventDescription"]);
                Assert.AreEqual("eventlog.journalctl", eventContext.Properties["eventId"]);
                Assert.AreEqual("journalctl", eventContext.Properties["eventSource"]);
                Assert.AreEqual("EventLog", eventContext.Properties["eventType"]);

                Assert.AreEqual("Unix", eventContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual("linux-x64", eventContext.Properties["platformArchitecture"]);
                Assert.AreEqual("CPU,OpenSSL,Cryptography", eventContext.Properties["tags"]);

                IDictionary<string, object> metadata = eventContext.Properties["metadata"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.AreEqual("Group A", metadata["groupId"]);
                Assert.AreEqual("System Pre-Check", metadata["intent"]);
                Assert.AreEqual("metis-support@company.com", metadata["owner"]);
                Assert.AreEqual("2.6", metadata["revision"]);

                IDictionary<string, object> hostMetadata = eventContext.Properties["metadata_host"] as IDictionary<string, object>;
                Assert.IsNotNull(metadata);
                Assert.AreEqual("Unix 6.11.0.1013", hostMetadata["osDescription"]);
                Assert.AreEqual("Unix", hostMetadata["osFamily"]);
                Assert.AreEqual("Ubuntu 24.04.2 LTS", hostMetadata["osName"]);
            }

            EventContext event1Context = messagesLogged.ElementAt(0).Item3 as EventContext;
            IDictionary<string, object> event1Info = event1Context.Properties["eventInfo"] as IDictionary<string, object>;
            Assert.IsNotNull(event1Info);
            object lastCheckPoint = event1Info["lastCheckPoint"];
            Assert.AreEqual("2025-07-23T20:30:48.6439102Z", lastCheckPoint is DateTime ? ((DateTime)lastCheckPoint).ToString("o") : lastCheckPoint);
            Assert.AreEqual("CRITICAL: Unexpected termination due to segmentation fault", event1Info["message"]);
            Assert.AreEqual("2", event1Info["priority"]);
            Assert.AreEqual("a1b2c3d4e5f6g7h8i9j0", event1Info["bootId"]);
            Assert.AreEqual("/usr/bin/myapp1", event1Info["exe"]);
            Assert.AreEqual("myapp1.service", event1Info["unit"]);

            EventContext event2Context = messagesLogged.ElementAt(1).Item3 as EventContext;
            IDictionary<string, object> event2Info = event2Context.Properties["eventInfo"] as IDictionary<string, object>;
            Assert.IsNotNull(event2Info);
            lastCheckPoint = event2Info["lastCheckPoint"];
            Assert.AreEqual("2025-07-23T20:30:48.6439102Z", lastCheckPoint is DateTime ? ((DateTime)lastCheckPoint).ToString("o") : lastCheckPoint);
            Assert.AreEqual("CRITICAL: Power supply unit failure detected on PSU1", event2Info["message"]);
            Assert.AreEqual("2", event2Info["priority"]);
            Assert.AreEqual("a1b2c3d4e5f6g7h8i9j0", event2Info["bootId"]);
            Assert.AreEqual("/usr/lib/systemd/systemd", event2Info["exe"]);
            Assert.AreEqual("power-monitor.service", event2Info["unit"]);
        }

        private static void AssertMetricsFromFileLogged(InMemoryLogger logger)
        {
            var messagesLogged = logger.MessagesLogged("OpenSSL.ScenarioResult");
            Assert.IsNotNull(messagesLogged);
            Assert.IsTrue(messagesLogged.Count() == 2);

            foreach (var message in messagesLogged)
            {
                LogLevel logSeverityLevel = message.Item1;
                Assert.AreEqual(LogLevel.Debug, logSeverityLevel);

                EventContext metricContext = message.Item3 as EventContext;
                Assert.IsNotNull(metricContext);

                // Part A properties
                Assert.AreEqual("linux-demo01", metricContext.Properties["appHost"]);
                Assert.AreEqual("PerfCheck", metricContext.Properties["appName"]);
                Assert.AreEqual("1.5.0", metricContext.Properties["appVersion"]);
                Assert.AreEqual("98733b6a-9a19-4c50-85ce-9ef95f74b79c", metricContext.ActivityId.ToString());
                Assert.AreEqual("8c3956be-54cd-456c-b784-06f41730f8fc", metricContext.ParentActivityId.ToString());
                Assert.AreEqual("2025-07-23T20:34:48.6439102Z", ((DateTime)metricContext.Properties["timestamp"]).ToString("o"));

                // Part B + C properties
                Assert.AreEqual("linux-demo01-client-01", metricContext.Properties["clientId"]);
                Assert.AreEqual("6c83d269-2dff-4fb5-9924-375d84602c5b", metricContext.Properties["experimentId"]);
                Assert.AreEqual("METIS-CPU-CRYPTOGRAPHIC (linux-x64)", metricContext.Properties["executionProfile"]);
                Assert.AreEqual("METIS-CPU-CRYPTOGRAPHIC", metricContext.Properties["executionProfileName"]);
                Assert.AreEqual("Cryptographic Operations", metricContext.Properties["metricCategorization"]);
                Assert.AreEqual("SHA256 algorithm operation rate", metricContext.Properties["metricDescription"]);
                Assert.AreEqual("HigherIsBetter", metricContext.Properties["metricRelativity"]);
                Assert.AreEqual("kilobytes/sec", metricContext.Properties["metricUnit"]);
                Assert.AreEqual("0", metricContext.Properties["metricVerbosity"]);
                Assert.AreEqual("Unix", metricContext.Properties["operatingSystemPlatform"]);
                Assert.AreEqual("linux-x64", metricContext.Properties["platformArchitecture"]);
                Assert.AreEqual("sha256", metricContext.Properties["scenario"]);
                Assert.AreEqual("2025-07-23T20:34:48.6298225Z", ((DateTime)metricContext.Properties["scenarioEndTime"]).ToString("o"));
                Assert.AreEqual("2025-07-23T20:24:48.6170306Z", ((DateTime)metricContext.Properties["scenarioStartTime"]).ToString("o"));
                Assert.AreEqual("CPU,OpenSSL,Cryptography", metricContext.Properties["tags"]);
                Assert.AreEqual("OpenSSL", metricContext.Properties["toolset"]);
                Assert.AreEqual("3.0.0", metricContext.Properties["toolsetVersion"]);
                Assert.IsTrue(metricContext.Properties["toolsetResults"].ToString().StartsWith("version: 3.0.0-beta3-dev"));
            }

            EventContext metric1Context = messagesLogged.ElementAt(0).Item3 as EventContext;
            Assert.AreEqual("sha256 16-byte", metric1Context.Properties["metricName"]);
            Assert.AreEqual(4530442.74, metric1Context.Properties["metricValue"]);

            EventContext metric2Context = messagesLogged.ElementAt(1).Item3 as EventContext;
            Assert.AreEqual("sha256 64-byte", metric2Context.Properties["metricName"]);
            Assert.AreEqual(15234880.42, metric2Context.Properties["metricValue"]);
        }

        private EventDataPoint CreateEvent()
        {
            return new EventDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                ClientId = Guid.NewGuid().ToString(),
                ExecutionProfile = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = Contracts.PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                SeverityLevel = 5,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                EventId = "eventlog.journalctl",
                EventDescription = "Critical system event",
                EventSource = "journalctl",
                EventCode = 500,
                EventType = "EventLog",
                EventInfo = new SortedMetadataDictionary
                {
                    ["eventId"] = "journalctl",
                    ["eventDescription"] = "Critical System Event",
                    ["eventSource"] = "Linux Event Log",
                    ["lastCheckPoint"] = "2025-07-31T20:01:40.0774251Z",
                    ["message"] = "CRITICAL: Unexpected termination due to segmentation fault",
                    ["priority"] = "2",
                    ["bootId"] = "a1b2c3d4e5f6g7h8i9j0",
                    ["exe"] = "/usr/bin/myapp1",
                    ["unit"] = "myapp1.service"
                },
                HostMetadata = new SortedMetadataDictionary
                {
                    ["osDescription"] = RuntimeInformation.OSDescription,
                    ["osFamily"] = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Unix",
                    ["osName"] = Environment.OSVersion.ToString()
                },
                Metadata = new SortedMetadataDictionary
                {
                    ["groupId"] = "Group A",
                    ["intent"] = "System Pre-Check",
                    ["owner"] = "metis-support@company.com",
                    ["revision"] = "2.6"
                }
            };
        }

        private MetricDataPoint CreateMetric()
        {
            return new MetricDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                MetricCategorization = "Cryptographic Operations",
                ClientId = Guid.NewGuid().ToString(),
                MetricDescription = "Total throughput while processing the algorithm.",
                ExecutionProfile = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                MetricName = "bandwidth",
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = Contracts.PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                MetricRelativity = MetricRelativity.HigherIsBetter,
                Scenario = "sha256",
                ScenarioEndTime = DateTime.UtcNow.AddMinutes(-1),
                ScenarioStartTime = DateTime.UtcNow.AddMinutes(-10),
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                Toolset = "OpenSSL",
                ToolsetResults = "version: 3.0.0-beta3-dev\nbuilt on: Fri Aug 13 03:16:55 2021 UTC\noptions: bn(64,64)\ncompiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG\nCPUINFO: OPENSSL_ia32cap=0xfffa32235f8bffff:0x415f46f1bf2fbb\nsha256         4530442.74 15234880.42",
                ToolsetVersion = "3.0.0",
                MetricUnit = "kilobytes/sec",
                MetricValue = 15234880.42,
                MetricVerbosity = 0,
                HostMetadata = new SortedMetadataDictionary
                {
                    ["osDescription"] = RuntimeInformation.OSDescription,
                    ["osFamily"] = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Unix",
                    ["osName"] = Environment.OSVersion.ToString()
                },
                Metadata = new SortedMetadataDictionary
                {
                    ["groupId"] = "Group A",
                    ["intent"] = "System Pre-Check",
                    ["owner"] = "metis-support@company.com",
                    ["revision"] = "2.6"
                }
            };
        }

        private class TestUploadTelemetry : UploadTelemetry
        {
            public TestUploadTelemetry(MockFixture mockFixture)
            : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public new EventContext CreateContext(TelemetryDataPoint dataPoint, DateTime ingestionTimestamp)
            {
                return base.CreateContext(dataPoint, ingestionTimestamp);
            }

            public new Task ProcessEventDataAsync(string filePath, DateTime ingestionTimestamp, EventContext telemetryContext)
            {
                return base.ProcessEventDataAsync(filePath, ingestionTimestamp, telemetryContext);
            }

            public new Task ProcessMetricsDataAsync(string filePath, DateTime ingestionTimestamp, EventContext telemetryContext)
            {
                return base.ProcessMetricsDataAsync(filePath, ingestionTimestamp, telemetryContext);
            }
        }
    }
}
