// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PackageManagerTests
    {
        private MockFixture mockFixture;
        private TestPackageManager packageManager;
        private DependencyPath mockDependency;
        private DependencyDescriptor mockDependencyDescription;

        [SetUp]
        public void SetupTest()
        {
            this.SetupMocks(PlatformID.Win32NT);

            // When we search for the package directory or directories within, they are found
            // by default.
            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        [TestCase(@"package.1.0.0.other", ArchiveType.Undefined)]
        [TestCase(@"package.1.0.0.zip", ArchiveType.Zip)]
        [TestCase(@"6.2.1.tar", ArchiveType.Tar)]
        [TestCase(@"6.2.1.tar.gz", ArchiveType.Tgz)]
        [TestCase(@"6.2.1.tgz", ArchiveType.Tgz)]
        [TestCase(@"6.2.1.tar.gzip", ArchiveType.Tgz)]
        [TestCase(@"C:\any\path\to\package.1.0.0.other", ArchiveType.Undefined)]
        [TestCase(@"C:\any\path\to\package.1.0.0.zip", ArchiveType.Zip)]
        [TestCase(@"/home/user/vc/content/linux-x64/packages/6.2.1.tar", ArchiveType.Tar)]
        [TestCase(@"/home/user/vc/content/linux-x64/packages/6.2.1.tar.gz", ArchiveType.Tgz)]
        [TestCase(@"/home/user/vccontent/linux-x64/packages/6.2.1.tgz", ArchiveType.Tgz)]
        [TestCase(@"/home/user/vccontent/linux-x64/packages/6.2.1.tar.gzip", ArchiveType.Tgz)]
        public void TryGetArchiveFileTypeExtensionCorrectlyIdentifiesTheArchiveType(string archivePath, ArchiveType expectedArchiveType)
        {
            Assert.AreEqual(
                expectedArchiveType == ArchiveType.Undefined ? false : true,
                PackageManager.TryGetArchiveFileType(archivePath, out ArchiveType actualArchiveType));

            Assert.AreEqual(expectedArchiveType, actualArchiveType);
        }

        [Test]
        public async Task PackageManagerDiscoversExtensionsThatExistInAUserDefinedLocation()
        {
            string expectedPackageName = "package_123";
            string userDefinedPackageLocation = $@"C:\any\location\defined\by\the\user\to\packages";
            string expectedPackageLocation = $@"{userDefinedPackageLocation}\{expectedPackageName}";

            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName, userDefinedPackageLocation, extensionsPackage: true);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(expectedPackageLocation, packages.First().Path);
            Assert.IsTrue(packages.First().Metadata.ContainsKey(PackageMetadata.Extensions));
            Assert.IsTrue(packages.First().Metadata[PackageMetadata.Extensions].ToBoolean(CultureInfo.InvariantCulture));
        }

        [Test]
        public async Task PackageManagerDiscoversExtensionsThatExistInTheDefaultPackagesDirectory()
        {
            string expectedPackageName = "package_123";
            string expectedPackageLocation = this.mockFixture.PlatformSpecifics.GetPackagePath(expectedPackageName);

            this.SetupPackageExistsInDefaultPackagesLocation(expectedPackageName, extensionsPackage: true);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(expectedPackageLocation, packages.First().Path);
            Assert.IsTrue(packages.First().Metadata.ContainsKey(PackageMetadata.Extensions));
            Assert.IsTrue(packages.First().Metadata[PackageMetadata.Extensions].ToBoolean(CultureInfo.InvariantCulture));
        }

        [Test]
        public async Task PackageManagerHandlesScenariosWhereThereAreNotAnyExtensionsOnTheSystemToDiscover()
        {
            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsEmpty(packages);
        }


        [Test]
        public async Task PackageManagerSearchesForExtensionsDuringDiscoveryUsingTheExpectedSearchPriority()
        {
            // Packages that exist in the user-defined location are selected first.
            string userDefinedPath = @"C:\any\user\defined\location";
            this.SetupPackageExistsInUserDefinedLocation("package1", userDefinedPath, extensionsPackage: true);

            // Packages that exist in the default 'packages' folder location are selected next
            // but only if they are not found in the user-defined location.
            this.SetupPackageExistsInDefaultPackagesLocation("package1", extensionsPackage: true);
            this.SetupPackageExistsInDefaultPackagesLocation("package2", extensionsPackage: true);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotEmpty(packages);

            IEnumerable<DependencyPath> package1Locations = packages.Where(pkg => pkg.Name == "package1");
            IEnumerable<DependencyPath> package2Locations = packages.Where(pkg => pkg.Name == "package2");

            Assert.IsNotEmpty(package1Locations);
            Assert.IsNotEmpty(package2Locations);
            Assert.IsTrue(package1Locations.Count() == 1);
            Assert.IsTrue(package2Locations.Count() == 1);

            Assert.AreEqual(Path.Combine(userDefinedPath, "package1"), package1Locations.First().Path);
            Assert.AreEqual(Path.Combine(this.mockFixture.PlatformSpecifics.PackagesDirectory, "package2"), package2Locations.First().Path);
        }

        [Test]
        public async Task PackageManagerDiscoversPackagesThatExistInAUserDefinedLocation()
        {
            string expectedPackageName = "package_123";
            string userDefinedPackageLocation = $@"C:\any\location\defined\by\the\user\to\packages";
            string expectedPackageLocation = $@"{userDefinedPackageLocation}\{expectedPackageName}";

            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName, userDefinedPackageLocation);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(expectedPackageLocation, packages.First().Path);
        }

        [Test]
        public async Task PackageManagerDiscoversPackagesThatExistInTheDefaultPackagesDirectory()
        {
            string expectedPackageName = "package_123";
            string expectedPackageLocation = this.mockFixture.PlatformSpecifics.GetPackagePath(expectedPackageName);

            this.SetupPackageExistsInDefaultPackagesLocation(expectedPackageName);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(expectedPackageLocation, packages.First().Path);
        }

        [Test]
        public async Task PackageManagerSearchesForPackagesDuringDiscoveryUsingTheExpectedSearchPriority()
        {
            // Packages that exist in the user-defined location are selected first.
            string userDefinedPath = @"C:\any\user\defined\location";
            this.SetupPackageExistsInUserDefinedLocation("package1", userDefinedPath);

            // Packages that exist in the default 'packages' folder location are selected next
            // but only if they are not found in the user-defined location.
            this.SetupPackageExistsInDefaultPackagesLocation("package1");
            this.SetupPackageExistsInDefaultPackagesLocation("package2");

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotEmpty(packages);

            IEnumerable<DependencyPath> package1Locations = packages.Where(pkg => pkg.Name == "package1");
            IEnumerable<DependencyPath> package2Locations = packages.Where(pkg => pkg.Name == "package2");

            Assert.IsNotEmpty(package1Locations);
            Assert.IsNotEmpty(package2Locations);
            Assert.IsTrue(package1Locations.Count() == 1);
            Assert.IsTrue(package2Locations.Count() == 1);

            Assert.AreEqual(Path.Combine(userDefinedPath, "package1"), package1Locations.First().Path);
            Assert.AreEqual(Path.Combine(this.mockFixture.PlatformSpecifics.PackagesDirectory, "package2"), package2Locations.First().Path);
        }

        [Test]
        public async Task PackageManagerHandlesScenariosWhereThereAreNotAnyPackagesOnTheSystemToDiscover()
        {
            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsEmpty(packages);
        }

        [Test]
        public async Task PackageManagerCanFindPackagesThatAreAlreadyRegistered()
        {
            string expectedPackageName = "package_987";
            string expectedPackageLocation = $@"C:\any\location\for\virtualclient\packages\{expectedPackageName}";

            this.SetupPackageIsRegistered(expectedPackageName, expectedPackageLocation);

            DependencyPath actualPackage = await this.packageManager.GetPackageAsync(expectedPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(actualPackage);
            Assert.AreEqual(expectedPackageName, actualPackage.Name);
            Assert.AreEqual(expectedPackageLocation, actualPackage.Path);
        }

        [Test]
        public async Task PackageManagerRegistersAPackageAsExpected()
        {
            bool packageRegistered = false;

            this.mockFixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.AreEqual(this.mockDependency.Name, stateId);
                Assert.IsNotNull(state);
                Assert.AreEqual(JObject.FromObject(this.mockDependency), state);
                packageRegistered = true;
            });

            await this.packageManager.RegisterPackageAsync(this.mockDependency, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(packageRegistered);
        }

        [Test]
        public void PackageManagerValidatesRequiredPropertiesAreDefinedWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();
            this.mockDependencyDescription.Clear();

            Assert.ThrowsAsync<ArgumentException>(() => this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()));
        }

        [Test]
        public void PackageManagerThrowsIfAnArchiveIsReferencedWithoutTheArchiveTypeDefinedWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();
            this.mockDependencyDescription.Remove(nameof(DependencyDescriptor.ArchiveType));

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()));

            Assert.AreEqual(ErrorReason.DependencyDescriptionInvalid, error.Reason);
            Assert.IsTrue(error.Message.StartsWith("The type of archive was not defined for the dependency"));
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesFromTheExpectedStore()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.IsTrue(object.ReferenceEquals(this.mockDependencyDescription, description));
            };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string expectedPackagePath = this.mockFixture.GetPackagePath(this.mockDependencyDescription.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockDependencyDescription.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(confirmed);
            Assert.AreEqual(expectedPackagePath, actualPackagePath);
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation_CustomInstallationPathProvided()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string customPath = "C:\\my\\custom\\package\\path";
            string expectedInstallationPath = this.mockFixture.Combine(customPath, this.mockDependencyDescription.Name);
            string expectedPackagePath = this.mockFixture.Combine(customPath, this.mockDependencyDescription.PackageName.ToLowerInvariant());

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                customPath,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(confirmed);
            Assert.AreEqual(expectedPackagePath, actualPackagePath);
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation_WhenPackageIsNotExtracted()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.mockDependencyDescription.Extract = false;
            string expectedPackagePath = this.mockFixture.GetPackagePath(this.mockDependencyDescription.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockDependencyDescription.PackageName, this.mockDependencyDescription.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(confirmed);
            Assert.AreEqual(expectedPackagePath, actualPackagePath);
        }

        [Test]
        public async Task PackageManagerDoesNotInstallADependencyPackageIfItIsAlreadyInstalledOnTheSystem()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            bool installed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) => installed = true;
            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsFalse(installed);
        }

        [Test]
        public async Task PackageManagerCreatesTheInstallationDirectoryIfItDoesNotExistWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // The package directory does not exist.
            this.mockFixture.Directory.Setup(dir => dir.Exists(this.packageManager.PackagesDirectory)).Returns(false);

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            this.mockFixture.Directory.Verify(dir => dir.CreateDirectory(this.packageManager.PackagesDirectory));
        }

        [Test]
        [TestCase("anypackage.1.0.0.zip")]
        [TestCase("ANYPACKAGE.1.0.0.ZIP")]
        public async Task PackageManagerExtractsDependencyPackagesDownloadedThatAreZipFilesToTheExpectedLocation(string name)
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.mockDependencyDescription.Name = name;
            this.mockDependencyDescription.ArchiveType = ArchiveType.Zip;
            string expectedArchivePath = this.mockFixture.GetPackagePath(name);
            string expectedDestinationPath = this.mockFixture.GetPackagePath("anypackage.1.0.0");

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) =>
            {
                Assert.AreEqual(expectedArchivePath, archivePath);
                Assert.AreEqual(expectedDestinationPath, destinationPath);
                Assert.AreEqual(ArchiveType.Zip, archiveType);
                packageExtracted = true;
            };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(packageExtracted);
        }

        [Test]
        [TestCase("anypackage.1.0.0.gz")]
        [TestCase("ANYPACKAGE.1.0.0.GZ")]
        [TestCase("anypackage.1.0.0.tar")]
        [TestCase("ANYPACKAGE.1.0.0.TAR")]
        [TestCase("anypackage.1.0.0.tgz")]
        [TestCase("ANYPACKAGE.1.0.0.TGZ")]
        [TestCase("anypackage.1.0.0.tar.gz")]
        [TestCase("ANYPACKAGE.1.0.0.TAR.GZ")]
        [TestCase("anypackage.1.0.0.tar.gzip")]
        [TestCase("ANYPACKAGE.1.0.0.TAR.GZIP")]
        public async Task PackageManagerExtractsDependencyPackagesDownloadedThatAreTarballFilesToTheExpectedLocation(string name)
        {
            this.SetupMocks(PlatformID.Unix);
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.mockDependencyDescription.Name = name;
            this.mockDependencyDescription.ArchiveType = ArchiveType.Tgz;
            string expectedArchivePath = this.mockFixture.GetPackagePath(name);
            string expectedDestinationPath = this.mockFixture.GetPackagePath("anypackage.1.0.0");

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) =>
            {
                Assert.AreEqual(expectedArchivePath, archivePath);
                Assert.AreEqual(expectedDestinationPath, destinationPath);
                Assert.AreEqual(ArchiveType.Tgz, archiveType);
                packageExtracted = true;
            };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(packageExtracted);
        }

        [Test]
        [TestCase(ArchiveType.Zip)]
        [TestCase(ArchiveType.Tgz)]
        public async Task PackageManagerDeletesDependencyArchiveFilesDownloadedAfterItHasSuccessfullyExtractedThePackageFromIt(ArchiveType archiveType)
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string expectedArchivePath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockDependencyDescription.Name);

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) => packageExtracted = true;

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            this.mockFixture.File.Verify(file => file.Delete(expectedArchivePath));
            Assert.IsTrue(packageExtracted);
        }

        [Test]
        public async Task PackageMangerRegistersDependencyPackagesThatAreInstalled()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockDependency };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            // When a package is registered
            this.mockFixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
                this.mockDependency.Name,
                It.IsAny<JObject>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy>()));
        }

        [Test]
        public async Task PackageMangerEnsuresPackagesInstalledAreRegisteredByThePackageNameDefinedInAdditionToAnyNamesDefinedWithinThePackageItself()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.mockDependencyDescription.PackageName = this.mockDependency.Name + "-other";
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockDependency };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockDependencyDescription,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            // When a package is registered a state object is written. In this scenario we register the package by the
            // name defined in the package metadata description. We additionally register it by the name that was provided
            // in the descriptor.
            //
            // e.g. anyname
            this.mockFixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
                this.mockDependency.Name,
                It.IsAny<JObject>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy>()));

            // e.g. anyname-other
            this.mockFixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
               this.mockDependencyDescription.PackageName,
               It.IsAny<JObject>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<IAsyncPolicy>()));
        }

        [Test]
        public async Task PackageManagerInstallsProfileExtensionsToTheExpectedLocation()
        {
            string profileExtensionsPath = this.mockFixture.Combine(
                this.mockFixture.ToPlatformSpecificPath(
                    this.mockDependency,
                    this.mockFixture.PlatformSpecifics.Platform,
                    this.mockFixture.PlatformSpecifics.CpuArchitecture).Path,
                "profiles");

            string expectedProfile = this.mockFixture.Combine(profileExtensionsPath, "ANY-PROFILE.json");
            string expectedInstallationPath = this.mockFixture.GetProfilesPath("ANY-PROFILE.json");

            // Setup profiles existing in the profile extensions directory.
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(profileExtensionsPath, "*.json", SearchOption.TopDirectoryOnly))
                .Returns(new List<string> { expectedProfile });
            this.mockFixture.File.Setup(file => file.GetLastWriteTimeUtc(expectedProfile)).Returns(DateTime.UtcNow);

            await this.packageManager.InstallExtensionsAsync(this.mockDependency, CancellationToken.None)
                .ConfigureAwait(false);

            this.mockFixture.File.Verify(file => file.Copy(expectedProfile, expectedInstallationPath, true));
        }

        [Test]
        public async Task PackageManagerInstallsBinaryExtensionsToTheExpectedLocation()
        {
            string binaryExtensionsPath = this.mockFixture.ToPlatformSpecificPath(
                this.mockDependency,
                this.mockFixture.PlatformSpecifics.Platform,
                this.mockFixture.PlatformSpecifics.CpuArchitecture).Path;

            string expectedBinary = this.mockFixture.Combine(binaryExtensionsPath, "Any.VirtualClient.Extensions.dll");
            string expectedSymbols = this.mockFixture.Combine(binaryExtensionsPath, "Any.VirtualClient.Extensions.pdb");
            string expectedInstallationPath1 = this.mockFixture.Combine(this.mockFixture.CurrentDirectory, "Any.VirtualClient.Extensions.dll");
            string expectedInstallationPath2 = this.mockFixture.Combine(this.mockFixture.CurrentDirectory, "Any.VirtualClient.Extensions.pdb");

            // Setup binaries + symbols existing in the binaries extensions directory.
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(binaryExtensionsPath, "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new List<string> { expectedBinary, expectedSymbols });
            this.mockFixture.File.Setup(file => file.GetLastWriteTimeUtc(expectedBinary)).Returns(DateTime.UtcNow);
            this.mockFixture.File.Setup(file => file.GetLastWriteTimeUtc(expectedSymbols)).Returns(DateTime.UtcNow);

            // Setup the binaries do NOT already exist in the target location.
            this.mockFixture.File.Setup(file => file.Exists(expectedInstallationPath1)).Returns(false);
            this.mockFixture.File.Setup(file => file.Exists(expectedInstallationPath2)).Returns(false);

            await this.packageManager.InstallExtensionsAsync(this.mockDependency, CancellationToken.None)
                .ConfigureAwait(false);

            this.mockFixture.File.Verify(file => file.Copy(expectedBinary, expectedInstallationPath1, true));
            this.mockFixture.File.Verify(file => file.Copy(expectedSymbols, expectedInstallationPath2, true));
        }

        [Test]
        public async Task PackageManagerDoesNotInstallAProfileExtensionsIfItAlreadyExistsOnTheSystem()
        {
            string profileExtensionsPath = this.mockFixture.Combine(
               this.mockFixture.ToPlatformSpecificPath(
                   this.mockDependency,
                   this.mockFixture.PlatformSpecifics.Platform,
                   this.mockFixture.PlatformSpecifics.CpuArchitecture).Path,
               "profiles");

            string profileExtension = this.mockFixture.Combine(profileExtensionsPath, "ANY-PROFILE.json");
            string targetProfilePath = this.mockFixture.GetProfilesPath("ANY-PROFILE.json");

            // Setup profiles existing in the profile extensions directory.
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(profileExtensionsPath, "*.json", SearchOption.TopDirectoryOnly))
                .Returns(new List<string> { profileExtension });

            // The profile already exists in the 'profiles' directory.
            this.mockFixture.File.Setup(file => file.Exists(targetProfilePath)).Returns(true);

            await this.packageManager.InstallExtensionsAsync(this.mockDependency, CancellationToken.None)
                .ConfigureAwait(false);

            this.mockFixture.File.Verify(file => file.Copy(profileExtension, targetProfilePath, false), Times.Never);
        }

        [Test]
        public async Task PackageManagerDoesNotInstallABinaryExtensionsIfItAlreadyExistsOnTheSystem()
        {
            string binaryExtensionsPath = this.mockFixture.Combine(this.mockDependency.Path, "binaries");
            string binaryExtension = this.mockFixture.Combine(binaryExtensionsPath, "MSFT.VirtualClient.Extensions.dll");
            string targetBinaryPath = this.mockFixture.Combine(this.mockFixture.CurrentDirectory, "MSFT.VirtualClient.Extensions.dll");

            // Setup binaries existing in the profile extensions directory.
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(binaryExtensionsPath, "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new List<string> { binaryExtension });

            // The binary already exists in the VC root directory.
            this.mockFixture.File.Setup(file => file.Exists(targetBinaryPath)).Returns(true);

            await this.packageManager.InstallExtensionsAsync(this.mockDependency, CancellationToken.None)
                .ConfigureAwait(false);

            this.mockFixture.File.Verify(file => file.Copy(binaryExtension, targetBinaryPath, false), Times.Never);
        }

        public void SetupMocks(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);

            this.packageManager = new TestPackageManager(
                this.mockFixture.StateManager.Object,
                this.mockFixture.FileSystem.Object,
                this.mockFixture.PlatformSpecifics,
                logger: NullLogger.Instance);

            this.mockDependency = new DependencyPath(
                "anyname",
                platform == PlatformID.Unix ? "/home/any/path/to/dependency" : @"C:\any\users\path\to\dependency",
                "AnyDescription");

            this.mockDependencyDescription = new BlobDescriptor
            {
                Name = "anyname.zip",
                ContainerName = "anycontainer",
                PackageName = "anyname",
                Extract = true,
                ArchiveType = ArchiveType.Zip
            };
        }

        private void SetupDependencyPackageInstallationDefaultMockBehaviors()
        {
            // The mock behavior setup here implements the default "happy path". This is the path
            // where the code performs the expected behaviors given the package has NOT been previously
            // downloaded/installed and does not hit any unexpected errors.
            this.mockFixture.Directory.SetupSequence(dir => dir.Exists(It.IsAny<string>()))
               .Returns(false)  // The package path does not already exist (and thus does not need to be deleted).
               .Returns(false)  // The blob file does not exist having been previously downloaded (and thus does not need to be deleted).
               .Returns(true);  // The package directory already exists (and so does not need to be created).
        }

        private void SetupPackageIsRegistered(string packageName, string packageLocation)
        {
            this.mockFixture.StateManager.OnGetState(packageName)
                .ReturnsAsync(JObject.FromObject(new DependencyPath(packageName, packageLocation)));

            this.mockFixture.Directory.Setup(dir => dir.Exists(packageLocation)).Returns(true);
        }

        private void SetupPackageExistsInUserDefinedLocation(string packageName, string userDefinedPackageLocation, bool extensionsPackage = false)
        {
            string packagePath = this.mockFixture.PlatformSpecifics.Combine(userDefinedPackageLocation, packageName);

            // No package registrations should be found.
            this.mockFixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables.Add(
                PackageManager.UserDefinedPackageLocationVariable,
                userDefinedPackageLocation);

            // The *.vcpkg definition will be found in the target packages directory
            string vcpkgFilePath = this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{packageName}.vcpkg");
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(userDefinedPackageLocation, "*.vcpkg", SearchOption.AllDirectories))
                .Returns(new string[] { vcpkgFilePath });

            // The *.vcpkg file has valid package/dependency definition as content.
            DependencyPath package = new DependencyPath(packageName, Path.GetDirectoryName(vcpkgFilePath));
            if (extensionsPackage)
            {
                package.Metadata[PackageMetadata.Extensions] = true;
            }

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(vcpkgFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package.ToJson());
        }

        private void SetupPackageExistsInDefaultPackagesLocation(string packageName, bool extensionsPackage = false)
        {
            string packagePath = this.mockFixture.PlatformSpecifics.GetPackagePath(packageName);

            // No package registrations should be found.
            this.mockFixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

            // The *.vcpkg definition will be found in the default packages directory
            string vcpkgFilePath = this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{packageName}.vcpkg");
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(this.mockFixture.PlatformSpecifics.PackagesDirectory, "*.vcpkg", SearchOption.AllDirectories))
                .Returns(new string[] { vcpkgFilePath });

            // The *.vcpkg file has valid package/dependency definition as content.
            DependencyPath package = new DependencyPath(packageName, Path.GetDirectoryName(vcpkgFilePath));
            if (extensionsPackage)
            {
                package.Metadata[PackageMetadata.Extensions] = true;
            }

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(vcpkgFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package.ToJson());
        }

        private class TestPackageManager : PackageManager
        {
            public TestPackageManager(IStateManager state, IFileSystem fileSystem, PlatformSpecifics platformSpecifics, ILogger logger = null)
                : base(state, fileSystem, platformSpecifics, logger)
            {
            }

            /// <summary>
            /// Delegate matches the signature of the DownloadDependencyPackageAsync method and enables
            /// custom behaviors to be injected in at runtime.
            /// </summary>
            public Action<DependencyDescriptor, string, CancellationToken> OnDownloadDependencyPackage { get; set; }

            /// <summary>
            /// Delegate matches the signature of the DiscoverPackagesAsync method and enables
            /// custom behaviors to be injected in at runtime.
            /// </summary>
            public Func<string, IEnumerable<DependencyPath>> OnDiscoverPackages { get; set; }

            /// <summary>
            /// Delegate matches the signature of the ExtractArchiveAsync method enabling
            /// custom behaviors to be injected in at runtime.
            /// </summary>
            public Action<string, string, ArchiveType, CancellationToken> OnExtractArchive { get; set; }

            protected override Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(string directoryPath, CancellationToken cancellationToken, bool extensionsOnly = false)
            {
                if (this.OnDiscoverPackages != null)
                {
                    return Task.FromResult(this.OnDiscoverPackages.Invoke(directoryPath));
                }

                return base.DiscoverPackagesAsync(directoryPath, cancellationToken, extensionsOnly);
            }

            protected override Task ExtractArchiveAsync(string archiveFilePath, string destinationPath, ArchiveType archiveType, CancellationToken cancellationToken)
            {
                this.OnExtractArchive?.Invoke(archiveFilePath, destinationPath, archiveType, cancellationToken);
                return Task.CompletedTask;
            }

            protected override Task DownloadDependencyPackageAsync(IBlobManager blobManager, DependencyDescriptor description, string fileInstallationPath, CancellationToken cancellationToken)
            {
                this.OnDownloadDependencyPackage?.Invoke(description, fileInstallationPath, cancellationToken);
                return Task.CompletedTask;
            }
        }
    }
}
