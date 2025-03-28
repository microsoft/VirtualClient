// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MLPerfTrainingExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(MLPerfTrainingExecutorTests), "Examples", "MLPerfTraining");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;
        private string exampleResults;
        private List<string> commandsExecuted = new List<string>();

        public void SetupTest(PlatformID platformID)
        {
            this.commandsExecuted = new List<string>();

            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);
            this.mockPackage = new DependencyPath("mlperftraining", this.mockFixture.GetPackagePath("mlperf"));

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            this.mockFixture.SetupPackage(this.mockPackage);

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
                { nameof(MLPerfTrainingExecutor.ConfigFile), "config_DGXA100_1x8x56x1.sh"},
                { nameof(MLPerfTrainingExecutor.PackageName), "mlperftraining"}
            };

            this.exampleResults = File.ReadAllText(this.mockFixture.Combine(MLPerfTrainingExecutorTests.ExamplesDirectory, "Example_bert_real_output.txt"));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                this.commandsExecuted.Add($"{command} {arguments}".Trim());
                IProcessProxy process = new InMemoryProcess();
                process.StandardOutput.Append(this.exampleResults);

                return process;
            };
        }

        [Test]
        public async Task MLPerfTrainingExecutorInitializesWorkloadAsExpected()
        {
            this.SetupTest(PlatformID.Unix);

            List<string> expectedCommands = new List<string>
            {
                "sudo usermod -aG docker anyuser",
                "sudo docker build --pull -t mlperf-training-anyuser-x86_64:language_model .",
                "sudo docker run --runtime=nvidia mlperf-training-anyuser-x86_64:language_model"
            };

            using (TestMLPerfTrainingExecutor executor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            CollectionAssert.AreEqual(expectedCommands, commandsExecuted);
        }

        [Test]
        public async Task MLPerfTrainingExecutorExecutesAsExpected()
        {
            this.SetupTest(PlatformID.Unix);
            IEnumerable<string> expectedCommands = this.GetExpectedCommands();
            
            using (TestMLPerfTrainingExecutor executor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            CollectionAssert.AreEqual(expectedCommands.ToArray(), commandsExecuted);
        }

        private IEnumerable<string> GetExpectedCommands()
        {
            List<string> commands = null;
            commands = new List<string>
            {
                "sudo usermod -aG docker anyuser",
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
