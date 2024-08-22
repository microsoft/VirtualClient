// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class ExtensionsDiscoveryTests
    {
        private static Assembly TestAssembly = Assembly.GetAssembly(typeof(ExtensionsDiscoveryTests));

        private FileSystem fileSystem;
        private PlatformSpecifics platformSpecifics;

        [SetUp]
        public void SetupTest()
        {
            this.fileSystem = new FileSystem();
            this.platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_LIBRARY_PATH, null);
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, null);
        }

        [Test]
        [Order(0)]
        public async Task PackageManagerDiscoversExtensionsInTheDefaultPackagesDirectory()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in the default 'packages' directory.
            //
            // Note that the project build will output 1 or more example extensions packages
            // to a 'packages' directory for the sake of testing in this project.
            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages' directory in the build output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 1);
                Assert.AreEqual("Example.VirtualClient.Extensions_1.dll", extensions.Binaries.ElementAt(0).Name);

                Assert.IsNotNull(extensions?.Profiles);
                Assert.IsNotEmpty(extensions.Profiles);
                Assert.IsTrue(extensions?.Profiles?.Count() == 3);
                Assert.AreEqual("EXAMPLE-EXTENSIONS-1.json", extensions.Profiles.ElementAt(0).Name);
                Assert.AreEqual("EXAMPLE-EXTENSIONS-1.yml", extensions.Profiles.ElementAt(1).Name);
                Assert.AreEqual("EXAMPLE-EXTENSIONS-2.yaml", extensions.Profiles.ElementAt(2).Name);
            }
        }

        [Test]
        [Order(1)]
        public async Task PackageManagerDiscoversExtensionsInADirectoryDefinedByThe_VC_PACKAGES_DIR_EnvironmentVariable()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in a directory defined in the VC_PACKAGES_DIR environment variable.
            //
            // Note that the project build will output 1 or more example extensions packages
            // to an 'extensions_packages' directory for the sake of testing in this project.
            string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
            string extensionsPackageDirectory = Path.Combine(testOutputDirectory, "extensions_packages", "extensions_package_2");

            // The VC_PACKAGES_DIR environment variable is used to allow a user to define the directory where
            // packages exist (including extensions) and to which packages should be downloaded. In practice this
            // directory is overridable on the PlatformSpecifics instance on application startup.
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, extensionsPackageDirectory);
            this.platformSpecifics.PackagesDirectory = extensionsPackageDirectory;

            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages' and 'extensions_packages/extensions_package_2' directories in the build
                // output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 1);
                Assert.IsNotNull(extensions?.Profiles);
                Assert.IsNotEmpty(extensions.Profiles);
                Assert.IsTrue(extensions?.Profiles?.Count() == 1);

                // Extensions in the environment variable-defined location.
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_2.dll") == 1);
                Assert.IsTrue(extensions.Profiles.Count(bin => bin.Name == "EXAMPLE-EXTENSIONS-2.json") == 1);
            }
        }

        [Test]
        [Order(2)]
        public async Task PackageManagerDiscoversExtensionsInADirectoryDefinedByThe_VC_PACKAGES_DIR_EnvironmentVariable_Multiple_Packages()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in a directory defined in the VC_PACKAGES_DIR environment variable.
            //
            // Note that the project build will output 1 or more example extensions packages
            // to an 'extensions_packages' directory for the sake of testing in this project.
            string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
            string extensionsPackageDirectory = Path.Combine(testOutputDirectory, "extensions_packages");

            // The VC_PACKAGES_DIR environment variable is used to allow a user to define the directory where
            // packages exist (including extensions) and to which packages should be downloaded. In practice this
            // directory is overridable on the PlatformSpecifics instance on application startup.
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, extensionsPackageDirectory);
            this.platformSpecifics.PackagesDirectory = extensionsPackageDirectory;

            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                // The VC_PACKAGES_PATH environment variable allows a user to define 1 or more paths
                // delimited by a semi-colon ';' where packages exist.
                Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, extensionsPackageDirectory);

                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages' and 'extensions_packages' directories in the build output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 2);
                Assert.IsNotNull(extensions?.Profiles);
                Assert.IsNotEmpty(extensions.Profiles);
                Assert.IsTrue(extensions?.Profiles?.Count() == 2);

                // Extensions in the environment variable-defined locations.
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_2.dll") == 1);
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_3.dll") == 1);
                Assert.IsTrue(extensions.Profiles.Count(bin => bin.Name == "EXAMPLE-EXTENSIONS-2.json") == 1);
                Assert.IsTrue(extensions.Profiles.Count(bin => bin.Name == "EXAMPLE-EXTENSIONS-3.yml") == 1);
            }
        }

        [Test]
        [Order(4)]
        public async Task PackageManagerDiscoversBinaryExtensionsInADirectoryDefinedByThe_VC_LIBRARY_PATH_EnvironmentVariable()
        {
            // Expected:
            // The package manager will discover binary extensions packages in a directory defined in
            // the VC_LIBRARY_PATH environment variable.
            //
            // Note that the project build will output 1 or more example binary extensions 
            // to an 'extensions' directory for the sake of testing in this project.
            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
                string extensionsPackageDirectory = Path.Combine(testOutputDirectory, "extensions", "extensions_1");

                // The VC_LIBRARY_PATH environment variable allows a user to define 1 or more paths
                // delimited by a semi-colon ';' where binary extensions exist.
                Environment.SetEnvironmentVariable(EnvironmentVariable.VC_LIBRARY_PATH, extensionsPackageDirectory);

                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages' and 'extensions' directories in the build
                // output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 2);

                // Default 'packages' location extensions
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_1.dll") == 1);

                // Extensions in the environment variable-defined location.
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_4.dll") == 1);
            }
        }

        [Test]
        [Order(5)]
        public async Task PackageManagerDiscoversBinaryExtensionsInADirectoryDefinedByThe_VC_LIBRARY_PATH_EnvironmentVariable_Multiple_Locations()
        {
            // Expected:
            // The package manager will discover binary extensions packages in a directory defined in
            // the VC_LIBRARY_PATH environment variable.
            //
            // Note that the project build will output 1 or more example binary extensions 
            // to an 'extensions' directory for the sake of testing in this project.
            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
                string extensionsDirectory1 = Path.Combine(testOutputDirectory, "extensions", "extensions_1");
                string extensionsDirectory2 = Path.Combine(testOutputDirectory, "extensions", "extensions_2");

                // The VC_LIBRARY_PATH environment variable allows a user to define 1 or more paths
                // delimited by a semi-colon ';' where binary extensions exist.
                Environment.SetEnvironmentVariable(EnvironmentVariable.VC_LIBRARY_PATH, $"{extensionsDirectory1};{extensionsDirectory2}");

                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages' and 'extensions' directories in the build
                // output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 3);

                // Default 'packages' location extensions
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_1.dll") == 1);

                // Extensions in the environment variable-defined location.
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_4.dll") == 1);
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_5.dll") == 1);
            }
        }

        [Test]
        [Order(6)]
        public async Task PackageManagerDiscoversExtensionsWhenBothSetsOfEnvironmentVariablesAreUsedAtTheSameTime_Putting_It_All_Together()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in a directory defined in the VC_PACKAGES_DIR environment variable.
            //
            // The package manager will discover binary extensions packages in a directory defined in
            // the VC_LIBRARY_PATH environment variable.
            string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
            string extensionsPackageDirectory = Path.Combine(testOutputDirectory, "extensions_packages");
            string extensionsDirectory1 = Path.Combine(testOutputDirectory, "extensions", "extensions_1");
            string extensionsDirectory2 = Path.Combine(testOutputDirectory, "extensions", "extensions_2");

            // The VC_PACKAGES_DIR environment variable is used to allow a user to define the directory where
            // packages exist (including extensions) and to which packages should be downloaded. In practice this
            // directory is overridable on the PlatformSpecifics instance on application startup.
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_LIBRARY_PATH, $"{extensionsDirectory1};{extensionsDirectory2}");
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, extensionsPackageDirectory);
            this.platformSpecifics.PackagesDirectory = extensionsPackageDirectory;

            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                PlatformExtensions extensions = await packageManager.DiscoverExtensionsAsync(CancellationToken.None);

                // See the 'packages', 'extensions', and 'extensions_packages' directories in the build
                // output location.
                Assert.IsNotNull(extensions?.Binaries);
                Assert.IsNotEmpty(extensions.Binaries);
                Assert.IsTrue(extensions?.Binaries?.Count() == 4);
                Assert.IsNotNull(extensions?.Profiles);
                Assert.IsNotEmpty(extensions.Profiles);
                Assert.IsTrue(extensions?.Profiles?.Count() == 2);

                // Extensions in the environment variable-defined locations.
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_2.dll") == 1);
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_3.dll") == 1);
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_4.dll") == 1);
                Assert.IsTrue(extensions.Binaries.Count(bin => bin.Name == "Example.VirtualClient.Extensions_5.dll") == 1);
                Assert.IsTrue(extensions.Profiles.Count(bin => bin.Name == "EXAMPLE-EXTENSIONS-2.json") == 1);
                Assert.IsTrue(extensions.Profiles.Count(bin => bin.Name == "EXAMPLE-EXTENSIONS-3.yml") == 1);
            }
        }

        [Test]
        [Order(20)]
        public async Task PackageManagerDiscoversPackagesInTheDefaultPackagesDirectory()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in the default 'packages' directory.
            //
            // Note that the project build will output 1 or more example extensions packages
            // to a 'packages' directory for the sake of testing in this project.
            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                IEnumerable<DependencyPath> packages = await packageManager.DiscoverPackagesAsync(CancellationToken.None);

                // See the 'packages' directory in the build output location.
                Assert.IsNotNull(packages);
                Assert.IsNotEmpty(packages);
                Assert.IsTrue(packages.Count() == 1);
                Assert.AreEqual("extensions_package_1", packages.ElementAt(0).Name);
            }
        }

        [Test]
        [Order(21)]
        public async Task PackageManagerDiscoversPackagesInADirectoryDefinedByThe_VC_PACKAGES_DIR_EnvironmentVariable()
        {
            // Expected:
            // The package manager will discover the extensions packages (related binaries/.dlls and profiles)
            // in the directory defined by the VC_PACKAGES_DIR environment variable.
            //
            // Note that the project build will output 1 or more example extensions packages
            // to a 'packages' directory for the sake of testing in this project.
            string testOutputDirectory = Path.GetDirectoryName(ExtensionsDiscoveryTests.TestAssembly.Location);
            string packageDirectory = Path.Combine(testOutputDirectory, "extensions_packages", "extensions_package_2");

            // The VC_PACKAGES_DIR environment variable is used to allow a user to define the directory where
            // packages exist (including extensions) and to which packages should be downloaded. In practice this
            // directory is overridable on the PlatformSpecifics instance on application startup.
            Environment.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, packageDirectory);
            this.platformSpecifics.PackagesDirectory = packageDirectory;

            using (PackageManager packageManager = new PackageManager(this.platformSpecifics, this.fileSystem))
            {
                IEnumerable<DependencyPath> packages = await packageManager.DiscoverPackagesAsync(CancellationToken.None);

                // See the 'packages' directory in the build output location.
                Assert.IsNotNull(packages);
                Assert.IsNotEmpty(packages);
                Assert.IsTrue(packages.Count() == 1);

                // Packages in the VC_PACKAGES_PATH environment variable locations.
                Assert.IsTrue(packages.Count(pkg => pkg.Name == "extensions_package_2") == 1);
            }
        }
    }
}
