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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DefaultCommandTests : MockFixture
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
        public void DefaultCommandNormalizesPwshCommandLinesCorrectly(string originalCommand, string expectedCommand)
        {
            string actualCommand = TestDefaultCommand.NormalizeForPowerShell(originalCommand);
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
        public void DefaultCommandNormalizesPowerShellCommandLinesCorrectly(string originalCommand, string expectedCommand)
        {
            string actualCommand = TestDefaultCommand.NormalizeForPowerShell(originalCommand);
            Assert.AreEqual(expectedCommand, actualCommand);
        }

        [Test]
        public async Task DefaultCommandExecutesTheExpectedFlowWhenProvidedCommandLineArguments()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestDefaultCommand command = new TestDefaultCommand
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
        public async Task DefaultCommandExecutesTheExpectedFlowWhenProvidedCommandLineArgumentsAsWellAsProfiles()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestDefaultCommand command = new TestDefaultCommand
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
        public async Task DefaultCommandExecutesTheExpectedFlowWhenProvidedProfileArguments()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                TestDefaultCommand command = new TestDefaultCommand
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
        public void DefaultCommandThrowsIfTheLogicCannotDetermineTheCorrectFlow(string commandLine)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // No command line or profiles provided.
                TestDefaultCommand command = new TestDefaultCommand
                {
                    Command = commandLine,
                    ClientId = "AnyAgent",
                    Timeout = ProfileTiming.OneIteration(),
                    ExecutionSystem = "AnySystem",
                    ExperimentId = Guid.NewGuid().ToString(),
                    InstallDependencies = false
                };

                NotSupportedException error = Assert.Throws<NotSupportedException>(() => command.Initialize(Array.Empty<string>(), this.PlatformSpecifics));

                Assert.AreEqual(
                    "Command line usage is not supported. The intended command or profile execution intentions are unclear.", 
                    error.Message);
            }
        }

        private class TestDefaultCommand : DefaultCommand
        {
            public Action OnExecuteCommand { get; set; }

            public Action OnExecuteProfiles { get; set; }

            public new static string NormalizeForPowerShell(string commandLine)
            {
                return DefaultCommand.NormalizeForPowerShell(commandLine);
            }

            protected override Task<int> ExecuteCommandAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
            {
                this.OnExecuteCommand?.Invoke();
                return Task.FromResult(0);
            }

            protected override Task<int> ExecuteProfilesAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
            {
                this.OnExecuteProfiles?.Invoke();
                return Task.FromResult(0);
            }

            public new void Initialize(string[] args, PlatformSpecifics platformSpecifics)
            {
                base.Initialize(args, platformSpecifics);
            }

            protected override IServiceCollection InitializeDependencies(string[] args, PlatformSpecifics platformSpecifics)
            {
                IServiceCollection dependencies = new ServiceCollection();
                dependencies.AddSingleton<ILogger>(NullLogger.Instance);

                return dependencies;
            }
        }
    }
}
