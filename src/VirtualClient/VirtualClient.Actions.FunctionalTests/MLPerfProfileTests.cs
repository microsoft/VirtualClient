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
    public class MLPerfProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-GPU-MLPERF.json")]
        public void MLPerfWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.fixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-GPU-MLPERF.json")]
        public async Task MLPerfWorkloadProfileExecutesTheExpectedDependenciesAndReboot(string profile)
        {
            List<string> expectedCommands = new List<string>
            {
                $"sudo apt update",
                $"sudo apt install build-essential -yq",
                $"sudo wget https://developer.download.nvidia.com/compute/cuda/12.0.0/local_installers/cuda_12.0.0_525.60.13_linux.run",
                $"sudo sh cuda_12.0.0_525.60.13_linux.run --silent",
                $"sudo bash -c \"echo 'export PATH=/usr/local/cuda-11.6/bin${{PATH:+:${{PATH}}}}' | sudo tee -a /home/[a-z]+/.bashrc\"",
                $"bash -c \"echo 'export LD_LIBRARY_PATH=/usr/local/cuda-11.6/lib64${{LD_LIBRARY_PATH:+:${{LD_LIBRARY_PATH}}}}' " +
                "| sudo tee -a /home/[a-z]+/.bashrc\""
            };

            this.fixture.Setup(PlatformID.Unix);
            this.fixture.SetupDisks(withRemoteDisks: false);
            this.fixture.SetupWorkloadPackage("mlperf");

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }

            VirtualClientRuntime.IsRebootRequested = false;
        }

        [Test]
        [TestCase("PERF-GPU-MLPERF.json")]
        public async Task MLPerfWorkloadProfileExecutesTheExpectedRemainingDependenciesAfterRebootAndExecuteWorkload(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetExpectedCommands();

            this.fixture.Setup(PlatformID.Unix);
            this.fixture.SetupDisks(withRemoteDisks: true);
            this.fixture.SetupWorkloadPackage("mlperf", expectedFiles: @"closed/NVIDIA/Makefile");

            string expectedStateId = nameof(CudaAndNvidiaGPUDriverInstallation);
            await this.fixture.StateManager.SaveStateAsync(expectedStateId, JObject.Parse("{ \"any\": \"state\" }"), CancellationToken.None)
                .ConfigureAwait(false);

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_MLPerf_Harness_Summary.json"));
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_MLPerf_Accuracy_Summary.json"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetExpectedCommands()
        {
            string setupCommand = "curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey "
            + "| sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg \\\n  "
            + "&& curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \\\n "
            + "   sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \\\n  "
            + "  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list";
            
            return new List<string>
            {
                $"sudo bash -c \"{setupCommand}\"",
                $"sudo apt-get update",
                $"sudo apt-get install -y nvidia-container-toolkit",
                $"sudo systemctl restart docker",
                "sudo usermod -aG docker [a-z]+",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make clean""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make link_dirs""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_data BENCHMARKS=bert""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_model BENCHMARKS=bert""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make preprocess_data BENCHMARKS=bert""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_data BENCHMARKS=rnnt""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_model BENCHMARKS=rnnt""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make preprocess_data BENCHMARKS=rnnt""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_data BENCHMARKS=ssd-mobilenet""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_model BENCHMARKS=ssd-mobilenet""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make preprocess_data BENCHMARKS=ssd-mobilenet""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_data BENCHMARKS=ssd-resnet34""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make download_model BENCHMARKS=ssd-resnet34""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make preprocess_data BENCHMARKS=ssd-resnet34""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make build""",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=high_accuracy --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=high_accuracy --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=triton --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=triton --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=high_accuracy_triton --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=high_accuracy_triton --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-mobilenet --scenarios=Offline,MultiStream,SingleStream --config_ver=default --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-mobilenet --scenarios=Offline,MultiStream,SingleStream --config_ver=default --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-mobilenet --scenarios=Offline,MultiStream,SingleStream --config_ver=triton --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-mobilenet --scenarios=Offline,MultiStream,SingleStream --config_ver=triton --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-resnet34 --scenarios=Offline,Server,SingleStream,MultiStream --config_ver=default --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-resnet34 --scenarios=Offline,Server,SingleStream,MultiStream --config_ver=default --test_mode=AccuracyOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-resnet34 --scenarios=Offline,Server,SingleStream,MultiStream --config_ver=triton --test_mode=PerformanceOnly --fast'",
                @"sudo docker exec -u [a-z]+ mlperf-inference-[a-z]+-x86_64 sudo bash -c ""export MLPERF_SCRATCH_PATH=(.*)/scratch && make run RUN_ARGS='--benchmarks=ssd-resnet34 --scenarios=Offline,Server,SingleStream,MultiStream --config_ver=triton --test_mode=AccuracyOnly --fast'",
            };
        }
    }
}
