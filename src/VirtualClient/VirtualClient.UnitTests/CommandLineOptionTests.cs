// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Parsing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class CommandLineOptionTests
    {
        private static readonly string ResourcesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(CommandLineOptionTests)).Location),
            "Resources");

        [Test]
        public void VirtualClientDefaultCommandRequiresTheProfileOptionBeSupplied()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                List<string> arguments = new List<string>();
                Assert.Throws<ArgumentException>(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsTrue(result.Errors.Any());
                    result.ThrowOnUsageError();
                });

                arguments.AddRange(new List<string>
                {
                    "--profile", "PERF-ANY-PROFILE.json"
                });

                Assert.DoesNotThrow(() =>
                {
                    ParseResult result = Program.SetupCommandLine(arguments.ToArray(), cancellationSource).Build().Parse(arguments);
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                });
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--agentid", "AgentID")]
        [TestCase("--clientId", "AgentID")]
        [TestCase("--clientid", "AgentID")]
        [TestCase("--client", "AgentID")]
        [TestCase("--a", "AgentID")]
        [TestCase("--port", "4501")]
        [TestCase("--api-port", "4501")]
        [TestCase("--contentStore", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--contentstore", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--content", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--cs", "https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123")]
        [TestCase("--contentPathPattern", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentpathpattern", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--contentPath", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--cspt", "anyname1/anyname2/{experimentId}/{agentId}/anyname3/{toolName}/{role}/{scenario}")]
        [TestCase("--debug", null)]
        [TestCase("--verbose", null)]
        [TestCase("--dependencies", null)]
        [TestCase("--eventHubConnectionString", "ConnectionString")]
        [TestCase("--eventhubconnectionstring", "ConnectionString")]
        [TestCase("--eventHub", "ConnectionString")]
        [TestCase("--eventhub", "ConnectionString")]
        [TestCase("--eh", "ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experimentid", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--fail-fast", null)]
        [TestCase("--flush-wait", "00:10:00")]
        [TestCase("--exit-wait", "00:10:00")]
        [TestCase("--fw", "00:10:00")]
        [TestCase("--i", "3")]
        [TestCase("--iterations", "3")]
        [TestCase("--layoutPath", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layoutpath", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--layout", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--lp", "C:\\any\\path\\to\\layout.json")]
        [TestCase("--log-to-file", null)]
        [TestCase("--logToFile", null)]
        [TestCase("--logtofile", null)]
        [TestCase("--ltf", null)]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--mt", "Key1=Value1,,,Key2=Value2")]
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
                    Assert.IsFalse(result.Errors.Any());
                    result.ThrowOnUsageError();
                }, $"Option '{option}' is not supported.");
            }
        }

        [Test]
        [TestCase("--agentId", "AgentID")]
        [TestCase("--agentid", "AgentID")]
        [TestCase("--clientId", "AgentID")]
        [TestCase("--clientid", "AgentID")]
        [TestCase("--client", "AgentID")]
        [TestCase("--a", "AgentID")]
        [TestCase("--debug", null)]
        [TestCase("--eventHubConnectionString", "ConnectionString")]
        [TestCase("--eventhubconnectionstring", "ConnectionString")]
        [TestCase("--eventHub", "ConnectionString")]
        [TestCase("--eventhub", "ConnectionString")]
        [TestCase("--eh", "ConnectionString")]
        [TestCase("--experimentId", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experimentid", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--experiment", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--e", "0B692DEB-411E-4AC1-80D5-AF539AE1D6B2")]
        [TestCase("--metadata", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--mt", "Key1=Value1,,,Key2=Value2")]
        [TestCase("--log-to-file", null)]
        [TestCase("--logToFile", null)]
        [TestCase("--logtofile", null)]
        [TestCase("--ltf", null)]
        [TestCase("--packageStore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packagestore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--packages", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
        [TestCase("--ps", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b")]
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
        [TestCase("--port", "4501")]
        [TestCase("--api-port", "4501")]
        [TestCase("--monitor", null)]
        [TestCase("--mon", null)]
        [TestCase("--ipAddress", "10.0.0.128")]
        [TestCase("--ip", "10.0.0.128")]
        [TestCase("--debug", null)]
        [TestCase("--log-to-file", null)]
        [TestCase("--logToFile", null)]
        [TestCase("--logtofile", null)]
        [TestCase("--ltf", null)]
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
                        "--contentStore", "BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123",
                        "--packageStore", "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b",
                        "--agentId", "007",
                        "--experimentId", "123456",
                        "--debug"
                    },
                    result.Tokens.Select(t => t.Value));

            }
        }
    }
}