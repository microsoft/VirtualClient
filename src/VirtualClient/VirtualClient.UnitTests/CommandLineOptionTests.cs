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
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class CommandLineOptionTests
    {
        private static readonly string ResourcesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(CommandLineOptionTests)).Location),
            "Resources");

        [Test]
        [TestCase("pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory /home/user/logs")]
        [TestCase("python /home/user/scripts/execute_script.py /home/user/logs")]
        [TestCase("pwsh -NonInteractive /home/user/scripts/Invoke-Script.ps1 -LogDirectory \"/home/user/log files\"")]
        [TestCase("python /home/user/scripts/execute_script.py \"/home/user/logs\"")]
        public async Task VirtualClientHandlesCommandExecutionScenariosAsExpected(string commandLine)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;

                string[] args = new string[] { commandLine };
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    cmd.OnExecuteCommand = () => commandFlowExecuted = true;
                    cmd.OnExecuteProfiles = () => profileFlowExecuted = true;

                    Assert.AreEqual(commandLine, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(commandFlowExecuted);
                Assert.IsFalse(profileFlowExecuted);
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
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;

                string[] args = new string[]
                {
                    expectedCommand,
                    "--profile=MONITORS-DEFAULT.json"
                };

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    cmd.OnExecuteCommand = () => commandFlowExecuted = true;
                    cmd.OnExecuteProfiles = () => profileFlowExecuted = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.First().ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(commandFlowExecuted);
                Assert.IsFalse(profileFlowExecuted);
            }
        }

        [Test]
        public async Task VirtualClientHandlesProfileExecutionScenariosAsExpected()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                // Expected flow is the command/command line execution flow. The profile
                // execution flow should not be executed.
                bool commandFlowExecuted = false;
                bool profileFlowExecuted = false;

                string[] args = new string[]
                {
                    "--profile=ANY-PROFILE.json",
                    "--profile=MONITORS-DEFAULT.json"
                };

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    cmd.OnExecuteCommand = () => commandFlowExecuted = true;
                    cmd.OnExecuteProfiles = () => profileFlowExecuted = true;

                    Assert.IsEmpty(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).ProfileName == "ANY-PROFILE.json");
                    Assert.IsTrue(cmd.Profiles.ElementAt(1).ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();

                Assert.IsTrue(profileFlowExecuted);
                Assert.IsFalse(commandFlowExecuted);
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
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(string.Join(" ", arguments));
                    result.ThrowOnUsageError();
                });

                Assert.IsTrue(error.Message.StartsWith(
                    $"Invalid Usage. The following command line options are not supported: {option}", 
                    StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        [TestCase("--agent-id", "AgentID")]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--agentid", "AgentID")]
        [TestCase("--clientId", "AgentID")]
        [TestCase("--clientid", "AgentID")]
        [TestCase("--client", "AgentID")]
        [TestCase("--c", "AgentID")]
        [TestCase("--port", "4501")]
        [TestCase("--api-port", "4501")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--content-store", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--contentStore", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--contentstore", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--cs", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content-path-template", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentPathTemplate", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentpathtemplate", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentPath", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentpath", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--content-path", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--cp", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--debug", null)]
        [TestCase("--verbose", null)]
        [TestCase("--dependencies", null)]
        [TestCase("--eventHubConnectionString", "Endpoint=ConnectionString")]
        [TestCase("--eventhubconnectionstring", "Endpoint=ConnectionString")]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
        [TestCase("--eventHub", "Endpoint=ConnectionString")]
        [TestCase("--eventhub", "Endpoint=ConnectionString")]
        [TestCase("--eh", "Endpoint=ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experimentid", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--fail-fast", null)]
        [TestCase("--ff", null)]
        [TestCase("--flush-wait", "00:10:00")]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--wait", "00:10:00")]
        [TestCase("--i", "3")]
        [TestCase("--iterations", "3")]
        [TestCase("--layout-path", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layoutPath", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layoutpath", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layout", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--lp", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ldir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "2")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "Information")]
        [TestCase("--ll", "Error")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--lr", "14400")]
        [TestCase("--lr", "10.00:00:00")]
        [TestCase("--log-to-file", null)]
        [TestCase("--ltf", null)]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--mt", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--pdir", "C:\\any\\path\\to\\packages")]
        [TestCase("--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packageStore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packagestore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packages", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--ps", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--pm", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--seed", "1234")]
        [TestCase("--sd", "1234")]
        [TestCase("--scenarios", "Scenario1")]
        [TestCase("--sc", "Scenario1")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--s", "Azure")]
        [TestCase("--t", "1440")]
        [TestCase("--timeout", "01:00:00")]
        [TestCase("--timeout", "01:00:00,deterministic")]
        [TestCase("--timeout", "01:00:00,deterministic*")]
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
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(string.Join(" ", arguments));

                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, message: $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--agentid", "AgentID")]
        [TestCase("--agent-id", "AgentID")]
        [TestCase("--clientId", "AgentID")]
        [TestCase("--clientid", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--client", "AgentID")]
        [TestCase("--c", "AgentID")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--debug", null)]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
        [TestCase("--eventHub", "Endpoint=ConnectionString")]
        [TestCase("--eventhub", "Endpoint=ConnectionString")]
        [TestCase("--eh", "Endpoint=ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experimentid", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--mt", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ldir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "Information")]
        [TestCase("--ll", "Error")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--lr", "14400")]
        [TestCase("--lr", "10.00:00:00")]
        [TestCase("--log-to-file", null)]
        [TestCase("--ltf", null)]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--pdir", "C:\\any\\path\\to\\packages")]
        [TestCase("--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packageStore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packagestore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packages", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--ps", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--s", "Azure")]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--package", "anypackage.1.0.0.zip",
                    "--name", "anypackage"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
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
                    "--iterations", "1",
                    "--layoutPath", "/home/user/any/layout.json"
                };

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                });
            }
        }


        [Test]
        [TestCase("--clean", null)]
        [TestCase("--clean", "all")]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "packages")]
        [TestCase("--clean", "state")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ldir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "Information")]
        [TestCase("--ll", "Error")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--lr", "14400")]
        [TestCase("--lr", "10.00:00:00")]
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
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("convert --p=ANY-PROFILE.json --path=C:\\Any\\Path")]
        [TestCase("convert --profile=ANY-PROFILE.json --output=C:\\Any\\Path")]
        [TestCase("convert --profile=ANY-PROFILE.json --output-path=C:\\Any\\Path")]
        public void VirtualClientConvertCommandSupportsAllExpectedOptions(string commandLine)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                string[] arguments = commandLine.Split(" ");

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments, cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                });
            }
        }

        [Test]
        [TestCase("--port", "4501")]
        [TestCase("--api-port", "4501")]
        [TestCase("--monitor", null)]
        [TestCase("--mon", null)]
        [TestCase("--ip-address", "10.0.0.128")]
        [TestCase("--ip", "10.0.0.128")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state")]
        [TestCase("--debug", null)]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ldir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "Information")]
        [TestCase("--ll", "Error")]
        [TestCase("--log-retention", "14400")]
        [TestCase("--log-retention", "10.00:00:00")]
        [TestCase("--lr", "14400")]
        [TestCase("--lr", "10.00:00:00")]
        [TestCase("--log-to-file", null)]
        [TestCase("--ltf", null)]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        public void VirtualClientRunApiCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "runapi"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
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

                ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
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
                        "--debug"
                    },
                    result.Tokens.Select(t => t.Value));

            }
        }

        private class TestExecuteCommand : ExecuteCommand
        {
            public Action OnExecuteCommand { get; set; }

            public Action OnExecuteProfiles { get; set; }

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