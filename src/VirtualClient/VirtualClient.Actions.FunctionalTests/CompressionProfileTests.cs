// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class CompressionProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Win32NT, Architecture.Arm64)]
        public void CompressionWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(platformID, architecture);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Unix, Architecture.Arm64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-COMPRESSION.json", PlatformID.Win32NT, Architecture.Arm64)]
        public async Task CompressionWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platformID, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platformID, architecture);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload is built.
            // - The workload generates valid results.
            this.mockFixture.Setup(platformID, architecture);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupWorkloadPackage("Compression");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("7z", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Compressor7zipResults.txt"));
                }
                else if (arguments.Contains("gzip", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append(TestDependencies.GetResourceFileContents("GzipResults.txt"));
                }
                else if (arguments.Contains("pbzip2", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append(TestDependencies.GetResourceFileContents("Pbzip2Results.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-COMPRESSION-LZBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-COMPRESSION-LZBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public void CompressionWorkloadProfileParametersAreInlinedCorrectly_LZbench(string profile, PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(platformID, architecture);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-COMPRESSION-LZBENCH.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-COMPRESSION-LZBENCH.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task CompressionWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform_LZbench(string profile, PlatformID platformID, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = this.GetLZBenchProfileExpectedCommands(platformID, architecture);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload is built.
            // - The workload generates valid results.
            this.mockFixture.Setup(platformID, architecture);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);

                if (arguments.Contains("lzbenchexecutor.sh", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("LzbenchResults.csv"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform, Architecture architecture)
        {
            List<string> commands = null;

            if (platform == PlatformID.Unix)
            {
                commands = new List<string>
                {
                    $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                    $"sudo unzip silesia.zip -d silesia",
                    $"sudo bash -c \"pbzip2 -fv /home/user/tools/VirtualClient/packages/pbzip2/silesia/*\"",
                    $"sudo bash -c \"pbzip2 -fvd /home/user/tools/VirtualClient/packages/pbzip2/silesia/*\"",
                    $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                    $"sudo unzip silesia.zip -d silesia",
                    $"sudo bash -c \"gzip -rvf /home/user/tools/VirtualClient/packages/gzip/silesia\"",
                    $"sudo bash -c \"gzip -rvfd /home/user/tools/VirtualClient/packages/gzip/silesia\""
                };
            }
            else
            {
                commands = new List<string>
                {
                    $"wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                    $"unzip silesia.zip -d silesia",
                    $"7z a -bt -mx1 -mmt -mm=LZMA -r 7zLZMAFastestMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx7 -mmt -mm=LZMA -r 7zLZMAMaximumMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=LZMA -r 7zLZMAUltraMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx1 -mmt -mm=BZIP2 -r 7zBZIP2FastestMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx7 -mmt -mm=BZIP2 -r 7zBZIP2MaximumMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=BZIP2 -r 7zBZIP2UltraMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx1 -mmt -mm=PPMd -r 7zPPMdFastestMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx7 -mmt -mm=PPMd -r 7zPPMdMaximumMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=PPMd -r 7zPPMdUltraMode.7z C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -r tarMode.tar C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=Deflate -r zipDeflateUltraMode.zip C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=Deflate64 -r zipDeflate64UltraMode.zip C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=BZIP2 -r zipBZIP2UltraMode.zip C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=LZMA -r zipLZMAUltraMode.zip C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*",
                    $"7z a -bt -mx9 -mmt -mm=PPMd -r zipPPMdUltraMode.zip C:\\users\\any\\tools\\VirtualClient\\packages\\7zip\\silesia\\*"
                };
            }

            return commands;
        }

        private IEnumerable<string> GetLZBenchProfileExpectedCommands(PlatformID platform, Architecture architecture)
        {
            return new List<string>
            {
                $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git",
                $"sudo make",
                $"wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"sudo unzip silesia.zip -d silesia",
                $"sudo bash lzbenchexecutor.sh \"-t16,16 -eall -o4 -r /home/user/tools/VirtualClient/packages/lzbench/silesia\""
            };
        }
    }
}
