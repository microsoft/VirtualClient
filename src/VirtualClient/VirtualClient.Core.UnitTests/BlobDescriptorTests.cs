// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class BlobDescriptorTests
    {
        private const string RoundTripDateTimeExpression = "[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{7}Z";
        private string experimentId = "testExperiment";
        private string agentId= "cluster01,f979bda0-0e88-409b-8911-f1a3be87241e,anyvm-01,03be736c-8a19-4f2c-8d73-3351f2692766";
        private BlobDescriptor description;
        private BlobDescriptor description2;

        [SetUp]
        public void SetupTest()
        {
            this.description = new BlobDescriptor
            {
                Name = "AnyName",
                ContainerName = "AnyContainer",
                PackageName = "AnyPackage",
                ArchiveType = ArchiveType.Zip,
                Extract = true
            };

            this.description2 = new BlobDescriptor
            {
                Name = "AnyOtherName",
                ContainerName = "AnyOtherContainer",
                PackageName = "AnyOtherPackage",
                ArchiveType = ArchiveType.Tgz,
                Extract = true
            };
        }

        [Test]
        public void BlobDescriptorSupportsTheFullRangeOfDifferentEncodings()
        {
            this.description.ContentEncoding = Encoding.UTF8;
            Assert.AreEqual(Encoding.UTF8, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.UTF32;
            Assert.AreEqual(Encoding.UTF32, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.ASCII;
            Assert.AreEqual(Encoding.ASCII, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.BigEndianUnicode;
            Assert.AreEqual(Encoding.BigEndianUnicode, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.Unicode;
            Assert.AreEqual(Encoding.Unicode, this.description.ContentEncoding);

            this.description.ContentEncoding = Encoding.Latin1;
            Assert.AreEqual(Encoding.Latin1, this.description.ContentEncoding);
        }

        [Test]
        public void BlobDescriptorCorrectlyImplementsEqualitySemantics()
        {
            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorEqualitySemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsEqualitySemantics<DependencyDescriptor>(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorCorrectlyImplementsHashcodeSemantics()
        {
            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorHashcodeSemanticsAreNotAffectedByNullPropertyValues()
        {
            this.description.PackageName = null;
            this.description2.PackageName = null;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics(() => this.description, () => this.description2);
        }

        [Test]
        public void BlobDescriptorHashCodesAreNotCaseSensitive()
        {
            DependencyDescriptor instance1 = new DependencyDescriptor(this.description);
            instance1.Name = instance1.Name.ToLowerInvariant();
            instance1.PackageName = instance1.PackageName.ToLowerInvariant();

            DependencyDescriptor instance2 = new DependencyDescriptor(this.description);
            instance1.Name = instance1.Name.ToUpperInvariant();
            instance1.PackageName = instance1.PackageName.ToUpperInvariant();

            Assert.AreEqual(instance1.GetHashCode(), instance2.GetHashCode());
        }

        [Test]
        public void BlobDescriptorCreatesTheExpectedPathForAGivenExperiment_Scenario1()
        {
            string componentName = "anymonitor";
            string fileName = "anyfile.log";

            string expectedPath = $"{this.agentId}/{componentName}/{BlobDescriptorTests.RoundTripDateTimeExpression}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName);

            Assert.IsTrue(Regex.IsMatch(blob.Name, expectedPath, RegexOptions.IgnoreCase));
        }

        [Test]
        public void BlobDescriptorCreatesTheExpectedPathForAGivenExperiment_Scenario2()
        {
            string componentName = "anymonitor";
            string fileName = "anyfile.log";
            DateTime timestamp = DateTime.UtcNow;

            string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp);

            Assert.AreEqual(expectedPath, blob.Name);
        }

        [Test]
        public void BlobDescriptorCreatesTheExpectedPathForAGivenExperimentAccountingForDifferentTimestampKinds()
        {
            string componentName = "anymonitor";
            string fileName = "anyfile.log";
            DateTime timestamp = DateTime.UtcNow;

            // Timestamp is in local time. It will be converted to UTC.
            string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp.ToLocalTime());

            Assert.AreEqual(expectedPath, blob.Name);

            // Timestamp is an undefined kind. It will be converted to UTC.
            blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, agentId, componentName, fileName, new DateTime(timestamp.ToLocalTime().Ticks, DateTimeKind.Unspecified));

            Assert.AreEqual(expectedPath, blob.Name);
        }

        [Test]
        public void BlobDescriptorCreatesTheExpectedCasingForPathsCreatedForAGivenExperimentWhenAScenarioIsIncluded()
        {
            string componentName = "anymonitor";
            string scenario = "anyscenario";
            string fileName = "anyfile.log";
            DateTime timestamp = DateTime.UtcNow;

            // Blob store paths should always be lower-cased.
            string expectedPath = $"{this.agentId}/{componentName}/{scenario}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId, componentName, fileName, timestamp, directoryPrefix: scenario);

            Assert.AreEqual(expectedPath, blob.Name);
        }

        [Test]
        public void BlobDescriptorUsesTheExpectedCasingForPathsCreatedForAGivenExperiment_1()
        {
            string componentName = "anymonitor";
            string fileName = "anyfile.log";
            DateTime timestamp = DateTime.UtcNow;

            // Blob store paths should always be lower-cased.
            string expectedPath = $"{this.agentId}/{componentName}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId.ToUpperInvariant(), componentName.ToUpperInvariant(), fileName.ToUpperInvariant(), timestamp);

            Assert.AreEqual(expectedPath, blob.Name);
        }

        [Test]
        public void BlobDescriptorUsesTheExpectedCasingForPathsCreatedForAGivenExperiment_2()
        {
            string componentName = "anymonitor";
            string scenario = "anyscenario";
            string fileName = "anyfile.log";
            DateTime timestamp = DateTime.UtcNow;

            // Blob store paths should always be lower-cased.
            string expectedPath = $"{this.agentId}/{componentName}/{scenario}/{timestamp.ToString("O")}-{fileName}".ToLowerInvariant().Replace(":", "_");
            BlobDescriptor blob = BlobDescriptor.ToBlobDescriptor(this.experimentId, this.agentId.ToUpperInvariant(), componentName.ToUpperInvariant(), fileName.ToUpperInvariant(), timestamp, directoryPrefix: scenario.ToUpperInvariant());

            Assert.AreEqual(expectedPath, blob.Name);
        }

        [Test]
        public void BlobDescriptorCreatesTheExpectedPathsForListOfFilesWithStartDirectoryNotDefined()
        {
            string componentName = "anymonitor";
            List<string> filePaths = new List<string>()
            {
                "/dev/a/b/c.txt",
                "/dev/a/b/d.txt",
                "/dev/a/e.txt",
            };

            DateTime startDateTime = DateTime.UtcNow;
            string dateTimeString = startDateTime.ToString("O").Replace(":", "_").ToLowerInvariant();
            string expectedPrefix = $"{this.agentId}/{componentName}".ToLowerInvariant();
            var blobLists = BlobDescriptor.ToBlobDescriptors(
                this.experimentId, this.agentId, componentName, filePaths, timestamp: startDateTime);

            Assert.AreEqual("/dev/a/b/c.txt", blobLists.ElementAt(0).Key);
            Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-c.txt", blobLists.ElementAt(0).Value.Name);
            Assert.AreEqual("/dev/a/b/d.txt", blobLists.ElementAt(1).Key);
            Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-d.txt", blobLists.ElementAt(1).Value.Name);
            Assert.AreEqual("/dev/a/e.txt", blobLists.ElementAt(2).Key);
            Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-e.txt", blobLists.ElementAt(2).Value.Name);
        }


        [Test]
        public void BlobDescriptorCreatesTheExpectedPathsForListOfFilesWithStartDirectoryDefined()
        {
            string componentName = "anymonitor";
            List<string> filePaths = new List<string>()
            {
                "/dev/a/b/c.txt",
                "/dev/a/b/d.txt",
                "/dev/a/e.txt",
            };

            string startDirectory = "/dev/a/";
            DateTime startDateTime = DateTime.UtcNow;
            string dateTimeString = startDateTime.ToString("O").Replace(":", "_").ToLowerInvariant();
            string expectedPrefix = $"{this.agentId}/{componentName}".ToLowerInvariant();
            var blobLists = BlobDescriptor.ToBlobDescriptors(
                this.experimentId, this.agentId, componentName, filePaths, timestamp: startDateTime, startDirectory: startDirectory);

            Assert.AreEqual("/dev/a/b/c.txt", blobLists.ElementAt(0).Key);
            Assert.AreEqual($"{expectedPrefix}/b/{dateTimeString}-c.txt", blobLists.ElementAt(0).Value.Name);
            Assert.AreEqual("/dev/a/b/d.txt", blobLists.ElementAt(1).Key);
            Assert.AreEqual($"{expectedPrefix}/b/{dateTimeString}-d.txt", blobLists.ElementAt(1).Value.Name);
            Assert.AreEqual("/dev/a/e.txt", blobLists.ElementAt(2).Key);
            Assert.AreEqual($"{expectedPrefix}/{dateTimeString}-e.txt", blobLists.ElementAt(2).Value.Name);
        }
    }
}