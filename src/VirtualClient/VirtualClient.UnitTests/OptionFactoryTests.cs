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
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Identity;

    [TestFixture]
    [Category("Unit")]
    public class OptionFactoryTests
    {
        [OneTimeSetUp]
        public void SetupFixture()
        {
            Environment.CurrentDirectory = MockFixture.GetDirectory(typeof(OptionFactoryTests));
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
        [TestCase("--clean")]
        public void CleanOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateCleanOption();
            ParseResult result = option.Parse($"{alias}=logs");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--clean")]
        public void CleanOptionSupportsExpectedTargetResources(string alias)
        {
            Option option = OptionFactory.CreateCleanOption();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
            Assert.IsTrue(result.Tokens.Count == 1);

            result = option.Parse($"{alias}=logs");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("logs", result.Tokens[1].Value);

            result = option.Parse($"{alias}=packages");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("packages", result.Tokens[1].Value);

            result = option.Parse($"{alias}=state");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("state", result.Tokens[1].Value);

            result = option.Parse($"{alias}=all");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("all", result.Tokens[1].Value);

            result = option.Parse($"{alias}=logs,packages,state");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("logs,packages,state", result.Tokens[1].Value);

            result = option.Parse($"{alias}=logs;packages;state");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("logs;packages;state", result.Tokens[1].Value);
        }

        [Test]
        [TestCase("--clean")]
        public void CleanOptionValidatesTheTargetsProvided(string alias)
        {
            Option option = OptionFactory.CreateCleanOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"{alias}=not,valid,targets"));
        }

        [Test]
        [TestCase("--agent-id")]
        [TestCase("--agentId")]
        [TestCase("--agentid")]
        [TestCase("--client-id")]
        [TestCase("--clientId")]
        [TestCase("--clientid")]
        [TestCase("--client")]
        [TestCase("--c")]
        public void ClientIdOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateClientIdOption();
            ParseResult result = option.Parse($"{alias}=Agent");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--content-store")]
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
        [TestCase("--logger=console")]
        [TestCase("--logger=console --logger=file")]
        public void LoggerOptionSupportsMultipleLoggerInputs(string input)
        {
            Option option = OptionFactory.CreateLoggerOption();
            ParseResult result = option.Parse(input);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleStorageAccountConnectionStrings))]
        public void ContentStoreOptionSupportsValidStoageAccountConnectionStrings(string connectionToken)
        {
            Option option = OptionFactory.CreateContentStoreOption();
            ParseResult result = option.Parse($"--content-store={connectionToken}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleStorageAccountSasUris))]
        public void ContentStoreOptionSupportsValidStorageAccountSasUris(string uri)
        {
            Option option = OptionFactory.CreateContentStoreOption();
            ParseResult result = option.Parse($"--content-store={uri}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityConnectionStrings), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void ContentStoreOptionSupportsConnectionStringsWithManagedIdentyReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            Option option = OptionFactory.CreateContentStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--content-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityUris), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void ContentStoreOptionSupportsUrisWithManagedIdentityReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            Option option = OptionFactory.CreateContentStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--content-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdConnectionStrings), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void ContentStoreOptionSupportsConnectionStringsWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    "123456789",
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            // Setup:
            // A matching certificate is found in the local store.
            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            Option option = OptionFactory.CreateContentStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--contentStore={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdUris), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void ContentStoreOptionSupportsUrisWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    "123456789",
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            // Setup:
            // A matching certificate is found in the local store.
            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            Option option = OptionFactory.CreateContentStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--contentStore={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void ContentStoreOptionValidatesTheConnectionTokenProvided()
        {
            Option option = OptionFactory.CreateContentStoreOption();
            Assert.Throws<SchemaException>(() => option.Parse($"--contentStore=NotAValidConnectionStringOrSasTokenUri"));
        }

        [Test]
        [TestCase("--content-path-template")]
        [TestCase("--content-path")]
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
            Option option = OptionFactory.CreateVerboseFlag();
            ParseResult result = option.Parse(alias);
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--event-hub")]
        [TestCase("--eventHub")]
        [TestCase("--eventhub")]
        [TestCase("--eventhubconnectionstring")]
        [TestCase("--eventHubConnectionString")]
        [TestCase("--eh")]
        public void EventHubConnectionStringOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateEventHubStoreOption();
            ParseResult result = option.Parse($"{alias}=Endpoint=ConnectionString");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleEventHubConnectionStrings))]
        public void EventHubConnectionStringOptionSupportsAccessPolicyConnectionStrings(string connectionToken)
        {
            Option option = OptionFactory.CreateEventHubStoreOption();
            ParseResult result = option.Parse($"--event-hub={connectionToken}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityConnectionStrings), new object[] { DependencyStore.StoreTypeAzureEventHubNamespace })]
        public void EventHubConnectionStringOptionSupportsConnectionStringsWithManagedIdentyReferences(string argument)
        {
            Option option = OptionFactory.CreateEventHubStoreOption();
            ParseResult result = option.Parse($"--event-hub={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityUris), new object[] { DependencyStore.StoreTypeAzureEventHubNamespace })]
        public void EventHubConnectionStringOptionSupportsUrisWithManagedIdentityReferences(string argument)
        {
            Option option = OptionFactory.CreateEventHubStoreOption();
            ParseResult result = option.Parse($"--event-hub={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--experiment-id")]
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
        [TestCase("--wait")]
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
        [TestCase("--ip-address")]
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
            Assert.Throws<ArgumentException>(() => option.Parse("--ip-address=NotAnIP"));

            // IPv4 format
            Assert.DoesNotThrow(() => option.Parse($"--ip-address=10.0.1.128"));

            // IPv6 format
            Assert.DoesNotThrow(() => option.Parse($"--ip-address=2001:db8:85a3:8d3:1319:8a2e:370:7348"));
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
        [TestCase("--key-vault")]
        [TestCase("--key-Vault")]
        [TestCase("--keyvault")]
        [TestCase("--keyVault")]
        [TestCase("--kv")]
        public void KeyVaultOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateKeyVaultOption();
            ParseResult result = option.Parse($"{alias}=https://my-keyvault.vault.azure.net/?miid=307591a4-abb2-4559-af59-b47177d140cf");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityConnectionStrings), new object[] { DependencyStore.StoreTypeAzureKeyVault })]
        public void KeyVaultOptionSupportsConnectionStringsWithManagedIdentyReferences(string argument)
        {
            Option option = OptionFactory.CreateKeyVaultOption();
            ParseResult result = option.Parse($"--key-vault={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityUris), new object[] { DependencyStore.StoreTypeAzureKeyVault })]
        public void KeyVaultOptionSupportsUrisWithManagedIdentityReferences(string argument)
        {
            Option option = OptionFactory.CreateKeyVaultOption();
            ParseResult result = option.Parse($"--key-vault={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdConnectionStrings), new object[] { DependencyStore.StoreTypeAzureKeyVault })]
        public void KeyVaultOptionSupportsConnectionStringsWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            Option option = OptionFactory.CreateKeyVaultOption();
            ParseResult result = option.Parse($"--kv={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdUris), new object[] { DependencyStore.StoreTypeAzureKeyVault })]
        public void KeyVaultOptionSupportsUrisWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            Option option = OptionFactory.CreateKeyVaultOption();
            ParseResult result = option.Parse($"--kv={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--layout-path")]
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
        [TestCase("--log-dir")]
        [TestCase("--ldir")]
        public void LogDirectoryOptionSupportsExpectedAliases(string alias)
        {
                Option option = OptionFactory.CreateLogDirectoryOption();
                ParseResult result = option.Parse($"{alias}=C:\\Any\\Directory\\Path");

                Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void LogDirectoryOptionSupportsFullPaths()
        {
            string path = OperatingSystem.IsWindows() ? "C:\\Any\\Directory\\Path" : "/home/any/directory/path";
            Option option = OptionFactory.CreateLogDirectoryOption();
            ParseResult result = option.Parse($"--log-dir={path}");

            string expectedPath = path;
            string actualPath = result.ValueForOption("--log-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(".\\Any\\Directory\\Path")]
        [TestCase("..\\Any\\Directory\\Path")]
        [TestCase("..\\..\\Any\\Directory\\Path")]
        public void LogDirectoryOptionSupportsRelativePaths(string path)
        {
            Option option = OptionFactory.CreateLogDirectoryOption();
            ParseResult result = option.Parse($"--log-dir={path}");

            string expectedPath = Path.GetFullPath(path);
            string actualPath = result.ValueForOption("--log-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase("--log-level")]
        [TestCase("--ll")]
        public void LogLevelOptionSupportsExpectedAliases(string alias)
        {
            foreach (LogLevel level in Enum.GetValues<LogLevel>())
            {
                int expectedLevel = (int)level;
                Option option = OptionFactory.CreateLogLevelOption();
                ParseResult result = option.Parse($"{alias}={expectedLevel}");
    
                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(expectedLevel, int.Parse(result.Tokens.ElementAt(1).Value));
            }
        }

        [Test]
        public void LogLevelOptionSupportsStringRepresentationsOfTheLogLevelEnumeration()
        {
            foreach (LogLevel level in Enum.GetValues<LogLevel>())
            {
                Option option = OptionFactory.CreateLogLevelOption();
                ParseResult result = option.Parse($"--log-level={level}");

                Assert.IsFalse(result.Errors.Any());
                Assert.AreEqual(level.ToString(), result.Tokens.ElementAt(1).Value);
            }
        }

        [Test]
        public void LogLevelOptionThrowsOnAnInvalidValue()
        {
            Option option = OptionFactory.CreateLogLevelOption();
            Assert.Throws<ArgumentException>(() => option.Parse($"--log-level=100"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--log-level=VeryVerbose"));
        }

        [Test]
        [TestCase("--log-retention")]
        [TestCase("--lr")]
        public void LogRetentionOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateLogRetentionOption();
            ParseResult result = option.Parse($"{alias}=14400");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCase("--log-retention")]
        [TestCase("--lr")]
        public void LogRetentionOptionSupportsBothIntegerMinutesAndTimeSpanFormats(string alias)
        {
            Option option = OptionFactory.CreateLogRetentionOption();
            ParseResult result = option.Parse($"{alias}=14400");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(14400, int.Parse(result.Tokens.ElementAt(1).Value));

            result = option.Parse($"{alias}=10.00:00:00");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(TimeSpan.FromDays(10), TimeSpan.Parse(result.Tokens.ElementAt(1).Value));
        }

        [Test]
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
        public void MetadataOptionSupportsTripleCommaDelimitedKeyValuePairs()
        {
            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse("--metadata:Key1=Value1,,,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void MetadataOptionSupportsSemiColonDelimitedKeyValuePairs()
        {

            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse("--metadata=Key1=Value1;Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1;Key2=Value2", result.Tokens[1].Value);
        }

        [Test]
        public void MetadataOptionSupportsCommaDelimitedKeyValuePairs()
        {

            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse("--metadata=Key1=Value1,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1,Key2=Value2", result.Tokens[1].Value);
        }

        [Test]
        public void MetadataOptionSupportsDelimitedPairsThatHaveValuesContainingDelimiters()
        {
            Option option = OptionFactory.CreateMetadataOption();
            ParseResult result = option.Parse("--metadata=Key1=Value1A;Value1B;Value1C;Key2=Value2,,,Key3=V3A;V3B;V3C");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1A;Value1B;Value1C;Key2=Value2,,,Key3=V3A;V3B;V3C", result.Tokens[1].Value);

            result = option.Parse("--metadata=Key1=Value1A,Value1B,Value1C,Key2=Value2,,,Key3=V3A,V3B,V3C");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1A,Value1B,Value1C,Key2=Value2,,,Key3=V3A,V3B,V3C", result.Tokens[1].Value);
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
        [TestCase("--package-dir")]
        [TestCase("--pdir")]
        public void PackageDirectoryOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreatePackageDirectoryOption();
            ParseResult result = option.Parse($"{alias}=\\Any\\Directory\\Path");

            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void PackageDirectoryOptionSupportsFullPaths()
        {
            string path = OperatingSystem.IsWindows() ? "C:\\Any\\Directory\\Path" : "/home/any/directory/path";
            Option option = OptionFactory.CreatePackageDirectoryOption();
            ParseResult result = option.Parse($"--package-dir={path}");

            string expectedPath = path;
            string actualPath = result.ValueForOption("--package-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(".\\Any\\Directory\\Path")]
        [TestCase("..\\Any\\Directory\\Path")]
        [TestCase("..\\..\\Any\\Directory\\Path")]
        public void PackageDirectoryOptionSupportsRelativePaths(string path)
        {
            Option option = OptionFactory.CreatePackageDirectoryOption();
            ParseResult result = option.Parse($"--package-dir={path}");

            string expectedPath = Path.GetFullPath(path);
            string actualPath = result.ValueForOption("--package-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase("--package-store")]
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
        [TestCaseSource(nameof(GetExampleStorageAccountConnectionStrings))]
        public void PackageStoreOptionSupportsValidStoageAccountConnectionStrings(string connectionToken)
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            ParseResult result = option.Parse($"--package-store={connectionToken}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleStorageAccountSasUris))]
        public void PackageStoreOptionSupportsValidStorageAccountSasUris(string uri)
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            ParseResult result = option.Parse($"--package-store={uri}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityConnectionStrings), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void PackageStoreOptionSupportsConnectionStringsWithManagedIdentyReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--package-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleManagedIdentityUris), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void PackageStoreOptionSupportsUrisWithManagedIdentityReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--package-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdConnectionStrings), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void PackageStoreOptionSupportsConnectionStringsWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    "123456789",
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            // Setup:
            // A matching certificate is found in the local store.
            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--package-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        [TestCaseSource(nameof(GetExampleMicrosoftEntraIdUris), new object[] { DependencyStore.StoreTypeAzureStorageBlob })]
        public void PackageStoreOptionSupportsUrisWithMicrosoftEntraIdAndCertificateReferences(string argument)
        {
            var mockCertManager = new Mock<ICertificateManager>();

            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    "123456789",
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            // Setup:
            // A matching certificate is found in the local store.
            mockCertManager
                .Setup(c => c.GetCertificateFromStoreAsync(
                    It.Is<string>(issuer => issuer == "ABC" || issuer == "ABC CA 01" || issuer == "CN=ABC CA 01, DC=ABC, DC=COM"),
                    It.Is<string>(subject => subject == "any.domain.com" || subject == "CN=any.domain.com"),
                    It.IsAny<IEnumerable<StoreLocation>>(),
                    StoreName.My))
                .ReturnsAsync(OptionFactoryTests.GenerateMockCertificate());

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            ParseResult result = option.Parse($"--package-store={argument}");
            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void PackageStoreOptionValidatesTheConnectionTokenProvided()
        {
            Option option = OptionFactory.CreatePackageStoreOption();
            Assert.Throws<SchemaException>(() => option.Parse($"--package-store=NotAValidConnectionStringOrSasTokenUri"));
        }

        [Test]
        public void PackageStoreOptionThrowsTheExpectedExceptionWhenTheUserDoesNotHavePermissionsToAccessTheCertificateStore()
        {
            var mockCertManager = new Mock<ICertificateManager>();
            mockCertManager.Setup(c => c.GetCertificateFromStoreAsync(It.IsAny<string>(), It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .Throws(() => new CryptographicException($"Permissions to certificate store denied."));

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            Assert.Throws<CryptographicException>(() => option.Parse(
                $"--package-store=CertificateThumbprint=AAAAAA;ClientId=BBBBBBBB;TenantId=CCCCCCCC;EndpointUrl=https://anystorageaccount.blob.core.windows.net/packages"));
        }

        [Test]
        public void PackageStoreOptionThrowsTheExpectedExceptionWhenTheUserDoesNotHavePermissionsToAccessTheCertificatesWithinTheStore()
        {
            var mockCertManager = new Mock<ICertificateManager>();
            mockCertManager.Setup(c => c.GetCertificateFromStoreAsync(It.IsAny<string>(), It.IsAny<IEnumerable<StoreLocation>>(), It.IsAny<StoreName>()))
                .Throws(() => new SecurityException($"Permissions to certificates denied."));

            Option option = OptionFactory.CreatePackageStoreOption(certificateManager: mockCertManager.Object);
            Assert.Throws<SecurityException>(() => option.Parse(
                $"--package-store=CertificateThumbprint=AAAAAA;ClientId=BBBBBBBB;TenantId=CCCCCCCC;EndpointUrl=https://anystorageaccount.blob.core.windows.net/packages"));
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
        public void ParametersOptionSupportsTripleCommaDelimitedKeyValuePairs()
        {
            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse("--parameters:Key1=Value1,,,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1,,,Key2=Value2", result.Tokens[1].Value);
        }

        [Test]
        public void ParametersOptionSupportsSemiColonDelimitedKeyValuePairs()
        {

            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse("--parameters=Key1=Value1;Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1;Key2=Value2", result.Tokens[1].Value);
        }

        [Test]
        public void ParametersOptionSupportsCommaDelimitedKeyValuePairs()
        {
            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse("--parameters=Key1=Value1,Key2=Value2");
            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1,Key2=Value2", result.Tokens[1].Value);
        }

        [Test]
        public void ParametersOptionSupportsDelimitedPairsThatHaveValuesContainingDelimiters()
        {
            Option option = OptionFactory.CreateParametersOption();
            ParseResult result = option.Parse("--parameters=Key1=Value1A;Value1B;Value1C;Key2=Value2,,,Key3=V3A;V3B;V3C");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1A;Value1B;Value1C;Key2=Value2,,,Key3=V3A;V3B;V3C", result.Tokens[1].Value);

            result = option.Parse("--parameters=Key1=Value1A,Value1B,Value1C,Key2=Value2,,,Key3=V3A,V3B,V3C");

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual("Key1=Value1A,Value1B,Value1C,Key2=Value2,,,Key3=V3A,V3B,V3C", result.Tokens[1].Value);
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

            // This line works on a linux machine. Option works when it is a path.
            // Assert.Throws<ArgumentException>(() => option.Parse($"--proxy-api=/any/relative/uri"));
            Assert.Throws<ArgumentException>(() => option.Parse($"--proxy-api=notavaliduri"));
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithThePackageStoreOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--package-store=https://any.blob.store", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--package-store=https://any.blob.store --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--package-store=https://any.blob.store" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --package-store=https://any.blob.store"));
            }
        }

        [Test]
        public void PackageStoreOptionDoesNotApplyDefaultWhenProxyIsUsed()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri" }, tokenSource);
                ParseResult result = commandBuilder.Build().Parse("--proxy-api=http://anyuri");
            }
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithTheContentStoreOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--content-store=https://any.blob.store", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--content-store=https://any.blob.store --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--content-store=https://any.blob.store" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --content-store=https://any.blob.store"));
            }
        }

        [Test]
        public void ProxyApiOptionsCannotBeUsedAtTheSameTimeWithTheEventHubConnectionStringOption()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                Option option = OptionFactory.CreateProxyApiOption();

                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { "--eventHub=sb://any.servicebus.hub?miid=1234567", "--proxy-api=http://anyuri" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--eventHub=sb://any.servicebus.hub?miid=1234567 --proxy-api=http://anyuri"));

                commandBuilder = Program.SetupCommandLine(new string[] { "--proxy-api=http://anyuri", "--eventHub=sb://any.servicebus.hub?miid=1234567" }, tokenSource);
                Assert.Throws<ArgumentException>(() => commandBuilder.Build().Parse("--proxy-api=http://anyuri --eventHub=sb://any.servicebus.hub?miid=1234567"));
            }
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
        [TestCase("--state-dir")]
        [TestCase("--sdir")]
        public void StateDirectoryOptionSupportsExpectedAliases(string alias)
        {
            Option option = OptionFactory.CreateStateDirectoryOption();
            ParseResult result = option.Parse($"{alias}=\\Any\\Directory\\Path");

            Assert.IsFalse(result.Errors.Any());
        }

        [Test]
        public void StateDirectoryOptionSupportsFullPaths()
        {
            string path = OperatingSystem.IsWindows() ? "C:\\Any\\Directory\\Path" : "/home/any/directory/path";
            Option option = OptionFactory.CreateStateDirectoryOption();
            ParseResult result = option.Parse($"--state-dir={path}");

            string expectedPath = path;
            string actualPath = result.ValueForOption("--state-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(".\\Any\\Directory\\Path")]
        [TestCase("..\\Any\\Directory\\Path")]
        [TestCase("..\\..\\Any\\Directory\\Path")]
        public void StateDirectoryOptionSupportsRelativePaths(string path)
        {
            Option option = OptionFactory.CreateStateDirectoryOption();
            ParseResult result = option.Parse($"--state-dir={path}");

            string expectedPath = Path.GetFullPath(path);
            string actualPath = result.ValueForOption("--state-dir")?.ToString();

            Assert.IsFalse(result.Errors.Any());
            Assert.AreEqual(expectedPath, actualPath);
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
            Assert.DoesNotThrow(() => option.Parse("--timeout=-1"));
            Assert.Throws<ArgumentException>(() => option.Parse("--timeout=-2"));
            Assert.DoesNotThrow(() => option.Parse("--timeout=NeVer"));
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

        [Test]
        public void IterationsOptionsCanParsedCorrectlyWithPackagesSasurl()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                string sasPart = "--packages=https://anystorageaccount.blob.core.windows.net/?sv=2020&ss=b&srt=c&sp=rwlacx&se=2Z&st=2021Z&spr=https";
                string eventhubPart = "--eventhub=\"Endpoint=sb://xxx.servicebus.windows.net/;S=Az;SKey=EZ=\"";
                string iterationPart = "--iterations=2";
                CommandLineBuilder commandBuilder = Program.SetupCommandLine(new string[] { }, tokenSource);
                ParseResult result = commandBuilder.Build().Parse($"{sasPart} {iterationPart} {eventhubPart}");
            }
        }

        private static X509Certificate2 GenerateMockCertificate()
        {
            using (RSA rsa = RSA.Create(1024))
            {
                var request = new CertificateRequest("cn=unittest", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                // Self-sign the certificate
                var certificate = request.CreateSelfSigned(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(10));

                // Optional: Export the certificate to a byte array or file
                byte[] certBytes = certificate.Export(X509ContentType.Pfx, "password");

                // Use the certificate for testing, e.g., with HttpClientHandler or other scenarios.

                return X509CertificateLoader.LoadPkcs12(certBytes, "password");
            }
        }

        private static IEnumerable<string> GetExampleEventHubConnectionStrings()
        {
            return new List<string>
            {
                "Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=9876",
                "Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=9876;EntityPath=telemetry-logs"
            };
        }

        private static IEnumerable<string> GetExampleManagedIdentityConnectionStrings(string storeType)
        {
            IEnumerable<string> examples = null;
            if (storeType == DependencyStore.StoreTypeAzureStorageBlob)
            {
                examples = new List<string>
                {
                    "EndpointUrl=https://anystorage.blob.core.windows.net;ManagedIdentityId=11223344",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/;ManagedIdentityId=11223344",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/container;ManagedIdentityId=11223344",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/container/;ManagedIdentityId=11223344"
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureEventHubNamespace)
            {
                examples = new List<string>
                {
                    "EndpointUrl=sb://any.servicebus.windows.net;ManagedIdentityId=11223344",
                    "EndpointUrl=sb://any.servicebus.windows.net/;ManagedIdentityId=11223344",
                    "EventHubNamespace=any.servicebus.windows.net/;ManagedIdentityId=11223344"
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureKeyVault)
            {
                examples = new List<string>
                {
                    "EndpointUrl=https://my-keyvault.vault.azure.net;ManagedIdentityId=11223344",
                    "EndpointUrl=https://my-keyvault.vault.azure.net/;ManagedIdentityId=11223344"
                };
            }

            return examples;
        }

        private static IEnumerable<string> GetExampleManagedIdentityUris(string storeType)
        {
            IEnumerable<string> examples = null;
            if (storeType == DependencyStore.StoreTypeAzureStorageBlob)
            {
                examples = new List<string>
                {
                    "https://anystorage.blob.core.windows.net?miid=11223344",
                    "https://anystorage.blob.core.windows.net/?miid=11223344",
                    "https://anystorage.blob.core.windows.net/container?miid=11223344",
                    "https://anystorage.blob.core.windows.net/container/?miid=11223344"
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureEventHubNamespace)
            {
                examples = new List<string>
                {
                    "sb://any.servicebus.windows.net?miid=11223344",
                    "sb://any.servicebus.windows.net/?miid=11223344"
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureKeyVault)
            {
                examples = new List<string>
                {
                    "https://my-keyvault.vault.azure.net?miid=11223344",
                    "https://my-keyvault.vault.azure.net/?miid=11223344"
                };
            }

            return examples;
        }

        private static IEnumerable<string> GetExampleMicrosoftEntraIdConnectionStrings(string storeType)
        {
            IEnumerable<string> examples = null;
            if (storeType == DependencyStore.StoreTypeAzureStorageBlob)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/container;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/container/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "EndpointUrl=https://anystorage.blob.core.windows.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "EndpointUrl=https://anystorage.blob.core.windows.net/container;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "\"EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC CA 01;CertificateSubject=any.domain.com\"",
                    "\"EndpointUrl=https://anystorage.blob.core.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=CN=ABC CA 01, DC=ABC, DC=COM;CertificateSubject=CN=any.domain.com\""
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureEventHubNamespace)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EventHubNamespace=any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "EndpointUrl=sb://any.servicebus.windows.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "EndpointUrl=sb://any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "EventHubNamespace=any.servicebus.windows.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com"
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureKeyVault)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "EndpointUrl=https://my-keyvault.vault.azure.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",
                    "EndpointUrl=https://my-keyvault.vault.azure.net/;ClientId=11223344;TenantId=55667788;CertificateThumbprint=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "EndpointUrl=https://my-keyvault.vault.azure.net;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com",
                    "EndpointUrl=https://my-keyvault.vault.azure.net/;ClientId=11223344;TenantId=55667788;CertificateIssuer=ABC;CertificateSubject=any.domain.com"
                };
            }

            return examples;
        }

        private static IEnumerable<string> GetExampleMicrosoftEntraIdUris(string storeType)
        {
            IEnumerable<string> examples = null;
            if (storeType == DependencyStore.StoreTypeAzureStorageBlob)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "https://anystorage.blob.core.windows.net?cid=11223344&tid=55667788&crtt=123456789",
                    "https://anystorage.blob.core.windows.net/?cid=11223344&tid=55667788&crtt=123456789",
                    "https://anystorage.blob.core.windows.net/container?cid=11223344&tid=55667788&crtt=123456789",
                    "https://anystorage.blob.core.windows.net/container/?cid=11223344&tid=55667788&crtt=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "https://anystorage.blob.core.windows.net?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "https://anystorage.blob.core.windows.net/?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "https://anystorage.blob.core.windows.net/container?cid=12345&tid=55667788&crti=ABC&crts=CN=any.domain.com",
                    "\"https://anystorage.blob.core.windows.net/?cid=12345&tid=55667788&crti=ABC CA 01&crts=CN=any.domain.com\"",
                    "\"https://anystorage.blob.core.windows.net/?cid=12345&tid=55667788&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com\""
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureEventHubNamespace)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "sb://any.servicebus.windows.net?cid=11223344&tid=55667788&crtt=123456789",
                    "sb://any.servicebus.windows.net/?cid=11223344&tid=55667788&crtt=123456789",
                    "sb://any.servicebus.windows.net/container?cid=11223344&tid=55667788&crtt=123456789",
                    "sb://any.servicebus.windows.net/container/?cid=11223344&tid=55667788&crtt=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "sb://any.servicebus.windows.net?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "sb://any.servicebus.windows.net/?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "\"sb://any.servicebus.windows.net?cid=12345&tid=55667788&crti=ABC CA 01&crts=CN=any.domain.com\"",
                    "\"sb://any.servicebus.windows.net/?cid=12345&tid=55667788&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com\""
                };
            }
            else if (storeType == DependencyStore.StoreTypeAzureKeyVault)
            {
                examples = new List<string>
                {
                    // Microsoft Entra IDs with certificates thumbprint references
                    "https://my-keyvault.vault.azure.net?cid=11223344&tid=55667788&crtt=123456789",
                    "https://my-keyvault.vault.azure.net/?cid=11223344&tid=55667788&crtt=123456789",

                    // Microsoft Entra IDs with certificates issuer and subject name references.
                    "https://my-keyvault.vault.azure.net?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "https://my-keyvault.vault.azure.net/?cid=12345&tid=55667788&crti=ABC&crts=any.domain.com",
                    "\"https://my-keyvault.vault.azure.net?cid=12345&tid=55667788&crti=ABC CA 01&crts=CN=any.domain.com\"",
                    "\"https://my-keyvault.vault.azure.net/?cid=12345&tid=55667788&crti=CN=ABC CA 01, DC=ABC, DC=COM&crts=CN=any.domain.com\""
                };
            }

            return examples;
        }

        private static IEnumerable<string> GetExampleStorageAccountConnectionStrings()
        {
            return new List<string>
            {
                "DefaultEndpointsProtocol=https;AccountName=anystorageaccount;EndpointSuffix=core.windows.net",
                "BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z"
            };
        }

        private static IEnumerable<string> GetExampleStorageAccountSasUris()
        {
            return new List<string>
            {
                "https://anystorageaccount.blob.core.windows.net?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https",
                "https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https",
                "https://anystorageaccount.blob.core.windows.net/container?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https",
                "https://anystorageaccount.blob.core.windows.net/container/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https"
            };
        }
    }
}