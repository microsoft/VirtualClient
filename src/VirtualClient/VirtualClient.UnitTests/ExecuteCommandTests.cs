// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ExecuteCommandTests : MockFixture
    {
        public void Setup(PlatformID platform)
        {
            this.Setup(platform, Architecture.Arm64);
        }

        [Test]
        [TestCase(
            @"pwsh /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs/pwsh",
            @"pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs/pwsh")
            ]
        [TestCase(
            @"pwsh -Command /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs/pwsh",
            @"pwsh -NonInteractive -Command /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs/pwsh")
            ]
        [TestCase(
            @"pwsh.exe C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs\pwsh",
            @"pwsh.exe -NonInteractive C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs\pwsh")
            ]
        [TestCase(
            @"pwsh.exe -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs\pwsh",
            @"pwsh.exe -NonInteractive -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs\pwsh")
            ]
        public void ExecuteCommandNormalizesPwshCommandLinesCorrectly(string originalCommand, string expectedCommand)
        {
            string actualCommand = TestExecuteCommand.NormalizeForPowerShell(originalCommand);
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [Test]
        [TestCase(
          @"powershell C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs",
          @"powershell -NonInteractive C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs")
            ]
        [TestCase(
          @"powershell -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs",
          @"powershell -NonInteractive -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs")
            ]
        [TestCase(
          @"powershell.exe C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs",
          @"powershell.exe -NonInteractive C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs")
            ]
        [TestCase(
          @"powershell.exe -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs",
          @"powershell.exe -NonInteractive -Command C:\Scripts\Invoke-Script.ps1 -Name AnyScript -LogDirectory C:\Logs")
            ]
        public void ExecuteCommandNormalizesPowerShellCommandLinesCorrectly(string originalCommand, string expectedCommand)
        {
            string actualCommand = TestExecuteCommand.NormalizeForPowerShell(originalCommand);
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [Test]
        public async Task ExecuteCommandExecutesTheExpectedFlowWhenProvidedCommandLineArguments()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestExecuteCommand command = new TestExecuteCommand
                {
                    Command = "pwsh /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs",
                    ClientId = "AnyAgent",
                    Timeout = ProfileTiming.OneIteration(),
                    ExecutionSystem = "AnySystem",
                    ExperimentId = Guid.NewGuid().ToString(),
                    InstallDependencies = false
                };

                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;
                command.OnExecuteCommand = () => commandFlowExecuted = true;
                command.OnExecuteProfiles = () => profileFlowExecuted = true;

                await command.ExecuteAsync(command.Command.Split(' '), tokenSource);

                Assert.IsTrue(commandFlowExecuted);
                Assert.IsFalse(profileFlowExecuted);
            }
        }

        [Test]
        public async Task ExecuteCommandExecutesTheExpectedFlowWhenProvidedCommandLineArgumentsAsWellAsProfiles()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestExecuteCommand command = new TestExecuteCommand
                {
                    Command = "pwsh /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs",
                    ClientId = "AnyAgent",
                    Timeout = ProfileTiming.OneIteration(),
                    ExecutionSystem = "AnySystem",
                    ExperimentId = Guid.NewGuid().ToString(),
                    InstallDependencies = false,
                    Profiles = new List<DependencyProfileReference>
                    {
                        new DependencyProfileReference("MONITORS-DEFAULT.json")
                    }
                };

                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;
                command.OnExecuteCommand = () => commandFlowExecuted = true;
                command.OnExecuteProfiles = () => profileFlowExecuted = true;

                await command.ExecuteAsync($"{command.Command} --profile=MONITORS-DEFAULT.json".Split(' '), tokenSource);

                Assert.IsTrue(commandFlowExecuted);
                Assert.IsFalse(profileFlowExecuted);
            }
        }

        [Test]
        public async Task ExecuteCommandExecutesTheExpectedFlowWhenProvidedProfileArguments()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestExecuteCommand command = new TestExecuteCommand
                {
                    ClientId = "AnyAgent",
                    Timeout = ProfileTiming.OneIteration(),
                    ExecutionSystem = "AnySystem",
                    ExperimentId = Guid.NewGuid().ToString(),
                    InstallDependencies = false,
                    Profiles = new List<DependencyProfileReference>
                    {
                        new DependencyProfileReference("ANY-PROFILE.json")
                    }
                };

                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;
                command.OnExecuteCommand = () => commandFlowExecuted = true;
                command.OnExecuteProfiles = () => profileFlowExecuted = true;

                await command.ExecuteAsync("--profile=ANY-PROFILE.json --timeout=10".Split(' '), tokenSource);

                Assert.IsTrue(profileFlowExecuted);
                Assert.IsFalse(commandFlowExecuted);
            }
        }

        [Test]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        public void ExecuteCommandThrowsIfTheLogicCannotDetermineTheCorrectFlow(string commandLine)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // No command line or profiles provided.
                TestExecuteCommand command = new TestExecuteCommand
                {
                    Command = commandLine,
                    ClientId = "AnyAgent",
                    Timeout = ProfileTiming.OneIteration(),
                    ExecutionSystem = "AnySystem",
                    ExperimentId = Guid.NewGuid().ToString(),
                    InstallDependencies = false
                };

                NotSupportedException error = Assert.ThrowsAsync<NotSupportedException>(() => command.ExecuteAsync(Array.Empty<string>(), tokenSource));

                Assert.AreEqual(
                    "Command line usage is not supported. The intended command or profile execution intentions are unclear.", 
                    error.Message);
            }
        }

        private class TestExecuteCommand : ExecuteCommand
        {
            public Action OnExecuteCommand { get; set; }

            public Action OnExecuteProfiles { get; set; }

            public new static string NormalizeForPowerShell(string commandLine)
            {
                return ExecuteCommand.NormalizeForPowerShell(commandLine);
            }

            protected override Task<int> ExecuteCommandAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                this.OnExecuteCommand?.Invoke();
                return Task.FromResult(0);
            }

            protected override Task<int> ExecuteProfilesAsync(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                this.OnExecuteProfiles?.Invoke();
                return Task.FromResult(0);
            }
        }
    }
}
