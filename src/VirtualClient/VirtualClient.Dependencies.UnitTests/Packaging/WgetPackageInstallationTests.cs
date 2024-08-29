// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.Packaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class WgetPackageInstallationTests
    {
        private MockFixture fixture;

        public void SetupDefaults(PlatformID platform, Architecture architecture)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);

            if (platform == PlatformID.Unix)
            {
                this.fixture.Parameters = new Dictionary<string, IConvertible>
                {
                    { nameof(WgetPackageInstallation.PackageName), "any-package" },
                    { nameof(WgetPackageInstallation.PackageUri), "https://any.company.com/packages/any-package.1.0.0.tar.gz" }
                };
            }
            else
            {
                this.fixture.Parameters = new Dictionary<string, IConvertible>
                {
                    { nameof(WgetPackageInstallation.PackageName), "any-package" },
                    { nameof(WgetPackageInstallation.PackageUri), "https://any.company.com/packages/any-package.1.0.0.zip" }
                };
            }

            // The wget/wget2 toolset is used to download the packages from the internet.
            this.fixture.PackageManager.OnGetPackage("wget").ReturnsAsync(new DependencyPath("wget", this.fixture.GetPackagePath("wget")));

            // Setup the wget/wget2 files to exist
            this.fixture.File.Setup(file => file.Exists(It.Is<string>(f => f.Contains("wget")))).Returns(true);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void WgetPackageInstallationThrowsIfTheWgetTooletPackageIsNotFoundOnWindows(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            // Setup the case where the package is not found on the system.
            this.fixture.PackageManager.OnGetPackage("wget").ReturnsAsync(null as DependencyPath);

            using (WgetPackageInstallation installation = new WgetPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => installation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.DependencyNotFound, error.Reason);
                Assert.IsTrue(error.Message.StartsWith("Missing required package."));
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task WgetPackageInstallationExecutesTheExpectedOperationsOnUnixSystems(Architecture architecture)
        {
            this.SetupDefaults(PlatformID.Unix, architecture);

            using (WgetPackageInstallation installation = new WgetPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                installation.RetryPolicy = Policy.NoOpAsync();

                string expectedDownloadPath = this.fixture.GetPackagePath("any-package.1.0.0.tar.gz");
                string expectedInstallationPath = this.fixture.GetPackagePath("any-package");

                bool downloadPathConfirmed = false;
                bool installationPathConfirmed = false;
                bool packageRegistrationConfirmed = false;

                // The package will be extracted on the system once it is downloaded.
                // e.g.
                // /packages/any-package.1.0.0.tar.gz -> /packages/any-package.1.0.0
                this.fixture.PackageManager.Setup(mgr => mgr.ExtractPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<ArchiveType>()))
                    .Callback<string, string, CancellationToken, ArchiveType>((filePath, destinationPath, token, archiveType) =>
                    {
                        downloadPathConfirmed = filePath.Equals(expectedDownloadPath);
                        installationPathConfirmed = destinationPath.Equals(expectedInstallationPath);
                    });

                // Once the package is extracted, it is registered on the system with Virtual Client.
                this.fixture.PackageManager.Setup(mgr => mgr.RegisterPackageAsync(It.IsAny<DependencyPath>(), It.IsAny<CancellationToken>()))
                    .Callback<DependencyPath, CancellationToken>((package, token) =>
                    {
                        packageRegistrationConfirmed = package.Name == installation.PackageName && package.Path == expectedInstallationPath;
                    });

                await installation.ExecuteAsync(CancellationToken.None);

                // On Windows, we use the 'wget2' toolset that we compiled from the source code.
                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"wget {installation.PackageUri}"), "Wget download command incorrect.");
                Assert.IsTrue(downloadPathConfirmed, "Archive file path incorrect.");
                Assert.IsTrue(installationPathConfirmed, "Package installation path incorrect.");
                Assert.IsTrue(packageRegistrationConfirmed, "Package registration incorrect.");
            }
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task WgetPackageInstallationExecutesTheExpectedOperationsOnWindowsSystems(Architecture architecture)
        {
            this.SetupDefaults(PlatformID.Win32NT, architecture);

            using (WgetPackageInstallation installation = new WgetPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                installation.RetryPolicy = Policy.NoOpAsync();

                string expectedDownloadPath = this.fixture.GetPackagePath("any-package.1.0.0.zip");
                string expectedInstallationPath = this.fixture.GetPackagePath("any-package");

                bool downloadPathConfirmed = false;
                bool installationPathConfirmed = false;
                bool packageRegistrationConfirmed = false;

                // The package will be extracted on the system once it is downloaded.
                // e.g.
                // /packages/any-package.1.0.0.tar.gz -> /packages/any-package.1.0.0
                this.fixture.PackageManager.Setup(mgr => mgr.ExtractPackageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<ArchiveType>()))
                    .Callback<string, string, CancellationToken, ArchiveType>((filePath, destinationPath, token, archiveType) =>
                    {
                        downloadPathConfirmed = filePath.Equals(expectedDownloadPath);
                        installationPathConfirmed = destinationPath.Equals(expectedInstallationPath);
                    });

                // Once the package is extracted, it is registered on the system with Virtual Client.
                this.fixture.PackageManager.Setup(mgr => mgr.RegisterPackageAsync(It.IsAny<DependencyPath>(), It.IsAny<CancellationToken>()))
                    .Callback<DependencyPath, CancellationToken>((package, token) =>
                    {
                        packageRegistrationConfirmed = package.Name == installation.PackageName && package.Path == expectedInstallationPath;
                    });

                await installation.ExecuteAsync(CancellationToken.None);

                // On Windows, we use the 'wget' toolset.
                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"wget.exe {installation.PackageUri}"), "Wget download command incorrect.");
                Assert.IsTrue(downloadPathConfirmed, "Archive file path incorrect.");
                Assert.IsTrue(installationPathConfirmed, "Package installation path incorrect.");
                Assert.IsTrue(packageRegistrationConfirmed, "Package registration incorrect.");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "wget")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "wget")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "wget.exe")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "wget.exe")]
        public async Task WgetPackageInstallationExecutesTheExpectedOperationsForNonArchiveFileType(PlatformID platform, Architecture architecture, string wgetBinary)
        {
            this.SetupDefaults(platform, architecture);

            this.fixture.Parameters = new Dictionary<string, IConvertible>
                {
                    { nameof(WgetPackageInstallation.PackageName), "any-package" },
                    { nameof(WgetPackageInstallation.PackageUri), "https://any.company.com/files/any-file.1.0.0.exe" }
                };

            using (WgetPackageInstallation installation = new WgetPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                installation.RetryPolicy = Policy.NoOpAsync();

                string expectedDownloadPath = this.fixture.GetPackagePath("any-file.1.0.0.exe");
                string expectedPackageRegistrationPath = this.fixture.GetPackagePath("any-package");
                string expectedDownloadedFileCopyPath = this.fixture.GetPackagePath("any-package", "any-file.1.0.0.exe");

                bool downloadPathConfirmed = false;
                bool installationPathConfirmed = false;
                bool packageRegistrationConfirmed = false;

                // The package will be copied to expectedDownloadedFileCopyPath on the system once it is downloaded.
                // e.g.
                // /packages/any-file.1.0.0.exe -> /packages/any-package/any-file.1.0.0.exe
                this.fixture.FileSystem.Setup(fe => fe.File.Copy(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .Callback<string, string, bool>((downloadFilePath, destinationPath, overWrite) =>
                    {
                        downloadPathConfirmed = downloadFilePath.Equals(expectedDownloadPath);
                        installationPathConfirmed = destinationPath.Equals(expectedDownloadedFileCopyPath);
                    });

                // Once the package is downloaded and copied, it is registered on the system with Virtual Client.
                this.fixture.PackageManager.Setup(mgr => mgr.RegisterPackageAsync(It.IsAny<DependencyPath>(), It.IsAny<CancellationToken>()))
                    .Callback<DependencyPath, CancellationToken>((package, token) =>
                    {
                        packageRegistrationConfirmed = package.Name == installation.PackageName && package.Path == expectedPackageRegistrationPath;
                    });

                await installation.ExecuteAsync(CancellationToken.None);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"{wgetBinary} {installation.PackageUri}"), "Wget download command incorrect.");
                Assert.IsTrue(downloadPathConfirmed, "Archive file path incorrect.");
                Assert.IsTrue(installationPathConfirmed, "Package installation path incorrect.");
                Assert.IsTrue(packageRegistrationConfirmed, "Package registration incorrect.");
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task WgetPackageInstallationRetriesOnTransientFailures(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaults(platform, architecture);

            using (WgetPackageInstallation installation = new WgetPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                int actualRetries = 0;
                int expectedRetries = 3;

                installation.RetryPolicy = Policy.Handle<Exception>(exc =>
                {
                    actualRetries++;
                    return true;
                })
                .WaitAndRetryAsync(expectedRetries, (retries => TimeSpan.Zero));

                this.fixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    if (!process.FullCommand().Contains("chmod"))
                    {
                        throw new ProcessException($"Wget package download failed");
                    }
                };

                try
                {
                    await installation.ExecuteAsync(CancellationToken.None);
                }
                catch (Exception)
                {
                }

                Assert.AreEqual(expectedRetries + 1, actualRetries);
            }
        }
    }
}
