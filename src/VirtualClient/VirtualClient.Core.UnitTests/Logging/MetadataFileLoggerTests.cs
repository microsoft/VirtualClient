// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MetadataFileLoggerTests
    {
        private MockFixture mockFixture;

        public void SetupTest(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, useUnixStylePathsOnly: true);
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void MetadataFileLoggerIdentifiesTheExpectedMetadata(PlatformID platform)
        {
            this.SetupTest(platform);

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, null);
            IDictionary<string, IConvertible> metadata = logger.GetMetadata();

            Assert.IsTrue(metadata.ContainsKey("clientId"));
            Assert.IsTrue(metadata.ContainsKey("experimentId"));
            Assert.IsTrue(metadata.ContainsKey("machineName"));
            Assert.IsTrue(metadata.ContainsKey("platformArchitecture"));
            Assert.IsTrue(metadata.ContainsKey("operatingSystemVersion"));
            Assert.IsTrue(metadata.ContainsKey("operatingSystemDescription"));
            Assert.IsTrue(metadata.ContainsKey("timestamp"));
            Assert.IsTrue(metadata.ContainsKey("timezone"));

            ISystemInfo systemInfo = this.mockFixture.Dependencies.GetService<ISystemInfo>();
            PlatformSpecifics platformSpecifics = this.mockFixture.PlatformSpecifics;

            Assert.AreEqual(systemInfo.AgentId, metadata["clientId"]);
            Assert.AreEqual(systemInfo.ExperimentId, metadata["experimentId"]);
            Assert.AreEqual(Environment.MachineName, metadata["machineName"]);
            Assert.AreEqual(platformSpecifics.PlatformArchitectureName, metadata["platformArchitecture"]);
            Assert.AreEqual(Environment.OSVersion.ToString(), metadata["operatingSystemVersion"]);
            Assert.AreEqual(RuntimeInformation.OSDescription, metadata["operatingSystemDescription"]);
            Assert.AreEqual(TimeZoneInfo.Local.StandardName, metadata["timezone"]);
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void MetadataFileLoggerWritesTheExpectedMetadataToFile(PlatformID platform)
        {
            this.SetupTest(platform);

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, null);
            IDictionary<string, IConvertible> expectedMetadata = logger.GetMetadata();

            this.mockFixture.FileSystem
                .Setup(fs => fs.Path.GetFullPath(It.IsAny<string>()))
                .Returns<string>(path => Path.GetFullPath(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((filePath, content) =>
                {
                    Assert.IsTrue(content.Contains("clientId"));
                    Assert.IsTrue(content.Contains("experimentId"));
                    Assert.IsTrue(content.Contains("machineName"));
                    Assert.IsTrue(content.Contains("platformArchitecture"));
                    Assert.IsTrue(content.Contains("operatingSystemVersion"));
                    Assert.IsTrue(content.Contains("operatingSystemDescription"));
                    Assert.IsTrue(content.Contains("timestamp"));
                    Assert.IsTrue(content.Contains("timezone"));
                });

            logger.WriteMetadataFile(expectedMetadata);
        }

        [Test]
        [Platform("Win")]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void MetadataFileLoggerWritesTheMetadataToTheExpectedFilePathLocation(PlatformID platform)
        {
            this.SetupTest(platform);

            string expectedFilePath = this.mockFixture.PlatformSpecifics.GetLogsPath("metadata.log");
            bool confirmed = false;

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, null);
            var metadata = new Dictionary<string, IConvertible>
            {
                { "any", "metadata" }
            };

            this.mockFixture.FileSystem
                .Setup(fs => fs.Path.GetFullPath(It.IsAny<string>()))
                .Returns<string>(path => Path.GetFullPath(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((actualFilePath, content) =>
                {
                    Assert.AreEqual(expectedFilePath, actualFilePath);
                    confirmed = true;
                });

            logger.WriteMetadataFile(metadata);
            Assert.IsTrue(confirmed);
        }

        [Test]
        [Platform("Win")]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public void MetadataFileLoggerWritesTheMetadataToTheExpectedFilePathLocationWhenAFileNameIsDefined(PlatformID platform)
        {
            this.SetupTest(platform);

            string expectedFileName = "marker.log";
            string expectedFilePath = this.mockFixture.PlatformSpecifics.GetLogsPath(expectedFileName);
            bool confirmed = false;

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, expectedFileName);
            var metadata = new Dictionary<string, IConvertible>
            {
                { "any", "metadata" }
            };

            this.mockFixture.FileSystem.Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns(null as string);

            this.mockFixture.FileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((actualFilePath, content) =>
                {
                    Assert.AreEqual(expectedFilePath, actualFilePath);
                    confirmed = true;
                });

            logger.WriteMetadataFile(metadata);
            Assert.IsTrue(confirmed);
        }

        [Test]
        [Platform("Win")]
        [TestCase(".\\logs\\marker.log")]
        [TestCase("..\\..\\logs\\marker.log")]
        [TestCase("..\\..\\test\\marker.log")]
        public void MetadataFileLoggerWritesTheMetadataToTheExpectedFilePathLocationWhenARelativeFilePathIsDefined(string relativeFilePath)
        {
            this.SetupTest(PlatformID.Win32NT);

            string expectedFilePath = Path.GetFullPath(relativeFilePath);
            bool confirmed = false;

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, relativeFilePath);
            var metadata = new Dictionary<string, IConvertible>
            {
                { "any", "metadata" }
            };

            this.mockFixture.FileSystem.Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(path => Path.GetDirectoryName(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.Path.GetFullPath(It.IsAny<string>()))
                .Returns<string>(path => Path.GetFullPath(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((actualFilePath, content) =>
                {
                    Assert.AreEqual(expectedFilePath, actualFilePath);
                    confirmed = true;
                });

            logger.WriteMetadataFile(metadata);
            Assert.IsTrue(confirmed);
        }

        [Test]
        [Platform("Win")]
        [TestCase("C:\\Users\\User\\logs\\marker.log")]
        [TestCase("S:\\Users\\User\\test\\marker.log")]
        public void MetadataFileLoggerWritesTheMetadataToTheExpectedFilePathLocationWhenAFullFilePathIsDefined(string relativeFilePath)
        {
            this.SetupTest(PlatformID.Win32NT);

            string expectedFilePath = Path.GetFullPath(relativeFilePath);
            bool confirmed = false;

            var logger = new TestMetadataFileLogger(this.mockFixture.Dependencies, relativeFilePath);
            var metadata = new Dictionary<string, IConvertible>
            {
                { "any", "metadata" }
            };

            this.mockFixture.FileSystem.Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(path => Path.GetDirectoryName(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.Path.GetFullPath(It.IsAny<string>()))
                .Returns<string>(path => Path.GetFullPath(path));

            this.mockFixture.FileSystem
                .Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((actualFilePath, content) =>
                {
                    Assert.AreEqual(expectedFilePath, actualFilePath);
                    confirmed = true;
                });

            logger.WriteMetadataFile(metadata);
            Assert.IsTrue(confirmed);
        }

        private class TestMetadataFileLogger : MetadataFileLogger
        {
            public TestMetadataFileLogger(IServiceCollection dependencies, string filePath)
                : base(dependencies, filePath)
            {
            }

            public new IDictionary<string, IConvertible> GetMetadata()
            {
                return base.GetMetadata();
            }

            public new void WriteMetadataFile(IDictionary<string, IConvertible> metadata)
            {
                base.WriteMetadataFile(metadata);
            }
        }
    }
}