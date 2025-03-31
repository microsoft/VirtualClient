// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Microsoft.Extensions.Azure;
    using Moq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="MockFixture"/> instances.
    /// </summary>
    public static class MockFixtureExtensions
    {
        /// <summary>
        /// Sets up the mock fixture for the scenario where no dependency packages exist.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        public static MockFixture ResetPackages(this MockFixture fixture)
        {
            fixture.ThrowIfNull(nameof(fixture));
            fixture.PackageManager.Reset();

            return fixture;
        }

        /// <summary>
        /// Sets up the mock fixture for the following aspects of the directory:
        /// <br/><br/>
        /// <b>Setups:</b>
        /// <list type="bullet">
        /// <item>The directory exists.</item>
        /// <item>Subdirectories associated with any files exist.</item>
        /// <item>Any files provided exist. File content is not setup. Use SetupFile() extension to setup individual files.</item>
        /// <item>Fixture Directory.GetFiles() method overloads setup to return matching files. SearchOption is supported. EnumerationOptions is not supported.</item>
        /// <item>Fixture Directory.EnumerateFiles() method overloads setup to return matching files. SearchOption is supported. EnumerationOptions is not supported.</item>
        /// </list>
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="directory">The directory to indicate exists.</param>
        /// <param name="files">The set of files to setup in the directory. Sets up all Directory.GetFiles() method overloads.</param>
        public static MockFixture SetupDirectory(this MockFixture fixture, string directory, params string[] files)
        {
            fixture.ThrowIfNull(nameof(fixture));
            directory.ThrowIfNullOrWhiteSpace(nameof(directory));

            fixture.Directory.Setup(d => d.Exists(directory)).Returns(true);

            if (files?.Any() == true)
            {
                // Setup:
                // All files within the directory exist.
                foreach (string file in files)
                {
                    fixture.File.Setup(f => f.Exists(file)).Returns(true);
                }

                // Setup:
                // All subdirectories within the directory exist.
                HashSet<string> subdirectories = new HashSet<string>();

                foreach (string file in files)
                {
                    string subDirectory = MockFixture.GetDirectoryName(file);
                    if (!subDirectory.Equals(directory))
                    {
                        subdirectories.Add(subDirectory);
                    }
                }

                foreach (string subdirectory in subdirectories)
                {
                    fixture.Directory.Setup(d => d.Exists(subdirectory)).Returns(true);
                }

                fixture.Directory.Setup(d => d.GetFiles(directory)).Returns(files);
                fixture.Directory.Setup(d => d.EnumerateFiles(directory)).Returns(files);

                IEnumerable<string> filesFound = null;
                fixture.Directory.Setup(d => d.GetFiles(directory, It.IsAny<string>()))
                    .Callback<string, string>((dir, searchPattern) =>
                    {
                        string searchExpression = MockFixtureExtensions.ConvertToRegularExpression(searchPattern);
                        filesFound = files.Where(f => Regex.IsMatch(f, searchExpression));
                    })
                    .Returns(() => filesFound?.ToArray());

                // IEnumerable<string> filesFound1b = null;
                fixture.Directory.Setup(d => d.EnumerateFiles(directory, It.IsAny<string>()))
                    .Callback<string, string>((dir, searchPattern) =>
                    {
                        string searchExpression = MockFixtureExtensions.ConvertToRegularExpression(searchPattern);
                        filesFound = files.Where(f => Regex.IsMatch(f, searchExpression));
                    })
                    .Returns(() => filesFound);

                // IEnumerable<string> filesFound3a = null;
                fixture.Directory.Setup(d => d.GetFiles(directory, It.IsAny<string>(), It.IsAny<SearchOption>()))
                    .Callback<string, string, SearchOption>((dir, searchPattern, searchOption) =>
                    {
                        string searchExpression = MockFixtureExtensions.ConvertToRegularExpression(searchPattern);
                        filesFound = files.Where(f => Regex.IsMatch(f, searchExpression));

                        if (searchOption == SearchOption.TopDirectoryOnly)
                        {
                            filesFound = filesFound?.Where(f => MockFixture.GetDirectoryName(f) == directory);
                        }
                    })
                    .Returns(() => filesFound?.ToArray());

                // IEnumerable<string> filesFound3b = null;
                fixture.Directory.Setup(d => d.EnumerateFiles(directory, It.IsAny<string>(), It.IsAny<SearchOption>()))
                    .Callback<string, string, SearchOption>((dir, searchPattern, searchOption) =>
                    {
                        string searchExpression = MockFixtureExtensions.ConvertToRegularExpression(searchPattern);
                        filesFound = files.Where(f => Regex.IsMatch(f, searchExpression));

                        if (searchOption == SearchOption.TopDirectoryOnly)
                        {
                            filesFound = filesFound?.Where(f => MockFixture.GetDirectoryName(f) == directory);
                        }
                    })
                    .Returns(() => filesFound);
            }

            return fixture;
        }

        /// <summary>
        /// Sets up the mock fixture for the following aspects of the system disks:
        /// <br/><br/>
        /// <b>Setups:</b>
        /// <list type="bullet">
        /// <item>The directory exists.</item>
        /// <item>Subdirectories associated with any files exist.</item>
        /// <item>Any files provided exist. File content is not setup. Use SetupFile() extension to setup individual files.</item>
        /// <item>Fixture Directory.GetFiles() method overloads setup to return matching files. SearchOption is supported. EnumerationOptions is not supported.</item>
        /// <item>Fixture Directory.EnumerateFiles() method overloads setup to return matching files. SearchOption is supported. EnumerationOptions is not supported.</item>
        /// </list>
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="disks">The disks to setup on the system.</param>
        public static MockFixture SetupDisks(this MockFixture fixture, params Disk[] disks)
        {
            fixture.ThrowIfNull(nameof(fixture));

            fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(disks);

            return fixture;
        }

        /// <summary>
        /// Sets up the mock fixture for the following aspects of the file:
        /// <br/><br/>
        /// <b>Setups:</b>
        /// <list type="bullet">
        /// <item>The file exists.</item>
        /// <item>Fixture File.ReadAllText() method overloads setup to return content if provided.</item>
        /// <item>Fixture File.ReadAllTextAsync() method overloads setup to return content if provided.</item>
        /// <item>Fixture File.ReadAllBytes() method overloads setup to return content if provided.</item>
        /// <item>Fixture File.ReadAllBytesAsync() method overloads setup to return content if provided.</item>
        /// <item>Fixture File.ReadAllLines() method overloads setup to return content if provided.</item>
        /// <item>Fixture File.ReadAllLinesAsync() method overloads setup to return content if provided.</item>
        /// </list>
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="file">The file to indicate exists.</param>
        /// <param name="content">The content of the file.</param>
        public static MockFixture SetupFile(this MockFixture fixture, string file, string content = null)
        {
            fixture.ThrowIfNull(nameof(fixture));
            file.ThrowIfNullOrWhiteSpace(nameof(file));

            fixture.File.Setup(f => f.Exists(file)).Returns(true);

            if (content != null)
            {
                fixture.File.Setup(f => f.ReadAllText(file)).Returns(content);
                fixture.File.Setup(f => f.ReadAllText(file, It.IsAny<Encoding>())).Returns(content);

                fixture.File.Setup(f => f.ReadAllTextAsync(file, It.IsAny<CancellationToken>())).ReturnsAsync(content);
                fixture.File.Setup(f => f.ReadAllTextAsync(file, It.IsAny<Encoding>(), It.IsAny<CancellationToken>())).ReturnsAsync(content);

                // We setup a delegate/action on the return to ensure we do not execute the
                // text-to-byte conversion unless called.
                fixture.File.Setup(f => f.ReadAllBytes(file)).Returns(() => Encoding.UTF8.GetBytes(content));
                fixture.File.Setup(f => f.ReadAllBytesAsync(file, It.IsAny<CancellationToken>())).ReturnsAsync(Encoding.UTF8.GetBytes(content));

                // We setup a delegate/action on the return to ensure we do not execute the
                // text-to-lines conversion unless called.
                fixture.File.Setup(f => f.ReadAllLines(file)).Returns(() => Regex.Split(content, "\r?\n"));
                fixture.File.Setup(f => f.ReadAllLines(file, It.IsAny<Encoding>())).Returns(() => Regex.Split(content, "\r?\n"));
                fixture.File.Setup(f => f.ReadAllLinesAsync(file, It.IsAny<CancellationToken>())).ReturnsAsync(Regex.Split(content, "\r?\n"));
                fixture.File.Setup(f => f.ReadAllLinesAsync(file, It.IsAny<Encoding>(), It.IsAny<CancellationToken>())).ReturnsAsync(Regex.Split(content, "\r?\n"));
            }

            return fixture;
        }

        /// <summary>
        /// Sets up the mock fixture for the following aspects of the package manager:
        /// <br/><br/>
        /// <b>Setups:</b>
        /// <list type="bullet">
        /// <item>The package exists.</item>
        /// <item>The package directory exists.</item>
        /// <item>All platform-specific folders within the package directory exist (e.g. linux-arm64, linux-x64, win-arm64, win-x64).</item>
        /// </list>
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="package">The package.</param>
        /// <param name="platformSpecific">True to setup the scenario where all platform-specific folders exist in the package.</param>
        public static MockFixture SetupPackage(this MockFixture fixture, DependencyPath package, bool platformSpecific = true)
        {
            fixture.ThrowIfNull(nameof(fixture));
            package.ThrowIfNull(nameof(package));

            fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(package.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);

            fixture.Directory.Setup(d => d.Exists(package.Path)).Returns(true);

            if (platformSpecific)
            {
                fixture.Directory.Setup(dir => dir.Exists(fixture.Combine(package.Path, PlatformSpecifics.WinArm64)))
                    .Returns(true);

                fixture.Directory.Setup(dir => dir.Exists(fixture.Combine(package.Path, PlatformSpecifics.WinX64)))
                   .Returns(true);

                fixture.Directory.Setup(dir => dir.Exists(fixture.Combine(package.Path, PlatformSpecifics.LinuxArm64)))
                   .Returns(true);

                fixture.Directory.Setup(dir => dir.Exists(fixture.Combine(package.Path, PlatformSpecifics.LinuxX64)))
                   .Returns(true);
            }

            return fixture;
        }

        /// <summary>
        /// Sets up the mock fixture for the following aspects of the package manager:
        /// <br/><br/>
        /// <b>Setups:</b>
        /// <list type="bullet">
        /// <item>The package exists.</item>
        /// <item>The package directory exists.</item>
        /// <item>When platform-specific folders are provided, those specific folders within the package directory exist (e.g. linux-arm64, linux-x64, win-arm64, win-x64).</item>
        /// </list>
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="package">The package.</param>
        /// <param name="platformSpecificFolders">The set of platform-specific folders to set as existing.</param>
        public static MockFixture SetupPackage(this MockFixture fixture, DependencyPath package, params string[] platformSpecificFolders)
        {
            fixture.ThrowIfNull(nameof(fixture));
            package.ThrowIfNull(nameof(package));

            fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(package.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package);

            if (platformSpecificFolders?.Any() == true)
            {
                foreach (string folder in platformSpecificFolders)
                {
                    fixture.Directory.Setup(dir => dir.Exists(fixture.Combine(package.Path, folder)))
                        .Returns(true);
                }
            }

            return fixture;
        }

        private static string ConvertToRegularExpression(string searchPattern)
        {
            if (searchPattern.Contains("*"))
            {
                // e.g.
                // *.txt -> .*\.txt - - matches /home/user/file1.txt but not /home/user/file1.exe
                return searchPattern?.Replace(".", "\\.").Replace("*", ".*");
            }
            else
            {
                // e.g.
                // file1     -> [/\\]+file1$     - matches /home/user/file1 but not /home/user/file1.txt
                // file1.txt -> [/\\]+file1.txt$ - matches /home/user/file1.txt but not /home/user/file1
                return $"[/\\\\]+{searchPattern?.Replace(".", @"\.")}$";
            }
        }
    }
}
