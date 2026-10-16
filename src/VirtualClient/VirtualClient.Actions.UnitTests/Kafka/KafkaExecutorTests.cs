// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class KafkaExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockKafkaPackage;
        private DependencyPath mockJdkPackage;

        public void SetupDefaults(PlatformID platformID)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platformID);
            this.mockKafkaPackage = new DependencyPath("kafka", this.fixture.GetPackagePath("kafka"));
            this.mockJdkPackage = new DependencyPath("javadevelopmentkit", this.fixture.GetPackagePath("javadevelopmentkit"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                ["Scenario"] = "Kafka_Scenario",
                ["PackageName"] = this.mockKafkaPackage.Name,
                ["ServerInstances"] = 1,
                ["Port"] = 9092
            };

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.ProcessManager.OnCreateProcess = (cmd, args, wd) => this.fixture.Process;
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockKafkaPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockKafkaPackage);
            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockJdkPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockJdkPackage);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "cmd")]
        [TestCase(PlatformID.Unix, "bash")]
        public async Task KafkaExecutorThrowsOnUnsupportedDistroAsync(PlatformID platformID, string command)
        {
            this.SetupDefaults(platformID);

            using (var executor = new TestKafkaExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
                Assert.IsTrue(executor.TestPlatformSpecificCommandType.Contains(command));
            }
        }

        [Test]
        public void KafkaExecutorValidationIdentifiesTheMaxClientInstanceCountParameter()
        {
            this.SetupDefaults(PlatformID.Unix);
            this.fixture.Parameters["Partitions"] = 1;
            this.fixture.Parameters["MaxClientInstances"] = 2;

            using (var executor = new TestKafkaExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                ArgumentException exception = Assert.Throws<ArgumentException>(() => executor.ValidateParameters());

                StringAssert.Contains(nameof(executor.MaxClientInstanceCount), exception.Message);
                StringAssert.Contains(nameof(executor.Partitions), exception.Message);
            }
        }

        private class TestKafkaExecutor : KafkaExecutor
        {
            public TestKafkaExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public string TestPlatformSpecificCommandType
            {
                get
                {
                    return this.PlatformSpecificCommandType ?? string.Empty;
                }
                set { }
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public void ValidateParameters()
            {
                base.Validate();
            }
        }
    }
}