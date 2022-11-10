// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies;

    [TestFixture]
    [Category("Functional")]
    public class SuperBenchmarkProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-GPU-SUPERBENCH.json")]
        public void SuperBenchmarkWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-GPU-SUPERBENCH.json")]
        public async Task SuperBenchmarkWorkloadProfileExecutesTheExpectedDependenciesAndReboot(string profile)
        {
            List<string> expectedCommands = new List<string>
            {
                $"sudo apt update",
                $"sudo apt install build-essential -yq",
                $"sudo wget https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run",
                $"sudo sh cuda_11.6.0_510.39.01_linux.run --silent",
                $"sudo bash -c \"echo 'export PATH=/usr/local/cuda-11.6/bin${{PATH:+:${{PATH}}}}' | sudo tee -a /home/[a-z]+/.bashrc\"",
                $"bash -c \"echo 'export LD_LIBRARY_PATH=/usr/local/cuda-11.6/lib64${{LD_LIBRARY_PATH:+:${{LD_LIBRARY_PATH}}}}' | " +
                $"sudo tee -a /home/[a-z]+/.bashrc\""
            };

            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupWorkloadPackage("SuperBenchmark", expectedFiles: @"runtimes/linux-x64/bin/sb");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }

            SystemManagement.IsRebootRequested = false;
        }

        [Test]
        [TestCase("PERF-GPU-SUPERBENCH.json")]
        public async Task SuperBenchmarkWorkloadProfileExecutesTheExpectedDependenciesAndWorkloadsAfterReboot(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupWorkloadPackage("SuperBenchmark", expectedFiles: @"runtimes/linux-x64/bin/sb");

            string expectedStateId = nameof(CudaAndNvidiaGPUDriverInstallation);
            await this.mockFixture.StateManager.SaveStateAsync(expectedStateId, JObject.Parse("{ \"any\": \"state\" }"), CancellationToken.None)
                .ConfigureAwait(false);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("sb", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SuperBenchmark.jsonl"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            string setupCommand = "distribution=$(. /etc/os-release;echo $ID$VERSION_ID) && " +
                "curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey | " +
                "sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg && " +
                "curl -s -L https://nvidia.github.io/libnvidia-container/$distribution/libnvidia-container.list | " +
                "sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' |  " +
                "sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list";

            return new List<string>
            {
                $"sudo bash -c \"{setupCommand}\"",
                $"sudo apt-get update",
                $"sudo apt-get install -y nvidia-docker2",
                $"sudo systemctl restart docker",
                $"sudo chmod -R 2777 \"/home/user/tools/VirtualClient\"",
                $"sudo git clone -b v0.5.0 https://github.com/microsoft/superbenchmark",
                $"sudo bash initialize.sh",
                $"sudo sb deploy --host-list localhost -i superbench/superbench:v0.5.0-cuda11.1.1",
                $"sudo sb run --host-list localhost -c default.yaml"
            };
        }
    }
}
