// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SystemManagementExtensionsTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockPackage = new DependencyPath("any_package", this.mockFixture.GetPackagePath("any_package.1.0.0"));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task GetPlatformSpecificPackageExtensionReturnsTheExpectedPackage(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // The package itself exists.
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(this.mockPackage);

            // Setup:
            // There is a platform-specific folder in the package.
            string expectedPlatformSpecificPath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPath))
                .Returns(true);

            DependencyPath package = await this.mockFixture.SystemManagement.Object.GetPlatformSpecificPackageAsync(
                this.mockPackage.Name, 
                CancellationToken.None);

            Assert.IsNotNull(package);
            Assert.AreEqual(expectedPlatformSpecificPath, package.Path);
        }

        [Test]
        public void GetPlatformSpecificPackageExtensionDefaultsThrowWhenThePackageItselfIsNotFound()
        {
            this.SetupTest();

            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(null as DependencyPath);

            DependencyException error = Assert.ThrowsAsync<DependencyException>(
                () => this.mockFixture.SystemManagement.Object.GetPlatformSpecificPackageAsync(this.mockPackage.Name, CancellationToken.None));

            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            Assert.AreEqual($"A package with the name '{this.mockPackage.Name}' was not found on the system.", error.Message);
        }

        [Test]
        public void GetPlatformSpecificPackageExtensionDefaultsThrowWhenThePackageIsFoundButWithoutTheExpectedPlatformArchitectureFolder()
        {
            this.SetupTest();

            // Setup:
            // The package itself exists.
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(this.mockPackage);

            // Setup:
            // However, there is NOT a platform-specific folder in the package.
            string expectedPlatformSpecificPath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPath))
                .Returns(false);

            DependencyException error = Assert.ThrowsAsync<DependencyException>(
                () => this.mockFixture.SystemManagement.Object.GetPlatformSpecificPackageAsync(this.mockPackage.Name, CancellationToken.None));

            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            Assert.AreEqual(
                $"The package '{this.mockPackage.Name}' exists but does not contain a folder for platform/architecture '{this.mockFixture.PlatformArchitectureName}'.",
                error.Message);
        }

        [Test]
        public void GetPlatformSpecificPackageExtensionDoesNotThrowWhenRequested_PackageDoesNotExist_Scenario()
        {
            this.SetupTest();

            // Setup:
            // The package does not exist.
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(null as DependencyPath);

            DependencyPath package = null;
            Assert.DoesNotThrowAsync(async () => package = await this.mockFixture.SystemManagement.Object.GetPlatformSpecificPackageAsync(
                this.mockPackage.Name,
                CancellationToken.None,
                throwIfNotfound: false));

            Assert.IsNull(package);
        }

        [Test]
        public void GetPlatformSpecificPackageExtensionDoesNotThrowWhenRequested_PackageExists_NoPlatformSpecificFolder_Scenario()
        {
            this.SetupTest();

            // Setup:
            // The package itself exists.
            this.mockFixture.PackageManager.OnGetPackage(this.mockPackage.Name)
                .ReturnsAsync(this.mockPackage);

            // Setup:
            // However, there is NOT a platform-specific folder in the package.
            string expectedPlatformSpecificPath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPath))
                .Returns(false);

            DependencyPath package = null;
            Assert.DoesNotThrowAsync(async () => package = await this.mockFixture.SystemManagement.Object.GetPlatformSpecificPackageAsync(
                this.mockPackage.Name,
                CancellationToken.None,
                throwIfNotfound: false));

            Assert.IsNull(package);
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            this.SetupTest();

            bool confirmed = false;
            string expectedBinary = "/home/any/path/to/VirtualClient";

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo");
                Assert.IsTrue(arguments == $"chmod +x \"{expectedBinary}\"");
                confirmed = true;

                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeAllFilesExecutableAsyncExtensionExecutesTheExpectedOperationToMakeABinaryExecutableOnAUnixSystem()
        {
            this.SetupTest();

            bool confirmed = false;
            string expectedDirectory = "/home/any/path/to/scripts";

            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo");
                Assert.IsTrue(arguments == $"chmod -R 2777 \"{expectedDirectory}\"");
                confirmed = true;

                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFilesExecutableAsync(expectedDirectory, PlatformID.Unix, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void MakeFileExecutableAsyncExtensionDoesNothingToMakeABinaryExecutableOnAWindowsSystem()
        {
            this.SetupTest();

            bool processExecuted = false;
            string expectedBinary = @"C:\any\path\to\VirtualClient.exe";

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                processExecuted = true;
                return new InMemoryProcess
                {
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            this.mockFixture.SystemManagement.Object.MakeFileExecutableAsync(expectedBinary, PlatformID.Win32NT, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsFalse(processExecuted);
        }
    }
}
