// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MLPerfExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public void MLPerfExecutorThrowsOnUnsupportedLinuxDistro()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            using (TestMLPerfExecutor MLPerfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestOS",
                    LinuxDistribution = LinuxDistribution.Flatcar
                };

                this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => MLPerfExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.LinuxDistributionNotSupported);
            }
        }

        [Test]
        public void MLPerfStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["Initialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            MLPerfExecutor.MLPerfState result = deserializedState?.ToObject<MLPerfExecutor.MLPerfState>();
            Assert.AreEqual(true, result.Initialized);
        }

        [Test]
        public async Task MLPerfCreatesScratchSpaceIfMLPerfScratchSpaceIsNotEmpty()
        {
            bool disksFetched = false;
            this.SetupDefaultMockBehavior(PlatformID.Unix);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>((CancellationToken) =>
            {
                disksFetched = true;
            })
            .ReturnsAsync(this.disks);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfExecutor.DiskFilter), "BiggestSize" },
                { nameof(MLPerfExecutor.Username), "anyuser" },
            };

            using (TestMLPerfExecutor mlperfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await mlperfExecutor.CreateScratchSpace(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(disksFetched);
        }

        [Test]
        public void MLPerfCreateScratchSpaceThrowsOnAbsenceOfDisks()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfExecutor.DiskFilter), "BiggestSize" },
                { nameof(MLPerfExecutor.Username), "anyuser" },
            };

            this.disks = null;

            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            using (TestMLPerfExecutor MLPerfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => MLPerfExecutor.CreateScratchSpace(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.WorkloadUnexpectedAnomaly);
            }
        }

        [Test]
        public void MLPerfCreateScratchSpaceThrowsOnAbsenceOfFilteredDisks()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfExecutor.DiskFilter), "SizeEqualTo:3072gb" },
                { nameof(MLPerfExecutor.Username), "anyuser" },
            };

            using (TestMLPerfExecutor MLPerfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => MLPerfExecutor.CreateScratchSpace(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.DependencyNotFound);
            }
        }

        [Test]
        public async Task MLPerfExecutorInitializesWorkloadAsExpected()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfExecutor.DiskFilter), "BiggestSize" },
                { nameof(MLPerfExecutor.Username), "anyuser" },
            };
            List<string> expectedCommands = new List<string>
            {
                "sudo usermod -aG docker anyuser",
                "sudo systemctl restart docker",
                "sudo systemctl start nvidia-fabricmanager",
                "sudo -u anyuser bash -c \"make prebuild MLPERF_SCRATCH_PATH=/dev/sdd1/scratch\"",
                "sudo docker ps",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make clean\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make link_dirs\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_data BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_model BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make preprocess_data BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_data BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_model BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make preprocess_data BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make build\"",
            };

            IEnumerable<string> expectedDirectories = new List<string>
            {
                "/dev/sdd1/scratch/data",
                "/dev/sdd1/scratch/models",
                "/dev/sdd1/scratch/preprocessed_data"
            };

            this.mockFixture.Directory.Setup(d => d.CreateDirectory(It.IsAny<string>())).Callback<string>((directory) =>
            {
                Assert.IsTrue(expectedDirectories.Select(ed => directory.Contains(ed)).Any());
            });

            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMLPerfExecutor mlperfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await mlperfExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands, commandsExecuted);
        }

        [Test]
        public async Task MLPerfExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            bool initializationVerified = false;

            this.mockFixture.StateManager.OnGetState()
            .Callback<String, CancellationToken, IAsyncPolicy>((stateId, cancellationToken, policy) =>
            {
                initializationVerified = true;
            })
            .ReturnsAsync(JObject.FromObject(new MLPerfExecutor.MLPerfState()
            {
                Initialized = true
            }));

            using (TestMLPerfExecutor mlperfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await mlperfExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(initializationVerified);
        }

        [Test]
        public async Task MLPerfExecutorExecutesAsExpected()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            string makeFileString = "mock Makefile";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(makeFileString);

            IEnumerable<string> expectedCommands = this.GetExpectedCommands();
            IEnumerable<string> expectedDirectories = new List<string>
            {
                "/dev/sdd1/scratch/data",
                "/dev/sdd1/scratch/models",
                "/dev/sdd1/scratch/preprocessed_data"
            };

            this.mockFixture.Directory.Setup(d => d.CreateDirectory(It.IsAny<string>())).Callback<string>((directory) =>
            {
                Assert.IsTrue(expectedDirectories.Select(ed => directory.Contains(ed)).Any());
            });

            List<string> commandsExecuted = new List<string>();

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMLPerfExecutor mlperfExecutor = new TestMLPerfExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await mlperfExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                await mlperfExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands.ToArray(), commandsExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);
            this.mockPackage = new DependencyPath("MLPerf", this.mockFixture.PlatformSpecifics.GetPackagePath("mlperf"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockFixture.File.Reset();

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfExecutor.DiskFilter), "BiggestSize" },
                { nameof(MLPerfExecutor.Username), "anyuser" },
                { nameof(MLPerfExecutor.Model), "bert"}
            };
        }

        private IEnumerable<string> GetExpectedCommands()
        {
            List<string> commands = null;
            commands = new List<string>
            {
                "sudo usermod -aG docker anyuser",
                "sudo systemctl restart docker",
                "sudo systemctl start nvidia-fabricmanager",
                "sudo -u anyuser bash -c \"make prebuild MLPERF_SCRATCH_PATH=/dev/sdd1/scratch\"",
                "sudo docker ps",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make clean\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make link_dirs\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_data BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_model BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make preprocess_data BENCHMARKS=bert\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_data BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make download_model BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make preprocess_data BENCHMARKS=3d-unet\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c " +
                "\"export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make build\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c \"" +
                "export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=PerformanceOnly --fast'\"",
                "sudo docker exec -u anyuser mlperf-inference-anyuser-x86_64 sudo bash -c \"" +
                "export MLPERF_SCRATCH_PATH=/dev/sdd1/scratch && make run RUN_ARGS='--benchmarks=bert --scenarios=Offline,Server,SingleStream --config_ver=default --test_mode=AccuracyOnly --fast'\""
            };

            return commands;
        }

        protected class TestMLPerfExecutor : MLPerfExecutor
        {
            public TestMLPerfExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task SetupEnvironmentAsync(CancellationToken cancellationToken)
            {
                return base.SetupEnvironmentAsync(cancellationToken);
            }

            public new Task CreateScratchSpace(CancellationToken cancellationToken)
            {
                return base.CreateScratchSpace(cancellationToken);
            }

            public new string GetContainerName()
            {
                return base.GetContainerName();
            }
        }
    }
}
