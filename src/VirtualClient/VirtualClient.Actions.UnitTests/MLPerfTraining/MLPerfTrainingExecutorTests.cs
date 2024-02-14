// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MLPerfTrainingExecutorTests
    {
        private DependencyFixture mockFixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;
        private string output;
        private List<string> commandsExecuted = new List<string>();

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new DependencyFixture();
            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public async Task MLPerfTrainingExecutorInitializesWorkloadAsExpected()
        { 
            List<string> expectedCommands = new List<string>
            {
                "sudo usermod -aG docker anyuser",
                "sudo docker build --pull -t mlperf-training-anyuser-x86_64:language_model .",
                "sudo docker run --runtime=nvidia mlperf-training-anyuser-x86_64:language_model"
            };

            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await MLPerfTrainingExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands, commandsExecuted);
        }

        [Test]
        public async Task MLPerfTrainingExecutorExecutesAsExpected()
        {
            IEnumerable<string> expectedCommands = this.GetExpectedCommands();
            
            using (TestMLPerfTrainingExecutor MLPerfTrainingExecutor = new TestMLPerfTrainingExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await MLPerfTrainingExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                await MLPerfTrainingExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands.ToArray(), commandsExecuted);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.commandsExecuted = new List<string>();
            this.mockFixture = new DependencyFixture();
            this.mockFixture.Setup(platformID);
            this.mockPackage = new DependencyPath("MLPerfTraining", this.mockFixture.PlatformSpecifics.GetPackagePath("mlperf"));

            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.AddRange(this.disks);
            this.mockFixture.SetupWorkloadPackage("mlperftraining", expectedFiles: @"win-x64\diskspd.exe");

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

            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\MLPerfTraining\Example_bert_real_output.txt");
            this.output = File.ReadAllText(outputPath);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                this.commandsExecuted.Add($"{command} {arguments}".Trim());
                process.StandardOutput.Append(this.output);

                return process;
            };
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
