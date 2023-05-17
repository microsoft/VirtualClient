// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using AutoFixture;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class FileUploadDescriptorTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void FileUploadDescriptorConstructorsValidateRequiredParameters_1(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor(invalidParameter, "container", "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", invalidParameter, "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "container", invalidParameter, "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "container", "utf-8", invalidParameter, "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "container", "utf-8", "text/plain", invalidParameter));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("  ")]
        public void FileUploadDescriptorConstructorsValidateRequiredParameters_2(string invalidParameter)
        {
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor(invalidParameter, "blobPath", "container", "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", invalidParameter, "container", "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "blobPath", invalidParameter, "utf-8", "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "blobPath", "container", invalidParameter, "text/plain", "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "blobPath", "container", "utf-8", invalidParameter, "C:\\any\\file.log"));
            Assert.Throws<ArgumentException>(() => new FileUploadDescriptor("blob", "blobPath", "container", "utf-8", "text/plain", invalidParameter));
        }

        [Test]
        public void FileUploadDescriptorConstructorsSetPropertiesToExpectedValues_1()
        {
            string expectedBlobName = "file.log";
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

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.IsNull(instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsEmpty(instance.Manifest);

            instance = new FileUploadDescriptor(expectedBlobName, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.IsNull(instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsNotEmpty(instance.Manifest);
            Assert.IsTrue(instance.Manifest.ContainsKey("experimentId"));
            Assert.IsTrue(instance.Manifest.TryGetValue("agentId", out IConvertible agent) && agent.ToString() == "Agent01");
            Assert.IsTrue(instance.Manifest.TryGetValue("toolName", out IConvertible tool) && tool.ToString() == "NTttcp");
            Assert.IsTrue(instance.Manifest.TryGetValue("component", out IConvertible component) && component.ToString() == "NTttcpExecutor");
        }

        [Test]
        public void FileUploadDescriptorConstructorsSetPropertiesToExpectedValues_2()
        {
            string expectedBlobName = "file.log";
            string expectedBlobPath = "/any/path/to/blob";
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

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.AreEqual(expectedBlobPath, instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsEmpty(instance.Manifest);

            instance = new FileUploadDescriptor(expectedBlobName, expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);

            Assert.AreEqual(expectedBlobName, instance.BlobName);
            Assert.AreEqual(expectedBlobPath, instance.BlobPath);
            Assert.AreEqual(expectedContainer, instance.ContainerName);
            Assert.AreEqual(expectedContentEncoding, instance.ContentEncoding);
            Assert.AreEqual(expectedContentType, instance.ContentType);
            Assert.AreEqual(expectedFilePath, instance.FilePath);
            Assert.IsNotNull(instance.Manifest);
            Assert.IsNotEmpty(instance.Manifest);
            Assert.IsTrue(instance.Manifest.ContainsKey("experimentId"));
            Assert.IsTrue(instance.Manifest.TryGetValue("agentId", out IConvertible agent) && agent.ToString() == "Agent01");
            Assert.IsTrue(instance.Manifest.TryGetValue("toolName", out IConvertible tool) && tool.ToString() == "NTttcp");
            Assert.IsTrue(instance.Manifest.TryGetValue("component", out IConvertible component) && component.ToString() == "NTttcpExecutor");
        }

        [Test]
        [TestCase("blob01")]
        [TestCase("/blob01")]
        [TestCase("blob01/")]
        [TestCase("/blob01/")]
        public void FileUploadDescriptorConstructorsHandleBlobNamesWithVariousPathSeparators(string blobName)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor(blobName, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("blob01", descriptor.BlobName);
        }

        [Test]
        [TestCase("any/blob/path")]
        [TestCase("/any/blob/path")]
        [TestCase("any/blob/path/")]
        [TestCase("/any/blob/path/")]
        public void FileUploadDescriptorConstructorsHandleBlobPathsWithVariousPathSeparators(string blobPath)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor("name", blobPath, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("/any/blob/path", descriptor.BlobPath);
        }

        [Test]
        [TestCase("blob01", "any/blob/path")]
        [TestCase("/blob01", "/any/blob/path")]
        [TestCase("blob01/", "any/blob/path/")]
        [TestCase("/blob01/", "/any/blob/path/")]
        public void FileUploadDescriptorBlobNameAndPathAreEasilyCombinable(string blobName, string blobPath)
        {
            FileUploadDescriptor descriptor = new FileUploadDescriptor(blobName, blobPath, "container", "utf-8", "text/plain", "/any/path/to/file.txt");
            Assert.AreEqual("/any/blob/path/blob01", $"{descriptor.BlobPath}/{descriptor.BlobName}");
        }

        [Test]
        public void FileUploadDescriptorObjectsAreJsonSerializable()
        {
            FileUploadDescriptor instance1 = new FileUploadDescriptor("Blob", "Container", "utf-8", "text/plain", "C:\\any\\file.log");
            SerializationAssert.IsJsonSerializable<FileUploadDescriptor>(instance1);

            FileUploadDescriptor instance2 = new FileUploadDescriptor("Blob", "Path", "Container", "utf-8", "text/plain", "C:\\any\\file.log");
            SerializationAssert.IsJsonSerializable<FileUploadDescriptor>(instance2);
        }

        [Test]
        public void FileUploadDescriptorObjectsAreJsonSerializable_WithManifest()
        {
            IDictionary<string, IConvertible> manifest = new Dictionary<string, IConvertible>
            {
                { "experimentId", Guid.NewGuid().ToString() },
                { "agentId", "Agent01" },
                { "toolName", "NTttcp" },
                { "component", "NTttcpExecutor" }
            };

            FileUploadDescriptor instance = new FileUploadDescriptor("Blob", "Container", "utf-8", "text/plain", "C:\\any\\file.log", manifest);
            SerializationAssert.IsJsonSerializable<FileUploadDescriptor>(instance);
        }

        [Test]
        public void ToBlobDescriptorCreatesTheExpectedDescriptor_1()
        {
            string expectedBlobName = "file.log";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.log";

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);
            BlobDescriptor descriptor = instance.ToBlobDescriptor();

            Assert.AreEqual(expectedBlobName, descriptor.Name);
            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding.WebName);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
        }

        [Test]
        public void ToBlobDescriptorCreatesTheExpectedDescriptor_2()
        {
            string expectedBlobName = "file.log";
            string expectedBlobPath = "/any/path/to/blob";
            string expectedContainer = "AnyContainer";
            string expectedContentEncoding = "utf-8";
            string expectedContentType = "text/plain";
            string expectedFilePath = "C:\\any\\file.log";

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath);
            BlobDescriptor descriptor = instance.ToBlobDescriptor();

            Assert.AreEqual($"{expectedBlobPath}/{expectedBlobName}", descriptor.Name);
            Assert.AreEqual(expectedContainer, descriptor.ContainerName);
            Assert.AreEqual(expectedContentEncoding, descriptor.ContentEncoding.WebName);
            Assert.AreEqual(expectedContentType, descriptor.ContentType);
        }

        [Test]
        public void ToBlobManifestDescriptorCreatesTheExpectedManifestDescriptor_1()
        {
            string expectedBlobName = "file.log";
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

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);
            BlobDescriptor descriptor = instance.ToBlobManifestDescriptor(out Stream manifestStream);

            using (manifestStream)
            {
                Assert.AreEqual("file.manifest", descriptor.Name);
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
        public void ToBlobManifestDescriptorCreatesTheExpectedManifestDescriptor_2()
        {
            string expectedBlobName = "file.log";
            string expectedBlobPath = "/any/path/to/blob";
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

            FileUploadDescriptor instance = new FileUploadDescriptor(expectedBlobName, expectedBlobPath, expectedContainer, expectedContentEncoding, expectedContentType, expectedFilePath, expectedManifest);
            BlobDescriptor descriptor = instance.ToBlobManifestDescriptor(out Stream manifestStream);

            using (manifestStream)
            {
                Assert.AreEqual($"{expectedBlobPath}/file.manifest", descriptor.Name);
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
