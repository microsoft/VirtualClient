// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO.Abstractions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FileBlobDescriptorTests
    {
        private MockFixture mockFixture;
        private Mock<IFileInfo> mockFileInfo;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFileInfo = new Mock<IFileInfo>();
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario1()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedRoundTripDate}-{expectedFileName}".ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario2()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedAgentId}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                expectedAgentId);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario3()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedComponentName = "anymonitor";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedAgentId}/{expectedComponentName}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                expectedAgentId,
                expectedComponentName);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario4()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedComponentName = "anymonitor";
            string expectedComponentScenario = "Scenario01";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                expectedAgentId,
                expectedComponentName,
                expectedComponentScenario);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario5()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedAgentId = "Agent01";
            string expectedComponentName = "anymonitor";
            string expectedComponentScenario = "Scenario01";
            string expectedRole = "Server";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedAgentId}-{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                expectedAgentId,
                expectedComponentName,
                expectedComponentScenario,
                expectedRole);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario6()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedComponentName = "anymonitor";
            string expectedComponentScenario = "Scenario01";
            string expectedRole = "Server";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                componentName: expectedComponentName,
                componentScenario: expectedComponentScenario,
                role: expectedRole);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario7()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedComponentName = "anymonitor";
            string expectedComponentScenario = "Scenario01";
            string expectedRole = "Server";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");
            string expectedSubDirectory = "/any/sub/directory";

            string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
            string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedSubDirectory.Trim('/')}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                componentName: expectedComponentName,
                componentScenario: expectedComponentScenario,
                role: expectedRole,
                preserveSubDirectory: expectedSubDirectory);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        [Test]
        public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario7_Windows()
        {
            DateTime expectedFileCreationTime = DateTime.UtcNow;
            string expectedContentType = HttpContentType.PlainText;
            string expectedExperimentId = Guid.NewGuid().ToString();
            string expectedComponentName = "anymonitor";
            string expectedComponentScenario = "Scenario01";
            string expectedRole = "Server";
            string expectedFileName = "anyfile.log";
            string expectedRoundTripDate = expectedFileCreationTime.ToString("O");
            string expectedSubDirectory = @"\any\sub\directory";

            string filePath = $@"C:\Users\User\virtualclient\packages\speccpu\logs\{expectedFileName}";
            string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedSubDirectory.Replace("\\", "/").Trim('/')}/{expectedRoundTripDate}-{expectedFileName}"
                .ToLowerInvariant().Replace(":", "_");

            this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
            this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

            FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
                this.mockFileInfo.Object,
                expectedContentType,
                expectedExperimentId,
                componentName: expectedComponentName,
                componentScenario: expectedComponentScenario,
                role: expectedRole,
                preserveSubDirectory: expectedSubDirectory);

            Assert.AreEqual(expectedExperimentId, blob.ContainerName);
            Assert.AreEqual(expectedBlobPath, blob.Name);
            Assert.AreEqual(expectedContentType, blob.ContentType);
        }

        ////[Test]
        ////public void BlobDescriptorCreatesTheExpectedPathForAGivenExperiment_Scenario2()
        ////{
        ////    string componentName = "anymonitor";
        ////    string fileName = "anyfile.log";
        ////    DateTime timestamp = DateTime.UtcNow;

        ////    string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
        ////    BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp);

        ////    Assert.AreEqual(expectedPath, blob.Name);
        ////}

        ////[Test]
        ////public void BlobDescriptorCreatesTheExpectedPathForAGivenExperimentAccountingForDifferentTimestampKinds()
        ////{
        ////    string componentName = "anymonitor";
        ////    string fileName = "anyfile.log";
        ////    DateTime timestamp = DateTime.UtcNow;

        ////    // Timestamp is in local time. It will be converted to UTC.
        ////    string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
        ////    BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp.ToLocalTime());

        ////    Assert.AreEqual(expectedPath, blob.Name);

        ////    // Timestamp is an undefined kind. It will be converted to UTC.
        ////    blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, agentId, componentName, fileName, new DateTime(timestamp.ToLocalTime().Ticks, DateTimeKind.Unspecified));

        ////    Assert.AreEqual(expectedPath, blob.Name);
        ////}

        ////[Test]
        ////public void BlobDescriptorCreatesTheExpectedCasingForPathsCreatedForAGivenExperimentWhenAScenarioIsIncluded()
        ////{
        ////    string componentName = "anymonitor";
        ////    string scenario = "anyscenario";
        ////    string fileName = "anyfile.log";
        ////    DateTime timestamp = DateTime.UtcNow;

        ////    // Blob store paths should always be lower-cased.
        ////    string expectedPath = $"{this.agentId}/{componentName}/{scenario}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
        ////    BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp, directoryPrefix: scenario);

        ////    Assert.AreEqual(expectedPath, blob.Name);
        ////}

        ////[Test]
        ////public void BlobDescriptorUsesTheExpectedCasingForPathsCreatedForAGivenExperiment_1()
        ////{
        ////    string componentName = "anymonitor";
        ////    string fileName = "anyfile.log";
        ////    DateTime timestamp = DateTime.UtcNow;

        ////    // Blob store paths should always be lower-cased.
        ////    string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
        ////    BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId.ToUpperInvariant(), componentName.ToUpperInvariant(), fileName.ToUpperInvariant(), timestamp);

        ////    Assert.AreEqual(expectedPath, blob.Name);
        ////}

        ////[Test]
        ////public void BlobDescriptorUsesTheExpectedCasingForPathsCreatedForAGivenExperiment_2()
        ////{
        ////    string componentName = "anymonitor";
        ////    string scenario = "anyscenario";
        ////    string fileName = "anyfile.log";
        ////    DateTime timestamp = DateTime.UtcNow;

        ////    // Blob store paths should always be lower-cased.
        ////    string expectedPath = $"{this.agentId}/{componentName}/{scenario}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
        ////    BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId.ToUpperInvariant(), componentName.ToUpperInvariant(), fileName.ToUpperInvariant(), timestamp, directoryPrefix: scenario.ToUpperInvariant());

        ////    Assert.AreEqual(expectedPath, blob.Name);
        ////}

        ////[Test]
        ////public void BlobDescriptorCreatesTheExpectedPathsForListOfFilesWithStartDirectoryNotDefined()
        ////{
        ////    string componentName = "anymonitor";
        ////    List<string> filePaths = new List<string>()
        ////    {
        ////        "/dev/a/b/c.txt",
        ////        "/dev/a/b/d.txt",
        ////        "/dev/a/e.txt",
        ////    };

        ////    DateTime startDateTime = DateTime.UtcNow;
        ////    string dateTimeString = startDateTime.ToString("O").Replace(":", "_").ToLowerInvariant();
        ////    string expectedPrefix = $"{this.agentId}/{componentName}".ToLowerInvariant();
        ////    var blobLists = BlobDescriptor.ToBlobDescriptors(
        ////        this.experimentId, this.agentId, componentName, filePaths, timestamp: startDateTime);

        ////    Assert.AreEqual("/dev/a/b/c.txt", blobLists.ElementAt(0).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-c.txt", blobLists.ElementAt(0).Value.Name);
        ////    Assert.AreEqual("/dev/a/b/d.txt", blobLists.ElementAt(1).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-d.txt", blobLists.ElementAt(1).Value.Name);
        ////    Assert.AreEqual("/dev/a/e.txt", blobLists.ElementAt(2).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-e.txt", blobLists.ElementAt(2).Value.Name);
        ////}


        ////[Test]
        ////public void BlobDescriptorCreatesTheExpectedPathsForListOfFilesWithStartDirectoryDefined()
        ////{
        ////    string componentName = "anymonitor";
        ////    List<string> filePaths = new List<string>()
        ////    {
        ////        "/dev/a/b/c.txt",
        ////        "/dev/a/b/d.txt",
        ////        "/dev/a/e.txt",
        ////    };

        ////    string startDirectory = "/dev/a/";
        ////    DateTime startDateTime = DateTime.UtcNow;
        ////    string dateTimeString = startDateTime.ToString("O").Replace(":", "_").ToLowerInvariant();
        ////    string expectedPrefix = $"{this.agentId}/{componentName}".ToLowerInvariant();
        ////    var blobLists = BlobDescriptor.ToBlobDescriptors(
        ////        this.experimentId, this.agentId, componentName, filePaths, timestamp: startDateTime, startDirectory: startDirectory);

        ////    Assert.AreEqual("/dev/a/b/c.txt", blobLists.ElementAt(0).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/b/{dateTimeString}-c.txt", blobLists.ElementAt(0).Value.Name);
        ////    Assert.AreEqual("/dev/a/b/d.txt", blobLists.ElementAt(1).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/b/{dateTimeString}-d.txt", blobLists.ElementAt(1).Value.Name);
        ////    Assert.AreEqual("/dev/a/e.txt", blobLists.ElementAt(2).Key);
        ////    Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-e.txt", blobLists.ElementAt(2).Value.Name);
        ////}
    }
}