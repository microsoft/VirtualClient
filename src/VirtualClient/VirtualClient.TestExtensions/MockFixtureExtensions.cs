// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Linq;
    using System.Threading;
    using Moq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="MockFixture"/> instances.
    /// </summary>
    public static class MockFixtureExtensions
    {
        /// <summary>
        /// Adds mock/fake workload dependency package to the package manager.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="package">The package to setup as the workload package returned.</param>
        /// <param name="expectedFiles">
        /// Files to add to the file system in the package directories. These will be added to the packages directory
        /// exactly as supplied (e.g. fio.exe -> ...\VirtualClient\packages\fio\1.0.0\fio.exe,
        /// runtimes\win-x64\fio.exe -> ...\VirtualClient\packages\fio\1.0.0\runtimes\win-x64\fio.exe)
        /// </param>
        public static MockFixture SetupWorkloadPackage(this MockFixture fixture, DependencyPath package, params string[] expectedFiles)
        {
            // Setup: The package exists
            fixture.PackageManager
                .Setup(mgr => mgr.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);

            // Setup: The package directory exists on the file system.
            fixture.Directory.Setup(dir => dir.Exists(package.Path)).Returns(true);

            // Setup: Any files expected within the workload package exist.
            if (expectedFiles?.Any() == true)
            {
                foreach (string filePath in expectedFiles)
                {
                    fixture.File.Setup(file => file.Exists(filePath)).Returns(true);
                }
            }

            return fixture;
        }
    }
}
