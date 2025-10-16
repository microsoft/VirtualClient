// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class UploadFilesTests : MockFixture
    {
        private string[] mockFiles;

        public void SetupTest(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.Setup(platform, architecture);

            this.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(UploadFiles.TargetDirectory), this.GetLogsPath() },
            };

            // Setup:
            // The target directory exists
            this.FileSystem.Setup(fs => fs.Directory.Exists(this.GetLogsPath()))
                .Returns(true);

            // Setup:
            // Files exist in the target directory.
            this.mockFiles = new string[]
            {
                this.GetLogsPath("file1.log"),
                this.GetLogsPath("directory1", "file2.log"),
                this.GetLogsPath("directory1", "directory2", "file3.log")
            };

            this.FileSystem.Setup(fs => fs.Directory.GetFiles(this.GetLogsPath(), "*.*", System.IO.SearchOption.AllDirectories))
                .Returns(() => this.mockFiles);

            // Setup:
            // The directory for each of the files.
            this.FileSystem.Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(path =>
                {
                    int indexOfLastSegment = path.LastIndexOf(platform == PlatformID.Unix ? '/' : '\\');
                    string directory = path.Substring(0, indexOfLastSegment);

                    return directory;
                });
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task UploadFilesUploadsTheExpectedFilesInTheTargetDirectory(PlatformID platform)
        {
            this.SetupTest(platform);

            string[] expectedLogFiles = this.mockFiles;
            var descriptorsUploaded = new List<FileUploadDescriptor>();

            // Setup:
            // The component is expected to write a request file to content uploads directory
            // for processing by the FileUploadMonitor in the background. This request file contains
            // the file path details and the target blob path location.
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => descriptorsUploaded.Add(content.FromJson<FileUploadDescriptor>()));

            using (var component = new UploadFiles(this.Dependencies, this.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(descriptorsUploaded.Count == 3);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.FilePath == expectedLogFiles[0]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.FilePath == expectedLogFiles[1]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.FilePath == expectedLogFiles[2]) == 1);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task UploadFilesUploadsTheFilesInTheTargetDirectoryToTheExpectedDefaultContentPathLocation(PlatformID platform)
        {
            // Scenario:
            // The default content path for file uploads it {experimentId}/{agentId}
            this.SetupTest(platform);

            string expectedExperimentId = this.SystemManagement.Object.ExperimentId.ToLowerInvariant();
            string expectedAgentId = Environment.MachineName.ToLowerInvariant();

            // Setup:
            // The following expected content paths should match with the mock files at the
            // top of this test class.
            string[] expectedBlobPaths = new string[]
            {
                $"/{expectedAgentId}/file1.log",
                $"/{expectedAgentId}/directory1/file2.log",
                $"/{expectedAgentId}/directory1/directory2/file3.log"
            };

            var descriptorsUploaded = new List<FileUploadDescriptor>();

            // Setup:
            // The component is expected to write a request file to content uploads directory
            // for processing by the FileUploadMonitor in the background. This request file contains
            // the file path details and the target blob path location.
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => descriptorsUploaded.Add(content.FromJson<FileUploadDescriptor>()));

            using (var component = new UploadFiles(this.Dependencies, this.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(descriptorsUploaded.Count == 3);
                Assert.IsTrue(descriptorsUploaded.All(desc => desc.ContainerName == expectedExperimentId));
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[0]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[1]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[2]) == 1);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task UploadFilesUploadsTheFilesInTheTargetDirectoryToTheExpectedContentPathLocationWhenDefined(PlatformID platform)
        {
            // Scenario:
            // The default content path for file uploads it {experimentId}/{agentId}
            this.SetupTest(platform);

            string expectedExperimentId = this.SystemManagement.Object.ExperimentId.ToLowerInvariant();
            string expectedAgentId = Environment.MachineName.ToLowerInvariant();
            string expectedContentPath = "{experimentId}/{agentId}/custom_workload";

            // Setup:
            // The following expected content paths should match with the mock files at the
            // top of this test class.
            string[] expectedBlobPaths = new string[]
            {
                $"/{expectedAgentId}/custom_workload/file1.log",
                $"/{expectedAgentId}/custom_workload/directory1/file2.log",
                $"/{expectedAgentId}/custom_workload/directory1/directory2/file3.log"
            };

            var descriptorsUploaded = new List<FileUploadDescriptor>();

            // Setup:
            // The component is expected to write a request file to content uploads directory
            // for processing by the FileUploadMonitor in the background. This request file contains
            // the file path details and the target blob path location.
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => descriptorsUploaded.Add(content.FromJson<FileUploadDescriptor>()));

            using (var component = new UploadFiles(this.Dependencies, this.Parameters))
            {
                // Setup:
                // When the user defines the content path, it should affect where the files
                // are uploaded to storage.
                component.ContentPathTemplate = expectedContentPath;

                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(descriptorsUploaded.Count == 3);
                Assert.IsTrue(descriptorsUploaded.All(desc => desc.ContainerName == expectedExperimentId));
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[0]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[1]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[2]) == 1);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        [TestCase(PlatformID.Win32NT)]
        public async Task UploadFilesUploadsTheFilesInTheTargetDirectoryToTheExpectedContentPathLocationWhenFlattened(PlatformID platform)
        {
            this.SetupTest(platform);

            string expectedExperimentId = this.SystemManagement.Object.ExperimentId.ToLowerInvariant();
            string expectedAgentId = Environment.MachineName.ToLowerInvariant();

            // Setup:
            // All subdirectories will be disregarded and the files will be uploaded as 
            // a flat list of files.
            string[] expectedBlobPaths = new string[]
            {
                $"/{expectedAgentId}/file1.log",
                $"/{expectedAgentId}/file2.log",
                $"/{expectedAgentId}/file3.log"
            };

            var descriptorsUploaded = new List<FileUploadDescriptor>();

            // Setup:
            // The component is expected to write a request file to content uploads directory
            // for processing by the FileUploadMonitor in the background. This request file contains
            // the file path details and the target blob path location.
            this.FileSystem
                .Setup(fs => fs.File.WriteAllTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => descriptorsUploaded.Add(content.FromJson<FileUploadDescriptor>()));

            // e.g.
            // ../file1.log
            // ../directory1/file2.log
            // ../directory1/directory2/file3.log
            //
            // to
            // ../file1.log
            // ../file2.log
            // ../file3.log
            this.Parameters[nameof(UploadFiles.Flatten)] = true;

            using (var component = new UploadFiles(this.Dependencies, this.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(descriptorsUploaded.Count == 3);
                Assert.IsTrue(descriptorsUploaded.All(desc => desc.ContainerName == expectedExperimentId));
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[0]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[1]) == 1);
                Assert.IsTrue(descriptorsUploaded.Count(desc => desc.BlobPath == expectedBlobPaths[2]) == 1);
            }
        }
    }
}
