// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class ExecuteCommandTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "anycommand";
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
        public async Task ExecuteCommandExecutesTheExpectedCommandOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Unix);

            using (TestExecuteCommand command = new TestExecuteCommand(this.mockFixture))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(process.FullCommand(), $"{expectedCommand} {expectedCommandArguments}".Trim());
                };

                await command.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        [TestCase("anycommand.exe", "anycommand.exe", null)]
        [TestCase("anycommand.exe  ", "anycommand.exe", null)]
        [TestCase("anycommand.exe --argument=value --argument2 value2", "anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("C:\\Users\\User\\anycommand.exe", "C:\\Users\\User\\anycommand.exe", null)]
        [TestCase("C:\\Users\\User\\anycommand.exe --argument=value --argument2 value2", "C:\\Users\\User\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("\"C:\\Users\\User\\Dir With Space\\anycommand.exe\" --argument=value --argument2 value2", "\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"", "--argument=value --argument2 value2")]
        public async Task ExecuteCommandExecutesTheExpectedCommandOnWindowsSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (TestExecuteCommand command = new TestExecuteCommand(this.mockFixture))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(process.FullCommand(), $"{expectedCommand} {expectedCommandArguments}".Trim());
                };

                await command.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        [TestCase("/home/user/anycommand&&/home/user/anyothercommand", "/home/user/anycommand;/home/user/anyothercommand")]
        [TestCase("/home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "/home/user/anycommand --argument=value;/home/user/anyothercommand --argument2=value2")]
        [TestCase("sudo anycommand&&anyothercommand", "sudo anycommand;sudo anyothercommand")]
        [TestCase("sudo /home/user/anycommand&&/home/user/anyothercommand", "sudo /home/user/anycommand;sudo /home/user/anyothercommand")]
        [TestCase("sudo /home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "sudo /home/user/anycommand --argument=value;sudo /home/user/anyothercommand --argument2=value2")]
        public async Task ExecuteCommandSupportsCommandChainingOnUnixSystems(string fullCommand, string expectedCommandExecuted)
        {
            this.SetupDefaults(PlatformID.Unix);

            using (TestExecuteCommand command = new TestExecuteCommand(this.mockFixture))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;
                List<string> expectedCommands = new List<string>(expectedCommandExecuted.Split(';'));

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove(process.FullCommand());
                };

                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(
            "sudo dmesg && sudo lsblk && sudo mount && sudo df -h && sudo find /sys -name scheduler -print",
            "sudo dmesg;sudo lsblk;sudo mount;sudo df -h;sudo find /sys -name scheduler -print")]
        public async Task ExecuteCommandSupportsCommandChainingOnUnixSystems_Bug_1(string fullCommand, string expectedCommandExecuted)
        {
            // Bug Scenario:
            // Spaces (whitespace) in the commands due to the chaining SHOULD NOT cause
            // parsing issues.
            //
            // e.g.
            // "sudo dmesg && sudo lsblk " resulting in the command being identified as "sudo o lsblk"

            this.SetupDefaults(PlatformID.Unix);

            using (TestExecuteCommand command = new TestExecuteCommand(this.mockFixture))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;
                List<string> expectedCommands = new List<string>(expectedCommandExecuted.Split(';'));

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove(process.FullCommand());
                };

                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase("C:\\\\Users\\User\\anycommand&&C:\\\\home\\user\\anyothercommand", "C:\\\\Users\\User\\anycommand;C:\\\\home\\user\\anyothercommand")]
        [TestCase("C:\\\\Users\\User\\anycommand --argument=1&&C:\\\\home\\user\\anyothercommand --argument=2", "C:\\\\Users\\User\\anycommand --argument=1;C:\\\\home\\user\\anyothercommand --argument=2")]
        public async Task ExecuteCommandSupportsCommandChainingOnWindowsSystems(string fullCommand, string expectedCommandExecuted)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (TestExecuteCommand command = new TestExecuteCommand(this.mockFixture))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;
                List<string> expectedCommands = new List<string>(expectedCommandExecuted.Split(';'));

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove(process.FullCommand());
                };

                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ExecuteCommandExecuteCommandsWhenThePlatformMatchesTheOnesDefinedInTheParameters_UnixSystems_SinglePlatformSpecified(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ExecuteCommandExecuteCommandsWhenThePlatformMatchesTheOnesDefinedInTheParameters_UnixSystems_MultiplePlatformsSpecified(PlatformID platform, Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] =
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}," +
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ExecuteCommandExecuteCommandsWhenThePlatformMatchesTheOnesDefinedInTheParameters_WindowsSystems_SinglePlatformSpecified(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ExecuteCommandExecuteCommandsWhenThePlatformMatchesTheOnesDefinedInTheParameters_WindowsSystems_MultiplePlatformsSpecified(PlatformID platform, Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] =
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.X64)}," +
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.Arm64)}";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(commandExecuted);
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task ExecuteCommandDoesNotExecuteCommandsUnlessThePlatformMatchesTheOnesDefinedInTheParameters_UnixSystems_SinglePlatformSpecified(Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(PlatformID.Unix, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, architecture);

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsFalse(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ExecuteCommandDoesNotExecuteCommandsUnlessThePlatformMatchesTheOnesDefinedInTheParameters_UnixSystems_MultiplePlatformSpecified(PlatformID platform, Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] =
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.X64)}," +
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Win32NT, Architecture.Arm64)}";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsFalse(commandExecuted);
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task ExecuteCommandDoesNotExecuteCommandsUnlessThePlatformMatchesTheOnesDefinedInTheParameters_WindowsSystems_SinglePlatformSpecified(Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(PlatformID.Win32NT, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] = PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, architecture);

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsFalse(commandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ExecuteCommandDoesNotExecuteCommandsUnlessThePlatformMatchesTheOnesDefinedInTheParameters_WindowsSystems_MultiplePlatformSpecified(PlatformID platform, Architecture architecture)
        {
            // Setup running on a Unix system but the supported platforms are
            // Windows systems.
            this.SetupDefaults(platform, architecture);

            // e.g.
            // linux-x64, win-arm64
            this.mockFixture.Parameters[nameof(ExecuteCommand.SupportedPlatforms)] =
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}," +
                $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => commandExecuted = true;

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                // We SHOULD NOT execute on the system when the platform/architecture does not match.
                await command.ExecuteAsync(CancellationToken.None);
                Assert.IsFalse(commandExecuted);
            }
        }

        [Test]
        public async Task ExecuteCommandResolvesPackagePathExpressionsOnInitializationInTheComponentParametersOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath:anypackage}\\build.exe&&{PackagePath:anypackage}\\build.exe install";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath:anypackage}";

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                await command.InitializeAsync(EventContext.None, CancellationToken.None);

                Assert.AreEqual($"{packagePath}\\build.exe&&{packagePath}\\build.exe install", command.Parameters[nameof(ExecuteCommand.Command)]);
                Assert.AreEqual(packagePath, command.Parameters[nameof(ExecuteCommand.WorkingDirectory)]);
            }
        }

        [Test]
        public async Task ExecuteCommandResolvesPackagePathExpressionsOnInitializationInTheComponentParametersOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath:anypackage}/configure&&{PackagePath:anypackage}/make";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath:anypackage}";

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                await command.InitializeAsync(EventContext.None, CancellationToken.None);

                Assert.AreEqual($"{packagePath}/configure&&{packagePath}/make", command.Parameters[nameof(ExecuteCommand.Command)]);
                Assert.AreEqual(packagePath, command.Parameters[nameof(ExecuteCommand.WorkingDirectory)]);
            }
        }

        [Test]
        [Platform("Win")]
        public async Task ExecuteCommandTheResolvedPackagePathExpressionsWhenExecutingCommandsOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath:anypackage}\\build.exe&&{PackagePath:anypackage}\\build.exe install";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath:anypackage}";

            List<string> expectedCommands = new List<string>
            {
                $"{packagePath}\\build.exe",
                $"{packagePath}\\build.exe install"
            };

            bool confirmed = false;
            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove(process.FullCommand());
                    Assert.AreEqual(packagePath, process.StartInfo.WorkingDirectory);
                    confirmed = true;
                };

                await command.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(confirmed);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task ExecuteCommandTheResolvedPackagePathExpressionsWhenExecutingCommandsOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath:anypackage}/configure&&{PackagePath:anypackage}/make";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath:anypackage}";

            List<string> expectedCommands = new List<string>
            {
                $"{packagePath}/configure",
                $"{packagePath}/make"
            };

            bool confirmed = false;
            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove(process.FullCommand());
                    Assert.AreEqual(packagePath, process.StartInfo.WorkingDirectory);
                    confirmed = true;
                };

                await command.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(confirmed);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task ExecuteCommandResolvesPlatformSpecificPackagePathExpressionsOnInitializationInTheComponentParametersOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");
            string platformSpecificPath = this.mockFixture.Combine(packagePath, "win-x64");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath/Platform} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath/Platform:anypackage}\\build.exe&&{PackagePath/Platform:anypackage}\\build.exe install";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath/Platform:anypackage}";

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                await command.InitializeAsync(EventContext.None, CancellationToken.None);

                Assert.AreEqual($"{platformSpecificPath}\\build.exe&&{platformSpecificPath}\\build.exe install", command.Parameters[nameof(ExecuteCommand.Command)]);
                Assert.AreEqual(platformSpecificPath, command.Parameters[nameof(ExecuteCommand.WorkingDirectory)]);
            }
        }

        [Test]
        public async Task ExecuteCommandResolvesPlatformSpecificPackagePathExpressionsOnInitializationInTheComponentParametersOnUnixSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            string packagePath = this.mockFixture.GetPackagePath("anypackage");
            string platformSpecificPath = this.mockFixture.Combine(packagePath, "linux-x64");

            // The package MUST exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("anypackage").ReturnsAsync(new DependencyPath("anypackage", packagePath));

            // The component uses the {PackagePath} referencing expression in both command and
            // working directory parameters.
            this.mockFixture.Parameters[nameof(ExecuteCommand.Command)] = "{PackagePath/Platform:anypackage}/configure&&{PackagePath/Platform:anypackage}/make";
            this.mockFixture.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = "{PackagePath/Platform:anypackage}";

            using (var command = new TestExecuteCommand(this.mockFixture))
            {
                await command.InitializeAsync(EventContext.None, CancellationToken.None);

                Assert.AreEqual($"{platformSpecificPath}/configure&&{platformSpecificPath}/make", command.Parameters[nameof(ExecuteCommand.Command)]);
                Assert.AreEqual(platformSpecificPath, command.Parameters[nameof(ExecuteCommand.WorkingDirectory)]);
            }
        }

        private class TestExecuteCommand : ExecuteCommand
        {
            public TestExecuteCommand(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
