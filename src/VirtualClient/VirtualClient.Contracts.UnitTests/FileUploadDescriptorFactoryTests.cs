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
        private FileUploadDescriptorFactory descriptorFactory;

        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFile = new Mock<IFileInfo>();
            this.descriptorFactory = new FileUploadDescriptorFactory();

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
            this.SetupDefaults();

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

            string contentPathPattern = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
            FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(context, contentPathPattern, timestamped: false);

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
            this.SetupDefaults();

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

            string contentPathPattern = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
            FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(context, contentPathPattern, timestamped: true);

            Assert.AreEqual(expectedExperimentId.ToLowerInvariant(), descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }

        [Test]
        [TestCase("customContainer/{experimentId}/{agentId}/fixedFolder/{toolName}/{role}/{scenario}", 0)]
        [TestCase("customContainer/expt_{experimentId}/{agentId}/fixedFolder/{toolName}/{role}_{scenario}", 1)]
        [TestCase("customContainer/expt_{experimentId}_agent_{agentId}/fixedFolder/{toolName}/{role}/{scenario}", 2)]
        [TestCase("customContainer/stringValue1/{experimentId}/{agentId}/fixedFolder,stringValue2/{toolName}/{role}/{scenario}", 3)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptorWithCustomTemplate(string contentPathPattern, int testCase)
        {
            this.SetupDefaults();

            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedContentType = HttpContentType.PlainText;
            string expectedContentEncoding = Encoding.UTF8.WebName;
            string expectedFilePath = this.mockFile.Object.FullName;
            string expectedFileName = $"{this.mockFile.Object.CreationTimeUtc.ToString("yyyy-MM-ddTHH-mm-ss-fffffZ")}-{this.mockFile.Object.Name}";

            string expectedBlobPath = string.Empty;

            switch (testCase)
            {
                case 0:
                    expectedBlobPath = string.Join('/', (new string[] { expectedExperimentId, "AgentIdA", "fixedFolder", "ToolA", "RoleA", "ScenarioA" })
                        .Where(i => i != null))
                        .ToLowerInvariant();
                    break;
                case 1:
                    expectedBlobPath = string.Join('/', (new string[] { $"expt_{expectedExperimentId}", "AgentIdA", "fixedFolder", "ToolA", "RoleA_ScenarioA" })
                        .Where(i => i != null))
                        .ToLowerInvariant();
                    break;
                case 2:
                    expectedBlobPath = string.Join('/', (new string[] { $"expt_{expectedExperimentId}_agent_AgentIdA", "fixedFolder", "ToolA", "RoleA", "ScenarioA" })
                        .Where(i => i != null))
                        .ToLowerInvariant();
                    break;
                case 3:
                    expectedBlobPath = string.Join('/', (new string[] { "stringValue1", expectedExperimentId, "AgentIdA", "fixedFolder,stringValue2", "ToolA", "RoleA", "ScenarioA" })
                        .Where(i => i != null))
                        .ToLowerInvariant();
                    break;
            }

            // The blob path itself is lower-cased. However, the file name casing is NOT modified.
            expectedBlobPath = !string.IsNullOrWhiteSpace(expectedBlobPath)
                ? $"/{expectedBlobPath}/{expectedFileName}"
                : $"/{expectedFileName}";

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                "AgentIdA",
                "ToolA",
                "ScenarioA",
                null,
                "RoleA");

            FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(context, contentPathPattern, timestamped: true);

            Assert.AreEqual("customContainer", descriptor.ContainerName);
            Assert.AreEqual(expectedFileName, descriptor.BlobName);
            Assert.AreEqual(expectedBlobPath, descriptor.BlobPath);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
            Assert.AreEqual(expectedFilePath, descriptor.FilePath);
        }
    }
}
