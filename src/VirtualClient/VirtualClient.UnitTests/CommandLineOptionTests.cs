// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CommandLineOptionTests
    {
        private static readonly string ResourcesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(CommandLineOptionTests)).Location),
            "Resources");

        [Test]
        [TestCase(
            "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00",
            "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs -Timeout 00:20:00")]
        [TestCase(
            "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\"",
            "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\"")]
        [TestCase(
            "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\" -Timeout 00:20:00",
            "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\" -Timeout 00:20:00")]
        [TestCase(
            "--command=pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\" -Timeout 00:20:00 -OtherDirectory \"/home/user/other dir\"",
            "pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\" -Timeout 00:20:00 -OtherDirectory \"/home/user/other dir\"")]
        [TestCase(
            "--command=pwsh -NonInteractive -C \"/home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log files' -Timeout 00:20:00 -OtherDirectory '/home/user/other dir'\"",
            "pwsh -NonInteractive -C \"/home/user/scripts/Invoke-Script.ps1 -LogDirectory '/home/user/log files' -Timeout 00:20:00 -OtherDirectory '/home/user/other dir'\"")]
        public async Task VirtualClientHandlesCommandExecutionScenariosAsExpected_Linux_PowerShell_Scenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool confirmed = false;

                string[] args = new string[] { commandLine };

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(preprocessedArgs, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
            "--command=pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory C:\\Users\\User\\logs -Timeout 00:20:00",
            "pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory C:\\Users\\User\\logs -Timeout 00:20:00")]
        [TestCase(
            "--command=pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\"",
            "pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\"")]
        [TestCase(
            "--command=pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\" -Timeout 00:20:00",
            "pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\" -Timeout 00:20:00")]
        [TestCase(
            "--command=pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\" -Timeout 00:20:00 -OtherDirectory \"C:\\Users\\User\\other dir\"",
            "pwsh -NonInteractive C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory \"C:\\Users\\User\\log files\" -Timeout 00:20:00 -OtherDirectory \"C:\\Users\\User\\other dir\"")]
        [TestCase(
            "--command=pwsh -NonInteractive -C \"C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory 'C:\\Users\\User\\log files' -Timeout 00:20:00 -OtherDirectory 'C:\\Users\\User\\other dir'\"",
            "pwsh -NonInteractive -C \"C:\\Users\\User\\scripts\\Invoke-Script.ps1 -LogDirectory 'C:\\Users\\User\\log files' -Timeout 00:20:00 -OtherDirectory 'C:\\Users\\User\\other dir'\"")]
        public async Task VirtualClientHandlesCommandExecutionScenariosAsExpected_Windows_PowerShell_Scenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool confirmed = false;

                string[] args = new string[] { commandLine };

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(preprocessedArgs, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
           "--command=python /home/user/scripts/execute_script.py /home/user/logs",
           "python /home/user/scripts/execute_script.py /home/user/logs")]
        [TestCase(
           "--command=python /home/user/scripts/execute_script.py \"/home/user/log dir\"",
           "python /home/user/scripts/execute_script.py \"/home/user/log dir\"")]
        [TestCase(
           "--command=python /home/user/scripts/execute_script.py \"/home/user/log dir\" 00:20:00",
           "python /home/user/scripts/execute_script.py \"/home/user/log dir\" 00:20:00")]
        [TestCase(
           "--command=python -c \"/home/user/scripts/execute_script.py '/home/user/log dir' 00:20:00\"",
           "python -c \"/home/user/scripts/execute_script.py '/home/user/log dir' 00:20:00\"")]
        [TestCase(
           "--command=python -c \"/home/user/scripts/execute_script.py --log_dir '/home/user/log dir' --timeout 00:20:00\"",
           "python -c \"/home/user/scripts/execute_script.py --log_dir '/home/user/log dir' --timeout 00:20:00\"")]
        [TestCase(
           "--command=python -c \"/home/user/scripts/execute_script.py --log_dir='/home/user/log dir' --timeout=00:20:00\"",
           "python -c \"/home/user/scripts/execute_script.py --log_dir='/home/user/log dir' --timeout=00:20:00\"")]
        public async Task VirtualClientHandlesCommandExecutionScenariosAsExpected_Linux_Python_Scenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool confirmed = false;

                string[] args = new string[] { commandLine };

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(preprocessedArgs, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase(
           "--command=bash -c \"/home/user/scripts/execute_script.sh\"",
           "bash -c \"/home/user/scripts/execute_script.sh\"")]
        [TestCase(
           "--command=bash -c \"/home/user/scripts/execute_script.sh --log_dir /home/user/logs\"",
           "bash -c \"/home/user/scripts/execute_script.sh --log_dir /home/user/logs\"")]
        [TestCase(
           "--command=bash -c \"/home/user/scripts/execute_script.sh --log_dir '/home/user/log dir'\"",
           "bash -c \"/home/user/scripts/execute_script.sh --log_dir '/home/user/log dir'\"")]
        public async Task VirtualClientHandlesCommandExecutionScenariosAsExpected_Linux_Bash_Scenarios(string commandLine, string expectedCommand)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool confirmed = false;

                string[] args = new string[] { commandLine };

                // System.CommandLine Quirk:
                // The library parsing logic will strip the \" from the end of the command line
                // vs. treating it as an explicit quotation mark to leave in place. There are no
                // hooks in the library implementation to override this behavior.
                //
                // To workaround this we replace the quotes with the HTML encoding. Each option can
                // then handle the HTML decoding as required. This happens in the preprocessing logic.
                string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(preprocessedArgs, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task VirtualClientHandlesCommandExecutionScenariosWithProfilesAdditionallyReferencedAsExpected()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                string expectedCommand = "pwsh /home/user/scripts/Invoke-Script.ps1 -Name AnyScript -LogDirectory /home/user/logs";
                bool confirmed = false;

                string[] args = new string[]
                {
                    $"--command={expectedCommand}",
                    $"--profile=MONITORS-DEFAULT.json"
                };

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.First().ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task VirtualClientHandlesProfileExecutionScenariosAsExpected()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool confirmed = false;

                string[] args = new string[]
                {
                    "--profile=ANY-PROFILE.json",
                    "--profile=MONITORS-DEFAULT.json"
                };

                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.IsNull(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).ProfileName == "ANY-PROFILE.json");
                    Assert.IsTrue(cmd.Profiles.ElementAt(1).ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.True(confirmed);
            }
        }

        [TestCase("--not-a-valid-option", "Option is not supported")]
        [TestCase("--packag", "Option is simply misspelled")]
        public void VirtualClientThrowsWhenAnUnrecognizedOptionIsSuppliedOnTheCommandLine(string option, string value)
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

                ArgumentException error = Assert.Throws<ArgumentException>(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource)
                        .Build()
                        .Parse(arguments);

                    result.ThrowOnUsageError();
                });

                Assert.IsTrue(error.Message.StartsWith(
                    $"Invalid Usage. The following command line options are not supported: {option}",
                    StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--api-port", "4501")]
        [TestCase("-c", null)]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("--content-store", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content-path-template", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--content-path", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("-d", null)]
        [TestCase("--dependencies", null)]
        [TestCase("--eventHubConnectionString", "Endpoint=ConnectionString")]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
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
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("-l", null)]
        [TestCase("--log-to-file", null)]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packages", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--seed", "1234")]
        [TestCase("--scenarios", "Scenario1")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--proxy", "https://proxy.azure.net?crti=issuerName&crts=subjectName")]
        [TestCase("--proxy", "https://proxy.azure.net")]
        [TestCase("--proxy", "https://192.168.1.10:8443")]
        [TestCase("--proxy", "https://192.168.1.10")]
        [TestCase("--proxy-api", "https://proxy.azure.net/?crti=issuerName&crts=subjectName")]
        [TestCase("--timeout", "01:00:00")]
        [TestCase("--timeout", "01:00:00,deterministic")]
        [TestCase("--timeout", "01:00:00,deterministic*")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientDefaultCommandSupportsAllExpectedOptions(string option, string value)
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
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource)
                        .Build()
                        .Parse(arguments);

                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, message: $"Option '{option}' is not supported.");
            }
        }


        [Test]
        [TestCase("--profile=PERF-ANY-PROFILE.json -n")]
        [TestCase("-n --profile=PERF-ANY-PROFILE.json")]
        [TestCase("--profile=PERF-ANY-PROFILE.json --n=not_a_valid_option")]
        [TestCase("--profile=PERF-ANY-PROFILE.json --notvalid=not_a_valid_option")]
        [TestCase("--profile=PERF-ANY-PROFILE.json --not-valid=not_a_valid_option")]
        [TestCase("--profile=PERF-ANY-PROFILE.json --not_valid=not_a_valid_option")]
        [TestCase("--profile=PERF-ANY-PROFILE.json --not-valid=not_a_valid_option --timeout=15")]
        [TestCase("--not-valid=not_a_valid_option --profile=PERF-ANY-PROFILE.json --timeout=15")]
        public void VirtualClientDefaultCommandHandlesUnrecognizedParameters_1(string commandLine)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                string[] args = commandLine.Split(' ');

                ArgumentException error = Assert.Throws<ArgumentException>(() => CommandFactory.CreateCommandBuilder(args, cancellationSource)
                    .Build()
                    .Parse(args));

                Assert.IsTrue(error.Message.StartsWith($"Invalid Usage. The following command line options are not supported:"));
            }
        }

        [Test]
        [TestCase("--profile PERF-ANY-PROFILE.json -n")]
        [TestCase("-n --profile PERF-ANY-PROFILE.json")]
        [TestCase("--profile PERF-ANY-PROFILE.json --n not_a_valid_option")]
        [TestCase("--profile PERF-ANY-PROFILE.json --notvalid not_a_valid_option")]
        [TestCase("--profile PERF-ANY-PROFILE.json --not-valid not_a_valid_option")]
        [TestCase("--profile PERF-ANY-PROFILE.json --not_valid not_a_valid_option")]
        [TestCase("--profile PERF-ANY-PROFILE.json --not-valid not_a_valid_option --timeout 15")]
        [TestCase("--not-valid not_a_valid_option --profile PERF-ANY-PROFILE.json --timeout 15")]
        public void VirtualClientDefaultCommandHandlesUnrecognizedParameters_2(string commandLine)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                string[] args = commandLine.Split(' ');

                ArgumentException error = Assert.Throws<ArgumentException>(() => CommandFactory.CreateCommandBuilder(args, cancellationSource)
                    .Build()
                    .Parse(args));

                Assert.IsTrue(error.Message.StartsWith($"Invalid Usage. The following command line options are not supported:"));
            }
        }

        [Test]
        [TestCase("--version")]
        [TestCase("--help")]
        [TestCase("--profile=PERF-ANY-PROFILE.json|--timeout=15|--scenarios=-ExcludeThisScenario")]
        [TestCase("--profile PERF-ANY-PROFILE.json|--timeout 15|--scenarios \"-ExcludeThisScenario\"")]
        [TestCase("--command=\"/any/script.py --log-path\"|--timeout=15|--scenarios \"-ExcludeThisScenario\"")]
        [TestCase("-C \"/any/script.py --log-path\"|--timeout=15|--scenarios \"-ExcludeThisScenario\"")]
        public void VirtualClientDefaultCommandHandlesTrickyParameterValues(string commandLine)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                // Note:
                // The pipe delimiting is used to simplify the parsing here and to mimic the way
                // the .NET command line parsing works for args passed into the program Main() function.

                string[] args = commandLine.Split('|');

                Assert.DoesNotThrow(() => CommandFactory.CreateCommandBuilder(args, cancellationSource)
                    .Build()
                    .Parse(args));
            }
        }

        [Test]
        public void VirtualClientDefaultCommandSupportsOptionRuns()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "--profile", "PERF-ANY-PROFILE.json",
                    
                    // -c, --clean
                    // -d, --dependencies
                    // -f, --fail-fast
                    // -l, --log-to-file
                    // -v, --verbose
                    "-cdflv"
                };

                bool cleanConfirmed = false;
                bool dependenciesConfirmed = false;
                bool failFastConfirmed = false;
                bool logToFileConfirmed = false;
                bool verboseConfirmed = false;
                string[] args = arguments.ToArray();
                CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(args, cancellationSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () =>
                    {
                        cleanConfirmed = cmd.CleanTargets != null;
                        dependenciesConfirmed = cmd.InstallDependencies;
                        failFastConfirmed = cmd.FailFast == true;
                        logToFileConfirmed = cmd.LogToFile == true;
                        verboseConfirmed = cmd.Verbose;
                    };

                    return cmd.ExecuteAsync(args, cancellationSource);
                });

                Assert.DoesNotThrowAsync(async () =>
                {
                    ParseResult result = commandBuilder
                        .Build()
                        .Parse(args);

                    await result.InvokeAsync();
                });

                Assert.IsTrue(cleanConfirmed);
                Assert.IsTrue(dependenciesConfirmed);
                Assert.IsTrue(failFastConfirmed);
                Assert.IsTrue(logToFileConfirmed);
                Assert.IsTrue(verboseConfirmed);
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("-c", null)]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--package", "anypackage.1.0.0.zip",
                    "--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        public void VirtualClientBootstrapCommandHandlesNoOpArguments()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--package", "anypackage.1.0.0.zip",
                    "--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b",
                    "--iterations", "1",
                    "--layout-path", "/home/user/any/layout.json"
                };

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                });
            }
        }


        [Test]
        [TestCase("-c", null)]
        [TestCase("--clean", null)]
        [TestCase("--clean", "all")]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "packages")]
        [TestCase("--clean", "state")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientCleanCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "clean"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("convert --profile=ANY-PROFILE.json --output-path=C:\\Any\\Path")]
        public void VirtualClientConvertCommandSupportsAllExpectedOptions(string commandLine)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                string[] arguments = commandLine.Split(" ");

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments, cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                });
            }
        }

        [Test]
        [TestCase("--api-port", "4501")]
        [TestCase("-m", null)]
        [TestCase("--monitor", null)]
        [TestCase("--ip-address", "10.0.0.128")]
        [TestCase("-c", null)]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientRunApiCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "api"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        public void VirtualClientCommandLineSupportsResponseFiles()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    $"@{Path.Combine(CommandLineOptionTests.ResourcesDirectory, "TestOptions.rsp")}"
                };

                ParseResult result = CommandFactory.CreateCommandBuilder(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                Assert.IsFalse(result.Errors.Any());
                Assert.DoesNotThrow(() => result.ThrowOnUsageError());

                // Based on the options found in the Resources/TestOptions.rsp file.
                Assert.IsTrue(result.Tokens.Count == 19);

                // All expected options and arguments were parsed.
                CollectionAssert.AreEqual(
                    new string[]
                    {
                        "--profile", "PERF-CPU-OPENSSL.json",
                        "--timeout", "01.00:00:00",
                        "--system", "Azure",
                        "--metadata", "Prop1=Value1,,,Prop2=Value2",
                        "--parameters", "Param1=Value1,,,Param2=Value2",
                        "--content-store", "BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123",
                        "--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b",
                        "--client-id", "007",
                        "--experiment-id", "123456",
                        "--verbose"
                    },
                    result.Tokens.Select(t => t.Value));

            }
        }

        private class TestExecuteProfileCommand : ExecuteProfileCommand
        {
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