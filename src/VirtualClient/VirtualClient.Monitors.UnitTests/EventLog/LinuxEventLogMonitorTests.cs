namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class LinuxEventLogMonitorTests : MockFixture
    {
        private static readonly string ExamplesFolder = MockFixture.GetDirectory(typeof(LinuxEventLogMonitorTests), "Examples", "EventLog");

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { "LogLevel", "Warning" }
            };
        }

        [Test]
        [TestCase(LogLevel.Critical, 2)]
        [TestCase(LogLevel.Error, 3)]
        [TestCase(LogLevel.Warning, 4)]
        [TestCase(LogLevel.Information, 6)]
        [TestCase(LogLevel.Debug, 6)]
        [TestCase(LogLevel.Trace, 7)]
        public async Task LinuxEventLogMonitorExecutesTheExpectedJournalCtlCommand(LogLevel level, int expectedPriority)
        {
            this.Parameters["LogLevel"] = level.ToString();

            string exampleEvents = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "linux_journalctl_example_1.txt"));
            DateTime lastCheckpoint = DateTime.Now.AddSeconds(-30);

            string expectedCommand = $"sudo journalctl --since=\"{lastCheckpoint.ToString("yyyy-MM-dd HH:mm:ss")}\" --priority={expectedPriority} --output=json-pretty --utc";

            bool confirmed = false;
            this.ProcessManager.OnProcessCreated = (process) =>
            {
                string actualCommand = process.FullCommand();
                if (actualCommand.StartsWith("sudo journalctl"))
                {
                    Assert.AreEqual(expectedCommand, actualCommand);
                    confirmed = true;
                }
            };

            using (TestEventLogMonitor monitor = new TestEventLogMonitor(this))
            {
                await monitor.ProcessEventsAsync(lastCheckpoint, EventContext.None, CancellationToken.None);
                Assert.IsTrue(confirmed);

            }
        }

        [Test]
        public void LinuxEventLogMonitorConvertsEventRecordsCorrectly_1()
        {
            string exampleXml = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "linux_journalctl_example_1.txt"));
            JObject eventRecord = JObject.Parse(
                @"{
                    ""_UID"" : ""1000"",
                    ""_SYSTEMD_OWNER_UID"" : ""1000"",
                    ""_CMDLINE"" : ""sudo dmidecode --type memory"",
                    ""SYSLOG_IDENTIFIER"" : ""sudo"",
                    ""_SYSTEMD_SLICE"" : ""user-1000.slice"",
                    ""_TRANSPORT"" : ""syslog"",
                    ""_AUDIT_LOGINUID"" : ""1000"",
                    ""_SOURCE_REALTIME_TIMESTAMP"" : ""1744224896339612"",
                    ""__MONOTONIC_TIMESTAMP"" : ""844307405"",
                    ""_EXE"" : ""/usr/bin/sudo"",
                    ""_SYSTEMD_SESSION"" : ""1"",
                    ""SYSLOG_FACILITY"" : ""10"",
                    ""_BOOT_ID"" : ""1dcc4995b7b349dc90454fd461970aeb"",
                    ""_SYSTEMD_INVOCATION_ID"" : ""59d8cae2570f4a44845e7317318bada9"",
                    ""SYSLOG_TIMESTAMP"" : ""Apr  9 18:54:56 "",
                    ""_COMM"" : ""sudo"",
                    ""_PID"" : ""1856"",
                    ""_GID"" : ""1000"",
                    ""_CAP_EFFECTIVE"" : ""1ffffffffff"",
                    ""__CURSOR"" : ""s=a353fda1ecc549628c93d8d4f2a59e91;i=c501;b=1dcc4995b7b349dc90454fd461970aeb;m=32531bcd;t=6325d015acebd;x=21bad4606b1d6eb8"",
                    ""__REALTIME_TIMESTAMP"" : ""1744224896339645"",
                    ""_SYSTEMD_CGROUP"" : ""/user.slice/user-1000.slice/session-1.scope"",
                    ""MESSAGE"" : ""junovmadmin : TTY=pts/0 ; PWD=... ; USER=root ; COMMAND=/usr/sbin/dmidecode --type memory"",
                    ""PRIORITY"" : ""5"",
                    ""_SELINUX_CONTEXT"" : ""unconfined\n"",
                    ""_AUDIT_SESSION"" : ""1"",
                    ""_MACHINE_ID"" : ""743691b0fc204defb0bcf6f1e9824f3a"",
                    ""_SYSTEMD_USER_SLICE"" : ""-.slice"",
                    ""_SYSTEMD_UNIT"" : ""session-1.scope"",
                    ""_HOSTNAME"" : ""demo-vm01""
            }");

            IDictionary<string, object> convertedData = TestEventLogMonitor.ConvertRecord(eventRecord);

            Assert.IsNotEmpty(convertedData);
            Assert.True(convertedData.Count == 31);

            Assert.IsTrue(convertedData.ContainsKey("_TIMESTAMP"));
            Assert.AreEqual(1000, convertedData["_UID"]);
            Assert.AreEqual(1000, convertedData["_SYSTEMD_OWNER_UID"]);
            Assert.AreEqual("sudo dmidecode --type memory", convertedData["_CMDLINE"]);
            Assert.AreEqual("sudo", convertedData["_SYSLOG_IDENTIFIER"]);
            Assert.AreEqual("syslog", convertedData["_TRANSPORT"]);
            Assert.AreEqual(1000, convertedData["_AUDIT_LOGINUID"]);
            Assert.AreEqual(1744224896339612, convertedData["_SOURCE_REALTIME_TIMESTAMP"]);
            Assert.AreEqual(844307405, convertedData["__MONOTONIC_TIMESTAMP"]);
            Assert.AreEqual("/usr/bin/sudo", convertedData["_EXE"]);
            Assert.AreEqual(1, convertedData["_SYSTEMD_SESSION"]);
            Assert.AreEqual(10, convertedData["_SYSLOG_FACILITY"]);
            Assert.AreEqual("1dcc4995b7b349dc90454fd461970aeb", convertedData["_BOOT_ID"]);
            Assert.AreEqual("59d8cae2570f4a44845e7317318bada9", convertedData["_SYSTEMD_INVOCATION_ID"]);
            Assert.AreEqual("Apr  9 18:54:56", convertedData["_SYSLOG_TIMESTAMP"]);
            Assert.AreEqual("sudo", convertedData["_COMM"]);
            Assert.AreEqual(1856, convertedData["_PID"]);
            Assert.AreEqual(1000, convertedData["_GID"]);
            Assert.AreEqual("1ffffffffff", convertedData["_CAP_EFFECTIVE"]);
            Assert.AreEqual("s=a353fda1ecc549628c93d8d4f2a59e91;i=c501;b=1dcc4995b7b349dc90454fd461970aeb;m=32531bcd;t=6325d015acebd;x=21bad4606b1d6eb8", convertedData["__CURSOR"]);
            Assert.AreEqual(1744224896339645, convertedData["__REALTIME_TIMESTAMP"]);
            Assert.AreEqual("/user.slice/user-1000.slice/session-1.scope", convertedData["_SYSTEMD_CGROUP"]);
            Assert.AreEqual("junovmadmin : TTY=pts/0 ; PWD=... ; USER=root ; COMMAND=/usr/sbin/dmidecode --type memory", convertedData["_MESSAGE"]);
            Assert.AreEqual(5, convertedData["_PRIORITY"]);
            Assert.AreEqual("unconfined", convertedData["_SELINUX_CONTEXT"]);
            Assert.AreEqual(1, convertedData["_AUDIT_SESSION"]);
            Assert.AreEqual("743691b0fc204defb0bcf6f1e9824f3a", convertedData["_MACHINE_ID"]);
            Assert.AreEqual("-.slice", convertedData["_SYSTEMD_USER_SLICE"]);
            Assert.AreEqual("session-1.scope", convertedData["_SYSTEMD_UNIT"]);
            Assert.AreEqual("demo-vm01", convertedData["_HOSTNAME"]);
        }

        [Test]
        public async Task LinuxEventLogMonitorProcessesEventsAsExpected()
        {
            string exampleEvents = System.IO.File.ReadAllText(Path.Combine(ExamplesFolder, "linux_journalctl_example_1.txt"));

            this.ProcessManager.OnProcessCreated = (process) =>
            {
                if (process.FullCommand().StartsWith("sudo journalctl"))
                {
                    process.StandardOutput.Append(exampleEvents);
                }
            };

            using (TestEventLogMonitor monitor = new TestEventLogMonitor(this))
            {
                DateTime lastCheckpoint = DateTime.Now.AddSeconds(-30);
                await monitor.ProcessEventsAsync(lastCheckpoint, EventContext.None, CancellationToken.None);

                var messagesLogged = this.Logger.MessagesLogged("EventLog");
                Assert.IsNotNull(messagesLogged);
                Assert.IsTrue(messagesLogged.Count() == 1);

                EventContext context = messagesLogged.First().Item3 as EventContext;
                Assert.IsNotNull(context);
                Assert.IsTrue(context.Properties.TryGetValue("eventInfo", out object eventInfo));
                IDictionary<string, object> eventInfoSet = eventInfo as IDictionary<string, object>;
                Assert.IsNotNull(eventInfoSet);
                Assert.IsTrue(eventInfoSet.ContainsKey("events"));
                Assert.IsTrue(eventInfoSet.ContainsKey("level"));
                Assert.IsTrue(eventInfoSet.ContainsKey("lastCheckPoint"));
            }
        }

        private class TestEventLogMonitor : LinuxEventLogMonitor
        {
            public TestEventLogMonitor(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public new static Dictionary<string, object> ConvertRecord(JObject eventRecord)
            {
                return LinuxEventLogMonitor.ConvertRecord(eventRecord);
            }

            public new Task ProcessEventsAsync(DateTime lastCheckPoint, EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ProcessEventsAsync(lastCheckPoint, telemetryContext, cancellationToken);
            }
        }
    }
}
