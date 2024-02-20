// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using AutoFixture;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class FileUploadDescriptorTests
    {
        private IFixture mockFixture;
        private Mock<IFileInfo> mockFile;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
            this.mockFile = new Mock<IFileInfo>();

            this.mockFile.Setup(file => file.Name).Returns("file1.log");
            this.mockFile.Setup(file => file.CreationTime).Returns(DateTime.Now);
            this.mockFile.Setup(file => file.CreationTimeUtc).Returns(DateTime.UtcNow);
            this.mockFile.Setup(file => file.Length).Returns(12345);
            this.mockFile.Setup(file => file.FullName).Returns("/home/user/virtualclient/logs/anytool/file1.log");
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void FileUploadDescriptorConstructorsValidateRequiredParameters_2(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor(invalidParameter, "container", "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blobPath", invalidParameter, "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blobPath", "container", invalidParameter, "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blobPath", "container", "utf-8", invalidParameter, "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blobPath", "container", "utf-8", "text/plain", invalidParameter));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void FileUploadDescriptorConstructorsSetPropertiesToExpectedValues()
        {
            string expectedBlobName = "file.log";
            string expectedBlobPath = "/any/path/to/blob/file.log";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.log";

            IDictionary<string, IConvertible> expectedManifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.AreEqual(expectedBlobPath, instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsFalse(instance.DeleteOnUpload);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsNotEmpty(instance.Manifest);
            Assert.IsTrue(instance.Manifest.ContainsKey("experimentId"));
            Assert.IsTrue(instance.Manifest.TryGetValue("agentId", out IConvertible agent) && agent.ToString() == "Agent01");
            Assert.IsTrue(instance.Manifest.TryGetValue("toolName", out IConvertible tool) && tool.ToString() == "NTttcp");
            Assert.IsTrue(instance.Manifest.TryGetValue("component", out IConvertible component) && component.ToString() == "NTttcpExecutor");

            instance = new FileUploadDescriptor(expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest, deleteOnUpload: true);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.AreEqual(expectedBlobPath, instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsTrue(instance.DeleteOnUpload);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsNotEmpty(instance.Manifest);
            Assert.IsTrue(instance.Manifest.ContainsKey("experimentId"));
            Assert.IsTrue(instance.Manifest.TryGetValue("agentId", out agent) && agent.ToString() == "Agent01");
            Assert.IsTrue(instance.Manifest.TryGetValue("toolName", out tool) && tool.ToString() == "NTttcp");
            Assert.IsTrue(instance.Manifest.TryGetValue("component", out component) && component.ToString() == "NTttcpExecutor");
        }

        [Test]
        [TestCase("blob01")]
        [TestCase("/blob01")]
        [TestCase("blob01/")]
        [TestCase("/blob01/")]
        [TestCase("\\blob01")]
        [TestCase("blob01\\")]
        [TestCase("\\blob01\\")]
        public void FileUploadDescriptorConstructorsHandleBlobPathsWithVariousPathSeparators_1(string blobPath)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor(blobPath, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("blob01", descriptor.BlobName);
            Assert.AreEqual("/blob01", descriptor.BlobPath);
        }

        [Test]
        [TestCase("any/blob/path/file.txt")]
        [TestCase("/any/blob/path/file.txt")]
        public void FileUploadDescriptorConstructorsHandleBlobPathsWithVariousPathSeparators_2_Linux(string blobPath)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor(blobPath, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("file.txt", descriptor.BlobName);
            Assert.AreEqual("/any/blob/path/file.txt", descriptor.BlobPath);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("any/blob/path/file.txt")]
        [TestCase("/any/blob/path/file.txt")]
        [TestCase("any\\blob\\path\\file.txt")]
        [TestCase("\\any\\blob\\path\\file.txt")]
        [TestCase("any/blob/path\\file.txt")]
        [TestCase("\\any/blob/path\\file.txt")]
        public void FileUploadDescriptorConstructorsHandleBlobPathsWithVariousPathSeparators_2_Windows(string blobPath)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor(blobPath, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("file.txt", descriptor.BlobName);
            Assert.AreEqual("/any/blob/path/file.txt", descriptor.BlobPath);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void FileUploadDescriptorObjectsAreJsonSerializable()
        {
            FileUploadDescriptor instance2 = new FileUploadDescriptor("Path", "Container", "utf-8", "text/plain", "C:\\any\\file.log");
            SerializationAssert.IsJsonSerializable<FileUploadDescriptor>(instance2);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void FileUploadDescriptorObjectsAreJsonSerializable_WithManifest()
        {
            IDictionary<string, IConvertible> manifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor("Path", "Container", "utf-8", "text/plain", "C:\\any\\file.log", manifest);
            SerializationAssert.IsJsonSerializable<FileUploadDescriptor>(instance);
        }

        [Test]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", null, null, null, null, null)]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", "Agent01", null, null, null, null)]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", "Agent01", "Tool.exe", null, null, null)]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", "Agent01", "Tool.exe", "Scenario01", null, null)]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", "Agent01", "Tool.exe", "Scenario01", "--duration=30 --output-text", null)]
        [TestCase("text/plain", "utf-8", "97fd2009-0675-4c1c-89c7-a7715f126be4", "Agent01", "Tool.exe", "Scenario01", "--duration=30 --output-text", "Client")]
        public void CreateManifestCreatesTheExpectedFileManifest_Scenario_1(
            string expectedContentType, string expectedContentEncoding, string expectedExperimentId, string expectedAgentId, string expectedToolName, string expectedScenario, string expectedToolArguments, string expectedRole)
        {
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "Parameter1", "Value1" },
                { "Parameter2", "00:10:00" },
                { "Parameter3", 12345 }
            };

            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "ExperimentName", "Any_Experiment_101" },
                { "Cluster", "Cluster01" },
                { "NodeId", "Node02" }
            };

            FileContext context = new FileContext(
                this.mockFile.Object,
                expectedContentType,
                expectedContentEncoding,
                expectedExperimentId,
                expectedAgentId,
                expectedToolName,
                expectedScenario,
                expectedToolArguments,
                expectedRole);

            string expectedBlobContainer = expectedExperimentId;
            string expectedBlobPath = $"/{string.Join('/', expectedAgentId, expectedToolName, expectedRole, expectedScenario, this.mockFile.Object.Name).TrimStart('/')}";
                
            IDictionary<string, IConvertible> manifest = FileUploadDescriptor.CreateManifest(context, expectedBlobContainer, expectedBlobPath, parameters, metadata);

            // The default metadata
            Assert.IsNotEmpty(manifest); 
            Assert.IsTrue(manifest.Count == 22);
            Assert.IsTrue(manifest.ContainsKey("agentId"));
            Assert.IsTrue(manifest.ContainsKey("appHost"));
            Assert.IsTrue(manifest.ContainsKey("experimentId"));
            Assert.IsTrue(manifest.ContainsKey("role"));
            Assert.IsTrue(manifest.ContainsKey("platform"));
            Assert.IsTrue(manifest.ContainsKey("toolName"));
            Assert.IsTrue(manifest.ContainsKey("toolArguments"));
            Assert.IsTrue(manifest.ContainsKey("scenario"));
            Assert.IsTrue(manifest.ContainsKey("blobPath"));
            Assert.IsTrue(manifest.ContainsKey("blobContainer"));
            Assert.IsTrue(manifest.ContainsKey("contentType"));
            Assert.IsTrue(manifest.ContainsKey("contentEncoding"));
            Assert.IsTrue(manifest.ContainsKey("fileName"));
            Assert.IsTrue(manifest.ContainsKey("fileSizeBytes"));
            Assert.IsTrue(manifest.ContainsKey("fileCreationTime"));
            Assert.IsTrue(manifest.ContainsKey("fileCreationTimeUtc"));

            Assert.AreEqual(expectedAgentId, manifest["agentId"]);
            Assert.AreEqual(Environment.MachineName, manifest["appHost"]);
            Assert.AreEqual(expectedExperimentId, manifest["experimentId"]);
            Assert.AreEqual(expectedRole, manifest["role"]);
            Assert.AreEqual(PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture), manifest["platform"]);
            Assert.AreEqual(expectedToolName, manifest["toolName"]);
            Assert.AreEqual(expectedToolArguments, manifest["toolArguments"]);
            Assert.AreEqual(expectedScenario, manifest["scenario"]);
            Assert.AreEqual(expectedBlobPath, manifest["blobPath"]);
            Assert.AreEqual(expectedBlobContainer, manifest["blobContainer"]);
            Assert.AreEqual(expectedContentType, manifest["contentType"]);
            Assert.AreEqual(expectedContentEncoding, manifest["contentEncoding"]);
            Assert.AreEqual(this.mockFile.Object.Name, manifest["fileName"]);
            Assert.AreEqual(this.mockFile.Object.Length, manifest["fileSizeBytes"]);
            Assert.AreEqual(this.mockFile.Object.CreationTime.ToString("o"), manifest["fileCreationTime"]);
            Assert.AreEqual(this.mockFile.Object.CreationTimeUtc.ToString("o"), manifest["fileCreationTimeUtc"]);

            // Metadata from the user on the command line.
            foreach (var entry in metadata)
            {
                Assert.IsTrue(manifest.ContainsKey(entry.Key));
                Assert.AreEqual(manifest[entry.Key], entry.Value);
            }

            // Parameters for the component itself
            foreach (var entry in parameters)
            {
                Assert.IsTrue(manifest.ContainsKey(entry.Key));
                Assert.AreEqual(manifest[entry.Key], entry.Value);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void ToBlobDescriptorCreatesTheExpectedDescriptor_1()
        {
            string expectedBlobPath = "/any/path/to/blob/file.log";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.log";

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);
            BlobDescriptor descriptor = instance.ToBlobDescriptor();

            Assert.AreEqual(expectedBlobPath, descriptor.Name);
            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding.WebName);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("any/blob/path/file.txt")]
        [TestCase("/any/blob/path/file.txt")]
        [TestCase("any\\blob\\path\\file.txt")]
        [TestCase("\\any\\blob\\path\\file.txt")]
        [TestCase("any/blob/path\\file.txt")]
        [TestCase("\\any/blob/path\\file.txt")]
        public void ToBlobDescriptorCreatesTheExpectedDescriptor_2(string blobPath)
        {
            string expectedBlobPath = $"/any/blob/path/file.txt";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.txt";

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);
            BlobDescriptor descriptor = instance.ToBlobDescriptor();

            Assert.AreEqual(expectedBlobPath, descriptor.Name);
            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding.WebName);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("blob01")]
        [TestCase("/blob01")]
        [TestCase("blob01/")]
        [TestCase("/blob01/")]
        [TestCase("\\blob01")]
        [TestCase("blob01\\")]
        [TestCase("\\blob01\\")]
        public void ToBlobManifestDescriptorCreatesTheExpectedDescriptor_Files_Without_Extensions(string blobPath)
        {
            string expectedBlobPath = $"/blob01";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\blob01";

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);
            BlobDescriptor descriptor = instance.ToBlobDescriptor();

            Assert.AreEqual(expectedBlobPath, descriptor.Name);
            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding.WebName);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void ToBlobManifestDescriptorCreatesTheExpectedManifestDescriptor_1()
        {
            string blobPath = "/any/path/to/blob/file.log";
            string expectedBlobPath = "/any/path/to/blob/file.manifest.json";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.log";

            IDictionary<string, IConvertible> expectedManifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor(blobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);
            BlobDescriptor descriptor = instance.ToBlobManifestDescriptor(out Stream manifestStream);

            using (manifestStream)
            {
                Assert.AreEqual(expectedBlobPath, descriptor.Name);
                Assert.AreEqual(expectedContainer, instance.ContainerName);
                Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
                Assert.AreEqual(expectedContentType, instance.ContentType);
                Assert.IsNotNull(manifestStream);
                Assert.IsTrue(manifestStream.Length > 0);

                using (StreamReader reader = new StreamReader(manifestStream))
                {
                    string descriptorManifest = instance.Manifest.ToJson().RemoveWhitespace();
                    string streamManifest = reader.ReadToEnd().RemoveWhitespace();

                    Assert.AreEqual(descriptorManifest, streamManifest);
                }
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("any/blob/path/file.txt")]
        [TestCase("/any/blob/path/file.txt")]
        [TestCase("any\\blob\\path\\file.txt")]
        [TestCase("\\any\\blob\\path\\file.txt")]
        [TestCase("any/blob/path\\file.txt")]
        [TestCase("\\any/blob/path\\file.txt")]
        public void ToBlobManifestDescriptorCreatesTheExpectedManifestDescriptor_2(string blobPath)
        {
            string expectedBlobPath = $"/any/blob/path/file.manifest.json";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.txt";

            IDictionary<string, IConvertible> expectedManifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor(blobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);
            BlobDescriptor descriptor = instance.ToBlobManifestDescriptor(out Stream manifestStream);

            using (manifestStream)
            {
                Assert.AreEqual(expectedBlobPath, descriptor.Name);
                Assert.AreEqual(expectedContainer, instance.ContainerName);
                Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
                Assert.AreEqual(expectedContentType, instance.ContentType);
                Assert.IsNotNull(manifestStream);
                Assert.IsTrue(manifestStream.Length > 0);

                using (StreamReader reader = new StreamReader(manifestStream))
                {
                    string descriptorManifest = instance.Manifest.ToJson().RemoveWhitespace();
                    string streamManifest = reader.ReadToEnd().RemoveWhitespace();

                    Assert.AreEqual(descriptorManifest, streamManifest);
                }
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase("blob01")]
        [TestCase("/blob01")]
        [TestCase("blob01/")]
        [TestCase("/blob01/")]
        [TestCase("\\blob01")]
        [TestCase("blob01\\")]
        [TestCase("\\blob01\\")]
        public void ToBlobManifestDescriptorCreatesTheExpectedManifestDescriptor_Files_Without_Extensions(string blobPath)
        {
            string expectedBlobPath = $"/blob01.manifest.json";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\blob01";

            IDictionary<string, IConvertible> expectedManifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor(blobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);
            BlobDescriptor descriptor = instance.ToBlobManifestDescriptor(out Stream manifestStream);

            using (manifestStream)
            {
                Assert.AreEqual(expectedBlobPath, descriptor.Name);
                Assert.AreEqual(expectedContainer, instance.ContainerName);
                Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
                Assert.AreEqual(expectedContentType, instance.ContentType);
                Assert.IsNotNull(manifestStream);
                Assert.IsTrue(manifestStream.Length > 0);

                using (StreamReader reader = new StreamReader(manifestStream))
                {
                    string descriptorManifest = instance.Manifest.ToJson().RemoveWhitespace();
                    string streamManifest = reader.ReadToEnd().RemoveWhitespace();

                    Assert.AreEqual(descriptorManifest, streamManifest);
                }
            }
        }
    }
}
