// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class ExecuteCommandTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        [TestCase("anycommand", "anycommand", null, false)]
        [TestCase("anycommand  ", "anycommand", null, false)]
        [TestCase("./anycommand", "./anycommand", null, false)]
        [TestCase("./anycommand --argument=value", "./anycommand", "--argument=value", false)]
        [TestCase("./anycommand --argument=value --argument2 value2", "./anycommand", "--argument=value --argument2 value2", false)]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag", "./anycommand", "--argument=value --argument2 value2 --flag", false)]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag   ", "./anycommand", "--argument=value --argument2 value2 --flag", false)]
        [TestCase("../../anycommand --argument=value --argument2 value2 --flag   ", "../../anycommand", "--argument=value --argument2 value2 --flag", false)]
        [TestCase("/home/user/anycommand", "/home/user/anycommand", null, false)]
        [TestCase("/home/user/anycommand --argument=value --argument2 value2", "/home/user/anycommand", "--argument=value --argument2 value2", false)]
        [TestCase("\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "\"/home/user/dir with space/anycommand\"", "--argument=value --argument2 value2", false)]
        [TestCase("sudo anycommand", "anycommand", null, true)]
        [TestCase("sudo ./anycommand", "./anycommand", null, true)]
        [TestCase("sudo /home/user/anycommand", "/home/user/anycommand", null, true)]
        [TestCase("sudo /home/user/anycommand --argument=value --argument2 value2", "/home/user/anycommand", "--argument=value --argument2 value2", true)]
        [TestCase("sudo \"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "\"/home/user/dir with space/anycommand\"", "--argument=value --argument2 value2", true)]
        public void ExecuteCommandCorrectlyIdentifiesThePartsOfTheCommandOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments, bool expectedRunElevated)
        {
            Assert.IsTrue(TestExecuteCommand.TryGetCommandParts(fullCommand, out string actualCommand, out string actualCommandArguments, out bool actualRunElevated));
            Assert.AreEqual(expectedCommand, actualCommand);
            Assert.AreEqual(expectedCommandArguments, actualCommandArguments);
            Assert.AreEqual(expectedRunElevated, actualRunElevated);
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
        public void ExecuteCommandCorrectlyIdentifiesThePartsOfTheCommandOnWindowsSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            Assert.IsTrue(TestExecuteCommand.TryGetCommandParts(fullCommand, out string actualCommand, out string actualCommandArguments, out bool actualRunElevated));
            Assert.AreEqual(expectedCommand, actualCommand);
            Assert.AreEqual(expectedCommandArguments, actualCommandArguments);
            Assert.IsFalse(actualRunElevated); // No "sudo" concept on Windows.
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
        [TestCase(".\\anycommand.exe", ".\\anycommand.exe", null)]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2", ".\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag   ", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value", ".\\anycommand.exe", "--argument=value")]
        [TestCase("..\\..\\anycommand.exe --argument=value", "..\\..\\anycommand.exe", "--argument=value")]
        [TestCase("C:\\Users\\User\\anycommand.exe", "C:\\Users\\User\\anycommand.exe", null)]
        [TestCase("C:\\Users\\User\\anycommand.exe --argument=value --argument2 value2", "C:\\Users\\User\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"--argument=value --argument2 value2", "\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"", "--argument=value --argument2 value2")]
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
        [TestCase("./anycommand&&./anyothercommand", "./anycommand;./anyothercommand")]
        [TestCase("./anycommand --argument=value&&./anyothercommand --argument2=value2", "./anycommand --argument=value;./anyothercommand --argument2=value2")]
        [TestCase("/home/user/anycommand&&/home/user/anyothercommand", "/home/user/anycommand;/home/user/anyothercommand")]
        [TestCase("/home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "/home/user/anycommand --argument=value;/home/user/anyothercommand --argument2=value2")]
        [TestCase("sudo anycommand&&anyothercommand", "sudo anycommand;sudo anyothercommand")]
        [TestCase("sudo /home/user/anycommand&&/home/user/anyothercommand", "sudo /home/user/anycommand;sudo /home/user/anyothercommand")]
        [TestCase("sudo /home/user/anycommand --argument=value&&/home/user/anyothercommand --argument2=value2", "sudo /home/user/anycommand --argument=value;sudo /home/user/anyothercommand --argument2=value2")]
        public async Task ExecuteCommandExecutesTheExpectedCommandOnUnixSystemsWhenMultipleCommandsAreProvided(string fullCommand, string expectedCommandExecuted)
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
        [TestCase(".\\anycommand&&.\\anyothercommand", ".\\anycommand;.\\anyothercommand")]
        [TestCase(".\\anycommand --argument=value&&.\\anyothercommand --argument2=value2", ".\\anycommand --argument=value;.\\anyothercommand --argument2=value2")]
        [TestCase("C:\\\\Users\\User\\anycommand&&C:\\\\home\\user\\anyothercommand", "C:\\\\Users\\User\\anycommand;C:\\\\home\\user\\anyothercommand")]
        [TestCase("C:\\\\Users\\User\\anycommand --argument=1&&C:\\\\home\\user\\anyothercommand --argument=2", "C:\\\\Users\\User\\anycommand --argument=1;C:\\\\home\\user\\anyothercommand --argument=2")]
        public async Task ExecuteCommandExecutesTheExpectedCommandOnWindowsSystemsWhenMultipleCommandsAreProvided(string fullCommand, string expectedCommandExecuted)
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

        private class TestExecuteCommand : ExecuteCommand
        {
            public TestExecuteCommand(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public static new bool TryGetCommandParts(string fullCommand, out string command, out string commandArguments, out bool runElevated)
            {
                return ExecuteCommand.TryGetCommandParts(fullCommand, out command, out commandArguments, out runElevated);
            }
        }
    }
}
