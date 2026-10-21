// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Validation;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class KafkaProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();

            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, "ClientAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", "Client"),
                new ClientInstance("ServerAgent", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task KafkaWorkloadProfileInstallsTheExpectedDependencies(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, "ClientAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", "Client"),
                new ClientInstance("ServerAgent", "1.2.3.5", "Server"));
            this.mockFixture.SetupFile(this.mockFixture.Combine(
                this.mockFixture.PackagesDirectory,
                "microsoft-jdk-21.0.1",
                PlatformSpecifics.GetPlatformArchitectureName(platform, architecture),
                "bin",
                platform == PlatformID.Win32NT ? "java.exe" : "java"));

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(
                "PERF-KAFKA.json", this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "kafka");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "javadevelopmentkit");
            }
        }

        [Test]
        public void KafkaWorkloadProfileMeetsTheSchemaRequirements()
        {
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(
                "PERF-KAFKA.json", this.mockFixture.Dependencies))
            {
                ValidationResult result = SchemaRules.Instance.Validate(executor.Profile);

                Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.ValidationErrors));
            }
        }

        [Test]
        [TestCase("PERF-KAFKA.json")]
        public void KafkaWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-KAFKA.json")]
        public void KafkaWorkloadProfileUsesTheWmicFreePackageOnWindows(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT, Architecture.X64, "ClientAgent").SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", "Client"),
                new ClientInstance("ServerAgent", "1.2.3.5", "Server"));

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                ExecutionProfileElement kafkaPackage = executor.Profile.Dependencies.Single(
                    dependency => dependency.Parameters.TryGetValue("PackageName", out IConvertible packageName)
                        && packageName.ToString() == "kafka");

                Assert.AreEqual("kafka_2.13-3.8.0-3.zip", kafkaPackage.Parameters["BlobName"].ToString());
            }
        }

        [Test]
        [TestCase("PERF-KAFKA.json")]
        public void KafkaWorkloadProfileActionsWillNotBeExecutedIfTheDependencyPackagesDoesNotExist(string profile)
        {
            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Any());
            }
        }
    }
}