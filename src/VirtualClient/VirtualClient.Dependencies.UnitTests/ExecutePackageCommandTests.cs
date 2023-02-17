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
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class ExecutePackageCommandTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                [nameof(ExecutePackageCommand.PackageName)] = "anypackage"
            };

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(new DependencyPath("anypackage", this.mockFixture.GetPackagePath("anypackage")));
        }

        [Test]
        [TestCase("anycommand", "anycommand", null)]
        [TestCase("anycommand  ", "anycommand", null)]
        [TestCase("./anycommand", "./anycommand", null)]
        [TestCase("./anycommand --argument=value", "./anycommand", "--argument=value")]
        [TestCase("./anycommand --argument=value --argument2 value2", "./anycommand", "--argument=value --argument2 value2")]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag", "./anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag   ", "./anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("../../anycommand --argument=value --argument2 value2 --flag   ", "../../anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("/home/user/anycommand", "/home/user/anycommand", null)]
        [TestCase("/home/user/anycommand --argument=value --argument2 value2", "/home/user/anycommand", "--argument=value --argument2 value2")]
        [TestCase("\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "\"/home/user/dir with space/anycommand\"", "--argument=value --argument2 value2")]
        [TestCase("sudo anycommand", "sudo", "anycommand")]
        [TestCase("sudo ./anycommand", "sudo", "./anycommand")]
        [TestCase("sudo /home/user/anycommand", "sudo", "/home/user/anycommand")]
        [TestCase("sudo /home/user/anycommand --argument=value --argument2 value2", "sudo", "/home/user/anycommand --argument=value --argument2 value2")]
        [TestCase("sudo \"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "sudo", "\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2")]
        public async Task ExecutePackageCommandExecutesTheExpectedCommandOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Unix);

            using (ExecutePackageCommand command = new ExecutePackageCommand(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;

                string expectedWorkingDirectory = (await command.GetPlatformSpecificPackageAsync("anypackage", CancellationToken.None)).Path;

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(process.FullCommand(), $"{expectedCommand} {expectedCommandArguments}".Trim());
                    Assert.AreEqual(expectedWorkingDirectory, process.StartInfo.WorkingDirectory);
                };

                await command.ExecuteAsync(CancellationToken.None);
            }
        }

        [Test]
        [TestCase("anycommand.exe", "anycommand.exe", null)]
        [TestCase("anycommand.exe  ", "anycommand.exe", null)]
        [TestCase(".\\anycommand.exe", ".\\anycommand.exe", null)]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2", ".\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag   ", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value", ".\\anycommand.exe", "--argument=value")]
        [TestCase("..\\..\\anycommand.exe --argument=value", "..\\..\\anycommand.exe", "--argument=value")]
        [TestCase("C:\\Users\\User\\anycommand.exe", "C:\\Users\\User\\anycommand.exe", null)]
        [TestCase("C:\\Users\\User\\anycommand.exe --argument=value --argument2 value2", "C:\\Users\\User\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"--argument=value --argument2 value2", "\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"", "--argument=value --argument2 value2")]
        public async Task ExecutePackageCommandExecutesTheExpectedCommandOnWindowsSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (ExecutePackageCommand command = new ExecutePackageCommand(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                command.Parameters[nameof(ExecuteCommand.Command)] = fullCommand;

                string expectedWorkingDirectory = (await command.GetPlatformSpecificPackageAsync("anypackage", CancellationToken.None)).Path;

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    Assert.AreEqual(process.FullCommand(), $"{expectedCommand} {expectedCommandArguments}".Trim());
                    Assert.AreEqual(expectedWorkingDirectory, process.StartInfo.WorkingDirectory);
                };

                await command.ExecuteAsync(CancellationToken.None);
            }
        }
    }
}
