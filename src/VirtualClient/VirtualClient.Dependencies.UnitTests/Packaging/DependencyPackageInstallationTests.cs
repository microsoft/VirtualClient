// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.Packaging
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposables cleaned up in [TearDown].")]
    public class DependencyPackageInstallationTests
    {
        private MockFixture mockFixture;
        private DependencyPackageInstallation installer;
        private DependencyPath installedDependency;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                ["BlobName"] = "any.package.1.2.3.4.zip",
                ["BlobContainer"] = "packages",
                ["PackageName"] = "any.package"
            });

            this.installer = new DependencyPackageInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            this.installedDependency = new DependencyPath(
                "any.package",
                this.mockFixture.PlatformSpecifics.GetPackagePath("any.package.1.2.3.4"));
        }

        [TearDown]
        public void CleanupTest()
        {
            this.installer.Dispose();
        }

        [Test]
        public async Task DependencyPackageInstallationInstallsTheExpectedPackage()
        {
            bool installed = false;
            this.mockFixture.PackageManager.OnInstallPackage(evaluate: (description, installationPath) =>
            {
                BlobDescriptor descriptor = new BlobDescriptor(description);

                installed = true;
                Assert.AreEqual("any.package.1.2.3.4.zip", descriptor.Name);
                Assert.AreEqual("packages", descriptor.ContainerName);
                Assert.IsNull(installationPath);
            })
            .ReturnsAsync(this.installedDependency.Path);

            await this.installer.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(installed);
        }

        [Test]
        public async Task DependencyPackageInstallationInstallsToExpectedPathWhenGiven()
        {
            string expectedPath = "C:\\my\\path";
            this.installer.Parameters[nameof(DependencyPackageInstallation.InstallationPath)] = expectedPath;

            bool installed = false;
            this.mockFixture.PackageManager.OnInstallPackage(evaluate: (description, installationPath) =>
            {
                installed = true;
                Assert.IsNotNull(installationPath);
                Assert.AreEqual(expectedPath, installationPath);
            })
            .ReturnsAsync(this.installedDependency.Path);

            await this.installer.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(installed);
        }

        [Test]
        public async Task DependencyPackageInstallationInstallsInTheExpectedPathWhenRelativeDiskReferenceIsFirstDisk()
        {
            Disk mockDisk = this.mockFixture.Create<Disk>();
            string relativePath = $"{{FirstDisk}}\\packages";
            string expectedPath = this.mockFixture.PlatformSpecifics.Combine(mockDisk.Volumes.First().AccessPaths.First(), "packages");

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Disk>() { mockDisk });
            this.installer.Parameters[nameof(DependencyPackageInstallation.InstallationPath)] = relativePath;

            bool installed = false;
            this.mockFixture.PackageManager.OnInstallPackage(evaluate: (description, installationPath) =>
            {
                installed = true;
                Assert.IsNotNull(installationPath);
                Assert.AreEqual(expectedPath, installationPath);
            })
            .ReturnsAsync(this.installedDependency.Path);

            await this.installer.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(installed);
        }

        [Test]
        public async Task DependencyPackageInstallationInstallsInTheExpectedPathWhenRelativeDiskReferenceIsLastDisk()
        {
            Disk mockDisk = this.mockFixture.Create<Disk>();
            string relativePath = $"{{LastDisk}}\\packages";
            string expectedPath = this.mockFixture.PlatformSpecifics.Combine(mockDisk.Volumes.First().AccessPaths.First(), "packages");

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Disk>() { mockDisk });
            this.installer.Parameters[nameof(DependencyPackageInstallation.InstallationPath)] = relativePath;

            bool installed = false;
            this.mockFixture.PackageManager.OnInstallPackage(evaluate: (description, installationPath) =>
            {
                installed = true;
                Assert.IsNotNull(installationPath);
                Assert.AreEqual(expectedPath, installationPath);
            })
            .ReturnsAsync(this.installedDependency.Path);

            await this.installer.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            Assert.IsTrue(installed);
        }

        [Test]
        public void DependencyPackageInstallationThrowsExceptionWhenFailedToResolveDiskReference()
        {
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Disk>() { this.mockFixture.Create<Disk>() });
            this.installer.Parameters[nameof(DependencyPackageInstallation.InstallationPath)] = "{MiddleDisk}\\packages";

            WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(() => this.installer.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exception.Reason);
        }

        [Test]
        public Task BlobPackageInstallerRegistersTheExpectedPackageAfterItIsInstalled()
        {
            this.mockFixture.PackageManager.OnInstallDependencyPackage().ReturnsAsync(this.installedDependency.Path);
            this.mockFixture.PackageManager.OnRegisterPackage(evaluate: packageRegistered =>
            {
                Assert.IsTrue(object.ReferenceEquals(this.installedDependency, packageRegistered));
            });

            return this.installer.ExecuteAsync(CancellationToken.None);
        }
    }
}
