// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    [TestFixture]
    [Category("Integration")]
    public class PackageManagerTests
    {
        // The VC PackageManager is a rather complex part of the core dependencies. The integration
        // tests below will operate against the actual file system with actual packages to ensure
        // that core behaviors work as expected.

        private PackageManager packageManager;
        private string packagesDirectory;
        private string resourcesDirectory;
        private IFileSystem fileSystem;
        private List<string> packages;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fileSystem = new FileSystem();
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

            this.resourcesDirectory = platformSpecifics.Combine(DependencyFixture.TestAssemblyDirectory, "Resources");
            this.packagesDirectory = platformSpecifics.PackagesDirectory;
            this.packageManager = new PackageManager(platformSpecifics, fileSystem);

            // Note:
            // The name of the packages MUST match the names of the .zip files in the
            // 'Resources' directory.
            this.packages = new List<string>
            {
                Path.Combine(this.packagesDirectory, "package1"),
                Path.Combine(this.packagesDirectory, "package2")
            };
        }

        [Test]
        [Order(1)]
        public async Task PackageManagerInitializesAndDiscoversPackagesThatArePreExistingOnTheSystem()
        {
            try
            {
                this.CleanupPackagesDirectory();
                this.CopyPackagesIntoPackagesDirectory();

                // The package manager will extract any archive files on the system (e.g. .zip, .tar.gz, .tgz).
                List<string> expectedDirectories = new List<string>
                {
                    Path.Combine(this.packagesDirectory, "package1"),
                    Path.Combine(this.packagesDirectory, "package2")
                };

                await this.packageManager.InitializePackagesAsync(CancellationToken.None).ConfigureAwait(false);
                foreach (string expectedDirectory in expectedDirectories)
                {
                    Assert.IsTrue(this.fileSystem.Directory.Exists(expectedDirectory));
                }

                // The package manager will discover *.vcpkg definitions in the package folders existing on
                // the system and will register each of them. The registrations are simple JSON files written
                // to the 'packages' directory.
                List<string> expectedRegistrations = new List<string>
                {
                     Path.Combine(this.packagesDirectory, "package1.vcpkgreg"),
                     Path.Combine(this.packagesDirectory, "package2.vcpkgreg")
                };

                await this.packageManager.DiscoverPackagesAsync(CancellationToken.None).ConfigureAwait(false);
                foreach (string expectedRegistrationFile in expectedRegistrations)
                {
                    Assert.IsTrue(this.fileSystem.File.Exists(expectedRegistrationFile));

                    DependencyPath registeredPackage = this.fileSystem.File.ReadAllText(expectedRegistrationFile).FromJson<DependencyPath>();
                    Assert.AreEqual(Path.GetFileNameWithoutExtension(expectedRegistrationFile), registeredPackage.Name);
                }
            }
            finally
            {
                this.CleanupPackagesDirectory();
            }
        }

        [Test]
        [Order(2)]
        public async Task PackageManagerCanFindPackagesThatAreRegisteredOnTheSystem()
        {
            try
            {
                this.CleanupPackagesDirectory();
                this.CopyPackagesIntoPackagesDirectory();

                Dictionary<string, string> expectedPackages = new Dictionary<string, string>
                {
                    { "package1", Path.Combine(this.packagesDirectory, "package1", "runtimes") },
                    { "package2", Path.Combine(this.packagesDirectory, "package2", "runtimes") }
                };

                await this.packageManager.InitializePackagesAsync(CancellationToken.None).ConfigureAwait(false);
                await this.packageManager.DiscoverPackagesAsync(CancellationToken.None).ConfigureAwait(false);

                foreach (var entry in expectedPackages)
                {
                    string expectedPackageName = entry.Key;
                    string expectedPackagePath = entry.Value;

                    DependencyPath actualPackage = await packageManager.GetPackageAsync(expectedPackageName, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.IsNotNull(actualPackage);
                    Assert.AreEqual(expectedPackagePath, actualPackage.Path);
                }
            }
            finally
            {
                this.CleanupPackagesDirectory();
            }
        }

        [Test]
        [Order(3)]
        public async Task PackageManagerDoesNotModifyExistingPackageRegistrationsDuringInitializationAndDiscovery()
        {
            try
            {
                this.CleanupPackagesDirectory();
                this.CopyPackagesIntoPackagesDirectory();

                DependencyPath preexistingRegistration = this.CreatePackageRegistration("package3");

                await this.packageManager.InitializePackagesAsync(CancellationToken.None).ConfigureAwait(false);
                await this.packageManager.DiscoverPackagesAsync(CancellationToken.None).ConfigureAwait(false);

                DependencyPath package = await packageManager.GetPackageAsync("package3", CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(preexistingRegistration.Equals(package));
            }
            finally
            {
                this.CleanupPackagesDirectory();
            }
        }

        [Test]
        [Order(4)]
        public void PackageManagerFolderIsolationWorksAsExpected()
        {
            try
            {
                this.CleanupPackagesDirectory();
                string connection = Environment.GetEnvironmentVariable("PACKAGE_STORE");

                if (string.IsNullOrWhiteSpace(connection))
                {
                    Assert.Inconclusive("The 'PACKAGE_STORE' environment variable must be set. This environment variable is required to run this test.");
                }

                int parallelism = 100;
                DateTime timeout = DateTime.UtcNow.AddMinutes(10);

                PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);
                List<Tuple<IPackageManager, IBlobManager, string>> packageManagers = new List<Tuple<IPackageManager, IBlobManager, string>>();

                for (int i = 0; i < parallelism; i++)
                {
                    string installationPath = Path.Combine(this.packagesDirectory, $"installation_{i}");
                    DependencyBlobStore blobStore = EndpointUtility.CreateBlobStoreReference(
                        DependencyStore.Packages,
                        connection,
                        new CertificateManager());

                    IBlobManager blobManager = DependencyFactory.CreateBlobManager(blobStore);
                    packageManagers.Add(new Tuple<IPackageManager, IBlobManager, string>(
                        new PackageManager(platformSpecifics, new FileSystem()),
                        blobManager,
                        installationPath));
                }

                string targetPackage = "example_package_kb.zip";
                while (DateTime.UtcNow < timeout)
                {
                    Parallel.ForEach(packageManagers, async (entry) =>
                    {
                        string installationPath = entry.Item3;

                        try
                        {
                            BlobDescriptor descriptor = new BlobDescriptor
                            {
                                Name = targetPackage,
                                ContainerName = "packages",
                                PackageName = "example_package",
                                ArchiveType = ArchiveType.Zip,
                                Extract = true
                            };

                            IPackageManager packageManager = entry.Item1;
                            IBlobManager blobManager = entry.Item2;

                            string actualInstallationPath = await packageManager.InstallPackageAsync(blobManager, descriptor, CancellationToken.None, installationPath: installationPath);
                            Debug.WriteLine($"Package Installation Complete");
                            Assert.IsTrue(this.fileSystem.Directory.Exists(actualInstallationPath));
                        }
                        catch (Exception exc)
                        {
                            Assert.Fail($"Package manager failed to install package due to exception: {exc}");
                        }
                        finally
                        {
                            this.CleanupPackagesDirectory(installationPath);
                        }
                    });
                }
            }
            finally
            {
                this.CleanupPackagesDirectory();
            }
        }


        [Test]
        [Order(5)]
        public void PackageManagerMutexIsolationLockingWorksAsExpected()
        {
            try
            {
                this.CleanupPackagesDirectory();
                string connection = Environment.GetEnvironmentVariable("PACKAGE_STORE");

                if (string.IsNullOrWhiteSpace(connection))
                {
                    Assert.Inconclusive("The 'PACKAGE_STORE' environment variable must be set. This environment variable is required to run this test.");
                }

                int parallelism = 100;
                DateTime timeout = DateTime.UtcNow.AddMinutes(20);

                PlatformSpecifics platformSpecifics = new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);
                List<Tuple<IPackageManager, IBlobManager>> packageManagers = new List<Tuple<IPackageManager, IBlobManager>>();

                for (int i = 0; i < parallelism; i++)
                {
                    DependencyBlobStore blobStore = EndpointUtility.CreateBlobStoreReference(
                        DependencyStore.Packages,
                        connection,
                        new CertificateManager());

                    IBlobManager blobManager = DependencyFactory.CreateBlobManager(blobStore);
                    packageManagers.Add(new Tuple<IPackageManager, IBlobManager>(
                        new IsolatedPackageManager(new PackageManager(platformSpecifics, new FileSystem())),
                        blobManager));
                }

                string targetPackage = "example_package_kb.zip";
                while (DateTime.UtcNow < timeout)
                {
                    try
                    {
                        Parallel.ForEach(packageManagers, async (entry) =>
                        {
                            try
                            {
                                BlobDescriptor descriptor = new BlobDescriptor
                                {
                                    Name = targetPackage,
                                    ContainerName = "packages",
                                    PackageName = "example_package",
                                    ArchiveType = ArchiveType.Zip,
                                    Extract = true
                                };

                                IPackageManager packageManager = entry.Item1;
                                IBlobManager blobManager = entry.Item2;

                                string installationPath = await packageManager.InstallPackageAsync(blobManager, descriptor, CancellationToken.None);
                                Debug.WriteLine($"Package Installation Complete");
                                Assert.IsTrue(this.fileSystem.Directory.Exists(installationPath));
                            }
                            catch (Exception exc)
                            {
                                Assert.Fail($"Package manager failed to install package due to exception: {exc}");
                            }
                        });
                    }
                    finally
                    {
                        this.CleanupPackagesDirectory();
                    }
                }
            }
            finally
            {
                this.CleanupPackagesDirectory();
            }
        }

        private void CleanupPackagesDirectory(string targetDirectory = null)
        {
            if (!string.IsNullOrWhiteSpace(targetDirectory) && this.fileSystem.Directory.Exists(targetDirectory))
            {
                this.fileSystem.Directory.Delete(targetDirectory, true);
            }
            else if (this.fileSystem.Directory.Exists(this.packagesDirectory))
            {
                this.fileSystem.Directory.Delete(this.packagesDirectory, true);
                this.fileSystem.Directory.CreateDirectory(this.packagesDirectory);
            }
        }

        private void CopyPackagesIntoPackagesDirectory()
        {
            foreach (string zipFile in this.fileSystem.Directory.EnumerateFiles(this.resourcesDirectory, "*.zip"))
            {
                this.fileSystem.File.Copy(zipFile, Path.Combine(this.packagesDirectory, Path.GetFileName(zipFile)), true);
            }
        }

        private DependencyPath CreatePackageRegistration(string packageName)
        {
            string registrationFileName = $"{packageName.ToLowerInvariant()}.vcpkgreg";
            string registrationFilePath = Path.Combine(this.packagesDirectory, registrationFileName);

            DependencyPath packageRegistration = new DependencyPath(
                packageName.ToLowerInvariant(),
                Path.Combine(this.packagesDirectory, packageName, "runtimes"),
                $"Description of '{packageName}'.{Guid.NewGuid()}",
                "1.2.3",
                new Dictionary<string, IConvertible>
                {
                    { "property1", "value1" },
                    { "property2", 1234 }
                });

            this.fileSystem.File.WriteAllText(registrationFilePath, packageRegistration.ToJson());

            return packageRegistration;
        }
    }
}
