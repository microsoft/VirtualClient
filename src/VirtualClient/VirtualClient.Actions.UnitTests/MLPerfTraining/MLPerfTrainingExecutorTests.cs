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
    public class MLPerfTrainingExecutorTests
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
        public void MLPerfTrainingExecutorThrowsOnUnsupportedLinuxDistro()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
                {
                    OperationSystemFullName = "TestOS",
                    LinuxDistribution = LinuxDistribution.Flatcar
                };

                this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => MLPerfTrainingExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.LinuxDistributionNotSupported);
            }
        }

        [Test]
        public void MLPerfTrainingStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["Initialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            MLPerfTrainingExecutor.MLPerfTrainingState result = deserializedState?.ToObject<MLPerfTrainingExecutor.MLPerfTrainingState>();
            Assert.AreEqual(true, result.Initialized);
        }

        [Test]
        public async Task MLPerfTrainingExecutorInitializesWorkloadAsExpected()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfTrainingExecutor.Username), "anyuser" },
                { nameof(MLPerfTrainingExecutor.Model), "bert" },
                { nameof(MLPerfTrainingExecutor.BatchSize), "45"},
                { nameof(MLPerfTrainingExecutor.Implementation), "pytorch-22.09"},
                { nameof(MLPerfTrainingExecutor.ContainerName), "language_model"},
                { nameof(MLPerfTrainingExecutor.ConfigFile), "config_DGXA100_1x8x56x1.sh"}
            };
            List<string> expectedCommands = new List<string>
            {
                "usermod -aG docker anyuser",
                "sudo docker build --pull -t mlperf-training-anyuser-x86_64:language_model .",
                "sudo docker run --runtime=nvidia mlperf-training-anyuser-x86_64:language_model"
            };

            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await MLPerfTrainingExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands, commandsExecuted);
        }

        [Test]
        public async Task MLPerfTrainingExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            bool initializationVerified = false;

            this.mockFixture.StateManager.OnGetState()
            .Callback<String, CancellationToken, IAsyncPolicy>((stateId, cancellationToken, policy) =>
            {
                initializationVerified = true;
            })
            .ReturnsAsync(JObject.FromObject(new MLPerfTrainingExecutor.MLPerfTrainingState()
            {
                Initialized = true
            }));

            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await MLPerfTrainingExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(initializationVerified);
        }

        [Test]
        public async Task MLPerfTrainingExecutorExecutesAsExpected()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MLPerfTrainingExecutor.Username), "anyuser" },
                { nameof(MLPerfTrainingExecutor.Model), "bert" },
                { nameof(MLPerfTrainingExecutor.BatchSize), "45"},
                { nameof(MLPerfTrainingExecutor.Implementation), "pytorch-22.09"},
                { nameof(MLPerfTrainingExecutor.ContainerName), "language_model"},
                { nameof(MLPerfTrainingExecutor.DataPath), "mlperf-training-data-bert.1.0.0"},
                { nameof(MLPerfTrainingExecutor.GPUCount), "8"},
                { nameof(MLPerfTrainingExecutor.Scenario), "training-mlperf-bert-batchsize-45-gpu-8"},
                { nameof(MLPerfTrainingExecutor.ConfigFile), "config_DGXA100_1x8x56x1.sh"}
            };

            IEnumerable<string> expectedCommands = this.GetExpectedCommands();

            List<string> commandsExecuted = new List<string>();

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await MLPerfTrainingExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                await MLPerfTrainingExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands.ToArray(), commandsExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);
            this.mockPackage = new DependencyPath("MLPerfTraining", this.mockFixture.PlatformSpecifics.GetPackagePath("mlperf"));

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
                { nameof(MLPerfTrainingExecutor.Username), "anyuser" },
                { nameof(MLPerfTrainingExecutor.Model), "bert"}
            };
        }

        private IEnumerable<string> GetExpectedCommands()
        {
            List<string> commands = null;
            commands = new List<string>
            {
                "usermod -aG docker anyuser",
                "sudo docker build --pull -t mlperf-training-anyuser-x86_64:language_model .",
                "sudo docker run --runtime=nvidia mlperf-training-anyuser-x86_64:language_model",
                "sudo su -c \"source config_DGXA100_1x8x56x1.sh; env BATCHSIZE=45 DGXNGPU=8 CUDA_VISIBLE_DEVICES=\"0,1,2,3,4,5,6,7\" CONT=mlperf-training-anyuser-x86_64:language_model DATADIR=/mlperftraining0/mlperf-training-data-bert.1.0.0/mlperf-training-package/hdf5/training-4320 DATADIR_PHASE2=/mlperftraining0/mlperf-training-data-bert.1.0.0/mlperf-training-package/hdf5/training-4320 EVALDIR=/mlperftraining0/mlperf-training-data-bert.1.0.0/mlperf-training-package/hdf5/eval_varlength CHECKPOINTDIR=/mlperftraining0/mlperf-training-data-bert.1.0.0/mlperf-training-package/phase1 CHECKPOINTDIR_PHASE1=/mlperftraining0/mlperf-training-data-bert.1.0.0/mlperf-training-package/phase1 ./run_with_docker.sh\""
            };

            return commands;
        }

        protected class TestMLPerfTrainingExecutor : MLPerfTrainingExecutor
        {
            public TestMLPerfTrainingExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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

            public new string GetContainerName()
            {
                return base.GetContainerName();
            }
        }
    }
}
