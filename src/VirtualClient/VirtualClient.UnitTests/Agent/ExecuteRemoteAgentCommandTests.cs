// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Agent
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Parsing;
    using System.Runtime.InteropServices;
    using System.Threading;
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
                CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .ParseArguments(arguments, out IEnumerable<Token> commandLineTokens);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(commandLineTokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
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
        [TestCase(
            "remote --command=ipconfig --packages=https://anystorage --timeout=01:00:00 --target=user@10.1.2.3;pass",
            "--command=\"ipconfig\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote --command=ipconfig --packages=https://anystorage --timeout=01:00:00 --target=user@10.1.2.3;pass --fail-fast --clean",
            "--command=\"ipconfig\" --packages=\"https://anystorage\" --timeout=01:00:00 --fail-fast --clean")]
        [TestCase(
            "remote --command=ipconfig --packages=https://anystorage --timeout=01:00:00 --target=user@10.1.2.3;pass -fc",
            "--command=\"ipconfig\" --packages=\"https://anystorage\" --timeout=01:00:00 -f -c")]
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_2(string originalCommand, string expectedTargetCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] arguments = originalCommand.Split(' ');
                CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .ParseArguments(arguments, out IEnumerable<Token> commandLineTokens);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(commandLineTokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
            }
        }

        [Test]
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
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_3(string originalCommand, string expectedTargetCommand)
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
                CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .ParseArguments(arguments, out IEnumerable<Token> commandLineTokens);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(commandLineTokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
            }
        }

        [TestCase(
            "remote -C ./packages/custom_scripts.1.0.0/execute_workload.py --log-dir any/log/dir --packages https://anystorage --timeout 01:00:00 --target user@10.1.2.3;pass --target user@10.1.2.4;pass",
            "-C=\"./packages/custom_scripts.1.0.0/execute_workload.py\" --log-dir=\"any/log/dir\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        [TestCase(
            "remote --command ./packages/custom_scripts.1.0.0/execute_workload.py --log-dir any/log/dir --packages https://anystorage --timeout 01:00:00 --target user@10.1.2.3;pass --target user@10.1.2.4;pass",
            "--command=\"./packages/custom_scripts.1.0.0/execute_workload.py\" --log-dir=\"any/log/dir\" --packages=\"https://anystorage\" --timeout=01:00:00")]
        public void SdkAgentRemoteCommandCreatesTheExpectedCommandLineToExecuteOnTargetAgents_4(string originalCommand, string expectedTargetCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] arguments = originalCommand.Split(' ');
                CommandFactory.SdkAgent.CreateCommandBuilder(arguments, tokenSource)
                    .ParseArguments(arguments, out IEnumerable<Token> commandLineTokens);

                var command = new TestExecuteRemoteAgentCommand();
                string actualTargetCommand = command.GetTargetCommandArguments(commandLineTokens);

                Assert.AreEqual(expectedTargetCommand, actualTargetCommand);
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
        }
    }
}
