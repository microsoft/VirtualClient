// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal partial class CommandLineOptionTests
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
        [TestCase(
           "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00")]
        [TestCase(
           "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout '00:20:00'",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout '00:20:00'")]
        [TestCase(
           "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log dir\" -Timeout \"00:20:00\"",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log dir\" -Timeout \"00:20:00\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonPowerShellScenarios(string commandLine, string expectedCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            // Note:
            // We are splitting on the pipe (|) character to avoid issues with arguments that
            // have spaces in them. The .NET command line parsing handles the spaces in arguments surrounded by
            // quotation marks. We are mimicking that behavior for correctness.

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                (commandBuilder.Command as Command).Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
           "remote|--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00|--target=any@1.2.3.4;pwd",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00")]
        [TestCase(
           "remote|--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout '00:20:00'|--target=any@1.2.3.4;pwd",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log dir' -Timeout '00:20:00'")]
        [TestCase(
           "remote|--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log dir\" -Timeout \"00:20:00\"|--target=any@1.2.3.4;pwd",
           "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log dir\" -Timeout \"00:20:00\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonPowerShellScenarios_Remote_Execution(string commandLine, string expectedCommand)
        {
            // Important:
            // See Notes at the top of the unit test class.

            // Note:
            // We are splitting on the pipe (|) character to avoid issues with arguments that
            // have spaces in them. The .NET command line parsing handles the spaces in arguments surrounded by
            // quotation marks. We are mimicking that behavior for correctness.

            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
         "--command=python -c /home/user/scripts/execute_script.py",
         "python -c /home/user/scripts/execute_script.py")]
        [TestCase(
         "--command=python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'",
         "python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'")]
        [TestCase(
         "--command=python -c \"/home/user/scripts/execute_script.py --log-dir '/home/user/log dir'\"",
         "python -c \"/home/user/scripts/execute_script.py --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonPythonScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
         "remote|--command=python -c /home/user/scripts/execute_script.py|--target=any@1.2.3.4;pwd",
         "python -c /home/user/scripts/execute_script.py")]
        [TestCase(
         "remote|--command=python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'|--target=any@1.2.3.4;pwd",
         "python -c '/home/user/scripts/execute_script.py --log-dir /home/user/logs'")]
        [TestCase(
         "remote|--command=python -c \"/home/user/scripts/execute_script.py --log-dir '/home/user/log dir'\"|--target=any@1.2.3.4;pwd",
         "python -c \"/home/user/scripts/execute_script.py --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonPythonScenarios_Remote_Execution(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
            "--command=bash -c /home/user/scripts/execute_script.sh",
            "bash -c /home/user/scripts/execute_script.sh")]
        [TestCase(
             "--command=bash -c '/home/user/scripts/execute_script.sh /home/user/logs'",
             "bash -c '/home/user/scripts/execute_script.sh /home/user/logs'")]
        [TestCase(
             "--command=bash -c \"/home/user/scripts/execute_script.sh --log-dir '/home/user/log dir'\"",
             "bash -c \"/home/user/scripts/execute_script.sh --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonBashScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
         "remote|--command=bash -c /home/user/scripts/execute_script.sh|--target=any@1.2.3.4;pwd",
         "bash -c /home/user/scripts/execute_script.sh")]
        [TestCase(
         "remote|--command=bash -c '/home/user/scripts/execute_script.sh /home/user/logs'|--target=any@1.2.3.4;pwd",
         "bash -c '/home/user/scripts/execute_script.sh /home/user/logs'")]
        [TestCase(
         "remote|--command=bash -c \"/home/user/scripts/execute_script.sh --log-dir '/home/user/log dir'\"|--target=any@1.2.3.4;pwd",
         "bash -c \"/home/user/scripts/execute_script.sh --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonBashScenarios_Remote_Execution(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
            "--command=cmd /C /home/user/scripts/execute_script.cmd",
            "cmd /C /home/user/scripts/execute_script.cmd")]
        [TestCase(
            "--command=cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'",
            "cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'")]
        [TestCase(
            "--command=cmd /C \"/home/user/scripts/execute_script.cmd --log-dir '/home/user/log dir'\"",
            "cmd /C \"/home/user/scripts/execute_script.cmd --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonBatchScriptScenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
            "remote|--command=cmd /C /home/user/scripts/execute_script.cmd|--target=any@1.2.3.4;pwd",
            "cmd /C /home/user/scripts/execute_script.cmd")]
        [TestCase(
            "remote|--command=cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'|--target=any@1.2.3.4;pwd",
            "cmd /C '/home/user/scripts/execute_script.cmd /home/user/logs'")]
        [TestCase(
            "remote|--command=cmd /C \"/home/user/scripts/execute_script.cmd --log-dir '/home/user/log dir'\"|--target=any@1.2.3.4;pwd",
            "cmd /C \"/home/user/scripts/execute_script.cmd --log-dir '/home/user/log dir'\"")]
        public async Task SdkAgentCommandLineParsingHandlesCommonBatchScriptScenarios_Remote_Execution(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                bool confirmed = false;
                string[] args = commandLine.Split('|');

                CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, tokenSource);
                (commandBuilder.Command.Children.FirstOrDefault(c => c.Name == "remote") as Command).Handler = CommandHandler.Create<TestCommand>(cmd =>
                {
                    confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return Task.FromResult(0);
                });

                ParseResult parseResult = commandBuilder.ParseArguments(args, out IEnumerable<Token> commandLineTokens);
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        private class TestCommand : CommandBase
        {
            public string Command { get; set; }

            public Action OnExecute { get; set; }

            protected override Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
            {
                this.OnExecute?.Invoke();
                return Task.FromResult(0);
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
