﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class OptionFactoryTests
    {
        [Test]
        [TestCase("--agentId")]
        [TestCase("--agentid")]
        [TestCase("--clientId")]
        [TestCase("--clientid")]
        [TestCase("--client")]
        [TestCase("--a")]
        public void AgentIdOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateAgentIdOption();
            ParseResult result = option.Parse($"{alias}=Agent");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--port")]
        [TestCase("--api-port")]
        public void ApiPortOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateApiPortOption();
            ParseResult result = option.Parse($"{alias}=4501");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ApiPortOptionSupportsSinglePortValues()
        {
            Option option = OptionFactory.CreateApiPortOption();
            ParseResult result = option.Parse($"--api-port=4501");
            Assert.IsFalse(result.Errors.Any());
            Assert.IsTrue(result.Tokens.First(t => t.Type == TokenType.Argument).Value == "4501");
        }

        [Test]
        public void ApiPortOptionSupportsPortPerRoleValues()
        {
            Option option = OptionFactory.CreateApiPortOption();
            ParseResult result = option.Parse($"--api-port=4501/Client,4502/Server");
            Assert.IsFalse(result.Errors.Any());
            Assert.IsTrue(result.Tokens.First(t => t.Type == TokenType.Argument).Value == "4501/Client,4502/Server");
        }

        [Test]
        public void ApiPortOptionValidatesInvalidSinglePortValues()
        {
            Option option = OptionFactory.CreateApiPortOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"--api-port=NotANumber"));
        }

        [Test]
        public void ApiPortOptionValidatesInvalidPortPerRoleValues()
        {
            Option option = OptionFactory.CreateApiPortOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"--api-port=NotANumber/Client"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--api-port=4501/Client,NotANumber/Server"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--api-port=4501\\Client,4502\\Server"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--api-port=4501,Client,4502,Server"));
        }

        [Test]
        [TestCase("--proxy-api")]
        [TestCase("--proxy")]
        public void ProxyApiOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateProxyApiOption();
            ParseResult result = option.Parse($"{alias}=http://anyuri");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ProxyApiOptionValidatesTheValueProvidedIsAnAbsoluteUri()
        {
            Option option = OptionFactory.CreateProxyApiOption();

            ParseResult result = null;
            Assert.DoesNotThrow(() => result = option.Parse($"--proxy-api=http://anyuri"));
            Assert.IsFalse(result.Errors.Any());

            Assert.Throws<ArgumentException>(() => option.Parse($"--proxy-api=/any/relative/uri"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--proxy-api=notavaliduri"));
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithThePackageStoreOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--packageStore=anystore", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--packageStore=anystore --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--packageStore=anystore" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --packageStore=anystore"));
            }
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithTheContentStoreOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--contentStore=anystore", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--contentStore=anystore --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--contentStore=anystore" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --contentStore=anystore"));
            }
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithTheEventHubConnectionStringOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--eventHubConnectionString=anyconnectionstring", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--eventHubConnectionString=anyconnectionstring --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--eventHubConnectionString=anyconnectionstring" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --eventHubConnectionString=anyconnectionstring"));
            }
        }

        [Test]
        [TestCase("--contentStore")]
        [TestCase("--contentstore")]
        [TestCase("--content")]
        [TestCase("--cs")]
        public void ContentStoreOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateContentStoreOption();
            ParseResult result = option.Parse($"{alias}=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=123");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorageaccount;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z")]
        [TestCase("https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https")]
        [TestCase("https://anystorageaccount.blob.core.windows.net/content?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https")]
        public void ContentStoreOptionSupportsValidConnectionStringsAndSasTokenUris(string connectionToken)
        {
            Option option = OptionFactory.CreateContentStoreOption();
            ParseResult result = option.Parse($"--contentStore={connectionToken}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ContentStoreOptionValidatesTheConnectionTokenProvided()
        {
            Option option = OptionFactory.CreateContentStoreOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"--contentStore=NotAValidConnectionStringOrSasTokenUri"));
        }

        [Test]
        [TestCase("--contentPathTemplate")]
        [TestCase("--contentpathtemplate")]
        [TestCase("--contentPath")]
        [TestCase("--contentpath")]
        [TestCase("--cp")]
        public void ContentPathTemplateOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateContentPathTemplateOption();
            ParseResult result = option.Parse($"{alias}=\"anyname1/anyname2/{{experimentId}}/{{agentId}}/anyname3/{{toolName}}/{{role}}/{{scenario}}\"");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--debug")]
        [TestCase("--verbose")]
        public void DebugFlagSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateDebugFlag();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--eventHubConnectionString")]
        [TestCase("--eventhubconnectionstring")]
        [TestCase("--eventHub")]
        [TestCase("--eventhub")]
        [TestCase("--eh")]
        public void EventHubConnectionStringOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateEventHubConnectionStringOption();
            ParseResult result = option.Parse($"{alias}=ConnectionString");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--experimentId")]
        [TestCase("--experimentid")]
        [TestCase("--experiment")]
        [TestCase("--e")]
        public void ExperimentIdOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateExperimentIdOption();
            ParseResult result = option.Parse($"{alias}=ID");
            Assert.IsFalse(result.Errors.Any());
        }


        [Test]
        [TestCase("--exit-wait")]
        [TestCase("--flush-wait")]
        [TestCase("--wt")]
        public void ExitWaitOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateExitWaitOption();
            ParseResult result = option.Parse($"{alias}=1234");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ExitWaitOptionValueMustBeAValidTimeSpanOrIntegerFormat()
        {
            Option option = OptionFactory.CreateExitWaitOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--exit-wait=NotValid"));
            Assert.DoesNotThrow(() => option.Parse("--exit-wait=01.00:30:00"));
            Assert.DoesNotThrow(() => option.Parse("--exit-wait=00:30:00"));
            Assert.DoesNotThrow(() => option.Parse("--exit-wait=1440"));
        }

        [Test]
        [TestCase("--fail-fast")]
        [TestCase("--ff")]
        public void FailFastFlagSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateFailFastFlag();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--ipAddress")]
        [TestCase("--ipaddress")]
        [TestCase("--ip")]
        public void IPAddressOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateIPAddressOption();
            ParseResult result = option.Parse($"{alias}=1.2.3.4");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void IPAddressOptionValueMustBeAValidIPv4OrIPv6Format()
        {
            Option option = OptionFactory.CreateIPAddressOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--ipAddress=NotAnIP"));

            // IPv4 format
            Assert.DoesNotThrow(() => option.Parse($"--ipAddress=10.0.1.128"));

            // IPv6 format
            Assert.DoesNotThrow(() => option.Parse($"--ipAddress=2001:db8:85a3:8d3:1319:8a2e:370:7348"));
        }

        [Test]
        [TestCase("--iterations")]
        [TestCase("--i")]
        public void IterationsOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateIterationsOption();
            ParseResult result = option.Parse($"{alias}=3");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void IterationsOptionValueMustBeAValidIntegerFormat()
        {
            Option option = OptionFactory.CreateIterationsOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--iterations=NotANumber"));
            Assert.Throws<ArgumentException>(() => option.Parse("--iterations=3.123"));

            Assert.DoesNotThrow(() => option.Parse($"--iterations=3"));
        }

        [Test]
        public void IterationsOptionValueMustBeGreaterThanZero()
        {
            Option option = OptionFactory.CreateIterationsOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--iterations=-1"));
        }

        [Test]
        [TestCase("--layoutPath")]
        [TestCase("--layoutpath")]
        [TestCase("--layout")]
        [TestCase("--lp")]
        public void LayoutPathOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateLayoutPathOption();
            ParseResult result = option.Parse($"{alias}=C:\\any\\path");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--logToFile")]
        [TestCase("--logtofile")]
        [TestCase("--log-to-file")]
        [TestCase("--ltf")]
        public void LogToFileFlagSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateLogToFileFlag();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--metadata")]
        [TestCase("--mt")]
        public void MetadataOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse($"{alias}:Key1=Value1");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void MetadataOptionSupportsDelimitedKeyValuePairs()
        {
            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse("--metadata:Key1=Value1,,,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--monitor")]
        [TestCase("--mon")]
        public void MonitorOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateMonitorFlag();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--name")]
        [TestCase("--n")]
        public void NameOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateNameOption();
            ParseResult result = option.Parse($"{alias}=anypackage");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--package")]
        [TestCase("--pkg")]
        public void PackageOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreatePackageOption();
            ParseResult result = option.Parse($"{alias}=anypackage.1.0.0.zip");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--packageStore")]
        [TestCase("--packagestore")]
        [TestCase("--packages")]
        [TestCase("--ps")]
        public void PackageStoreOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            ParseResult result = option.Parse($"{alias}=https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("DefaultEndpointsProtocol=https;AccountName=anystorageaccount;EndpointSuffix=core.windows.net")]
        [TestCase("BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z")]
        [TestCase("https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https")]
        [TestCase("https://anystorageaccount.blob.core.windows.net/packages?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https")]
        public void PackageStoreOptionSupportsValidConnectionStringsAndSasTokenUris(string connectionToken)
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            ParseResult result = option.Parse($"--packageStore={connectionToken}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void PackageStoreOptionValidatesTheConnectionTokenProvided()
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"--packageStore=NotAValidConnectionStringOrSasTokenUri"));
        }

        [Test]
        [TestCase("--parameters")]
        [TestCase("--pm")]
        public void ParametersOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse($"{alias}:Key1=Value1");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ParametersOptionSupportsDelimitedKeyValuePairs()
        {
            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse("--parameters:Key1=Value1,,,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--profile")]
        [TestCase("--p")]
        public void ProfileOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateProfileOption();
            ParseResult result = option.Parse($"{alias}=Profile");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ProfileOptionCanBeSuppliedAnyNumberOfTimes()
        {
            Option option = OptionFactory.CreateProfileOption();
            ParseResult result = option.Parse("--profile=Profile1 --profile=Profile2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--scenarios")]
        [TestCase("--sc")]
        public void ScenariosOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateScenariosOption();
            ParseResult result = option.Parse($"{alias}=Scenario1");
            Assert.IsFalse(result.Errors.Any());

            result = option.Parse($"{alias}=Scenario1,Scenario2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ScenariosOptionCanBeSuppliedAnyNumberOfTimes()
        {
            Option option = OptionFactory.CreateScenariosOption();
            ParseResult result = option.Parse("--scenarios=Scenario1 --scenarios=Scenario2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--seed")]
        [TestCase("--sd")]
        public void SeedOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateSeedOption();
            ParseResult result = option.Parse($"{alias}=1234");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--system")]
        [TestCase("--s")]
        public void SystemOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateSystemOption();
            ParseResult result = option.Parse($"{alias}=Profile");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--timeout")]
        [TestCase("--t")]
        public void TimeoutOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateTimeoutOption();
            ParseResult result = option.Parse($"{alias}=1234");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void TimeoutOptionValueMustBeAValidTimeSpanOrIntegerFormat()
        {
            Option option = OptionFactory.CreateTimeoutOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=NotValid"));
            Assert.DoesNotThrow(() => option.Parse("--timeout=01.00:30:00"));
            Assert.DoesNotThrow(() => option.Parse("--timeout=00:30:00"));
            Assert.DoesNotThrow(() => option.Parse("--timeout=1440"));
        }

        [Test]
        public void TimeoutOptionSupportsDeterministicInstructions()
        {
            // The current action must be allowed to complete before a timeout will
            // be honored.
            Option option = OptionFactory.CreateTimeoutOption();
            ParseResult result = option.Parse("--timeout=1234,deterministic");
            Assert.IsFalse(result.Errors.Any());

            option = OptionFactory.CreateTimeoutOption();
            result = option.Parse("--timeout=00:30:00,deterministic*");
            Assert.IsFalse(result.Errors.Any());

            // All actions in the profile must be allowed to complete before a timeout will
            // be honored
            option = OptionFactory.CreateTimeoutOption();
            result = option.Parse("--timeout=1234,deterministic*");
            Assert.IsFalse(result.Errors.Any());

            option = OptionFactory.CreateTimeoutOption();
            result = option.Parse("--timeout=00:30:00,deterministic*");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void TimeoutOptionSupportsCommonDelimitersForDeterministicInstructions()
        {
            Option option = OptionFactory.CreateTimeoutOption();
            ParseResult result = option.Parse("--timeout=1234,deterministic");
            Assert.IsFalse(result.Errors.Any());

            option = OptionFactory.CreateTimeoutOption();
            result = option.Parse("--timeout=00:30:00;deterministic");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void TimeoutOptionValidatesTheDeterministicInstructionsSupplied()
        {
            Option option = OptionFactory.CreateTimeoutOption();
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=1440,notvalid"));
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=00:01:00,notvalid"));
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=00:01:00,deterministic,no"));
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=00:01:00,deterministic*,no"));

            Assert.DoesNotThrow(() => option.Parse("--timeout=01.00:30:00,deterministic"));
            Assert.DoesNotThrow(() => option.Parse("--timeout=1440,deterministic*"));
        }

        [Test]
        public void TimeoutOptionsCannotBeUsedAtTheSameTimeAsAProfileIterationsOptionAndViceVersa()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--profile=ANY.json", "--timeout=1440", "--iterations=3" }, tokenSource);
                ArgumentException error = Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--profile=ANY.json --timeout=1440 --iterations=3"));
                Assert.AreEqual("Invalid usage. The timeout option cannot be used at the same time as the profile iterations option.", error.Message);

                commandBuilder = Program.SetupCommandLine(new string[] { "--profile=ANY.json --iterations=3 --timeout=1440" }, tokenSource);
                error = Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--iterations=3 --timeout=1440"));
                Assert.AreEqual("Invalid usage. The profile iterations option cannot be used at the same time as the timeout option.", error.Message);
            }
        }

        [Test]
        public void TimeoutOptionsCannotBeUsedAtTheSameTimeAsTheDependenciesFlag()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--profile=ANY.json", "--timeout=1440", "--dependencies" }, tokenSource);
                ArgumentException error = Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--profile=ANY.json --timeout=1440 --dependencies"));
                Assert.AreEqual("Invalid usage. The timeout option cannot be used when a dependencies flag is provided.", error.Message);
            }
        }

        [Test]
        public void IterationsOptionsCannotBeUsedAtTheSameTimeAsTheDependenciesFlag()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--profile=ANY.json", "--iterations=3", "--dependencies" }, tokenSource);
                ArgumentException error = Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--profile=ANY.json --iterations=3 --dependencies"));
                Assert.AreEqual("Invalid usage. The profile iterations option cannot be used when a dependencies flag is provided.", error.Message);
            }
        }
    }
}