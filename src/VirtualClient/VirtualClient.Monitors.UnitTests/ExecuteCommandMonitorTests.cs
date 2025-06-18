// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
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

            using (TestExecuteCommandMonitor command = new TestExecuteCommandMonitor(this))
            {
                command.Parameters[nameof(ExecuteCommandMonitor.Command)] = fullCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual($"{expectedCommand} {expectedCommandArguments}".Trim(), process.FullCommand());
                };

                await command.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
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

            using (TestExecuteCommandMonitor command = new TestExecuteCommandMonitor(this))
            {
                command.Parameters[nameof(ExecuteCommandMonitor.Command)] = fullCommand;

                this.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual($"{expectedCommand} {expectedCommandArguments}".Trim(), process.FullCommand());
                };

                await command.ExecuteCommandAsync(EventContext.None, CancellationToken.None);
            }
        }

        private class TestExecuteCommandMonitor : ExecuteCommandMonitor
        {
            public TestExecuteCommandMonitor(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
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
