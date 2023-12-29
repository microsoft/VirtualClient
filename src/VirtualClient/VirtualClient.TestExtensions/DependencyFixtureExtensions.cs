// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods to help with common test setup needs in the
    /// functional tests in this project.
    /// </summary>
    public static class DependencyFixtureExtensions
    {
        /// <summary>
        /// Returns true if the expected workload metrics were captured.
        /// </summary>
        public static bool AreMetricsCaptured(
            this EventContext telemetryContext,
            string scenarioName = null,
            string metricName = null,
            string toolName = null,
            double? metricValue = null,
            string metricUnit = null,
            string metricCategorization = null)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            bool metricsCaptured = true;
            if (scenarioName != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("scenarioName") 
                    && telemetryContext.Properties["scenarioName"].ToString() == scenarioName;
            }

            if (metricName != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("metricName")
                    && telemetryContext.Properties["metricName"].ToString() == metricName;
            }

            if (toolName != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("toolName")
                    && telemetryContext.Properties["toolName"].ToString() == toolName;
            }

            if (metricValue != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("metricValue")
                    && telemetryContext.Properties["metricValue"].Equals(metricValue.Value);
            }

            if (metricUnit != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("metricUnit")
                    && telemetryContext.Properties["metricUnit"].ToString() == metricUnit;
            }

            if (metricCategorization != null && metricsCaptured)
            {
                metricsCaptured = telemetryContext.Properties.ContainsKey("metricCategorization")
                    && telemetryContext.Properties["metricCategorization"].ToString() == metricCategorization;
            }

            return metricsCaptured;
        }

        /// <summary>
        /// Confirms the workload commands were executed. This method uses regular expressions
        /// to evaluate equality so the commands passed in can be explicit or using regular expressions
        /// syntax.
        /// </summary>
        public static bool CommandsExecuted(this InMemoryProcessManager processManager, params string[] commands)
        {
            bool executed = true;
            List<IProcessProxy> processesConfirmed = new List<IProcessProxy>();

            foreach (string command in commands)
            {
                // There are certain characters in the commands/arguments that are reserved characters in regular
                // expressions. To work around this, we do a direct string comparison first that does not use a regular
                // expression. If this does not resolve a match, we use the regular expression. This enables developers to
                // use either exact matches or regular expression matches as they see fit.
                IProcessProxy matchingProcess = processManager.Processes.FirstOrDefault(
                    proc => (proc.FullCommand() == command));

                matchingProcess ??= processManager.Processes.FirstOrDefault(
                    proc => Regex.IsMatch(proc.FullCommand(), command, RegexOptions.IgnoreCase) 
                    && !processesConfirmed.Any(otherProc => object.ReferenceEquals(proc, otherProc)));

                if (matchingProcess == null)
                {
                    executed = false;
                    break;
                }

                processesConfirmed.Add(matchingProcess);
            }

            return executed;
        }

        /// <summary>
        /// Confirms the workload commands were executed. This method uses regular expressions
        /// to evaluate equality so the commands passed in can be explicit or using regular expressions
        /// syntax.
        /// </summary>
        public static bool CommandsExecutedInWorkingDirectory(this InMemoryProcessManager processManager, string workingDir, params string[] commands)
        {
            bool executed = true;
            List<IProcessProxy> processesConfirmed = new List<IProcessProxy>();

            foreach (string command in commands)
            {
                IProcessProxy matchingProcess = processManager.Processes.FirstOrDefault(
                    proc => (proc.FullCommand() == command 
                    && proc.StartInfo.WorkingDirectory == workingDir));

                matchingProcess ??= processManager.Processes.FirstOrDefault(
                    proc => Regex.IsMatch(proc.FullCommand(), command, RegexOptions.IgnoreCase)
                    && proc.StartInfo.WorkingDirectory == workingDir
                    && !processesConfirmed.Any(otherProc => object.ReferenceEquals(proc, otherProc)));

                if (matchingProcess == null)
                {
                    executed = false;
                    break;
                }

                processesConfirmed.Add(matchingProcess);
            }

            return executed;
        }

        /// <summary>
        /// Returns true if the file was made attributable using the 'chmod' command on Unix/Linux
        /// systems (e.g. chmod +x "/any/path/to/binary"). Default attributes = '+x'
        /// </summary>
        public static bool IsChmodAttributed(this DependencyFixture fixture, string filePath, string attributes = "+x")
        {
            return fixture.ProcessManager.CommandsExecuted($"sudo chmod {Regex.Escape(attributes)} \"{Regex.Escape(filePath)}\"");
        }

        /// <summary>
        /// Extension returns true if the process command and arguments match the full command provided.
        /// Note that full command can be a regular expression and is evaluated as such.
        /// </summary>
        public static bool IsMatch(this IProcessProxy process, string fullCommand, bool exactMatch = false)
        {
            string processCommand = string.IsNullOrEmpty(process.StartInfo?.Arguments)
                ? process.StartInfo.FileName
                : $"{process.StartInfo?.FileName} {process.StartInfo.Arguments}";

            return Regex.IsMatch(processCommand, fullCommand, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Sets the package and version to be returned as installed when checked on a Linux system.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="packageVersions">The packages and versions to stage as installed. Key = package name (e.g. gcc). Value = package version (e.g. 10).</param>
        public static DependencyFixture SetupLinuxPackagesInstalled(this DependencyFixture fixture, IDictionary<string, string> packageVersions)
        {
            fixture.ProcessManager.OnProcessCreated = process =>
            {
                if (packageVersions.TryGetValue(process.StartInfo.FileName, out string packageVersion))
                {
                    process.StandardOutput.Append($"{process.StartInfo.FileName} (Distro {packageVersion}distro1~18.04) {packageVersion}");
                }
            };

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake directory to the file system.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="directoryPath">The path to the file to add to the system.</param>
        public static DependencyFixture SetupDirectory(this DependencyFixture fixture, string directoryPath)
        {
            fixture.FileSystem.Directory.CreateDirectory(directoryPath);

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake directory to the file system.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="packageName">The name of the workload package.</param>
        /// <param name="directoryPath">The path to the file to add to the system.</param>
        public static DependencyFixture SetupDirectory(this DependencyFixture fixture, string packageName, string directoryPath)
        {
            string packagePath = fixture.PlatformSpecifics.Combine(fixture.PackagesDirectory, packageName);
            fixture.SetupDirectory(fixture.PlatformSpecifics.Combine(packagePath, directoryPath));

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake disks to the dependency fixture disk manager for
        /// workload profile test scenarios that operate against disks.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="withRemoteDisks">True if remote disks should be included. Note that remote disks are required if unformatted disks are required.</param>
        /// <param name="withUnformatted">True if some of the disks (e.g. the remote/managed disks) should be unformatted.</param>
        /// <returns></returns>
        public static DependencyFixture SetupDisks(this DependencyFixture fixture, bool withRemoteDisks = true, bool withUnformatted = true)
        {
            IEnumerable<Disk> disks = fixture.CreateDisks(fixture.Platform, true);
            if (withUnformatted)
            {
                disks.ToList().ForEach(disk =>
                {
                    if (!disk.IsOperatingSystem())
                    {
                        // We determine that a disk is formatted by looking to see if it
                        // has any volumes/partitions defined. An unformatted disk will NOT
                        // have any volumes/partitions.
                        (disk.Volumes as List<DiskVolume>).Clear();
                    }
                });
            }

            if (!withRemoteDisks)
            {
                fixture.DiskManager.AddRange(disks.Where(disk => disk.IsOperatingSystem()));
            }
            else
            {
                fixture.DiskManager.AddRange(disks);
            }

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake file to the file system.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="filePath">The path to the file to add to the system.</param>
        /// <param name="content">Contents of the file.</param>
        public static DependencyFixture SetupFile(this DependencyFixture fixture, string filePath, byte[] content = null)
        {
            if (content == null)
            {
                fixture.FileSystem.File.Create(filePath);
            }
            else
            {
                fixture.FileSystem.File.WriteAllBytes(filePath, content);
            }

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake file to the file system.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="packageName">The name of the workload package.</param>
        /// <param name="filePath">The path to the file to add to the system.</param>
        /// <param name="content">Contents of the file.</param>
        public static DependencyFixture SetupFile(this DependencyFixture fixture, string packageName, string filePath, string content)
        {
            return fixture.SetupFile(packageName, filePath, Encoding.UTF8.GetBytes(content));
        }

        /// <summary>
        /// Adds mock/fake workload dependency package to the package manager.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="packageName">The name of the workload package.</param>
        /// <param name="filePath">The path to the file to add to the system.</param>
        /// <param name="content">Contents of the file.</param>
        public static DependencyFixture SetupFile(this DependencyFixture fixture, string packageName, string filePath, byte[] content)
        {
            string packagePath = fixture.PlatformSpecifics.Combine(fixture.PackagesDirectory, packageName);
            fixture.SetupFile(fixture.PlatformSpecifics.Combine(packagePath, filePath), content);

            return fixture;
        }

        /// <summary>
        /// Adds mock/fake workload dependency package to the package manager.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <param name="packageName">The name of the workload package.</param>
        /// <param name="metadata">Add specifics if needed.</param>
        /// <param name="expectedFiles">
        /// Files to add to the file system in the package directories. These will be added to the packages directory
        /// exactly as supplied (e.g. fio.exe -> ...\VirtualClient\packages\fio\1.0.0\fio.exe,
        /// runtimes\win-x64\fio.exe -> ...\VirtualClient\packages\fio\1.0.0\runtimes\win-x64\fio.exe)
        /// </param>
        public static DependencyFixture SetupWorkloadPackage(
            this DependencyFixture fixture, 
            string packageName,
            IDictionary<string, IConvertible> metadata = null,
            params string[] expectedFiles)
        {
            string packagePath = fixture.PlatformSpecifics.Combine(fixture.PackagesDirectory, packageName);
            DependencyPath package = new DependencyPath(packageName, packagePath, $"{packageName} workload package", metadata: metadata);

            fixture.PackageManager.RegisterPackageAsync(package, CancellationToken.None)
                .GetAwaiter().GetResult();

            if (expectedFiles?.Any() == true)
            {
                foreach (string filePath in expectedFiles)
                {
                    fixture.SetupFile(fixture.PlatformSpecifics.Combine(packagePath, filePath));
                }
            }

            return fixture;
        }
    }
}
