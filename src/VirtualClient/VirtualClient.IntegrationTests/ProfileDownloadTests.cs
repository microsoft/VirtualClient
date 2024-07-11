// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    [TestFixture]
    [Category("Integration")]
    internal class ProfileDownloadTests
    {
        private IServiceCollection dependencies;
        private string profileDownloadDirectory;
        private IDictionary<string, Uri> profiles;

        [SetUp]
        public void SetupTest()
        {
            // The tests within this class are meant to evaluate the code paths that are used to download profiles
            // from a target storage account. All relevant authentication scenarios are being tested including:
            // 1) Blob-Anonymous Read (anonymous auth).
            // 2) Authentication using SAS Tokens.
            // 3) Authentication using Microsoft Entra ID/Apps with a local certificate.
            // 
            // Requirements:
            // In order to execute the tests below, the following requirements must be met.
            // 1) 1 container within a storage account that has blob anonymous read access.
            // 2) 1 container exists within the storage account that has private access.
            // 3) The profile must exist in both of the containers noted above.
            // 4) A SAS URI must be defined to the profile in container #2 above.
            // 5) A Microsoft Entra ID/App must be given "Storage Account Blob Data Reader" access to the storage account.
            // 6) A certificate that can be used to authenticate with the Microsoft Entra ID/App must be installed on the local system.

            this.profileDownloadDirectory = Path.Combine(MockFixture.TestAssemblyDirectory, "profile-download-test");
            this.profiles = new Dictionary<string, Uri>
            {
                { "Public", new Uri("https://virtualclientinternal.blob.core.windows.net/test-public/TEST-CERTS.json") },
                { "PublicWithVirtualPath", new Uri("https://virtualclientinternal.blob.core.windows.net/test/profiles/crc/TEST-CERTS.json") }, 
                { "SasUri", new Uri("https://virtualclientinternal.blob.core.windows.net/test/TEST-CERTS.json?{SAS_HERE}") },
                { "SasUriWithVirtualPath", new Uri("https://virtualclientinternal.blob.core.windows.net/test/profiles/crc/TEST-CERTS.json?{SAS_HERE}") },
                { "MicrosoftEntraId", new Uri("https://virtualclientinternal.blob.core.windows.net/test/TEST-CERTS.json?cid=a6610095-4c4d-4f09-ae0d-60a7f9be5cc9&tid=72f988bf-86f1-41af-91ab-2d7cd011db47&crti=AME&crts=virtualclient.azure.com") },
                { "MicrosoftEntraIdWithVirtualPath", new Uri("https://virtualclientinternal.blob.core.windows.net/test/profiles/crc/TEST-CERTS.json?cid=a6610095-4c4d-4f09-ae0d-60a7f9be5cc9&tid=72f988bf-86f1-41af-91ab-2d7cd011db47&crti=AME&crts=virtualclient.azure.com") }
            };

            this.dependencies = new ServiceCollection();
            this.dependencies.AddSingleton<IFileSystem>(new FileSystem());
            this.dependencies.AddSingleton<ICertificateManager>(new CertificateManager());
        }

        [Test]
        public async Task ProfileDownloadsForPublicAccessUrisAreSupported()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["Public"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }

        [Test]
        public async Task ProfileDownloadsForPublicAccessUrisAreSupported_WithVirtualPaths()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["PublicWithVirtualPath"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }

        [Test]
        public async Task ProfileDownloadsForSasUrisAreSupported()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["SasUri"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }

        [Test]
        public async Task ProfileDownloadsForSasUrisAreSupported_WithVirtualPaths()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["SasUriWithVirtualPath"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }

        [Test]
        public async Task ProfileDownloadsForMicrosoftEntraIdsWithCertificates()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["MicrosoftEntraId"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }

        [Test]
        public async Task ProfileDownloadsForMicrosoftEntraIdsWithCertificates_WithVirtualPaths()
        {
            IFileSystem fileSystem = this.dependencies.GetService<IFileSystem>();
            ICertificateManager certificateManager = this.dependencies.GetService<ICertificateManager>();

            Uri profileUri = this.profiles["MicrosoftEntraIdWithVirtualPath"];
            DependencyProfileReference profile = EndpointUtility.CreateProfileReference(profileUri.ToString(), certificateManager);
            string downloadFilePath = Path.Combine(this.profileDownloadDirectory, profile.ProfileName);

            IProfileManager profileManager = new ProfileManager();
            using (var fs = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                await profileManager.DownloadProfileAsync(profile.ProfileUri, fs, CancellationToken.None, profile.Credentials);
            }

            Assert.IsTrue(fileSystem.File.Exists(downloadFilePath));
        }
    }
}
