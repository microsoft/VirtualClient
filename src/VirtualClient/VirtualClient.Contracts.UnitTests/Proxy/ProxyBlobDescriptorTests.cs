// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ProxyBlobDescriptorTests
    {
        [Test]
        public void ProxyBlobDescriptorObjectsAreJsonSerializable()
        {
            ProxyBlobDescriptor descriptor = new ProxyBlobDescriptor(
                "Packages",
                "any.package.1.2.3.zip",
                "anycontainer",
                "application/octet-stream",
                "utf8",
                blobPath: "/any/path/to/blob");

            SerializationAssert.IsJsonSerializable<ProxyBlobDescriptor>(descriptor);

            descriptor = new ProxyBlobDescriptor(
                "Packages",
                "any.package.1.2.3.zip",
                "anycontainer",
                "application/octet-stream",
                "utf8",
                blobPath: "/any/path/to/blob",
                source: "VirtualClient");

            SerializationAssert.IsJsonSerializable<ProxyBlobDescriptor>(descriptor);
        }

        [Test]
        [TestCase("blobname.log", false)]
        [TestCase("/blobname.log", false)]
        [TestCase("/any/blobname.log", true)]
        [TestCase("any/blobname.log", true)]
        [TestCase("/any/path/to/blob/blobname.log", true)]
        [TestCase("any/path/to/blob/blobname.log", true)]
        public void ProxyBlobDescriptorCorrectlyIdentifiesWhenAPathExistsInABlobName(string fullBlobName, bool hasBlobPath)
        {
            Assert.AreEqual(hasBlobPath, ProxyBlobDescriptor.TryGetBlobPath(fullBlobName, out string actualBlobName, out string actualBlobPath));
        }

        [Test]
        [TestCase("/any/blobname.log", "blobname.log", "/any")]
        [TestCase("any/blobname.log", "blobname.log", "/any")]
        [TestCase("/any/path/to/blob/blobname.log", "blobname.log", "/any/path/to/blob")]
        [TestCase("any/path/to/blob/blobname.log", "blobname.log", "/any/path/to/blob")]
        public void ProxyBlobDescriptorParsesPathsFromBlobNamesCorrectly(string fullBlobName, string expectedBlobName, string expectedBlobPath)
        {
            Assert.IsTrue(ProxyBlobDescriptor.TryGetBlobPath(fullBlobName, out string actualBlobName, out string actualBlobPath));
            Assert.AreEqual(expectedBlobName, actualBlobName);
            Assert.AreEqual(expectedBlobPath, actualBlobPath);
        }
    }
}
