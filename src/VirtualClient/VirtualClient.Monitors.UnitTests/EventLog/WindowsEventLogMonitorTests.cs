namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.CodeAnalysis;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class WindowsEventLogMonitorTests : MockFixture
    {
        private static readonly string ExamplesFolder =  MockFixture.GetDirectory(typeof(WindowsEventLogMonitorTests), "Examples", "EventLog");

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { "LogNames", "Application" },
                { "Query", "*[System[Level <= 5]]" },
                { "MonitorFrequency", "00:00:00" },
                { "MonitorWarmupPeriod", "00:00:00" }
            };
        }

        [Test]
        public void WindowsEventLogMonitorConvertsEventRecordsCorrectly_1()
        {
            string exampleXml = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_information_example_1.txt"));
            XmlDocument eventRecord = new XmlDocument();
            eventRecord.LoadXml(exampleXml);

            IDictionary<string, object> convertedData = TestEventLogMonitor.ConvertRecord(eventRecord);

            Assert.IsNotEmpty(convertedData);
            Assert.True(convertedData.Count == 16);
            Assert.AreEqual("Microsoft-Windows-Security-SPP", convertedData["Provider_Name"]);
            Assert.AreEqual("E23B33B0-C8C9-472C-A5F9-F2BDFEA0F156", convertedData["Provider_Guid"]);
            Assert.AreEqual("Software Protection Platform Service", convertedData["Provider_EventSourceName"]);
            Assert.AreEqual("16394", convertedData["EventID"]);
            Assert.AreEqual("49152", convertedData["EventID_Qualifiers"]);
            Assert.AreEqual("0", convertedData["Version"]);
            Assert.AreEqual("4", convertedData["Level"]);
            Assert.AreEqual("0", convertedData["Task"]);
            Assert.AreEqual("0", convertedData["Opcode"]);
            Assert.AreEqual("0x80000000000000", convertedData["Keywords"]);
            Assert.AreEqual("2025-03-28T18:58:40.3500678Z", convertedData["TimeCreated_SystemTime"]);
            Assert.AreEqual("32196", convertedData["EventRecordID"]);
            Assert.AreEqual("25048", convertedData["Execution_ProcessID"]);
            Assert.AreEqual("0", convertedData["Execution_ThreadID"]);
            Assert.AreEqual("Application", convertedData["Channel"]);
            Assert.AreEqual("computer01", convertedData["Computer"]);
        }

        [Test]
        public void WindowsEventLogMonitorConvertsEventRecordsCorrectly_2()
        {
            string exampleXml = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_warning_example_1.txt"));
            XmlDocument eventRecord = new XmlDocument();
            eventRecord.LoadXml(exampleXml);

            IDictionary<string, object> convertedData = TestEventLogMonitor.ConvertRecord(eventRecord);

            Assert.IsNotEmpty(convertedData);
            Assert.True(convertedData.Count == 17);
            Assert.AreEqual("Microsoft-Windows-Search", convertedData["Provider_Name"]);
            Assert.AreEqual("CA4E628D-8567-4896-AB6B-835B221F373F", convertedData["Provider_Guid"]);
            Assert.AreEqual("Windows Search Service", convertedData["Provider_EventSourceName"]);
            Assert.AreEqual("10024", convertedData["EventID"]);
            Assert.AreEqual("32768", convertedData["EventID_Qualifiers"]);
            Assert.AreEqual("0", convertedData["Version"]);
            Assert.AreEqual("3", convertedData["Level"]);
            Assert.AreEqual("3", convertedData["Task"]);
            Assert.AreEqual("0", convertedData["Opcode"]);
            Assert.AreEqual("0x80000000000000", convertedData["Keywords"]);
            Assert.AreEqual("2025-03-28T16:13:09.8269843Z", convertedData["TimeCreated_SystemTime"]);
            Assert.AreEqual("32120", convertedData["EventRecordID"]);
            Assert.AreEqual("30888", convertedData["Execution_ProcessID"]);
            Assert.AreEqual("0", convertedData["Execution_ThreadID"]);
            Assert.AreEqual("Application", convertedData["Channel"]);
            Assert.AreEqual("computer01", convertedData["Computer"]);
            Assert.AreEqual("23576", convertedData["Data_FilterHostProcessID"]);
        }

        [Test]
        public void WindowsEventLogMonitorConvertsEventRecordsCorrectly_3()
        {
            string exampleXml = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_error_example_1.txt"));
            XmlDocument eventRecord = new XmlDocument();
            eventRecord.LoadXml(exampleXml);

            IDictionary<string, object> convertedData = TestEventLogMonitor.ConvertRecord(eventRecord);

            Assert.IsNotEmpty(convertedData);
            Assert.True(convertedData.Count == 30);
            Assert.AreEqual("Application Error", convertedData["Provider_Name"]);
            Assert.AreEqual("a0e9b465-b939-57d7-b27d-95d8e925ff57", convertedData["Provider_Guid"]);
            Assert.AreEqual("1000", convertedData["EventID"]);
            Assert.AreEqual("0", convertedData["Version"]);
            Assert.AreEqual("2", convertedData["Level"]);
            Assert.AreEqual("100", convertedData["Task"]);
            Assert.AreEqual("0", convertedData["Opcode"]);
            Assert.AreEqual("0x8000000000000000", convertedData["Keywords"]);
            Assert.AreEqual("2025-03-26T23:00:53.1703727Z", convertedData["TimeCreated_SystemTime"]);
            Assert.AreEqual("31613", convertedData["EventRecordID"]);
            Assert.AreEqual("36408", convertedData["Execution_ProcessID"]);
            Assert.AreEqual("17704", convertedData["Execution_ThreadID"]);
            Assert.AreEqual("Application", convertedData["Channel"]);
            Assert.AreEqual("computer01", convertedData["Computer"]);
            Assert.AreEqual("S-1-5-18", convertedData["Security_UserID"]);
            Assert.AreEqual("AIMTService.exe", convertedData["Data_AppName"]);
            Assert.AreEqual("4.5.0.1060", convertedData["Data_AppVersion"]);
            Assert.AreEqual("67b96b15", convertedData["Data_AppTimeStamp"]);
            Assert.AreEqual("AIMTService.exe", convertedData["Data_ModuleName"]);
            Assert.AreEqual("4.5.0.1060", convertedData["Data_ModuleVersion"]);
            Assert.AreEqual("67b96b15", convertedData["Data_ModuleTimeStamp"]);
            Assert.AreEqual("c0000005", convertedData["Data_ExceptionCode"]);
            Assert.AreEqual("000000000006d09e", convertedData["Data_FaultingOffset"]);
            Assert.AreEqual("0x1628", convertedData["Data_ProcessId"]);
            Assert.AreEqual("0x1db9cd4243d6a89", convertedData["Data_ProcessCreationTime"]);
            Assert.AreEqual("C:\\Program Files\\WindowsApps\\AdvancedMicroDevicesInc-2.AIM-TAMS_4.5.1060.0_x64__0a9344xs7nr4m\\AIMTService\\AIMTService.exe", convertedData["Data_AppPath"]);
            Assert.AreEqual("C:\\Program Files\\WindowsApps\\AdvancedMicroDevicesInc-2.AIM-TAMS_4.5.1060.0_x64__0a9344xs7nr4m\\AIMTService\\AIMTService.exe", convertedData["Data_ModulePath"]);
            Assert.AreEqual("657f30a3-ff7c-4894-ae59-326034b0e702", convertedData["Data_IntegratorReportId"]);
            Assert.AreEqual("AdvancedMicroDevicesInc-2.AIM-TAMS_4.5.1060.0_x64__0a9344xs7nr4m", convertedData["Data_PackageFullName"]);
            Assert.AreEqual("App", convertedData["Data_PackageRelativeAppId"]);
        }

        [Test]
        public async Task WindowsEventLogMonitorProcessesEventsAsExpected()
        {
            string exampleXml1 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_error_example_1.txt"));
            string exampleXml2 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_information_example_1.txt"));
            string exampleXml3 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_warning_example_1.txt"));

            XmlDocument eventRecord1 = new XmlDocument();
            eventRecord1.LoadXml(exampleXml1);

            XmlDocument eventRecord2 = new XmlDocument();
            eventRecord2.LoadXml(exampleXml2);

            XmlDocument eventRecord3 = new XmlDocument();
            eventRecord3.LoadXml(exampleXml3);

            using (TestEventLogMonitor monitor = new TestEventLogMonitor(this))
            {
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("Application", "Event 1 happened", eventRecord1));
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("Application", "Event 2 happened", eventRecord2));
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("Application", "Event 3 happened", eventRecord3));

                await monitor.ProcessEventsAsync(EventContext.None, CancellationToken.None);

                var messagesLogged = this.Logger.MessagesLogged("EventLog");
                Assert.IsNotNull(messagesLogged);
                Assert.IsTrue(messagesLogged.Count() == 1);
            }
        }

        [Test]
        public async Task WindowsEventLogMonitorProcessesEventsAsExpectedWhenMultipleEventLogChannelsAreCaptured()
        {
            string exampleXml1 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_error_example_1.txt"));
            string exampleXml2 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_information_example_1.txt"));
            string exampleXml3 = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "windows_event_log_warning_example_1.txt"));

            XmlDocument eventRecord1 = new XmlDocument();
            eventRecord1.LoadXml(exampleXml1);

            XmlDocument eventRecord2 = new XmlDocument();
            eventRecord2.LoadXml(exampleXml2);

            XmlDocument eventRecord3 = new XmlDocument();
            eventRecord3.LoadXml(exampleXml3);

            using (TestEventLogMonitor monitor = new TestEventLogMonitor(this))
            {
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("Application", "Event 1 happened", eventRecord1));
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("Application", "Event 2 happened", eventRecord2));
                monitor.Queue.Add(new WindowsEventLogMonitor.LogEvent("System", "Event 3 happened", eventRecord3));

                await monitor.ProcessEventsAsync(EventContext.None, CancellationToken.None);

                var messagesLogged = this.Logger.MessagesLogged("EventLog");
                Assert.IsNotNull(messagesLogged);
                Assert.IsTrue(messagesLogged.Count() == 2);
            }
        }

        [Test]
        public void WindowsEventLogMonitorHandlesAnEmptyQueue()
        {
            using (TestEventLogMonitor monitor = new TestEventLogMonitor(this))
            {
                Assert.DoesNotThrowAsync(() => monitor.ProcessEventsAsync(EventContext.None, CancellationToken.None));
            }
        }

        private class TestEventLogMonitor : WindowsEventLogMonitor
        {
            public TestEventLogMonitor(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            // Expose the event data queue so that the unit tests can validate
            // event messages added.
            public new BlockingCollection<LogEvent> Queue
            {
                get
                {
                    return base.Queue;
                }
            }

            public new Task ProcessEventsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ProcessEventsAsync(telemetryContext, cancellationToken);
            }

            public static new Dictionary<string, object> ConvertRecord(XmlDocument eventRecord)
            {
                return WindowsEventLogMonitor.ConvertRecord(eventRecord);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}
