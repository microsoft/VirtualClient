// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Text;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    [TestFixture]
    [Category("Unit")]
    public class FileUploadDescriptorFactoryTests
    {
        private MockFixture mockFixture;
        private Mock<IFileInfo> mockFile;
        private FileUploadDescriptorFactory descriptorFactory;

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFile = new Mock<IFileInfo>();
            this.descriptorFactory = new FileUploadDescriptorFactory();

            DateTime fileCreationTime = DateTime.Now;
            this.mockFile.Setup(file => file.Name).Returns("file.txt");
            this.mockFile.Setup(file => file.CreationTime).Returns(fileCreationTime);
            this.mockFile.Setup(file => file.CreationTimeUtc).Returns(fileCreationTime.ToUniversalTime());

            if (platform == PlatformID.Unix)
            {
                this.mockFile.Setup(file => file.FullName).Returns("/home/user/VirtualClient/logs/speccpu/file.txt");
            }
            else if (platform == PlatformID.Win32NT)
            {
                this.mockFile.Setup(file => file.FullName).Returns("C:\\Users\\AnyUser\\VirtualClient\\logs\\speccpu\\file.txt");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_1(PlatformID platform)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                string expectedContentType = HttpContentType.PlainText;
                string expectedContentEncoding = Encoding.UTF8.WebName;
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedBlobPath = $"{component.AgentId}/{component.TypeName}/{component.Scenario}/{this.mockFile.Object.Name}".ToLowerInvariant();

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(component, this.mockFile.Object, expectedContentType, expectedContentEncoding);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_2_Toolname_Provided(PlatformID platform)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                string expectedContentType = HttpContentType.PlainText;
                string expectedContentEncoding = Encoding.UTF8.WebName;
                string expectedToolName = "ToolABC";
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedBlobPath = $"{component.AgentId}/{expectedToolName}/{component.Scenario}/{this.mockFile.Object.Name}".ToLowerInvariant();

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(component, this.mockFile.Object, expectedContentType, expectedContentEncoding, expectedToolName);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_3_Timestamp_Provided(PlatformID platform)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                DateTime expectedFileTimestamp = DateTime.UtcNow;
                string expectedContentType = HttpContentType.Binary;
                string expectedContentEncoding = Encoding.ASCII.WebName;
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedFileName = $"{expectedFileTimestamp.ToString("yyyy-MM-ddTHH-mm-ss-fffffK")}-{this.mockFile.Object.Name}";
                string expectedBlobPath = $"{component.AgentId}/{component.TypeName}/{component.Scenario}/".ToLowerInvariant() + expectedFileName;

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(component, this.mockFile.Object, expectedContentType, expectedContentEncoding, fileTimestamp: expectedFileTimestamp);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, "Client")]
        [TestCase(PlatformID.Unix, "Server")]
        [TestCase(PlatformID.Win32NT, "Client")]
        [TestCase(PlatformID.Win32NT, "Server")]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_4_Different_Roles_Provided(PlatformID platform, string role)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                component.Parameters["Role"] = role;

                string expectedContentType = HttpContentType.Binary;
                string expectedContentEncoding = Encoding.ASCII.WebName;
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedFileName = this.mockFile.Object.Name;
                string expectedBlobPath = $"{component.AgentId}/{component.TypeName}/{role}/{component.Scenario}/{this.mockFile.Object.Name}".ToLowerInvariant();

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(component, this.mockFile.Object, expectedContentType, expectedContentEncoding);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_5_ToolName_And_Timestamp_Provided(PlatformID platform)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                DateTime expectedFileTimestamp = DateTime.UtcNow;
                string expectedContentType = HttpContentType.Binary;
                string expectedContentEncoding = Encoding.ASCII.WebName;
                string expectedToolName = "ToolABC";
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedFileName = $"{expectedFileTimestamp.ToString("yyyy-MM-ddTHH-mm-ss-fffffK")}-{this.mockFile.Object.Name.ToLowerInvariant()}";
                string expectedBlobPath = $"{component.AgentId}/{expectedToolName}/{component.Scenario}/".ToLowerInvariant() + expectedFileName;

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(
                    component, this.mockFile.Object, expectedContentType, expectedContentEncoding, toolname: expectedToolName, fileTimestamp: expectedFileTimestamp);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, "Client")]
        [TestCase(PlatformID.Unix, "Server")]
        [TestCase(PlatformID.Win32NT, "Client")]
        [TestCase(PlatformID.Win32NT, "Server")]
        public void FileUploadDescriptorFactoryCreatesTheExpectedDescriptor_Scenario_6_ToolName_Timestamp_And_Roles_Provided(PlatformID platform, string role)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                component.Parameters["Role"] = role;

                DateTime expectedFileTimestamp = DateTime.UtcNow;
                string expectedContentType = HttpContentType.Binary;
                string expectedContentEncoding = Encoding.ASCII.WebName;
                string expectedToolName = "ToolABC";
                string expectedFilePath = this.mockFile.Object.FullName;
                string expectedFileName = $"{expectedFileTimestamp.ToString("yyyy-MM-ddTHH-mm-ss-fffffK")}-{this.mockFile.Object.Name.ToLowerInvariant()}";
                string expectedBlobPath = $"{component.AgentId}/{expectedToolName}/{role}/{component.Scenario}/".ToLowerInvariant() + expectedFileName;

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(
                    component, this.mockFile.Object, expectedContentType, expectedContentEncoding, toolname: expectedToolName, fileTimestamp: expectedFileTimestamp);

                Assert.AreEqual(component.ExperimentId.ToLowerInvariant(), descriptor.ContainerName);
                Assert.AreEqual(expectedBlobPath, descriptor.BlobName);
                Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding);
                Assert.AreEqual(expectedContentType, descriptor.ContentType);
                Assert.AreEqual(expectedFilePath, descriptor.FilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void FileUploadDescriptorFactoryCreatesTheExpectedFileManifest_Scenario_1(PlatformID platform)
        {
            this.SetupDefaults(platform);

            using (TestExecutor component = new TestExecutor(this.mockFixture))
            {
                component.Metadata.AddRange(new Dictionary<string, IConvertible>
                {
                    { "ExperimentName", "Any_Experiment_101" },
                    { "Cluster", "Cluster01" },
                    { "NodeId", "Node02" }
                });

                component.Parameters.AddRange(new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "Value1" },
                    { "Parameter2", "00:10:00" },
                    { "Parameter3", 12345 }
                });

                FileUploadDescriptor descriptor = this.descriptorFactory.CreateDescriptor(component, this.mockFile.Object, "text/plain", "utf-8");

                string json = descriptor.ToJson();

                // The default metadata
                Assert.IsNotEmpty(descriptor.Manifest);
                Assert.IsTrue(descriptor.Manifest.Count == 14);
                Assert.IsTrue(descriptor.Manifest.ContainsKey("AgentId"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("ExperimentId"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("Platform"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("ToolName"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("Scenario"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("FileName"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("FileCreationTime"));
                Assert.IsTrue(descriptor.Manifest.ContainsKey("FileCreationTimeUtc"));
                Assert.AreEqual(descriptor.Manifest["AgentId"], component.AgentId);
                Assert.AreEqual(descriptor.Manifest["ExperimentId"], component.ExperimentId);
                Assert.AreEqual(descriptor.Manifest["Platform"], component.PlatformSpecifics.PlatformArchitectureName);
                Assert.AreEqual(descriptor.Manifest["ToolName"], component.TypeName);
                Assert.AreEqual(descriptor.Manifest["Scenario"], component.Scenario);
                Assert.AreEqual(descriptor.Manifest["FileName"], this.mockFile.Object.Name);
                Assert.AreEqual(descriptor.Manifest["FileCreationTime"], this.mockFile.Object.CreationTime.ToString("o"));
                Assert.AreEqual(descriptor.Manifest["FileCreationTimeUtc"], this.mockFile.Object.CreationTimeUtc.ToString("o"));

                // Metadata from the user on the command line.
                foreach (var entry in component.Metadata)
                {
                    Assert.IsTrue(descriptor.Manifest.ContainsKey(entry.Key));
                    Assert.AreEqual(descriptor.Manifest[entry.Key], entry.Value);
                }

                // Parameters for the component itself
                foreach (var entry in component.Parameters)
                {
                    Assert.IsTrue(descriptor.Manifest.ContainsKey(entry.Key));
                    Assert.AreEqual(descriptor.Manifest[entry.Key], entry.Value);
                }
            }
        }
    }
}
