// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class TelemetryDataPointTests
    {
        [Test]
        public void TelemetryDataPointClassesAreJsonSerializable_With_Simple_Structures()
        {
            TelemetryDataPoint dataPoint = new TelemetryDataPoint
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
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
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
        public void TelemetryDataPointClassesAreJsonSerializable_With_More_Complex_Structures()
        {
            TelemetryDataPoint dataPoint = new TelemetryDataPoint
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
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                SeverityLevel = (int)LogLevel.Information,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
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

            SerializationAssert.IsJsonSerializable(dataPoint);
        }

        [Test]
        public void TelemetryDataPointClassesAreYamlSerializable_With_Simple_Structures()
        {
            TelemetryDataPoint dataPoint = new TelemetryDataPoint
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
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
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
        public void TelemetryDataPointClassesAreYamlSerializable_With_More_Complex_Structures()
        {
            TelemetryDataPoint dataPoint = new TelemetryDataPoint
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
                PlatformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                SeverityLevel = 2,
                Tags = "Performance;CPU",
                Timestamp = DateTime.UtcNow,
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
        public void TelemetryDataPointValidatesRequiredInformation()
        {
            TelemetryDataPoint dataPoint = new TelemetryDataPoint();
            SchemaException error = Assert.Throws<SchemaException>(() => dataPoint.Validate());
            Assert.IsTrue(error.Message.Contains("The application host is required (appHost)."));
            Assert.IsTrue(error.Message.Contains("The application name is required (appName)."));
            Assert.IsTrue(error.Message.Contains("The application version is required (appVersion)."));
            Assert.IsTrue(error.Message.Contains("A timestamp is required (timestamp)."));
            Assert.IsTrue(error.Message.Contains("A client ID is required (clientId)."));
            Assert.IsTrue(error.Message.Contains("An experiment ID is required (experimentId)."));
            Assert.IsTrue(error.Message.Contains("The operating system platform is required (operatingSystemPlatform)."));

            dataPoint.AppHost = "host01";
            dataPoint.AppName = "app01";
            dataPoint.AppVersion = "1.2.3";
            dataPoint.SeverityLevel = 1;
            dataPoint.Timestamp = DateTime.UtcNow;
            dataPoint.ClientId = "client01";
            dataPoint.ExecutionProfile = "PRE-CHECK";
            dataPoint.ExperimentId = Guid.NewGuid().ToString();
            dataPoint.OperatingSystemPlatform = PlatformID.Unix.ToString();

            Assert.DoesNotThrow(() => dataPoint.Validate());
        }
    }
}
