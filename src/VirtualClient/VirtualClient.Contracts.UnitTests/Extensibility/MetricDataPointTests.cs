// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class MetricDataPointTests
    {
        [Test]
        public void MetricDataPointClassesAreJsonSerializable_With_Simple_Structures()
        {
            MetricDataPoint dataPoint = new MetricDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                MetricCategorization = "Cryptographic Operations",
                ClientId = Guid.NewGuid().ToString(),
                MetricDescription = "Total throughput while processing the algorithm.",
                ProfileName = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                MetricName = "bandwidth",
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                MetricRelativity = MetricRelativity.HigherIsBetter,
                ScenarioName = "sha256",
                ScenarioEndTime = DateTime.UtcNow.AddMinutes(-10),
                ScenarioStartTime = DateTime.UtcNow,
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                ToolName = "OpenSSL",
                ToolResults = "version: 3.0.0-beta3-dev\nbuilt on: Fri Aug 13 03:16:55 2021 UTC\noptions: bn(64,64)\ncompiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG\nCPUINFO: OPENSSL_ia32cap=0xfffa32235f8bffff:0x415f46f1bf2fbb\nsha256         4530442.74 15234880.42",
                ToolVersion = "3.0.0",
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

            SerializationAssert.IsJsonSerializable(dataPoint);
        }

        [Test]
        public void MetricDataPointClassesAreJsonSerializable_With_More_Complex_Structures()
        {
            MetricDataPoint dataPoint = new MetricDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                MetricCategorization = "Cryptographic Operations",
                ClientId = Guid.NewGuid().ToString(),
                MetricDescription = "Total throughput while processing the algorithm.",
                ProfileName = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                MetricName = "bandwidth",
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                MetricRelativity = MetricRelativity.HigherIsBetter,
                ScenarioName = "sha256",
                ScenarioEndTime = DateTime.UtcNow.AddMinutes(-10),
                ScenarioStartTime = DateTime.UtcNow,
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                ToolName = "OpenSSL",
                ToolResults = "version: 3.0.0-beta3-dev\nbuilt on: Fri Aug 13 03:16:55 2021 UTC\noptions: bn(64,64)\ncompiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG\nCPUINFO: OPENSSL_ia32cap=0xfffa32235f8bffff:0x415f46f1bf2fbb\nsha256         4530442.74 15234880.42",
                ToolVersion = "3.0.0",
                MetricUnit = "kilobytes/sec",
                MetricValue = 15234880.42,
                MetricVerbosity = 0,
                HostMetadata = new SortedMetadataDictionary
                {
                    ["osDescription"] = RuntimeInformation.OSDescription,
                    ["osFamily"] = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Unix",
                    ["osName"] = Environment.OSVersion.ToString(),
                    ["array"] = new string[]
                    {
                        "Value1",
                        "Value2",
                        "Value3"
                    },
                    ["dictionary"] = new SortedDictionary<string, object>
                    {
                        ["entry1"] = "Value4",
                        ["entry2"] = 12345
                    }
                },
                Metadata = new SortedMetadataDictionary
                {
                    ["groupId"] = "Group A",
                    ["intent"] = "System Pre-Check",
                    ["owner"] = "metis-support@company.com",
                    ["revision"] = "2.6",

                    // Dictionary with nested + complex objects.
                    ["dictionary_with_nesting"] = new SortedDictionary<string, object>
                    {
                        ["entryWithNesting1"] = new Dictionary<string, object>
                        {
                            ["nestedValue1"] = true,
                            ["nestedValue2"] = DateTime.UtcNow
                        },
                        ["entryWithNesting2"] = new Dictionary<string, object>
                        {
                            ["nestedValue3"] = new Tuple<string, string>("Item1", "Tuple"),
                            ["nestedValue4"] = new string[]
                            {
                                "arrayItem1",
                                "arrayItem2"
                            }
                        }
                    }
                }
            };

            SerializationAssert.IsJsonSerializable(dataPoint);
        }

        [Test]
        public void MetricDataPointClassesAreYamlSerializable_With_Simple_Structures()
        {
            MetricDataPoint dataPoint = new MetricDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                MetricCategorization = "Cryptographic Operations",
                ClientId = Guid.NewGuid().ToString(),
                MetricDescription = "Total throughput while processing the algorithm.",
                ProfileName = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                MetricName = "bandwidth",
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                MetricRelativity = MetricRelativity.HigherIsBetter,
                ScenarioName = "sha256",
                ScenarioEndTime = DateTime.UtcNow.AddMinutes(-10),
                ScenarioStartTime = DateTime.UtcNow,
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                ToolName = "OpenSSL",
                ToolResults = "version: 3.0.0-beta3-dev\nbuilt on: Fri Aug 13 03:16:55 2021 UTC\noptions: bn(64,64)\ncompiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG\nCPUINFO: OPENSSL_ia32cap=0xfffa32235f8bffff:0x415f46f1bf2fbb\nsha256         4530442.74 15234880.42",
                ToolVersion = "3.0.0",
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

            SerializationAssert.IsYamlSerializable(dataPoint);
        }

        [Test]
        public void MetricDataPointClassesAreYamlSerializable_With_More_Complex_Structures()
        {
            MetricDataPoint dataPoint = new MetricDataPoint
            {
                AppHost = "metis-01",
                AppName = "PerfCheck",
                AppVersion = "1.5.0",
                MetricCategorization = "Cryptographic Operations",
                ClientId = Guid.NewGuid().ToString(),
                MetricDescription = "Total throughput while processing the algorithm.",
                ProfileName = "ANY-EXECUTION-PROFILE",
                ExecutionSystem = "Metis",
                ExperimentId = Guid.NewGuid().ToString(),
                MetricName = "bandwidth",
                OperatingSystemPlatform = Environment.OSVersion.Platform.ToString(),
                OperationId = Guid.NewGuid(),
                OperationParentId = Guid.NewGuid(),
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                MetricRelativity = MetricRelativity.HigherIsBetter,
                ScenarioName = "sha256",
                ScenarioEndTime = DateTime.UtcNow.AddMinutes(-10),
                ScenarioStartTime = DateTime.UtcNow,
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
                ToolName = "OpenSSL",
                ToolResults = "version: 3.0.0-beta3-dev\nbuilt on: Fri Aug 13 03:16:55 2021 UTC\noptions: bn(64,64)\ncompiler: gcc -fPIC -pthread -m64 -Wa,--noexecstack -Wall -O3 -DOPENSSL_USE_NODELETE -DL_ENDIAN -DOPENSSL_PIC -DOPENSSL_BUILDING_OPENSSL -DNDEBUG\nCPUINFO: OPENSSL_ia32cap=0xfffa32235f8bffff:0x415f46f1bf2fbb\nsha256         4530442.74 15234880.42",
                ToolVersion = "3.0.0",
                MetricUnit = "kilobytes/sec",
                MetricValue = 15234880.42,
                MetricVerbosity = 0,
                HostMetadata = new SortedMetadataDictionary
                {
                    ["osDescription"] = RuntimeInformation.OSDescription,
                    ["osFamily"] = Environment.OSVersion.Platform == PlatformID.Win32NT ? "Windows" : "Unix",
                    ["osName"] = Environment.OSVersion.ToString(),
                    ["array"] = new string[]
                    {
                        "Value1",
                        "Value2",
                        "Value3"
                    },
                    ["dictionary"] = new SortedDictionary<string, object>
                    {
                        ["entry1"] = "Value4",
                        ["entry2"] = 12345
                    },

                },
                Metadata = new SortedMetadataDictionary
                {
                    ["groupId"] = "Group A",
                    ["intent"] = "System Pre-Check",
                    ["owner"] = "metis-support@company.com",
                    ["revision"] = "2.6",

                    // Dictionary with nested + complex objects.
                    ["dictionary_with_nesting"] = new SortedDictionary<string, object>
                    {
                        ["entryWithNesting1"] = new Dictionary<string, object>
                        {
                            ["nestedValue1"] = true,
                            ["nestedValue2"] = DateTime.UtcNow
                        },
                        ["entryWithNesting2"] = new Dictionary<string, object>
                        {
                            ["nestedValue3"] = new Tuple<string, string>("Item1", "Tuple"),
                            ["nestedValue4"] = new string[]
                            {
                                "arrayItem1",
                                "arrayItem2"
                            }
                        }
                    }
                }
            };

            SerializationAssert.IsYamlSerializable(dataPoint);
        }

        [Test]
        public void MetricDataPointValidatesRequiredInformation()
        {
            MetricDataPoint dataPoint = new MetricDataPoint();
            SchemaException error = Assert.Throws<SchemaException>(() => dataPoint.Validate());
            Assert.IsTrue(error.Message.Contains("The application host is required (appHost)."));
            Assert.IsTrue(error.Message.Contains("The application name is required (appName)."));
            Assert.IsTrue(error.Message.Contains("The application version is required (appVersion)."));
            Assert.IsTrue(error.Message.Contains("A timestamp is required (timestamp)."));
            Assert.IsTrue(error.Message.Contains("A client ID is required (clientId)."));
            Assert.IsTrue(error.Message.Contains("An experiment ID is required (experimentId)."));
            Assert.IsTrue(error.Message.Contains("The operating system platform is required (operatingSystemPlatform)."));
            Assert.IsTrue(error.Message.Contains("The metric name is required (metricName)."));
            Assert.IsTrue(error.Message.Contains("The metric value is required (metricValue)."));
            Assert.IsTrue(error.Message.Contains("The metric relativity is required (metricRelativity)."));
            Assert.IsTrue(error.Message.Contains("A profile name is required (profileName)."));
            Assert.IsTrue(error.Message.Contains("The scenario name is required (scenarioName)."));
            Assert.IsTrue(error.Message.Contains("The scenario end time/timestamp is required (scenarioEndTime)."));
            Assert.IsTrue(error.Message.Contains("The scenario start time/timestamp is required (scenarioStartTime)."));
            Assert.IsTrue(error.Message.Contains("The tool name is required (toolName)."));

            dataPoint.AppHost = "host01";
            dataPoint.AppName = "app01";
            dataPoint.AppVersion = "1.2.3";
            dataPoint.SeverityLevel = 1;
            dataPoint.Timestamp = DateTime.UtcNow;
            dataPoint.ClientId = "client01";
            dataPoint.ProfileName = "PRE-CHECK";
            dataPoint.ExperimentId = Guid.NewGuid().ToString();
            dataPoint.MetricName = "metric01";
            dataPoint.MetricRelativity = MetricRelativity.HigherIsBetter;
            dataPoint.MetricValue = 1.234;
            dataPoint.OperatingSystemPlatform = PlatformID.Unix.ToString();
            dataPoint.ScenarioName = "scenario01";
            dataPoint.ScenarioStartTime = DateTime.UtcNow.AddMinutes(-5);
            dataPoint.ScenarioEndTime = DateTime.UtcNow;
            dataPoint.ToolName = "toolset01";

            Assert.DoesNotThrow(() => dataPoint.Validate());
        }
    }
}
