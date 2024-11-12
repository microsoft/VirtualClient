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
    using Microsoft.CodeAnalysis;
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
        private DependencyPath mockPackage;
        private DependencyDescriptor mockPackageDescriptor;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
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
        public async Task PackageManagerEnsuresThePackagesDirectoryExistsWhenInitializing()
        {
            string packageDirectory = this.mockFixture.GetPackagePath();

            // Setup:
            // The packages directory already exists.
            this.mockFixture.Directory
                .Setup(dir => dir.Exists(packageDirectory))
                .Returns(true);

            await this.packageManager.InitializePackagesAsync(CancellationToken.None);

            // The package manager does not attempt to create the directory.
            this.mockFixture.Directory.Verify(dir => dir.CreateDirectory(packageDirectory), Times.Never);

            // Setup:
            // The packages directory does not exist.
            this.mockFixture.Directory
                .Setup(dir => dir.Exists(packageDirectory))
                .Returns(false);

            await this.packageManager.InitializePackagesAsync(CancellationToken.None);

            // The package manager is expected to create the directory.
            this.mockFixture.Directory.Verify(dir => dir.CreateDirectory(packageDirectory), Times.Once);
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
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation, "*.dll", SearchOption.TopDirectoryOnly))
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
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation1, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedBinaryLocation1 });

            // *.dlls will be found in the second packages directory
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsBinaryLocation2, "*.dll", SearchOption.TopDirectoryOnly))
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
            string extensionsPackageName1 = "any.extensions_1.pkg";
            string extensionsBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsPackageLocation = this.mockFixture.StandardizePath("C:/any/location/defined/by/the/user/to/packages");
            string extensionsPlatformSpecificPackageLocation1 = this.mockFixture.Combine(extensionsPackageLocation, extensionsPackageName1, this.mockFixture.PlatformArchitectureName);
            string expectedBinaryLocation = this.mockFixture.Combine(extensionsPlatformSpecificPackageLocation1, extensionsBinaryName);

            // Extensions packages having the same name exist in the user-defined packages location.
            this.SetupPackageExists(extensionsPackageName1, extensionsPackageLocation, true);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_DIR] = extensionsPackageLocation;
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).PackagesDirectory = extensionsPackageLocation;

            // A platform-specific directory/content exists in both the default and
            // user-defined extensions package location.
            this.mockFixture.Directory.Setup(dir => dir.Exists(extensionsPlatformSpecificPackageLocation1))
                .Returns(true);

            // *.dlls will be found in the extensions package platform-specific directories
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsPlatformSpecificPackageLocation1, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[]
                {
                    this.mockFixture.Combine(extensionsPlatformSpecificPackageLocation1, extensionsBinaryName)
                });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Binaries);
            Assert.IsTrue(extensions.Binaries.Count() == 1);
            Assert.AreEqual(extensions.Binaries.First().FullName, expectedBinaryLocation);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void PackageManagerThrowsWhenDuplicateExtensionsBinariesAreFoundDuringDiscovery()
        {
            string extensionsPackageName1 = "any.extensions_1.pkg";
            string extensionsPackageName2 = "any.extensions_2.pkg";
            string extensionsBinaryName = "Any.VirtualClient.Extensions.dll";
            string extensionsPackageLocation = this.mockFixture.StandardizePath("C:/any/location/defined/by/the/user/to/packages");
            string extensionsPlatformSpecificPackageLocation1 = this.mockFixture.Combine(extensionsPackageLocation, extensionsPackageName1, this.mockFixture.PlatformArchitectureName);
            string extensionsPlatformSpecificPackageLocation2 = this.mockFixture.Combine(extensionsPackageLocation, extensionsPackageName2, this.mockFixture.PlatformArchitectureName);

            // Extensions packages having the same name exist in the user-defined packages location.
            this.SetupPackageExists(new string[] { extensionsPackageName1, extensionsPackageName2 }, extensionsPackageLocation, true);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_DIR] = extensionsPackageLocation;
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).PackagesDirectory = extensionsPackageLocation;

            // A platform-specific directory/content exists in both the default and
            // user-defined extensions package location.
            this.mockFixture.Directory.Setup(dir => dir.Exists(extensionsPlatformSpecificPackageLocation1))
                .Returns(true);

            this.mockFixture.Directory.Setup(dir => dir.Exists(extensionsPlatformSpecificPackageLocation2))
                .Returns(true);

            // *.dlls will be found in the extensions package platform-specific directories
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsPlatformSpecificPackageLocation1, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[] 
                {
                    this.mockFixture.Combine(extensionsPlatformSpecificPackageLocation1, extensionsBinaryName)
                });

            // *.dlls will be found in the extensions package platform-specific directories
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(extensionsPlatformSpecificPackageLocation2, "*.dll", SearchOption.TopDirectoryOnly))
                .Returns(new string[]
                {
                    this.mockFixture.Combine(extensionsPlatformSpecificPackageLocation2, extensionsBinaryName)
                });

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.DiscoverExtensionsAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DuplicateExtensionsFound, error.Reason);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversBinariesThatExistInAnExtensionsPackageLocationDefinedInTheDefaultLocation()
        {
            string expectedPackageName = "any.extensions.pkg";
            string expectedBinaryName = "Any.VirtualClient.Extensions.dll";
            string defaultPackageLocation = this.mockFixture.GetPackagePath();
            string expectedPlatformSpecificPackageLocation = this.mockFixture.Combine(defaultPackageLocation, expectedPackageName, this.mockFixture.PlatformArchitectureName);
            string expectedBinaryLocation = this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, expectedBinaryName);

            // The extensions package exists
            this.SetupPackageExists(expectedPackageName, defaultPackageLocation, true);

            // A platform-specific directory/content exists in the extensions package.
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // The *.dll will be found in the extensions package platform-specific directory
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(expectedPlatformSpecificPackageLocation, "*.dll", SearchOption.TopDirectoryOnly))
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
            string extensionsPackageLocation = this.mockFixture.Combine("any", "location", "defined", "by", "the", "user", "to", "packages");
            string expectedPlatformSpecificPackageLocation = this.mockFixture.Combine(extensionsPackageLocation, expectedPackageName, this.mockFixture.PlatformArchitectureName);
            string expectedProfileLocation = this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles", expectedProfileName);

            // The extensions package exists
            this.SetupPackageExists(expectedPackageName, extensionsPackageLocation, true);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_DIR] = extensionsPackageLocation;
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).PackagesDirectory = extensionsPackageLocation;

            // A platform-specific directory/content exists in the extensions package.
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // A profiles directory exists for the platform.
            this.mockFixture.Directory.Setup(dir => dir.Exists(this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles")))
                .Returns(true);

            // The profiles will be found in the extensions package platform-specific directory
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
                .Returns(new string[] { expectedProfileLocation });

            PlatformExtensions extensions = await this.packageManager.DiscoverExtensionsAsync(CancellationToken.None);

            Assert.IsNotNull(extensions);
            Assert.IsNotEmpty(extensions.Profiles);
            Assert.IsTrue(extensions.Profiles.Count() == 1);
            Assert.AreEqual(extensions.Profiles.First().FullName, expectedProfileLocation);
        }

        [Test]
        [TestCase("ANY-PROFILE.json")]
        [TestCase("ANY-PROFILE.yml")]
        [TestCase("ANY-PROFILE.yaml")]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversProfileExtensionsThatExistInAPackageInTheDefaultPackagesLocation(string expectedProfileName)
        {
            string expectedPackageName = "any.extensions.pkg";
            string defaultPackageLocation = this.mockFixture.GetPackagePath();
            string expectedPlatformSpecificPackageLocation = this.mockFixture.Combine(defaultPackageLocation, expectedPackageName, this.mockFixture.PlatformArchitectureName);
            string expectedProfileLocation = this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles", expectedProfileName);

            // The extensions package exists
            this.SetupPackageExists(expectedPackageName, defaultPackageLocation, true);

            // A platform-specific directory/content exists in the extensions package.
            this.mockFixture.Directory.Setup(dir => dir.Exists(expectedPlatformSpecificPackageLocation))
                .Returns(true);

            // A profiles directory exists for the platform.
            this.mockFixture.Directory.Setup(dir => dir.Exists(this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles")))
                .Returns(true);

            // The profiles will be found in the extensions package platform-specific directory
            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(this.mockFixture.Combine(expectedPlatformSpecificPackageLocation, "profiles"), "*.*", SearchOption.TopDirectoryOnly))
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

            this.SetupPackageExists(expectedPackageName, userDefinedPackageLocation);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_DIR] = userDefinedPackageLocation;
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).PackagesDirectory = userDefinedPackageLocation;

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(this.mockFixture.StandardizePath(expectedPackageLocation), this.mockFixture.StandardizePath(packages.First().Path));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerDiscoversPackagesThatExistInTheDefaultPackagesDirectory()
        {
            string expectedPackageName = "package_123";
            string defaultPackageLocation = this.mockFixture.GetPackagePath();
            string expectedPackageLocation = this.mockFixture.GetPackagePath(expectedPackageName);

            this.SetupPackageExists(expectedPackageName, defaultPackageLocation);

            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None);

            Assert.IsNotNull(packages);
            Assert.IsNotEmpty(packages);
            Assert.IsTrue(packages.Count() == 1);
            Assert.AreEqual(expectedPackageName, packages.First().Name);
            Assert.AreEqual(this.mockFixture.StandardizePath(expectedPackageLocation), this.mockFixture.StandardizePath(packages.First().Path));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void PackageManagerThrowsWhenDuplicatePackagesAreFoundDuringDiscoveryInTheDefaultPackagesDirectory()
        {
            // Packages that exist in the user-defined location are selected first.
            string defaultPackageLocation = this.mockFixture.GetPackagePath();
            this.SetupPackageExists("package1", defaultPackageLocation);

            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(defaultPackageLocation, "*.vcpkg", SearchOption.AllDirectories))
               .Returns(new List<string>
               {
                   this.mockFixture.Combine(defaultPackageLocation, "package1a.vcpkg") ,
                   this.mockFixture.Combine(defaultPackageLocation, "package1b.vcpkg")
               });

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(".vcpkg")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    // The *.vcpkg file has valid package/dependency definition as content.
                    DependencyPath package = new DependencyPath("package1", this.mockFixture.Combine(defaultPackageLocation, "package1a.vcpkg"));
                    return package.ToJson();
                });

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.DiscoverPackagesAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DuplicatePackagesFound, error.Reason);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void PackageManagerThrowsWhenDuplicatePackagesAreFoundDuringDiscoveryInAUserDefinedLocation()
        {
            // Packages that exist in the user-defined location are selected first.
            string userDefinedPath = @"C:/any/user/defined/location";
            this.SetupPackageExists("package1", userDefinedPath);

            // Setup a mock location via the environment variable.
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).EnvironmentVariables[EnvironmentVariable.VC_PACKAGES_DIR] = userDefinedPath;
            (this.packageManager.PlatformSpecifics as TestPlatformSpecifics).PackagesDirectory = userDefinedPath;

            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(userDefinedPath, "*.vcpkg", SearchOption.AllDirectories))
               .Returns(new List<string>
               {
                   this.mockFixture.Combine(userDefinedPath, "package1a.vcpkg") ,
                   this.mockFixture.Combine(userDefinedPath, "package1b.vcpkg")
               });

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(".vcpkg")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    // The *.vcpkg file has valid package/dependency definition as content.
                    DependencyPath package = new DependencyPath("package1", this.mockFixture.Combine(userDefinedPath, "package1a.vcpkg"));
                    return package.ToJson();
                });

            DependencyException error = Assert.ThrowsAsync<DependencyException>(() => this.packageManager.DiscoverPackagesAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DuplicatePackagesFound, error.Reason);
        }

        [Test]
        public async Task PackageManagerHandlesScenariosWhereThereAreNotAnyPackagesOnTheSystemToDiscover()
        {
            IEnumerable<DependencyPath> packages = await this.packageManager.DiscoverPackagesAsync(CancellationToken.None);

            Assert.IsNotNull(packages);
            Assert.IsEmpty(packages);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerFindsPackagesThatExistAndAreRegisteredInTheDefaultPackagesLocation()
        {
            string expectedPackageName = "package_987";
            string expectedPackagePath = this.mockFixture.GetPackagePath(expectedPackageName);
            string expectedPackageRegistrationFile = this.mockFixture.GetPackagePath($"{expectedPackageName}.vcpkgreg");

            this.mockFixture.File.Setup(file => file.Exists(expectedPackageRegistrationFile)).Returns(true);

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(expectedPackageRegistrationFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    // The *.vcpkg file has valid package/dependency definition as content.
                    DependencyPath package = new DependencyPath(expectedPackageName, expectedPackagePath);
                    return package.ToJson();
                });

            DependencyPath actualPackage = await this.packageManager.GetPackageAsync(expectedPackageName, CancellationToken.None);

            Assert.IsNotNull(actualPackage);
            Assert.AreEqual(expectedPackageName, actualPackage.Name);
            Assert.AreEqual(expectedPackagePath, actualPackage.Path);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task PackageManagerFindsPackagesThatExistAndAreRegisteredInTheBuiltInToolsLocation()
        {
            string expectedPackageName = "tools_package_123";
            string expectedPackagePath = this.mockFixture.GetToolsPath(expectedPackageName);
            string expectedPackageRegistrationFile = this.mockFixture.GetToolsPath($"{expectedPackageName}.vcpkgreg");

            this.mockFixture.File.Setup(file => file.Exists(expectedPackageRegistrationFile)).Returns(true);

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(expectedPackageRegistrationFile, It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    // The *.vcpkg file has valid package/dependency definition as content.
                    DependencyPath package = new DependencyPath(expectedPackageName, expectedPackagePath);
                    return package.ToJson();
                });

            DependencyPath actualPackage = await this.packageManager.GetPackageAsync(expectedPackageName, CancellationToken.None);

            Assert.IsNotNull(actualPackage);
            Assert.AreEqual(expectedPackageName, actualPackage.Name);
            Assert.AreEqual(expectedPackagePath, actualPackage.Path);
        }

        [Test]
        public async Task PackageManagerRegistersAPackageAsExpectedInTheDefaultPackagesDirectory()
        {
            bool packageRegistered = false;
            string packageName = "anypackage.1.0.0";
            string expectedPackagePath = this.mockFixture.GetPackagePath(packageName.ToLowerInvariant());
            string expectedRegistrationPath = this.mockFixture.GetPackagePath($"{packageName.ToLowerInvariant()}.vcpkgreg");

            DependencyPath expectedPackage = new DependencyPath(packageName, expectedPackagePath, "AnyDescription");

            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(expectedRegistrationPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    DependencyPath actualPackage = content.FromJson<DependencyPath>();
                    Assert.AreEqual(expectedPackage.Name, actualPackage.Name);
                    Assert.AreEqual(expectedPackage.Path, actualPackage.Path);
                    packageRegistered = true;
                });

            await this.packageManager.RegisterPackageAsync(expectedPackage, CancellationToken.None);
            Assert.IsTrue(packageRegistered);
        }

        [Test]
        public async Task PackageManagerRegistersAPackageAsExpectedInAUserDefinedPackagesDirectory()
        {
            bool packageRegistered = false;
            string packageName = "anypackage.1.0.0";
            string userDefinedPackagesPath = this.mockFixture.Combine("any", "user", "defined", "packages", "path");
            string expectedPackagePath = this.mockFixture.Combine(userDefinedPackagesPath, packageName.ToLowerInvariant());
            string expectedRegistrationPath = this.mockFixture.Combine(userDefinedPackagesPath, $"{packageName.ToLowerInvariant()}.vcpkgreg");

            DependencyPath expectedPackage = new DependencyPath(packageName, expectedPackagePath, "AnyDescription");
            this.packageManager.PlatformSpecifics.PackagesDirectory = userDefinedPackagesPath;

            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(expectedRegistrationPath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    DependencyPath actualPackage = content.FromJson<DependencyPath>();
                    Assert.AreEqual(expectedPackage.Name, actualPackage.Name);
                    Assert.AreEqual(expectedPackage.Path, actualPackage.Path);
                    packageRegistered = true;
                });

            await this.packageManager.RegisterPackageAsync(expectedPackage, CancellationToken.None);
            Assert.IsTrue(packageRegistered);
        }

        [Test]
        public void PackageManagerValidatesRequiredPropertiesAreDefinedWhenInstallingDependencyPackages()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();
            this.mockPackageDescriptor.Clear();

            Assert.ThrowsAsync<ArgumentException>(() => this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
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
                this.mockFixture.PackagesBlobManager.Object,
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
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);
        }

        [Test]
        public async Task PackageManagerInstallsDependencyPackagesToTheExpectedLocation()
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string expectedPackagePath = this.mockFixture.GetPackagePath(this.mockPackageDescriptor.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
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
            string expectedInstallationPath = this.mockFixture.Combine(customPath, this.mockPackageDescriptor.Name);
            string expectedPackagePath = this.mockFixture.Combine(customPath, this.mockPackageDescriptor.PackageName.ToLowerInvariant());

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
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
            string expectedPackagePath = this.mockFixture.GetPackagePath(this.mockPackageDescriptor.PackageName.ToLowerInvariant());
            string expectedInstallationPath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.PackageName, this.mockPackageDescriptor.Name);

            bool confirmed = false;
            this.packageManager.OnDownloadDependencyPackage = (description, installationPath, token) =>
            {
                Assert.AreEqual(expectedInstallationPath, installationPath);
                confirmed = true;
            };

            string actualPackagePath = await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
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
            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
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
            this.mockFixture.Directory.Setup(dir => dir.Exists(this.packageManager.PackagesDirectory)).Returns(false);

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
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

            this.mockPackageDescriptor.Name = name;
            this.mockPackageDescriptor.ArchiveType = ArchiveType.Zip;
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
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

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
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

            Assert.IsTrue(packageExtracted);
        }

        [Test]
        [TestCase(ArchiveType.Zip)]
        [TestCase(ArchiveType.Tgz)]
        public async Task PackageManagerDeletesDependencyArchiveFilesDownloadedAfterItHasSuccessfullyExtractedThePackageFromIt(ArchiveType archiveType)
        {
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            string expectedArchivePath = this.mockFixture.Combine(this.packageManager.PackagesDirectory, this.mockPackageDescriptor.Name);

            bool packageExtracted = false;
            this.packageManager.OnExtractArchive = (archivePath, destinationPath, archiveType, token) => packageExtracted = true;

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

            this.mockFixture.File.Verify(file => file.Delete(expectedArchivePath));
            Assert.IsTrue(packageExtracted);
        }

        [Test]
        public async Task PackageManagerRegistersDependencyPackagesThatAreInstalledInTheDefaultPackagesDirectory()
        {
            string expectedRegistrationFile = this.mockFixture.GetPackagePath($"{this.mockPackage.Name.ToLowerInvariant()}.vcpkgreg");
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockPackage };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

            // When a package is registered
            this.mockFixture.File.Verify(file => file.WriteAllTextAsync(
                expectedRegistrationFile,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PackageManagerRegistersDependencyPackagesThatAreInstalledInAUserDefinedPackagesLocation()
        {
            string userDefinedPackagesDirectory = this.mockFixture.Combine("any", "user", "defined", "packages", "location");
            string expectedPackagePath = this.mockFixture.Combine(userDefinedPackagesDirectory, "anypackage.1.0.0");
            this.mockPackage = new DependencyPath("anypackage.1.0.0", expectedPackagePath);

            string expectedRegistrationFile = this.mockFixture.Combine(userDefinedPackagesDirectory, $"{this.mockPackage.Name.ToLowerInvariant()}.vcpkgreg");
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            this.packageManager.PlatformSpecifics.PackagesDirectory = userDefinedPackagesDirectory;

            // Ensure the package definition is discovered
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockPackage };

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

            // When a package is registered
            this.mockFixture.File.Verify(file => file.WriteAllTextAsync(
                expectedRegistrationFile,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PackageManagerEnsuresPackagesInstalledAreRegisteredByThePackageNameDefinedInAdditionToAnyNamesDefinedWithinThePackageItself()
        {     
            this.SetupDependencyPackageInstallationDefaultMockBehaviors();

            // Ensure the package definition is discovered
            this.mockPackageDescriptor.PackageName = this.mockPackage.Name + "-other";
            this.packageManager.OnDiscoverPackages = (path) => new List<DependencyPath> { this.mockPackage };

            string expectedRegistrationFile1 = this.mockFixture.GetPackagePath($"{this.mockPackage.Name.ToLowerInvariant()}.vcpkgreg");
            string expectedRegistrationFile2 = this.mockFixture.GetPackagePath($"{this.mockPackageDescriptor.PackageName.ToLowerInvariant()}.vcpkgreg");

            await this.packageManager.InstallPackageAsync(
                this.mockFixture.PackagesBlobManager.Object,
                this.mockPackageDescriptor,
                CancellationToken.None,
                retryPolicy: Policy.NoOpAsync());

            // When a package is registered a state object is written. In this scenario we register the package by the
            // name defined in the package metadata description. We additionally register it by the name that was provided
            // in the descriptor.
            //
            // e.g. anyname
            // When a package is registered
            this.mockFixture.File.Verify(file => file.WriteAllTextAsync(
                expectedRegistrationFile1,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()));

            // e.g. anyname-other
            this.mockFixture.File.Verify(file => file.WriteAllTextAsync(
                expectedRegistrationFile2,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        }

        public void SetupMocks(PlatformID platform, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture, useUnixStylePathsOnly: true);

            this.packageManager = new TestPackageManager(
                this.mockFixture.PlatformSpecifics,
                this.mockFixture.FileSystem.Object,
                logger: NullLogger.Instance);

            this.mockPackage = new DependencyPath(
                "anyname",
                this.mockFixture.GetPackagePath("anyname.1.0.0"),
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

        private void SetupPackageExists(string packageName, string packagesPath, bool extensionsPackage = false)
        {
            this.SetupPackageExists(new string[] { packageName }, packagesPath, extensionsPackage);
        }

        private void SetupPackageExists(IEnumerable<string> packageNames, string packagesPath, bool extensionsPackage = false)
        {
            List<string> vcpkgFiles = new List<string>();

            // No package registrations should be found.
            this.mockFixture.StateManager.OnGetState().ReturnsAsync(null as JObject);

            // The packages parent directory exists.
            this.mockFixture.Directory.Setup(dir => dir.Exists(packagesPath))
                .Returns(true);

            foreach (string packageName in packageNames)
            {
                string packagePath = this.mockFixture.PlatformSpecifics.Combine(packagesPath, packageName);

                // A package directory/content exists in the extensions package.
                this.mockFixture.Directory.Setup(dir => dir.Exists(this.mockFixture.Combine(packagesPath, packageName)))
                    .Returns(true);

                // The *.vcpkg definition will be found in the target packages directory
                string vcpkgFilePath = this.mockFixture.Combine(packagePath, $"{packageName}.vcpkg");
                vcpkgFiles.Add(vcpkgFilePath);

                this.mockFixture.File.Setup(file => file.ReadAllTextAsync(vcpkgFilePath, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(() =>
                    {
                        // The *.vcpkg file has valid package/dependency definition as content.
                        DependencyPath package = new DependencyPath(packageName, Path.GetDirectoryName(vcpkgFilePath));
                        if (extensionsPackage)
                        {
                            package.Metadata[PackageMetadata.Extensions] = true;
                        }

                        return package.ToJson();
                    });
            }

            this.mockFixture.Directory.Setup(dir => dir.EnumerateFiles(packagesPath, "*.vcpkg", SearchOption.AllDirectories))
                .Returns(vcpkgFiles);
        }

        private class TestPackageManager : PackageManager
        {
            public TestPackageManager(PlatformSpecifics platformSpecifics, IFileSystem fileSystem, ILogger logger = null)
                : base(platformSpecifics, fileSystem, logger)
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
