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

        [Test]
        [Platform("Win")]
        [TestCase(@"C:\Users\User\Profiles\ANY-PROFILE.json")]
        [TestCase(@".\Profiles\ANY-PROFILE.json")]
        [TestCase(@"..\Profiles\ANY-PROFILE.json")]
        public async Task VirtualClientSupportsLocalPathReferencesForProfiles_Windows_Style_Paths(string pathReference)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] args = new string[]
                {
                    $"--profile={pathReference}"
                };

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    string expectedFullPath = Path.GetFullPath(pathReference);

                    Assert.IsEmpty(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).IsFullPath);
                    Assert.AreEqual(expectedFullPath, cmd.Profiles.ElementAt(0).ProfileName);

                    return Task.FromResult(1);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();
            }
        }

        [Test]
        [Platform("Unix")]
        [TestCase(@"/home/user/profiles/ANY-PROFILE.json")]
        [TestCase(@"./profiles/ANY-PROFILE.json")]
        [TestCase(@"../profiles/ANY-PROFILE.json")]
        public async Task VirtualClientSupportsLocalPathReferencesForProfiles_Unix_Style_Paths(string pathReference)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] args = new string[]
                {
                    $"--profile={pathReference}"
                };

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    string expectedFullPath = Path.GetFullPath(pathReference);

                    Assert.IsEmpty(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).IsFullPath);
                    Assert.AreEqual(expectedFullPath, cmd.Profiles.ElementAt(0).ProfileName);

                    return Task.FromResult(1);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();
            }
        }

        [Test]
        [TestCase(@"http://anystorage/location/ANY-PROFILE.json")]
        [TestCase(@"https://anystorage/location/ANY-PROFILE.json")]
        public async Task VirtualClientSupportsUriPathReferencesForProfiles(string pathReference)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] args = new string[]
                {
                    $"--profile={pathReference}"
                };

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, tokenSource);
                commandBuilder.Command.Handler = CommandHandler.Create<TestExecuteCommand>(cmd =>
                {
                    Assert.IsEmpty(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.AreEqual("ANY-PROFILE.json", cmd.Profiles.ElementAt(0).ProfileName);
                    Assert.AreEqual(pathReference, cmd.Profiles.ElementAt(0).ProfileUri.ToString());

                    return Task.FromResult(1);
                });

                ParseResult parseResult = commandBuilder.Build().Parse(args);
                parseResult.ThrowOnUsageError();
                await parseResult.InvokeAsync();
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
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--c", "AgentID")]
        [TestCase("--port", "4501")]
        [TestCase("--api-port", "4501")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("--content-store", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--cs", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content-path-template", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--content-path", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--cp", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--dependencies", null)]
        [TestCase("--eventHubConnectionString", "Endpoint=ConnectionString")]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--fail-fast", null)]
        [TestCase("--ff", null)]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--wait", "00:10:00")]
        [TestCase("--i", "3")]
        [TestCase("--iterations", "3")]
        [TestCase("--kv", "https://anyvault.vault.windows.net")]
        [TestCase("--key-vault", "https://anyvault.vault.windows.net")]
        [TestCase("--layout-path", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layout", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--lp", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ldir", "C:\\any\\path\\to\\logs")]
        [TestCase("--ll", "3")]
        [TestCase("--log-level", "2")]
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
        [TestCase("--proxy", "https://proxy.azure.net?crti=issuerName&crts=subjectName")]
        [TestCase("--proxy", "https://proxy.azure.net")]
        [TestCase("--proxy", "https://192.168.1.10:8443")]
        [TestCase("--proxy", "https://192.168.1.10")]
        [TestCase("--proxy-api", "https://proxy.azure.net/?crti=issuerName&crts=subjectName")]
        [TestCase("--t", "1440")]
        [TestCase("--timeout", "01:00:00")]
        [TestCase("--timeout", "01:00:00,deterministic")]
        [TestCase("--timeout", "01:00:00,deterministic*")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("--tdir", "C:\\any\\path\\to\\temp")]
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
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(string.Join(" ", arguments));

                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, message: $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--c", "AgentID")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
        [TestCase("--eventHubConnectionString", "Endpoint=ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--mt", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--n", "anypackage")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
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
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--pdir", "C:\\any\\path\\to\\packages")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--s", "Azure")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("--tdir", "C:\\any\\path\\to\\temp")]
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
                    "--package-store", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b",
                    "--iterations", "1",
                    "--layout-path", "/home/user/any/layout.json"
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
        [TestCase("--logger", "file")]
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
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--pdir", "C:\\any\\path\\to\\packages")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("--tdir", "C:\\any\\path\\to\\temp")]
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
        [TestCase("--logger", "file")]
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
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--sdir", "C:\\any\\path\\to\\state")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("--tdir", "C:\\any\\path\\to\\temp")]
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

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--c", "AgentID")]
        [TestCase("--clean", null)]
        [TestCase("--clean", "logs")]
        [TestCase("--clean", "logs,packages,state,temp")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--parameters", "helloWorld=123,,,TenantId=789203498")]
        [TestCase("--pm", "testing")]
        [TestCase("--verbose", null)]
        public void VirtualClientGetTokenCommandSupportsOnlyExpectedOptions(string option, string value)
        {            
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "get-token",
                    "--kv", "https://anyvault.vault.azure.net/?cid=1...&tid=2"
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