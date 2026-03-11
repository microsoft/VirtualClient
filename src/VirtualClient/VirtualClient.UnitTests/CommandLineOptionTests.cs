// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNull(cmd.Profiles);

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
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
                    $"-C {expectedCommand}",
                    "--profile=MONITORS-DEFAULT.json"
                };

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.AreEqual(expectedCommand, cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.First().ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    cmd.OnExecute = () => confirmed = true;

                    Assert.IsNull(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).ProfileName == "ANY-PROFILE.json");
                    Assert.IsTrue(cmd.Profiles.ElementAt(1).ProfileName == "MONITORS-DEFAULT.json");

                    return cmd.ExecuteAsync(args, tokenSource);
                });

                ParseResult parseResult = parser.Parse();
                await parseResult.InvokeAsync();

                Assert.IsTrue(confirmed);
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    string expectedFullPath = Path.GetFullPath(pathReference);

                    Assert.IsNull(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).IsFullPath);
                    Assert.AreEqual(expectedFullPath, cmd.Profiles.ElementAt(0).ProfileName);

                    return Task.FromResult(1);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    string expectedFullPath = Path.GetFullPath(pathReference);

                    Assert.IsNull(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.IsTrue(cmd.Profiles.ElementAt(0).IsFullPath);
                    Assert.AreEqual(expectedFullPath, cmd.Profiles.ElementAt(0).ProfileName);

                    return Task.FromResult(1);
                });

                ParseResult parseResult = parser.Parse();
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

                CommandLineParser parser = CommandLineParser.Create(args, tokenSource);
                parser.Builder.Command.Handler = CommandHandler.Create<TestExecuteProfileCommand>(cmd =>
                {
                    Assert.IsNull(cmd.Command);
                    Assert.IsNotNull(cmd.Profiles);
                    Assert.IsNotEmpty(cmd.Profiles);
                    Assert.AreEqual("ANY-PROFILE.json", cmd.Profiles.ElementAt(0).ProfileName);
                    Assert.AreEqual(pathReference, cmd.Profiles.ElementAt(0).ProfileUri.ToString());

                    return Task.FromResult(1);
                });

                ParseResult parseResult = parser.Parse();
                await parseResult.InvokeAsync();
            }
        }

        [TestCase("--not-a-valid-option", "Option is not supported")]
        [TestCase("--packag", "Option is simply misspelled")]
        public void VirtualClientThrowsWhenAnUnrecognizedOptionIsSuppliedOnTheCommandLine(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
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
                    CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                });

                Assert.IsTrue(error.Message.StartsWith(
                    $"Invalid Usage. The following command line options are not supported: {option}",
                    StringComparison.OrdinalIgnoreCase));
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--api-port", "4501")]
        [TestCase("-a", null)]
        [TestCase("--archive-logs", null)]
        [TestCase("--archive-logs", "C:\\Users\\AnyUser\\archive\\logs")]
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
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
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
        [TestCase("--scenarios", "Scenario1")]
        [TestCase("--state-dir", "C:\\any\\path\\to\\state")]
        [TestCase("--system", "Azure")]
        [TestCase("--proxy", "https://proxy.azure.net?crti=issuerName&crts=subjectName")]
        [TestCase("--proxy", "https://proxy.azure.net")]
        [TestCase("--proxy", "https://192.168.1.10:8443")]
        [TestCase("--proxy", "https://192.168.1.10")]
        [TestCase("--proxy-api", "https://proxy.azure.net/?crti=issuerName&crts=subjectName")]
        [TestCase("--target", "any@10.1.2.3;pass")]
        [TestCase("--timeout", "01:00:00")]
        [TestCase("--timeout", "01:00:00,deterministic")]
        [TestCase("--timeout", "01:00:00,deterministic*")]
        [TestCase("--temp-dir", "C:\\any\\path\\to\\temp")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientDefaultCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
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
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, message: $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--api-port", "4501")]
        [TestCase("--event-hub", "sb://any.servicebus")]
        [TestCase("-m", null)]
        [TestCase("--monitor", null)]
        [TestCase("--ip-address", "10.0.0.128")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientApiCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
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
                    ParseResult result = CommandLineParser.Create(arguments, tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--event-hub", "sb://any.servicebus")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--system", "Azure")]
        [TestCase("--verbose", null)]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptionsForPackageDownloads(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
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
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--event-hub", "sb://any.servicebus")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--system", "Azure")]
        [TestCase("--verbose", null)]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptionsForCertificateDownloads_1(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--cert-name", "any-cert",
                    "--key-vault", "https://any.vault.azure.net",
                    "--tenant-id", "123456"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--event-hub", "sb://any.servicebus")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--system", "Azure")]
        [TestCase("--verbose", null)]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptionsForCertificateDownloads_Token_Scenario(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--cert-name", "any-cert",
                    "--key-vault", "https://any.vault.azure.net",
                    "--token", "123456ABCDEFG"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--event-hub", "Endpoint=ConnectionString")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--name", "anypackage")]
        [TestCase("--logger", "file")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--package-dir", "C:\\any\\path\\to\\packages")]
        [TestCase("--system", "Azure")]
        [TestCase("--verbose", null)]
        public void VirtualClientBootstrapCommandSupportsAllExpectedOptionsForCertificateDownloads_Token_File_Scenario(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "bootstrap",
                    "--cert-name", "any-cert",
                    "--key-vault", "https://any.vault.azure.net",
                    "--token-file", "C:\\Users\\Any\\access_token.txt"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        public void VirtualClientBootstrapCommandHandlesNoOpArguments()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
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
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                });
            }
        }

        [Test]
        public void VirtualClientBootstrapCommandRequiresATargetResourceToBeSpecified()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                ArgumentException error = Assert.Throws<ArgumentException>(() =>
                {
                    string[] args = new[]
                    {
                        "bootstrap"
                    };

                    CommandLineParser.Create(args, tokenSource).Parse();
                });

                Assert.AreEqual(
                    "Invalid usage. At least one type of target resource must be specified for the bootstrap command." +
                    "Use --package to install a package or --cert-name to install a certificate.", 
                    error.Message);
            }
        }

        [Test]
        public void VirtualClientBootstrapCommandValidatesParametersRequiredForCertificateInstallation()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CommandLineParser parser = CommandLineParser.Create(Array.Empty<string>(), tokenSource);

                ArgumentException error = Assert.Throws<ArgumentException>(() =>
                {
                    string[] args = new[]
                    {
                        "bootstrap",
                        "--cert-name=any-cert"
                    };

                    CommandLineParser.Create(args, tokenSource).Parse();
                });

                Assert.AreEqual("The Key Vault URI must be provided (--key-vault) when installing a certificate.", error.Message);

                error = Assert.Throws<ArgumentException>(() =>
                {
                    string[] args = new[]
                    {
                        "bootstrap",
                        "--cert-name=any-cert",
                        "--key-vault=https://any.vault"
                    };

                    CommandLineParser.Create(args, tokenSource).Parse();
                });

                Assert.AreEqual("The Azure tenant ID must be provided (--tenant-id) when installing a certificate.", error.Message);
            }
        }

        [Test]
        [TestCase("convert --profile=ANY-PROFILE.json --output-dir=C:\\Any\\Path")]
        public void VirtualClientConvertCommandSupportsAllExpectedOptions(string commandLine)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string[] arguments = commandLine.Split(" ");

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments, tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                });
            }
        }

        [Test]
        [TestCase("--output-file", "C:\\Users\\Any\\access_token.txt")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientGetTokenCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "get-token",
                    "--key-vault=https://any.vault.azure.net",
                    "--tenant-id=123456"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--content-path", "{experimentId}/any/{toolname}")]
        [TestCase("--content-path-template", "{experimentId}/any/{toolname}")]
        [TestCase("--event-hub", "sb://any.servicebus")]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--logger", "csv")]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("--system", "Azure")]
        [TestCase("--verbose", null)]
        public void VirtualClientUploadFilesCommandSupportsAllExpectedOptionsForCertificateDownloads(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "upload-files",
                    "--content-store", "https://anystorage",
                    "--directory", "C:\\Any\\User\\logs"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--client-id", "AgentID")]
        [TestCase("--directory", "/home/any/logs")]
        [TestCase("--files", "/home/any/logs/file1.metrics;/home/any/logs/file2.metrics")]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--experiment-id", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("-i", null)]
        [TestCase("--intrinsic", null)]
        [TestCase("--log-dir", "C:\\any\\path\\to\\logs")]
        [TestCase("--logger", "csv")]
        [TestCase("--log-level", "2")]
        [TestCase("--log-level", "Information")]
        [TestCase("--match", "*.metrics")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--parameters", "Param1=Value1,,,Param2=Value2")]
        [TestCase("-r", null)]
        [TestCase("--recursive", null)]
        [TestCase("--system", "any")]
        [TestCase("-v", null)]
        [TestCase("--verbose", null)]
        public void VirtualClientUploadTelemetryCommandSupportsAllExpectedOptions(string option, string value)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    "upload-telemetry",
                    "--format", "CSV",
                    "--schema", "Metrics",
                    "--event-hub", "sb://any.servicebus.net"
                };

                arguments.Add(option);
                if (value != null)
                {
                    arguments.Add(value);
                }

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = CommandLineParser.Create(arguments.ToArray(), tokenSource).Parse();
                    Assert.IsFalse(result.Errors.Any());
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        public void VirtualClientCommandLineSupportsResponseFiles()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>()
                {
                    $"@{Path.Combine(CommandLineOptionTests.ResourcesDirectory, "TestOptions.rsp")}"
                };

                ParseResult result = CommandLineParser.Create(arguments, tokenSource).Parse();
                Assert.IsFalse(result.Errors.Any());

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