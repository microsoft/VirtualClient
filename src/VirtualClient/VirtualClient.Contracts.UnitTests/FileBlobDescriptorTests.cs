// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FileBlobDescriptorTests
    {
        ////private MockFixture mockFixture;
        ////private Mock<IFileInfo> mockFileInfo;

        ////[SetUp]
        ////public void SetupTest()
        ////{
        ////    this.mockFixture = new MockFixture();
        ////    this.mockFileInfo = new Mock<IFileInfo>();
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario1()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedRoundTripDate}-{expectedFileName.ToLowerInvariant()}";

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario2()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedAgentId = "Agent01";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedAgentId}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        expectedAgentId);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario3()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedAgentId = "Agent01";
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedAgentId}/{expectedComponentName}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        expectedAgentId,
        ////        expectedComponentName);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario4()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedAgentId = "Agent01";
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        expectedAgentId,
        ////        expectedComponentName,
        ////        expectedComponentScenario);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario5()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedAgentId = "Agent01";
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRole = "Server";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedAgentId}-{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        expectedAgentId,
        ////        expectedComponentName,
        ////        expectedComponentScenario,
        ////        expectedRole);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario6()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRole = "Server";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        componentName: expectedComponentName,
        ////        componentScenario: expectedComponentScenario,
        ////        role: expectedRole);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario7()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRole = "Server";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");
        ////    string expectedSubDirectory = "/any/sub/directory";

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedSubDirectory.Trim('/')}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        componentName: expectedComponentName,
        ////        componentScenario: expectedComponentScenario,
        ////        role: expectedRole,
        ////        subPath: expectedSubDirectory);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario7_Windows()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRole = "Server";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");
        ////    string expectedSubDirectory = @"\any\sub\directory";

        ////    string filePath = $@"C:\Users\User\virtualclient\packages\speccpu\logs\{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/{expectedSubDirectory.Replace("\\", "/").Trim('/')}/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        componentName: expectedComponentName,
        ////        componentScenario: expectedComponentScenario,
        ////        role: expectedRole,
        ////        subPath: expectedSubDirectory);

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario8()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRole = "Server";
        ////    string expectedFileName = "anyfile.log";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    string filePath = $"/home/user/virtualclient/packages/speccpu/a/b/logs/{expectedFileName}";
        ////    string expectedBlobPath = $"{expectedRole}/{expectedComponentName}/{expectedComponentScenario}/b/logs/{expectedRoundTripDate}-{expectedFileName}"
        ////        .ToLowerInvariant().Replace(":", "_");

        ////    this.mockFileInfo.Setup(file => file.FullName).Returns(filePath);
        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    FileBlobDescriptor blob = FileBlobDescriptor.ToBlobDescriptor(
        ////        this.mockFileInfo.Object,
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        componentName: expectedComponentName,
        ////        componentScenario: expectedComponentScenario,
        ////        role: expectedRole,
        ////        subPathAfter: $"/home/user/virtualclient/packages/speccpu/a");

        ////    Assert.AreEqual(expectedExperimentId, blob.ContainerName);
        ////    Assert.AreEqual(expectedBlobPath, blob.Name);
        ////    Assert.AreEqual(expectedContentType, blob.ContentType);
        ////}

        ////[Test]
        ////public void FileBlobDescriptorCreatesTheExpectedPathForAGivenFileAndSetOfDescriptors_Scenario8_2()
        ////{
        ////    DateTime expectedFileCreationTime = DateTime.UtcNow;
        ////    string expectedContentType = HttpContentType.PlainText;
        ////    string expectedExperimentId = Guid.NewGuid().ToString();
        ////    string expectedAgentId = "agent01";
        ////    string expectedComponentName = "anymonitor";
        ////    string expectedComponentScenario = "Scenario01";
        ////    string expectedRoundTripDate = expectedFileCreationTime.ToString("O");

        ////    List<string> filePaths = new List<string>
        ////    {
        ////        "/home/user/virtualclient/packages/speccpu/a/b/logs/log1.txt",
        ////        "/home/user/virtualclient/packages/speccpu/a/b/c/logs/log1.txt",
        ////        "/home/user/virtualclient/packages/speccpu/a/b/d/logs/log1.txt",
        ////        "/home/user/virtualclient/packages/speccpu/a/b/c/d/e/logs/log1.txt"
        ////    };

        ////    this.mockFileInfo.SetupSequence(file => file.FullName)
        ////        .Returns(filePaths[0])
        ////        .Returns(filePaths[1])
        ////        .Returns(filePaths[2])
        ////        .Returns(filePaths[3]);

        ////    this.mockFileInfo.Setup(file => file.CreationTimeUtc).Returns(expectedFileCreationTime);

        ////    IEnumerable<FileBlobDescriptor> descriptors = FileBlobDescriptor.ToBlobDescriptors(
        ////        new List<IFileInfo>
        ////        {
        ////            // One file object per file path above. The setups will account for the different
        ////            // paths and creation times.
        ////            this.mockFileInfo.Object,
        ////            this.mockFileInfo.Object,
        ////            this.mockFileInfo.Object,
        ////            this.mockFileInfo.Object
        ////        },
        ////        expectedContentType,
        ////        expectedExperimentId,
        ////        expectedAgentId,
        ////        componentName: expectedComponentName,
        ////        componentScenario: expectedComponentScenario,
        ////        subPathAfter: $"/home/user/virtualclient/packages/speccpu/a");

        ////    Assert.IsNotEmpty(descriptors);
        ////    Assert.IsTrue(descriptors.Count() == filePaths.Count);
        ////    Assert.IsTrue(descriptors.All(desc => desc.ContainerName == expectedExperimentId));
        ////    Assert.IsTrue(descriptors.All(desc => desc.ContentType == expectedContentType));

        ////    Assert.AreEqual(
        ////        $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/b/logs/{expectedRoundTripDate}-log1.txt".ToLowerInvariant().Replace(":", "_"),
        ////        descriptors.ElementAt(0).Name);

        ////    Assert.AreEqual(
        ////        $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/b/c/logs/{expectedRoundTripDate}-log1.txt".ToLowerInvariant().Replace(":", "_"),
        ////        descriptors.ElementAt(1).Name);

        ////    Assert.AreEqual(
        ////       $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/b/d/logs/{expectedRoundTripDate}-log1.txt".ToLowerInvariant().Replace(":", "_"),
        ////       descriptors.ElementAt(2).Name);

        ////    Assert.AreEqual(
        ////      $"{expectedAgentId}/{expectedComponentName}/{expectedComponentScenario}/b/c/d/e/logs/{expectedRoundTripDate}-log1.txt".ToLowerInvariant().Replace(":", "_"),
        ////      descriptors.ElementAt(3).Name);
        ////}
    }
}