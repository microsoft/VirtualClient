// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
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
        private MockFixture fixture;
        private TestPackageManager packageManager;
        private DependencyPath mockPackage;
        private DependencyDescriptor mockPackageDescriptor;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            this.SetupMocks(PlatformID.Win32NT);

            // When we search for the package directory or directories within, they are found
            // by default.
            this.fixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        [TestCase(@"package.1.0.0.other", ArchiveType.Undefined)]
        [TestCase(@"package.1.0.0.zip", ArchiveType.Zip)]
        [TestCase(@"6.2.1.tar", ArchiveType.Tar)]
        [TestCase(@"6.2.1.tar.gz", ArchiveType.Tgz)]
        [TestCase(@"6.2.1.tgz", ArchiveType.Tgz)]
        [TestCase(@"6.2.1.tar.gzip", ArchiveType.Tgz)]
        [TestCase(@"C:/any/path/to/package.1.0.0.other", ArchiveType.Undefined)]
        [TestCase(@"C:/any/path/to/package.1.0.0.zip", ArchiveType.Zip)]
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
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinaryExtensionsThatExistInALocationDefinedByEnvironmentVariable()
        {
            string expectedBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsBinaryLocation = $@"C:/any/location/defined/by/the/user";
            string expectedBinaryLocation = $@"{extensionsBinaryLocation}/{expectedBinaryName}";

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables.Add(
                EnvironmentVariable.VC_LIBRARY_PATH,
                extensionsBinaryLocation);

            // The *.dll will be found in the target packages directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 1);
            Assert.AreEqual(extensions.Binaries.First().FullName, expectedBinaryLocation);
        }

        [Test]
        [TestCase("C:/any/location/defined/by/the/user/1;C:/any/location/defined/by/the/user/2")]
        [TestCase("C:/any/location/defined/by/the/user/1;  C:/any/location/defined/by/the/user/2")]
        [TestCase(";C:/any/location/defined/by/the/user/1;C:/any/location/defined/by/the/user/2;")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinaryExtensionsThatExistInALocationDefinedByEnvironmentVariable_Multiple_Locations(string environmentVariableValue)
        {
            string expectedBinaryName1 = "Any.VirtualClient.Extensions_1.dll";
            string expectedBinaryName2 = "Any.VirtualClient.Extensions_2.dll";
            string extensionsBinaryLocation1 = $@"C:/any/location/defined/by/the/user/1";
            string extensionsBinaryLocation2 = $@"C:/any/location/defined/by/the/user/2";
            string expectedBinaryLocation1 = $@"{extensionsBinaryLocation1}/{expectedBinaryName1}";
            string expectedBinaryLocation2 = $@"{extensionsBinaryLocation2}/{expectedBinaryName2}";

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables.Add(
                EnvironmentVariable.VC_LIBRARY_PATH,
                environmentVariableValue);

            // *.dlls will be found in the first packages directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation1, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation1 });

            // *.dlls will be found in the second packages directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation2, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation2 });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 2);
            Assert.AreEqual(extensions.Binaries.ElementAt(0).FullName, expectedBinaryLocation1);
            Assert.AreEqual(extensions.Binaries.ElementAt(1).FullName, expectedBinaryLocation2);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinariesThatExistInAnExtensionsPackageLocationDefinedByEnvironmentVariable()
        {
            string expectedPackageName = "any.extensions.pkg";
            string expectedBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsPackageLocation = this.fixture.Combine("any", "location", "defined", "by", "the", "user", "to", "packages");
            string expectedPlatformSpecificPackageLocation = this.fixture.Combine(extensionsPackageLocation, expectedPackageName, this.fixture.PlatformArchitectureName);
            string expectedBinaryLocation = this.fixture.Combine(expectedPlatformSpecificPackageLocation, expectedBinaryName);

            // The extensions package exists
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName, extensionsPackageLocation, true);

            // A platform-specific directory/content exists in the extensions package.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // The *.dll will be found in the extensions package platform-specific directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(expectedPlatformSpecificPackageLocation, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 1);
            Assert.AreEqual(extensions.Binaries.First().FullName, expectedBinaryLocation);
        }

        [Test]
        [TestCase("C:/any/location/defined/by/the/user/to/packages/1;C:/any/location/defined/by/the/user/to/packages/2")]
        [TestCase("C:/any/location/defined/by/the/user/to/packages/1;  C:/any/location/defined/by/the/user/to/packages/2")]
        [TestCase(";C:/any/location/defined/by/the/user/to/packages/1;C:/any/location/defined/by/the/user/to/packages/2;")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinariesThatExistInAnExtensionsPackageLocationDefinedByEnvironmentVariable_Multiple_Locations(string environmentVariableValue)
        {
            string expectedPackageName1 = "any.extensions.pkg_1";
            string expectedPackageName2 = "any.extensions.pkg_2";
            string expectedBinaryName1 = "Any.VirtualClient.Extensions_1.dll";
            string expectedBinaryName2 = "Any.VirtualClient.Extensions_2.dll";
            string extensionsPackageLocation1 = this.fixture.StandardizePath("C:/any/location/defined/by/the/user/to/packages/1");
            string extensionsPackageLocation2 = this.fixture.StandardizePath("C:/any/location/defined/by/the/user/to/packages/2");
            string expectedPlatformSpecificPackageLocation1 = this.fixture.Combine(extensionsPackageLocation1, expectedPackageName1, this.fixture.PlatformArchitectureName);
            string expectedPlatformSpecificPackageLocation2 = this.fixture.Combine(extensionsPackageLocation2, expectedPackageName2, this.fixture.PlatformArchitectureName);
            string expectedBinaryLocation1 = this.fixture.Combine(expectedPlatformSpecificPackageLocation1, expectedBinaryName1);
            string expectedBinaryLocation2 = this.fixture.Combine(expectedPlatformSpecificPackageLocation2, expectedBinaryName2);

            // The extensions packages exists
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName1, extensionsPackageLocation1, true);
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName2, extensionsPackageLocation2, true);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_PATH] = environmentVariableValue;

            // A platform-specific directory/content exists in the extensions packages.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation1))
                .Returns(true);

            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation2))
                .Returns(true);

            // *.dlls will be found in the extensions package platform-specific directories
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(expectedPlatformSpecificPackageLocation1, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation1 });

            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(expectedPlatformSpecificPackageLocation2, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation2 });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 2);
            Assert.AreEqual(extensions.Binaries.ElementAt(0).FullName, expectedBinaryLocation1);
            Assert.AreEqual(extensions.Binaries.ElementAt(1).FullName, expectedBinaryLocation2);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void PackageManagerThrowsWhenDuplicateExtensionsBinariesAreFoundDuringDiscovery()
        {
            string extensionsPackageName = "any.extensions_1.pkg";
            string defaultPackageName = "any.extensions_2.pkg";
            string extensionsBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsPackageLocation = this.fixture.StandardizePath("C:/any/location/defined/by/the/user/to/packages/1");
            string defaultPackageLocation = this.fixture.GetPackagePath();
            string extensionsPlatformSpecificPackageLocation = this.fixture.Combine(extensionsPackageLocation, extensionsPackageName, this.fixture.PlatformArchitectureName);
            string defaultPlatformSpecificPackageLocation = this.fixture.Combine(defaultPackageLocation, defaultPackageName, this.fixture.PlatformArchitectureName);

            // Extensions packages exist in both the default and a user-defined location
            this.SetupPackageExistsInDefaultPackagesLocation(defaultPackageName, true);
            this.SetupPackageExistsInUserDefinedLocation(extensionsPackageName, extensionsPackageLocation, true);

            // User has provided an alternate location for extensions packages.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_PATH] = extensionsPackageLocation;

            // A platform-specific directory/content exists in both the default and
            // user-defined extensions package location.
            this.fixture.Directory.Setup(dir => dir.Exists(extensionsPlatformSpecificPackageLocation))
                .Returns(true);

            this.fixture.Directory.Setup(dir => dir.Exists(defaultPlatformSpecificPackageLocation))
                .Returns(true);

            // *.dlls will be found in the extensions package platform-specific directories
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsPlatformSpecificPackageLocation, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { this.fixture.Combine(extensionsPlatformSpecificPackageLocation, extensionsBinaryName) });

            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(defaultPlatformSpecificPackageLocation, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { this.fixture.Combine(defaultPlatformSpecificPackageLocation, extensionsBinaryName) });

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.DiscoverExtensionsAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DuplicateExtensionsFound, error.Reason);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinariesThatExistInAnExtensionsPackageLocationDefinedInTheDefaultLocation()
        {
            string expectedPackageName = "any.extensions.pkg";
            string expectedBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsPackageLocation = this.fixture.GetPackagePath();
            string expectedPlatformSpecificPackageLocation = this.fixture.Combine(extensionsPackageLocation, expectedPackageName, this.fixture.PlatformArchitectureName);
            string expectedBinaryLocation = this.fixture.Combine(expectedPlatformSpecificPackageLocation, expectedBinaryName);

            // The extensions package exists
            this.SetupPackageExistsInDefaultPackagesLocation(expectedPackageName, true);

            // A platform-specific directory/content exists in the extensions package.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // The *.dll will be found in the extensions package platform-specific directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(expectedPlatformSpecificPackageLocation, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 1);
            Assert.AreEqual(extensions.Binaries.First().FullName, expectedBinaryLocation);
        }

        [Test]
        [TestCase("ANY-PROFILE.json")]
        [TestCase("ANY-PROFILE.yml")]
        [TestCase("ANY-PROFILE.yaml")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversProfileExtensionsThatExistInAPackageLocationDefinedByEnvironmentVariable(string expectedProfileName)
        {
            string expectedPackageName = "any.extensions.pkg";
            string extensionsPackageLocation = this.fixture.Combine("any", "location", "defined", "by", "the", "user", "to", "packages");
            string expectedPlatformSpecificPackageLocation = this.fixture.Combine(extensionsPackageLocation, expectedPackageName, this.fixture.PlatformArchitectureName);
            string expectedProfileLocation = this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles", expectedProfileName);

            // The extensions package exists
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName, extensionsPackageLocation, true);

            // A platform-specific directory/content exists in the extensions package.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // A profiles directory exists for the platform.
            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles")))
                .Returns(true);

            // The profiles will be found in the extensions package platform-specific directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedProfileLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Profiles);
            Assert.IsTrue(extensions.Profiles.Count() == 1);
            Assert.AreEqual(extensions.Profiles.First().FullName, expectedProfileLocation);
        }

        [Test]
        [TestCase("C:/any/location/defined/by/the/user/1;C:/any/location/defined/by/the/user/2")]
        [TestCase("C:/any/location/defined/by/the/user/1;  C:/any/location/defined/by/the/user/2")]
        [TestCase(";C:/any/location/defined/by/the/user/1;C:/any/location/defined/by/the/user/2;")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversProfileExtensionsThatExistInAPackageLocationDefinedByEnvironmentVariable_Multiple_Locations(string environmentVariableValue)
        {
            string expectedProfile1 = "ANY-PROFILE-1.json";
            string expectedProfile2 = "ANY-PROFILE-2.json";
            string expectedPackageName1 = "any.extensions_1.pkg";
            string expectedPackageName2 = "any.extensions_2.pkg";
            string extensionsPackageLocation1 = $"C:/any/location/defined/by/the/user/1";
            string extensionsPackageLocation2 = $"C:/any/location/defined/by/the/user/2";
            string expectedPlatformSpecificPackageLocation1 = this.fixture.Combine(extensionsPackageLocation1, expectedPackageName1, this.fixture.PlatformArchitectureName);
            string expectedPlatformSpecificPackageLocation2 = this.fixture.Combine(extensionsPackageLocation2, expectedPackageName2, this.fixture.PlatformArchitectureName);
            string expectedProfileLocation1 = this.fixture.Combine(expectedPlatformSpecificPackageLocation1, "profiles", expectedProfile1);
            string expectedProfileLocation2 = this.fixture.Combine(expectedPlatformSpecificPackageLocation2, "profiles", expectedProfile2);

            // The extensions packages exist
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName1, extensionsPackageLocation1, true);
            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName2, extensionsPackageLocation2, true);

            // User has provided an alternate location for extensions packages.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_PATH] = environmentVariableValue;

            // A platform-specific directory/content exists in the extensions packages.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation1))
                .Returns(true);

            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation2))
                .Returns(true);

            // Profiles directories exists for the platform.
            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(expectedPlatformSpecificPackageLocation1, "profiles")))
                .Returns(true);

            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(expectedPlatformSpecificPackageLocation2, "profiles")))
                .Returns(true);

            // Profiles exist in each of the extensions packages 'profiles' directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(this.fixture.Combine(expectedPlatformSpecificPackageLocation1, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedProfileLocation1 });

            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(this.fixture.Combine(expectedPlatformSpecificPackageLocation2, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedProfileLocation2 });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Profiles);
            Assert.IsTrue(extensions.Profiles.Count() == 2);
            Assert.AreEqual(extensions.Profiles.ElementAt(0).FullName, expectedProfileLocation1);
            Assert.AreEqual(extensions.Profiles.ElementAt(1).FullName, expectedProfileLocation2);
        }

        [Test]
        [TestCase("ANY-PROFILE.json")]
        [TestCase("ANY-PROFILE.yml")]
        [TestCase("ANY-PROFILE.yaml")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversProfileExtensionsThatExistInAPackageInTheDefaultPackagesLocation(string expectedProfileName)
        {
            string expectedPackageName = "any.extensions.pkg";
            string extensionsPackageLocation = this.fixture.GetPackagePath();
            string expectedPlatformSpecificPackageLocation = this.fixture.Combine(extensionsPackageLocation, expectedPackageName, this.fixture.PlatformArchitectureName);
            string expectedProfileLocation = this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles", expectedProfileName);

            // The extensions package exists
            this.SetupPackageExistsInDefaultPackagesLocation(expectedPackageName, true);

            // A platform-specific directory/content exists in the extensions package.
            this.fixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // A profiles directory exists for the platform.
            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles")))
                .Returns(true);

            // The profiles will be found in the extensions package platform-specific directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(this.fixture.Combine(expectedPlatformSpecificPackageLocation, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedProfileLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Profiles);
            Assert.IsTrue(extensions.Profiles.Count() == 1);
            Assert.AreEqual(extensions.Profiles.First().FullName, expectedProfileLocation);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversPackagesThatExistInAUserDefinedLocation()
        {
            string expectedPackageName = "package_123";
            string userDefinedPackageLocation = $@"C:/any/location/defined/by/the/user/to/packages";
            string expectedPackageLocation = $@"{userDefinedPackageLocation}/{expectedPackageName}";

            this.SetupPackageExistsInUserDefinedLocation(expectedPackageName, userDefinedPackageLocation);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(this.fixture.StandardizePath(expectedPackageLocation), this.fixture.StandardizePath(packages.First().Path));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversPackagesThatExistInTheDefaultPackagesDirectory()
        {
            string expectedPackageName = "package_123";
            string expectedPackageLocation = this.fixture.PlatformSpecifics.GetPackagePath(expectedPackageName);

            this.SetupPackageExistsInDefaultPackagesLocation(expectedPackageName);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(this.fixture.StandardizePath(expectedPackageLocation), this.fixture.StandardizePath(packages.First().Path));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void PackageManagerThrowsWhenDuplicatePackagesAreFoundDuringDiscovery()
        {
            // Packages that exist in the user-defined location are selected first.
            string userDefinedPath = @"C:/any/user/defined/location";
            this.SetupPackageExistsInUserDefinedLocation("package1", userDefinedPath);

            // Packages that exist in the default 'packages' folder location are selected next
            // but only if they are not found in the user-defined location.
            this.SetupPackageExistsInDefaultPackagesLocation(new string[] { "package1", "package2" });

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.DiscoverPackagesAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DuplicatePackagesFound, error.Reason);
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
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerCanFindPackagesThatAreAlreadyRegistered()
        {
            string expectedPackageName = "package_987";
            string expectedPackageLocation = $@"C:/any/location/for/virtualclient/packages/{expectedPackageName}";

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

            this.fixture.StateManager.OnSaveState((stateId, state) =>
            {
                Assert.AreEqual(this.mockPackage.Name, stateId);
                Assert.IsNotNull(state);
                Assert.AreEqual(JObject.FromObject(this.mockPackage), state);
                packageRegistered = true;
            });

            await this.packageManager.RegisterPackageAsync(this.mockPackage, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(packageRegistered);
        }

        [Test]
        public void PackageManagerValidatesRequiredPropertiesAreDefinedWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();
            this.mockPackageDescriptor.Clear();

            Assert.ThrowsAsync<ArgumentException>(() => this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()));
        }

        [Test]
        public void PackageManagerThrowsIfAnArchiveIsReferencedWithoutTheArchiveTypeDefinedWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();
            this.mockPackageDescriptor.Remove(nameof(DependencyDescriptor.ArchiveType));

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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
                Assert.IsTrue(object.ReferenceEquals(this.mockPackageDescriptor, description));
            };

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string expectedPackagePath = this.fixture.GetPackagePath(this.mockPackageDescriptor.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.fixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsTrue(confirmed);
            Assert.AreEqual(expectedPackagePath, actualPackagePath);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation_CustomInstallationPathProvided()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string customPath = "C:/my/custom/package/path";
            string expectedInstallationPath = this.fixture.Combine(customPath, this.mockPackageDescriptor.Name);
            string expectedPackagePath = this.fixture.Combine(customPath, this.mockPackageDescriptor.PackageName.ToLowerInvariant());

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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

            this.mockPackageDescriptor.Extract = false;
            string expectedPackagePath = this.fixture.GetPackagePath(this.mockPackageDescriptor.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.fixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.PackageName, this.mockPackageDescriptor.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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
            this.fixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            Assert.IsFalse(installed);
        }

        [Test]
        public async Task PackageManagerCreatesTheInstallationDirectoryIfItDoesNotExistWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // The package directory does not exist.
            this.fixture.Directory.Setup(dir => dir.Exists(this.packageManager.PackagesDirectory)).Returns(false);

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            this.fixture.Directory.Verify(dir => dir.CreateDirectory(this.packageManager.PackagesDirectory));
        }

        [Test]
        [TestCase("anypackage.1.0.0.zip")]
        [TestCase("ANYPACKAGE.1.0.0.ZIP")]
        public async Task PackageManagerExtractsDependencyPackagesDownloadedThatAreZipFilesToTheExpectedLocation(string name)
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.mockPackageDescriptor.Name = name;
            this.mockPackageDescriptor.ArchiveType = ArchiveType.Zip;
            string expectedArchivePath = this.fixture.GetPackagePath(name);
            string expectedDestinationPath = this.fixture.GetPackagePath("anypackage.1.0.0");

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) =>
            {
                Assert.AreEqual(expectedArchivePath, archivePath);
                Assert.AreEqual(expectedDestinationPath, destinationPath);
                Assert.AreEqual(ArchiveType.Zip, archiveType);
                packageExtracted = true;
            };

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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

            this.mockPackageDescriptor.Name = name;
            this.mockPackageDescriptor.ArchiveType = ArchiveType.Tgz;
            string expectedArchivePath = this.fixture.GetPackagePath(name);
            string expectedDestinationPath = this.fixture.GetPackagePath("anypackage.1.0.0");

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) =>
            {
                Assert.AreEqual(expectedArchivePath, archivePath);
                Assert.AreEqual(expectedDestinationPath, destinationPath);
                Assert.AreEqual(ArchiveType.Tgz, archiveType);
                packageExtracted = true;
            };

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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

            string expectedArchivePath = this.fixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.Name);

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) => packageExtracted = true;

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            this.fixture.File.Verify(file => file.Delete(expectedArchivePath));
            Assert.IsTrue(packageExtracted);
        }

        [Test]
        public async Task PackageMangerRegistersDependencyPackagesThatAreInstalled()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockPackage };

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            // When a package is registered
            this.fixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
                this.mockPackage.Name,
                It.IsAny<JObject>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy>()));
        }

        [Test]
        public async Task PackageMangerEnsuresPackagesInstalledAreRegisteredByThePackageNameDefinedInAdditionToAnyNamesDefinedWithinThePackageItself()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.mockPackageDescriptor.PackageName = this.mockPackage.Name + "-other";
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockPackage };

            await this.packageManager.InstallPackageAsync(
                this.fixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);

            // When a package is registered a state object is written. In this scenario we register the package by the
            // name defined in the package metadata description. We additionally register it by the name that was provided
            // in the descriptor.
            //
            // e.g. anyname
            this.fixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
                this.mockPackage.Name,
                It.IsAny<JObject>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IAsyncPolicy>()));

            // e.g. anyname-other
            this.fixture.StateManager.Verify(mgr => mgr.SaveStateAsync(
               this.mockPackageDescriptor.PackageName,
               It.IsAny<JObject>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<IAsyncPolicy>()));
        }

        public void SetupMocks(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture, useUnixStylePathsOnly: true);

            this.packageManager = new TestPackageManager(
                this.fixture.StateManager.Object,
                this.fixture.FileSystem.Object,
                this.fixture.PlatformSpecifics,
                logger: NullLogger.Instance);

            this.mockPackage = new DependencyPath(
                "anyname",
                platform == PlatformID.Unix ? "/home/any/path/to/dependency" : @"C:/any/users/path/to/dependency",
                "AnyDescription");

            this.mockPackageDescriptor = new BlobDescriptor
            {
                Name = "anyname.zip",
                ContainerName = "anycontainer",
                PackageName = "anyname",
                Extract = true,
                ArchiveType = ArchiveType.Zip
            };
        }

        private void SetupExtensionsExistsInUserDefinedLocation(string binaryName, string userDefinedBinaryLocation, bool extensionsPackage = false)
        {
            string packagePath = this.fixture.PlatformSpecifics.Combine(userDefinedBinaryLocation, binaryName);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables.Add(
                EnvironmentVariable.VC_LIBRARY_PATH,
                userDefinedBinaryLocation);

            // The *.vcpkg definition will be found in the target packages directory
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(userDefinedBinaryLocation, "*.dll", SearchOption.AllDirectories))
                .Returns(new string[] { this.fixture.Combine(userDefinedBinaryLocation, "Any.VirtualClient.Extensions.dll") });
        }

        private void SetupDependencyPackageInstallationDefaultMockBehaviors()
        {
            // The mock behavior setup here implements the default "happy path". This is the path
            // where the code performs the expected behaviors given the package has NOT been previously
            // downloaded/installed and does not hit any unexpected errors.
            this.fixture.Directory.SetupSequence(dir => dir.Exists(It.IsAny<string>()))
               .Returns(false)  // The package path does not already exist (and thus does not need to be deleted).
               .Returns(false)  // The blob file does not exist having been previously downloaded (and thus does not need to be deleted).
               .Returns(true);  // The package directory already exists (and so does not need to be created).
        }

        private void SetupPackageIsRegistered(string packageName, string packageLocation)
        {
            this.fixture.StateManager.OnGetState(packageName)
                .ReturnsAsync(JObject.FromObject(new DependencyPath(packageName, packageLocation)));

            this.fixture.Directory.Setup(dir => dir.Exists(packageLocation)).Returns(true);
        }

        private void SetupPackageExistsInUserDefinedLocation(string packageName, string userDefinedPackageLocation, bool extensionsPackage = false)
        {
            string packagePath = this.fixture.PlatformSpecifics.Combine(userDefinedPackageLocation, packageName);

            // No package registrations should be found.
            this.fixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_PATH] = userDefinedPackageLocation;

            // The packages parent directory exists.
            this.fixture.Directory.Setup(dir => dir.Exists(userDefinedPackageLocation))
                .Returns(true);

            // A package directory/content exists in the extensions package.
            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(userDefinedPackageLocation, packageName)))
                .Returns(true);

            // The *.vcpkg definition will be found in the target packages directory
            string vcpkgFilePath = this.fixture.Combine(packagePath, $"{packageName}.vcpkg");
            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(userDefinedPackageLocation, "*.vcpkg", SearchOption.AllDirectories))
                .Returns(new string[] { vcpkgFilePath });

            // The *.vcpkg file has valid package/dependency definition as content.
            DependencyPath package = new DependencyPath(packageName, Path.GetDirectoryName(vcpkgFilePath));
            if (extensionsPackage)
            {
                package.Metadata[PackageMetadata.Extensions] = true;
            }

            this.fixture.File.Setup(file => file.ReadAllTextAsync(vcpkgFilePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package.ToJson());
        }

        private void SetupPackageExistsInDefaultPackagesLocation(string packageName, bool extensionsPackage = false)
        {
            this.SetupPackageExistsInDefaultPackagesLocation(new string[] { packageName }, extensionsPackage);
        }

        private void SetupPackageExistsInDefaultPackagesLocation(IEnumerable<string> packageNames, bool extensionsPackage = false)
        {
            List<string> vcpkgFiles = new List<string>();

            // No package registrations should be found.
            this.fixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

            // The packages directory exists.
            this.fixture.Directory.Setup(dir => dir.Exists(this.fixture.Combine(this.fixture.GetPackagePath())))
                .Returns(true);

            foreach (string packageName in packageNames)
            {
                string packagePath = this.fixture.PlatformSpecifics.GetPackagePath(packageName);

                // A package directory/content exists in the package.
                this.fixture.Directory.Setup(dir => dir.Exists(packagePath))
                    .Returns(true);

                // The *.vcpkg definition will be found in the default packages directory
                string vcpkgFilePath = this.fixture.PlatformSpecifics.Combine(packagePath, $"{packageName}.vcpkg");
                vcpkgFiles.Add(vcpkgFilePath);

                // The *.vcpkg file has valid package/dependency definition as content.
                DependencyPath package = new DependencyPath(packageName, Path.GetDirectoryName(vcpkgFilePath));
                if (extensionsPackage)
                {
                    package.Metadata[PackageMetadata.Extensions] = true;
                }

                this.fixture.File.Setup(file => file.ReadAllTextAsync(vcpkgFilePath, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(package.ToJson());
            }

            this.fixture.Directory.Setup(dir => dir.EnumerateFiles(this.fixture.PlatformSpecifics.PackagesDirectory, "*.vcpkg", SearchOption.AllDirectories))
                .Returns(vcpkgFiles);
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
