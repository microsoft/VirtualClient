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
    public class EventDataPointTests
    {
        [Test]
        public void EventDataPointClassesAreJsonSerializable_With_Simple_Structures()
        {
            EventDataPoint dataPoint = new EventDataPoint
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

            SerializationAssert.IsJsonSerializable(dataPoint);
        }

        [Test]
        public void EventDataPointClassesAreJsonSerializable_With_More_Complex_Structures()
        {
            EventDataPoint dataPoint = new EventDataPoint
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
        public void EventDataPointClassesAreYamlSerializable_With_Simple_Structures()
        {
            EventDataPoint dataPoint = new EventDataPoint
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

            SerializationAssert.IsYamlSerializable(dataPoint);
        }

        [Test]
        public void EventDataPointClassesAreYamlSerializable_With_More_Complex_Structures()
        {
            EventDataPoint dataPoint = new EventDataPoint
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
        public void EventDataPointValidatesRequiredInformation()
        {
            EventDataPoint dataPoint = new EventDataPoint();
            SchemaException error = Assert.Throws<SchemaException>(() => dataPoint.Validate());
            Assert.IsTrue(error.Message.Contains("The application host is required (appHost)."));
            Assert.IsTrue(error.Message.Contains("The application name is required (appName)."));
            Assert.IsTrue(error.Message.Contains("The application version is required (appVersion)."));
            Assert.IsTrue(error.Message.Contains("A timestamp is required (timestamp)."));
            Assert.IsTrue(error.Message.Contains("A client ID is required (clientId)."));
            Assert.IsTrue(error.Message.Contains("An experiment ID is required (experimentId)."));
            Assert.IsTrue(error.Message.Contains("The operating system platform is required (operatingSystemPlatform)."));
            Assert.IsTrue(error.Message.Contains("The event ID is required (eventId)."));
            Assert.IsTrue(error.Message.Contains("The event source is required (eventSource)."));
            Assert.IsTrue(error.Message.Contains("The event type is required (eventType)."));

            dataPoint.AppHost = "host01";
            dataPoint.AppName = "app01";
            dataPoint.AppVersion = "1.2.3";
            dataPoint.SeverityLevel = 1;
            dataPoint.Timestamp = DateTime.UtcNow;
            dataPoint.ClientId = "client01";
            dataPoint.EventId = "event01";
            dataPoint.EventType = "EventLog";
            dataPoint.EventSource = "journalctl";
            dataPoint.ExecutionProfile = "PRE-CHECK";
            dataPoint.ExperimentId = Guid.NewGuid().ToString();
            dataPoint.OperatingSystemPlatform = PlatformID.Unix.ToString();

            Assert.DoesNotThrow(() => dataPoint.Validate());
        }
    }
}
