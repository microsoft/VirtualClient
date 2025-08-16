// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Dependencies;

    [TestFixture]
    [Category("Unit")]
    internal class ExecuteCommandMonitorTests : MockFixture
    {
        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);
            this.Parameters[nameof(ExecuteCommandMonitor.Command)] = "anycommand";
            this.Parameters[nameof(ExecuteCommandMonitor.MonitorEventType)] = "any_event_type";
            this.Parameters[nameof(ExecuteCommandMonitor.MonitorEventSource)] = "any_event_source";
        }

        [Test]
        [TestCase("anycommand", "anycommand", null)]
        [TestCase("anycommand  ", "anycommand", null)]
        [TestCase("anycommand --argument=value", "anycommand", "--argument=value")]
        [TestCase("/home/user/anycommand", "/home/user/anycommand", null)]
        [TestCase("/home/user/anycommand --argument=value --argument2 value2", "/home/user/anycommand", "--argument=value --argument2 value2")]
        [TestCase("\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "\"/home/user/dir with space/anycommand\"", "--argument=value --argument2 value2")]
        [TestCase("sudo anycommand", "sudo", "anycommand")]
        [TestCase("sudo /home/user/anycommand", "sudo", "/home/user/anycommand")]
        [TestCase("sudo /home/user/anycommand --argument=value --argument2 value2", "sudo", "/home/user/anycommand --argument=value --argument2 value2")]
        [TestCase("sudo \"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "sudo", "\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2")]
        public async Task ExecuteCommandMonitorExecutesTheExpectedCommandOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Unix);

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = fullCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual($"{expectedCommand} {expectedCommandArguments}".Trim(), process.FullCommand());
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        [TestCase("anycommand.exe", "anycommand.exe", null)]
        [TestCase("anycommand.exe  ", "anycommand.exe", null)]
        [TestCase("anycommand.exe --argument=value --argument2 value2", "anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("C:\\Users\\User\\anycommand.exe", "C:\\Users\\User\\anycommand.exe", null)]
        [TestCase("C:\\Users\\User\\anycommand.exe --argument=value --argument2 value2", "C:\\Users\\User\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("\"C:\\Users\\User\\Dir With Space\\anycommand.exe\" --argument=value --argument2 value2", "\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"", "--argument=value --argument2 value2")]
        public async Task ExecuteCommandMonitorExecutesTheExpectedCommandOnWindowsSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = fullCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual($"{expectedCommand} {expectedCommandArguments}".Trim(), process.FullCommand());
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        [TestCase("/home/user/anycommand&&/home/user/anyothercommand", "/home/user/anycommand;/home/user/anyothercommand")]
        [TestCase("/home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "/home/user/anycommand --argument=value;/home/user/anyothercommand --argument2=value2")]
        [TestCase("sudo anycommand&&anyothercommand", "sudo anycommand;sudo anyothercommand")]
        [TestCase("sudo /home/user/anycommand&&/home/user/anyothercommand", "sudo /home/user/anycommand;sudo /home/user/anyothercommand")]
        [TestCase("sudo /home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "sudo /home/user/anycommand --argument=value;sudo /home/user/anyothercommand --argument2=value2")]
        public async Task ExecuteCommandMonitorSupportsCommandChaining(string originalCommand, string expectedCommandExecuted)
        {
            this.SetupDefaults(PlatformID.Unix);
            List<string> commandsExecuted = new List<string>(expectedCommandExecuted.Split(";"));

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = originalCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    commandsExecuted.Remove(process.FullCommand());
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                Assert.IsEmpty(commandsExecuted);
            }
        }

        [Test]
        [TestCase(
           "sudo dmesg && sudo lsblk && sudo mount && sudo df -h && sudo find /sys -name scheduler -print",
           "sudo dmesg;sudo lsblk;sudo mount;sudo df -h;sudo find /sys -name scheduler -print")]
        public async Task ExecuteCommandMonitorSupportsCommandChainingOnUnixSystems_Bug_1(string fullCommand, string expectedCommandExecuted)
        {
            // Bug Scenario:
            // Spaces (whitespace) in the commands due to the chaining SHOULD NOT cause
            // parsing issues.
            //
            // e.g.
            // "sudo dmesg && sudo lsblk " resulting in the command being identified as "sudo o lsblk"

            this.SetupDefaults(PlatformID.Unix);
            List<string> commandsExecuted = new List<string>(expectedCommandExecuted.Split(";"));

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = fullCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    commandsExecuted.Remove(process.FullCommand());
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                Assert.IsEmpty(commandsExecuted);
            }
        }

        [Test]
        [TestCase("C:\\\\Users\\User\\anycommand&&C:\\\\home\\user\\anyothercommand", "C:\\\\Users\\User\\anycommand;C:\\\\home\\user\\anyothercommand")]
        [TestCase("C:\\\\Users\\User\\anycommand --argument=1&&C:\\\\home\\user\\anyothercommand --argument=2", "C:\\\\Users\\User\\anycommand --argument=1;C:\\\\home\\user\\anyothercommand --argument=2")]
        public async Task ExecuteCommandSupportsCommandChainingOnWindowsSystems(string originalCommand, string expectedCommandExecuted)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = originalCommand;
                List<string> commandsExecuted = new List<string>(expectedCommandExecuted.Split(';'));

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    commandsExecuted.Remove(process.FullCommand());
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                Assert.IsEmpty(commandsExecuted);
            }
        }

        [Test]
        public async Task ExecuteCommandMonitorHandlesAnomaliesWhenWorkingDirectoriesAreDefined_1()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "anyscript.sh";
            string workingDirectory = "/home/user/scripts";
            string expectedCommand = this.Combine(workingDirectory, command);
            string expectedWorkingDirectory = workingDirectory;

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = command;
                monitor.Parameters[nameof(ExecuteCommandMonitor.WorkingDirectory)] = workingDirectory;

                bool confirmed = false;
                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(expectedCommand, process.FullCommand());
                    Assert.AreEqual(expectedWorkingDirectory, process.StartInfo.WorkingDirectory);
                    confirmed = true;
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task ExecuteCommandMonitorHandlesAnomaliesWhenWorkingDirectoriesAreDefined_2()
        {
            this.SetupDefaults(PlatformID.Unix);

            string command = "\"anyscript.sh\"";
            string workingDirectory = "/home/user/scripts";
            string expectedCommand = $"\"{this.Combine(workingDirectory, "anyscript.sh")}\"";
            string expectedWorkingDirectory = workingDirectory;

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = command;
                monitor.Parameters[nameof(ExecuteCommandMonitor.WorkingDirectory)] = workingDirectory;

                bool confirmed = false;
                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(expectedCommand, process.FullCommand());
                    Assert.AreEqual(expectedWorkingDirectory, process.StartInfo.WorkingDirectory);
                    confirmed = true;
                };

                await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task ExecuteCommandMonitorHandlesCommandBashScriptUsages()
        {
            this.SetupDefaults(PlatformID.Unix);

            IDictionary<string, string> usages = new Dictionary<string, string>
            {
                { 
                    "bash -c \"anyscript.sh --arg1=123 --arg2=456 --flag\"",
                    "bash -c \"anyscript.sh --arg1=123 --arg2=456 --flag\""
                },
                {
                    "sudo bash -c \"anyscript.sh --arg1=123 --arg2=456 --flag\"",
                    "sudo bash -c \"anyscript.sh --arg1=123 --arg2=456 --flag\""
                },
                {
                    $@"sudo bash -c "".\packages\scripts.1.0.0\anyscript.sh --arg1=123 --arg2=456 --flag""",
                    $@"sudo bash -c ""{Path.GetFullPath($@".\packages\scripts.1.0.0\anyscript.sh")} --arg1=123 --arg2=456 --flag"""
                }
            };

            using (TestExecuteCommandMonitor monitor = new TestExecuteCommandMonitor(this))
            {
                foreach (var entry in usages)
                {
                    string command = entry.Key;
                    string expectedCommand = entry.Value;

                    monitor.Parameters[nameof(ExecuteCommandMonitor.Command)] = command;

                    bool confirmed = false;
                    this.ProcessManager.OnProcessCreated = (process) =>
                    {
                        Assert.AreEqual(expectedCommand, process.FullCommand());
                        confirmed = true;
                    };

                    await monitor.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
                    Assert.IsTrue(confirmed);
                }
            }
        }

        private class TestExecuteCommandMonitor : ExecuteCommandMonitor
        {
            public TestExecuteCommandMonitor(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
                this.RetryPolicy = Policy.NoOpAsync();
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteCommandAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteCommandAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
