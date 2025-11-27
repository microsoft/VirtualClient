// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Agent
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
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
    public class ExecuteRemoteAgentCommandTests
    {
        // Notes on .NET Command Line Parsing
        // The .NET command line parsing automatically removes quotation marks from arguments on the command 
        // line and converts the command string into an array.
        //
        // e.g.
        // --command="/any/path/to/binary.exe --log-dir='/logs'" will be converted to --command=/any/path/to/binary.exe --log-dir='/logs'
        //
        // The unit tests below are written with the presumption that the array that is parsed thus matches
        // how the .NET command line parsing behavior works.

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--api-port", "4501")]
        [TestCase("-c", null)]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("-C", "/any/command")]
        [TestCase("--command", "/any/command")]
        [TestCase("--content-store", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content-path-template", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--content-path", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("-d", null)]
        [TestCase("--dependencies", null)]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("-f", null)]
        [TestCase("--fail-fast", null)]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--iterations", "3")]
        [TestCase("--key-vault", "https://anyvault.vault.windows.net")]
        [TestCase("--layout-path", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layout", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--logger", "file")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packages", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--scenarios", "Scenario1")]
        [TestCase("--system", "Azure")]
        [TestCase("--proxy", "https://proxy.azure.net?crti=issuerName&crts=subjectName")]
        [TestCase("--proxy", "https://proxy.azure.net")]
        [TestCase("--proxy", "https://192.168.1.10:8443")]
        [TestCase("--proxy", "https://192.168.1.10")]
        [TestCase("--proxy-api", "https://proxy.azure.net/?crti=issuerName&crts=subjectName")]
        [TestCase("--timeout", "01:00:00")]
        [TestCase("--timeout", "01:00:00,deterministic")]
        [TestCase("--timeout", "01:00:00,deterministic*")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void SdkAgentRemoteCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "--profile", "PERF-ANY-PROFILE.json"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.SdkAgent.CreateCommandBuilder(arguments.ToArray(), cancellationSource)
                        .Build()
                        .Parse(arguments);

                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, message: $"Option '{option}' is not supported.");
            }
        }

        [Test]
        public void SdkAgentRemoteCommandPreventsConfusingOperationsOnTheControllerSystemMeantOnlyForTheTargetSystem()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] args = new string[] { "remote", "--profile=ANY-PROFILE.json", "--scenarios=-Scenario1,-Scenario2", "--dependencies", "--target=any@1.2.3.4;pwd" };

                var cmd = new TestExecuteRemoteAgentCommand
                {
                    Profiles = new List<DependencyProfileReference>
                    {
                        new DependencyProfileReference("ANY-PROFILE.json")
                    }
                };

                cmd.Initialize(args, new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64));

                // The following are not allowed on the controller system when using the "remote" command line:
                // 1) installation of dependencies on the controller.
                // 2) Targeting specific scenarios in the profile on the controller. This would be a source
                //    of confusion when the execution of the profile did nothing because a scenario meant for the
                //    target system does not exist in the profile for the remote execution components.
                Assert.IsFalse(cmd.InstallDependencies);
                Assert.IsNull(cmd.Scenarios);
            }
        }

        [Test]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=https://anystorage --timeout=01:00:00 --target=user@10.1.2.3;pass",
            "--profile=ANY-PROFILE.json --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=https://anystorage --timeout=01:00:00 --target=user@10.1.2.3;pass --target=user@10.1.2.4;pass",
            "--profile=ANY-PROFILE.json --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote --profile=ANY-PROFILE.json --packages=https://anystorage --content=https://anystorage --timeout=01:00:00 --package-dir=/any/packages --target=user@10.1.2.3;pass --target=user@10.1.2.4;pass",
            "--profile=ANY-PROFILE.json --packages=\"https://anystorage\" --content=\"https://anystorage\" --timeout=01:00:00 --package-dir=\"/any/packages\"")]
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_1(string originalCommand, string expectedTargetCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] arguments = originalCommand.Split(' ');
                ParseResult result = CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .Build()
                    .Parse(arguments);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(result.Tokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
            }
        }

        [Test]
        [TestCase(
            "remote|--command=ipconfig|--packages=https://anystorage|--timeout=01:00:00|--target=user@10.1.2.3;pass",
            "--command=\"ipconfig\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote|--command=ipconfig /all|--packages=https://anystorage|--timeout=01:00:00|--target=user@10.1.2.3;pass",
            "--command=\"ipconfig /all\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py|--log-dir=any/log dir|--packages=https://anystorage|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py\" --log-dir=\"any/log dir\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'|--packages=https://anystorage|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'|--packages=https://anystorage|--timeout=01:00:00|--fail-fast|--clean|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'\" --packages=\"https://anystorage\" --timeout=01:00:00 --fail-fast --clean")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'|--packages=https://anystorage|--fail-fast|--timeout=01:00:00|--clean|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'\" --packages=\"https://anystorage\" --fail-fast --timeout=01:00:00 --clean")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'|--packages=https://anystorage|-fc|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'\" --packages=\"https://anystorage\" -f -c --timeout=01:00:00")]
        [TestCase(
            "remote|--fail-fast|--clean|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'|--packages=https://anystorage|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--fail-fast --clean --command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir='/any/log dir'\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_2(string originalCommand, string expectedTargetCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            // Note:
            // We are splitting on the pipe (|) character to avoid issues with arguments that
            // have spaces in them. The .NET command line parsing handles the spaces in arguments surrounded by
            // quotation marks. We are mimicking that behavior for correctness.
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] arguments = originalCommand.Split('|');
                ParseResult result = CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .Build()
                    .Parse(arguments);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(result.Tokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
            }
        }

        [Test]
        [TestCase(
            "remote|--command=ipconfig|--packages=anystore|--timeout=01:00:00|--target=user@10.1.2.3;pass",
            "--command=ipconfig --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir|--packages=anystore|--timeout=01:00:00|--target=user@10.1.2.3;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote|--command=./packages/custom_scripts.1.0.0/execute_workload.py|--log-dir=/any/log/dir|--packages=anystore|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py\" --log-dir=\"/any/log/dir\" --packages=anystore --timeout=01:00:00")]
        [TestCase(
            "remote|-C ./packages/custom_scripts.1.0.0/execute_workload.py|--log-dir=/any/log/dir|--packages=anystore|--timeout=01:00:00|--target=user@10.1.2.3;pass|--target=user@10.1.2.4;pass",
            "-C \"./packages/custom_scripts.1.0.0/execute_workload.py\" --log-dir=/any/log/dir\" --packages=anystore --timeout=01:00:00")]
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_3(string originalCommand, string expectedTargetCommand)
        {
            var command = new TestExecuteRemoteAgentCommand();
            string actualTargetCommand = command.GetTargetCommandArguments(originalCommand.Split('|'));

            Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
        }

        [Test]
        [TestCase(
           "remote|--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00|--target=any@1.2.3.4;pwd",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00")]
        [TestCase(
           "remote|--command=\"pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00\"|--target=any@1.2.3.4;pwd",
           "\"pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00\"")]
        [TestCase(
           "remote|--command=\"pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout 00:20:00\"|--target=any@1.2.3.4;pwd",
           "\"pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout 00:20:00\"")]
        public async Task SdkAgentRemoteCommandHandlesCommonPowerShellScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;

                string[] args = commandLine.Split('|');

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(preprocessedArgs, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestExecuteRemoteAgentCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
          "remote|--command=\"python -c /home/user/scripts/execute_script.py\"|--target=any@1.2.3.4;pwd",
          "\"python -c /home/user/scripts/execute_script.py\"")]
        [TestCase(
          "remote|--command=\"python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'\"|--target=any@1.2.3.4;pwd",
          "\"python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'\"")]
        [TestCase(
          "remote|--command=\"python -c '/home/user/scripts/execute_script.py --log-dir /home/user/log dir'\"|--target=any@1.2.3.4;pwd",
          "\"python -c '/home/user/scripts/execute_script.py --log-dir /home/user/log dir'\"")]
        public async Task SdkAgentRemoteCommandHandlesCommonPythonScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;

                string[] args = commandLine.Split('|');

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(preprocessedArgs, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestExecuteRemoteAgentCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
         "remote|--command=\"bash -c /home/user/scripts/execute_script.sh\"|--target=any@1.2.3.4;pwd",
         "\"bash -c /home/user/scripts/execute_script.sh\"")]
        [TestCase(
         "remote|--command=\"bash -c '/home/user/scripts/execute_script.sh /home/user/logs'\"|--target=any@1.2.3.4;pwd",
         "\"bash -c '/home/user/scripts/execute_script.sh /home/user/logs'\"")]
        [TestCase(
         "remote|--command=\"bash -c '/home/user/scripts/execute_script.sh --log-dir /home/user/log dir'\"|--target=any@1.2.3.4;pwd",
         "\"bash -c '/home/user/scripts/execute_script.sh --log-dir /home/user/log dir'\"")]
        public async Task SdkAgentRemoteCommandHandlesCommonBashScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;

                string[] args = commandLine.Split('|');

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(preprocessedArgs, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestExecuteRemoteAgentCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });
                
                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
         "remote|--command=\"cmd /C /home/user/scripts/execute_script.cmd\"|--target=any@1.2.3.4;pwd",
         "\"cmd /C /home/user/scripts/execute_script.cmd\"")]
        [TestCase(
         "remote|--command=\"cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'\"|--target=any@1.2.3.4;pwd",
         "\"cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'\"")]
        [TestCase(
         "remote|--command=\"cmd /C '/home/user/scripts/execute_script.cmd --log-dir /home/user/log dir'\"|--target=any@1.2.3.4;pwd",
         "\"cmd /C '/home/user/scripts/execute_script.cmd --log-dir /home/user/log dir'\"")]
        public async Task SdkAgentRemoteCommandHandlesCommonCmdScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;

                string[] args = commandLine.Split('|');

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(preprocessedArgs, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestExecuteRemoteAgentCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        private class TestExecuteRemoteAgentCommand : ExecuteRemoteAgentCommand
        {
            public new void Initialize(string[] args, PlatformSpecifics platformSpecifics)
            {
                base.Initialize(args, platformSpecifics);
            }

            public new string GetTargetCommandArguments(IEnumerable<Token> commandLineTokens)
            {
                return base.GetTargetCommandArguments(commandLineTokens);
            }

            public new string GetTargetCommandArguments(string[] commandArguments)
            {
                return base.GetTargetCommandArguments(commandArguments);
            }
        }
    }
}
