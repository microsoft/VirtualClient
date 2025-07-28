// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection.Metadata;
    using System.Text;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class FileUploadDescriptorFactoryTests
    {
        private MockFixture mockFixture;
        private Mock<IFileInfo> mockFile;

        public void Setup()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFile = new Mock<IFileInfo>();

            DateTime fileCreationTime = DateTime.Now;
            this.mockFile.Setup(file => file.Name).Returns("File.txt");
            this.mockFile.Setup(file => file.FullName).Returns("/home/user/VirtualClient/logs/speccpu/File.txt");
            this.mockFile.Setup(file => file.CreationTime).Returns(fileCreationTime);
            this.mockFile.Setup(file => file.CreationTimeUtc).Returns(fileCreationTime.ToUniversalTime());
        }

        [Test]
        [TestCase(null, null, null, null)]
        [TestCase("Agent01", null, null, null)]
        [TestCase("Agent01", "ToolA", null, null)]
        [TestCase("Agent01", "ToolA", "Scenario01", null)]
        [TestCase("Agent01", "ToolA", "Scenario01", "Client")]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_When_Not_Timestamped(string expectedAgentId, string expectedToolName, string expectedScenario, string expectedRole)
        {
            this.Setup();

            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = this.mockFile.Object.Name;
            string expectedBlobPath = string.Join('/', (new string[] { expectedAgentId, expectedToolName, expectedRole, expectedScenario })
                .Where(i => i != null))
                .ToLowerInvariant();

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = !string.IsNullOrWhiteSpace(expectedBlobPath)
                ? $"/{expectedBlobPath}/{expectedFileName}"
                : $"/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            string pathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: false, pathTemplate: pathTemplate);

            Assert.AreEqual(expectedExperimentId.ToLowerInvariant(), descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }


        [Test]
        [TestCase(null, null, null, null)]
        [TestCase("Agent01", null, null, null)]
        [TestCase("Agent01", "ToolA", null, null)]
        [TestCase("Agent01", "ToolA", "Scenario01", null)]
        [TestCase("Agent01", "ToolA", "Scenario01", "Client")]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_When_Timestamped(string expectedAgentId, string expectedToolName, string expectedScenario, string expectedRole)
        {
            this.Setup();

            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";
            string expectedBlobPath = string.Join('/', (new string[] { expectedAgentId, expectedToolName, expectedRole, expectedScenario })
                .Where(i => i != null))
                .ToLowerInvariant();

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = !string.IsNullOrWhiteSpace(expectedBlobPath)
                ? $"/{expectedBlobPath}/{expectedFileName}"
                : $"/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            string pathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: true, pathTemplate: pathTemplate);

            Assert.AreEqual(expectedExperimentId.ToLowerInvariant(), descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithDefaultContentPathTemplate_Scenario_1()
        {
            //  Default Template:
            //  {experimentId}/{agentId}/{toolName}/{scenario}
            this.Setup();

            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedToolName = "Toolkit";
            string expectedScenario = "Cycle-VegaServer";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";
            string expectedBlobPath = string.Join('/', (new string[]
            {
                expectedAgentId,
                expectedToolName,
                expectedScenario
            })
            .Where(i => i != null))
            .ToLowerInvariant();

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"/{expectedBlobPath}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario);

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: true);

            Assert.AreEqual(expectedExperimentId, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithDefaultContentPathTemplate_Scenario_2()
        {
            //  Default Template:
            //  {experimentId}/{agentId}/{toolName}/{role}/{scenario}
            this.Setup();

            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedToolName = "Toolkit";
            string expectedRole = "Client";
            string expectedScenario = "Cycle-VegaServer";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";
            string expectedBlobPath = string.Join('/', (new string[]
            {
                expectedAgentId,
                expectedToolName,
                expectedRole,
                expectedScenario
            })
            .Where(i => i != null))
            .ToLowerInvariant();

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"/{expectedBlobPath}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                role: expectedRole);

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: true);

            Assert.AreEqual(expectedExperimentId, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase
        (
            "customcontainer",
            "customcontainer",
            "/"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentId}",
            "customcontainer",
            "/cfad01a9-8F5c-4210-841e-63210ed6a85d"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId}/{Scenario}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d/cycle-vegaserver"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{Scenario}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d,revision01/cycle-vegaserver"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{AgentId}/{Scenario}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d,revision01/642,042728166da7,37,192.168.2.155/cycle-vegaserver"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{AgentId}/{Scenario}/{Role}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d,revision01/642,042728166da7,37,192.168.2.155/cycle-vegaserver/client"
        )]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithCustomContentPathTemplates(string contentPathTemplate, string expectedContainer, string expectedBlobPath)
        {
            this.Setup();

            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "Revision01";
            string expectedToolName = "Toolkit";
            string expectedScenario = "Cycle-VegaServer";
            string expectedRole = "Client";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"{expectedBlobPath.ToLowerInvariant().TrimEnd('/')}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            IDictionary<string, IConvertible> componentMetadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, metadata: componentMetadata, timestamped: true, pathTemplate: contentPathTemplate);

            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase
        (
            "customcontainer",
            "customcontainer",
            "/"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentId}",
            "customcontainer",
            "/cfad01a9-8F5c-4210-841e-63210ed6a85d"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{AgentId}/{Scenario}/{Role}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d,revision01/642,042728166da7,37,192.168.2.155/cycle-vegaserver/client"
        )]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithCustomContentPathTemplatesDefinedInCommandLineMetadata(string contentPathTemplate, string expectedContainer, string expectedBlobPath)
        {
            this.Setup();

            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "Revision01";
            string expectedToolName = "Toolkit";
            string expectedScenario = "Cycle-VegaServer";
            string expectedRole = "Client";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"{expectedBlobPath.ToLowerInvariant().TrimEnd('/')}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            IDictionary<string, IConvertible> componentMetadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { "ContentPathTemplate", contentPathTemplate }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, metadata: componentMetadata, timestamped: true);

            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase("contentpathtemplate")]
        [TestCase("CONTENTPATHTEMPLATE")]
        [TestCase("ContentPathTemplate")]
        [TestCase("contentPathTemplate")]
        public void FileUploadDescriptorFactoryIsNotCaseSensitiveOnContentPathTemplatesDefinedInCommandLineMetadata(string contentPathTemplateParameterName)
        {
            this.Setup();

            string expectedContentPathTemplate = "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{AgentId}/{Scenario}/{Role}";
            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "Revision01";
            string expectedToolName = "Toolkit";
            string expectedScenario = "Cycle-VegaServer";
            string expectedRole = "Client";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            string expectedContainer = "customcontainer";
            string expectedBlobPath = $"/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8f5c-4210-841e-63210ed6a85d,revision01/642,042728166da7,37,192.168.2.155/cycle-vegaserver/client/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            IDictionary<string, IConvertible> componentMetadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { contentPathTemplateParameterName, expectedContentPathTemplate }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, metadata: componentMetadata, timestamped: true);

            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase
        (
            "customcontainer",
            "customcontainer",
            "/"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentId}",
            "customcontainer",
            "/cfad01a9-8F5c-4210-841e-63210ed6a85d"
        )]
        [TestCase
        (
            "customcontainer/{ExperimentDefinitionId}/{ExperimentName}/{ExperimentId},{Revision}/{AgentId}/{Scenario}/{Role}",
            "customcontainer",
            "/645f6639-98c6-41ee-a853-b0a3ab8ce0a3/test_experiment/cfad01a9-8F5c-4210-841e-63210ed6a85d,revision01/642,042728166da7,37,192.168.2.155/cycle-vegaserver/client"
        )]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithCustomContentPathTemplatesDefinedInComponentParameters(string contentPathTemplate, string expectedContainer, string expectedBlobPath)
        {
            this.Setup();

            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "Revision01";
            string expectedToolName = "Toolkit";
            string expectedScenario = "Cycle-VegaServer";
            string expectedRole = "Client";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"{expectedBlobPath.ToLowerInvariant().TrimEnd('/')}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            IDictionary<string, IConvertible> componentParameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { "ContentPathTemplate", contentPathTemplate }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, parameters: componentParameters, timestamped: true);

            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase
        (
            "CUSTOMCONTAINER",
            "customcontainer",
            "/"
        )]
        [TestCase
        (
            "CustomContainer/ANY/other/pAtH/WiTH/MIXed/CAsinG",
            "customcontainer",
            "/any/other/path/with/mixed/casing"
        )]
        public void FileUploadDescriptorFactoryCreatesBlobPathsWithTheExpectedCasing(string contentPathTemplate, string expectedContainer, string expectedBlobPath)
        {
            this.Setup();

            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = $"{expectedBlobPath.ToLowerInvariant().TrimEnd('/')}/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                "AnyExperimentId",
                "AnyAgentId",
                "AnyToolName",
                "AnyScenario",
                "AnyCommandLine",
                "AnyRole");

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: true, pathTemplate: contentPathTemplate);

            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase("/")]
        [TestCase("{ThisDoesNotExist}")]
        public void FileUploadDescriptorFactoryValidatesTheContentPath(string invalidContentPathTemplate)
        {
            this.Setup();

            FileContext context = new FileContext(
                this.mockFile.Object,
                HttpContentType.PlainText,
                Encoding.UTF8.WebName,
                "AnyExperimentId",
                "AnyAgentId",
                "AnyToolName",
                "AnyScenario",
                "AnyCommandLine",
                "AnyRole");

            Assert.Throws<SchemaException>(() => FileUploadDescriptorFactory.CreateDescriptor(context, timestamped: true, pathTemplate: invalidContentPathTemplate));
        }

        [Test]
        public void FileUploadDescriptorFactorySupportsEitherOrPathTemplateDefinitions_1()
        {
            this.Setup();

            string expectedContainerName = "customcontainer";
            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "6.2";
            string expectedToolName = "Toolkit";
            string expectedScenario = "RunToolkitCommand";
            string expectedToolkitCommand = "Cycle-VegaServer";
            string expectedRole = "Client";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            string contentPathTemplate = "customcontainer/{experimentDefinitionId}/{experimentName}/{experimentId},{revision}/{toolkitCommand|scenario}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            string expectedBlobPath = string.Join("/",
                $"/{expectedExperimentDefinitionId}/{expectedExperimentName}/{expectedExperimentId},{expectedRevision}/{expectedToolkitCommand}".ToLowerInvariant(),
                expectedFileName);

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null,
                expectedRole);

            IDictionary<string, IConvertible> componentParameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { "ToolkitCommand", expectedToolkitCommand },
                { "Scenario", expectedScenario }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, parameters: componentParameters, timestamped: true, pathTemplate: contentPathTemplate);

            Assert.AreEqual(expectedContainerName, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        public void FileUploadDescriptorFactorySupportsEitherOrPathTemplateDefinitions_2()
        {
            this.Setup();

            string expectedContainerName = "customcontainer";
            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "6.2";
            string expectedToolName = "AnyTool";
            string expectedScenario = "ExecuteToolset";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // A parameter "toolkitCommand" does not exist. The template should use the "scenario" parameter instead.
            string contentPathTemplate = "customcontainer/{experimentDefinitionId}/{experimentName}/{experimentId},{revision}/{toolkitCommand|scenario}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            string expectedBlobPath = string.Join("/",
                $"/{expectedExperimentDefinitionId}/{expectedExperimentName}/{expectedExperimentId},{expectedRevision}/{expectedScenario}".ToLowerInvariant(),
                expectedFileName);

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null);

            IDictionary<string, IConvertible> componentParameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { "Scenario", expectedScenario }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, parameters: componentParameters, timestamped: true, pathTemplate: contentPathTemplate);

            Assert.AreEqual(expectedContainerName, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        public void FileUploadDescriptorFactoryHandlesCasesWhereTheTemplateReferencesAParameterThatDoesNotExist()
        {
            this.Setup();

            string expectedContainerName = "customcontainer";
            string expectedExperimentName = "Test_Experiment";
            string expectedExperimentId = "cfad01a9-8F5c-4210-841e-63210ed6a85d";
            string expectedExperimentDefinitionId = "645f6639-98c6-41ee-a853-b0a3ab8ce0a3";
            string expectedAgentId = "642,042728166da7,37,192.168.2.155";
            string expectedRevision = "6.2";
            string expectedToolName = "AnyTool";
            string expectedScenario = "ExecuteToolset";
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            // A parameter "toolkitCommand" does not exist. The factory should just remove the parameter in the
            // final blob path.
            string contentPathTemplate = "customcontainer/{experimentDefinitionId}/{experimentName}/{experimentId},{revision}/{toolkitCommand}/{scenario}";

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            string expectedBlobPath = string.Join("/",
                $"/{expectedExperimentDefinitionId}/{expectedExperimentName}/{expectedExperimentId},{expectedRevision}/{expectedScenario}".ToLowerInvariant(),
                expectedFileName);

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                null);

            IDictionary<string, IConvertible> componentParameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "Revision", expectedRevision },
                { "ExperimentDefinitionId", expectedExperimentDefinitionId },
                { "ExperimentName", expectedExperimentName },
                { "Scenario", expectedScenario }
            };

            FileUploadDescriptor descriptor = FileUploadDescriptorFactory.CreateDescriptor(context, parameters: componentParameters, timestamped: true, pathTemplate: contentPathTemplate);

            Assert.AreEqual(expectedContainerName, descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }
    }
}
