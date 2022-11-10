// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Test assert methods common to testing different workloads.
    /// </summary>
    public static class WorkloadAssert
    {
        /// <summary>
        /// Confirms the Apt package is installed.
        /// </summary>
        public static void AptPackageInstalled(DependencyFixture fixture, string packageName)
        {
            Assert.IsNotNull(
                fixture.ProcessManager.Processes.FirstOrDefault(proc => proc.StartInfo.FileName == "sudo" && proc.StartInfo.Arguments == "apt update"),
                "apt update command should be called before installation of apt packages.");

            Assert.IsNotNull(
               fixture.ProcessManager.Processes.FirstOrDefault(proc => proc.StartInfo.FileName == "sudo" && proc.StartInfo.Arguments.Contains("apt install")
                && proc.StartInfo.Arguments.Contains(packageName)),
               "apt package not installed.");
        }

        /// <summary>
        /// Confirms the workload commands were executed. This method uses regular expressions
        /// to evaluate equality so the commands passed in can be explicit or using regular expressions
        /// syntax.
        /// </summary>
        public static void CommandsExecuted(DependencyFixture fixture, params string[] commands)
        {
            List<IProcessProxy> processesConfirmed = new List<IProcessProxy>();

            foreach (string command in commands)
            {
                IProcessProxy matchingProcess = null;
                try
                {
                    // Try to match regex
                    matchingProcess = fixture.ProcessManager.Processes.FirstOrDefault(
                        proc => Regex.IsMatch($"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}".Trim(), command, RegexOptions.IgnoreCase)
                        && !processesConfirmed.Any(otherProc => object.ReferenceEquals(proc, otherProc)));
                }
                catch
                {
                }

                // Or command exact match
                matchingProcess = matchingProcess ?? fixture.ProcessManager.Processes.FirstOrDefault(
                    proc => $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}".Trim() == command
                    && !processesConfirmed.Any(otherProc => object.ReferenceEquals(proc, otherProc)));

                Assert.IsNotNull(matchingProcess, $"The command '{command}' was not executed.");
                processesConfirmed.Add(matchingProcess);
            }
        }

        /// <summary>
        /// Confirms all disks are initialized as expected (e.g. disks are formatted).
        /// </summary>
        public static void DisksAreInitialized(DependencyFixture fixture, Action<IEnumerable<Disk>> additionalValidation = null)
        {
            IEnumerable<Disk> disks = fixture.DiskManager.GetDisksAsync(CancellationToken.None)
                .GetAwaiter().GetResult();

            WorkloadAssert.DisksAreFormatted(disks);
            additionalValidation?.Invoke(disks);
        }

        /// <summary>
        /// Confirms all disks have access paths associated.
        /// </summary>
        public static void DisksHaveAccessPaths(DependencyFixture fixture, Action<IEnumerable<Disk>> additionalValidation = null)
        {
            IEnumerable<Disk> disks = fixture.DiskManager.GetDisksAsync(CancellationToken.None)
                .GetAwaiter().GetResult();

            foreach (Disk disk in disks)
            {
                string accessPath = disk.DevicePath;
                Assert.IsNotNull(accessPath);
                additionalValidation?.Invoke(disks);
            }
        }

        /// <summary>
        /// Confirms IIS installation commands are executed
        /// </summary>
        public static void IISInstalled(DependencyFixture fixture)
        {
            IEnumerable<string> expectedCommands = WorkloadAssert.GetIISInstallationExpectedCommands(PlatformID.Win32NT);

            fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = fixture.CreateProcess(command, arguments, workingDir);
                return process;
            };

            WorkloadAssert.CommandsExecuted(fixture, expectedCommands.ToArray());
        }

        /// <summary>
        /// Confirms the workload package was installed.
        /// </summary>
        public static void ParameterReferencesInlined(ExecutionProfile profile)
        {
            const string parameterReference = ExecutionProfile.ParameterPrefix;
            foreach (ExecutionProfileElement action in profile.Actions)
            {
                Assert.IsFalse(
                    action.Parameters.Any(entry => entry.Value != null && entry.Value.ToString().Contains(parameterReference)),
                    $"Some parameters in the profile actions are not inlined.");
            }

            foreach (ExecutionProfileElement dependency in profile.Dependencies)
            {
                Assert.IsFalse(
                    dependency.Parameters.Any(entry => entry.Value != null && entry.Value.ToString().Contains(parameterReference)),
                    $"Some parameters in the profile dependencies are not inlined.");
            }

            foreach (ExecutionProfileElement monitor in profile.Monitors)
            {
                Assert.IsFalse(
                    monitor.Parameters.Any(entry => entry.Value != null && entry.Value.ToString().Contains(parameterReference)),
                    $"Some parameters in the profile monitors are not inlined.");
            }
        }

        /// <summary>
        /// Confirms the workload package was installed.
        /// </summary>
        public static void WorkloadPackageInstalled(DependencyFixture fixture, string packageName, Action<DependencyPath> additionalValidation = null)
        {
            DependencyPath package = fixture.PackageManager.GetPackageAsync(packageName, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.IsNotNull(package);
            Assert.IsTrue(string.Equals(packageName, package.Name, StringComparison.OrdinalIgnoreCase));

            additionalValidation?.Invoke(package);
        }

        private static void DisksAreFormatted(IEnumerable<Disk> disks)
        {
            Assert.IsTrue(disks.All(disk => disk.Volumes.Any()), "One or more disks are not formatted.");
        }

        private static IEnumerable<string> GetIISInstallationExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            commands = new List<string>
            {
                "Install-WindowsFeature -name Web-Server,Net-Framework-45-Core,Web-Asp-Net45,NET-Framework-45-ASPNET -IncludeManagementTools",
                "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic",
                "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging"
            };

            return commands;
        }
    }
}
