// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HadoopTerasortExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private DependencyPath jdkMockPackage;
        private string teragenRawText;
        private string terasortRawText;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetUpFixture()
        {
            this.fixture = new MockFixture();
            this.teragenRawText = File.ReadAllText(@"Examples\Hadoop\HadoopTeragenExample.txt");
            this.terasortRawText = File.ReadAllText(@"Examples\Hadoop\HadoopTerasortExample.txt");
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void HadoopTerasortExecutorThrowsOnInitializationWhenTheWorkloadPackageIsNotFound(PlatformID platform)
        {
            this.SetupDefaultBehavior();
            this.fixture.PackageManager.OnGetPackage("hadoop-3.3.5").ReturnsAsync(null as DependencyPath);

            using (TestHadoopTerasortExecutor hadoopTerasortExecutor = new TestHadoopTerasortExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => hadoopTerasortExecutor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void HadoopTerasortExecutorThrowsIfCannotFindJdkPackage(PlatformID platform)
        {
            this.SetupDefaultBehavior();
            this.fixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(null as DependencyPath);

            using (TestHadoopTerasortExecutor hadoopTerasortExecutor = new TestHadoopTerasortExecutor(this.fixture))
            {
                DependencyException exception = Assert.ThrowsAsync<DependencyException>(
                    () => hadoopTerasortExecutor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, exception.Reason);
            }
        }

        [Test]
        public void HadoopExecutorStateIsSerializeable()
        {
            this.SetupDefaultBehavior();
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["HadoopExecutorStateInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            HadoopTerasortExecutor.HadoopExecutorState result = deserializedState?.ToObject<HadoopTerasortExecutor.HadoopExecutorState>();
            Assert.AreEqual(true, result.HadoopExecutorStateInitialized);
        }

        [Test]
        public async Task HadoopTerasortExecutorExecutesTheCorrectWorkloadCommands()
        {
            this.SetupDefaultBehavior();

            using (TestHadoopTerasortExecutor hadoopTerasortExecutor = new TestHadoopTerasortExecutor(this.fixture))
            {
                await hadoopTerasortExecutor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void HadoopTerasortExecutorThrowsWhenTheWorkloadDoesNotProduceValidResults(PlatformID platform)
        {
            this.SetupDefaultBehavior();
            this.defaultOutput.Clear();
            this.fixture.Process.StandardError = null;

            using (TestHadoopTerasortExecutor executor = new TestHadoopTerasortExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

                WorkloadResultsException exception = Assert.ThrowsAsync<WorkloadResultsException>(
                    () => executor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadResultsNotFound, exception.Reason);
            }
        }

        private void SetupDefaultBehavior(PlatformID platform = PlatformID.Unix)
        {
            this.fixture.Setup(platform);
            this.mockPackage = new DependencyPath("hadoop-3.3.5", this.fixture.PlatformSpecifics.GetPackagePath("hadoop-3.3.5"));
            this.jdkMockPackage = new DependencyPath("javadevelopmentkit", this.fixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"));
            this.fixture.PackageManager.OnGetPackage("hadoop-3.3.5").ReturnsAsync(this.mockPackage);
            this.fixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(this.jdkMockPackage);

            this.fixture.File.Reset();
            this.fixture.File.Setup(fe => fe.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.File.Setup(fe => fe.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.teragenRawText);

            this.fixture.File.Setup(fe => fe.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));

            this.fixture.FileSystem.SetupGet(fs => fs.File)
                .Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(HadoopTerasortExecutor.PackageName), "hadoop-3.3.5" },
                { nameof(HadoopTerasortExecutor.JdkPackageName), "javadevelopmentkit" },
                { nameof(HadoopTerasortExecutor.Scenario), "HadoopTerasortWorkload" },
                { nameof(HadoopTerasortExecutor.RowCount), "10000" }
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Hadoop", "HadoopTeragenExample.txt");
            string results = File.ReadAllText(resultsPath);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(results);

            this.fixture.Process.StandardError.Append(this.defaultOutput);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;
        }

        private class TestHadoopTerasortExecutor : HadoopTerasortExecutor
        {
            public TestHadoopTerasortExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}
