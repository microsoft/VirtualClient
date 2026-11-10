// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class ProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        public void AspNetBombardierProfilePreservesDefaultFailurePolicy()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(
                "PERF-WEB-ASPNET-BOMBARDIER.json",
                this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
                Assert.That(executor.Profile.Actions[0].Parameters.ContainsKey("FailFast"), Is.False);
                Assert.That(executor.Profile.Actions[1].Parameters["ServerPort"].ToString(), Is.EqualTo("9876"));
            }
        }

        [Test]
        [TestCase("9876")]
        [TestCase("9800")]
        public async Task AspNetBombardierProfileResolvesTheConfiguredPortForBothActions(string port)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(
                "PERF-WEB-ASPNET-BOMBARDIER.json",
                this.mockFixture.Dependencies,
                parameterOverrides: new Dictionary<string, IConvertible> { ["ServerPort"] = port }))
            {
                var serverParameters = executor.Profile.Actions[0].Parameters;
                var clientParameters = executor.Profile.Actions[1].Parameters;
                Assert.That(clientParameters.ContainsKey("ServerPort"), Is.True);
                await ProfileExpressionEvaluator.Instance.EvaluateAsync(this.mockFixture.Dependencies, clientParameters, CancellationToken.None);
                Assert.That(serverParameters["ServerPort"].ToString(), Is.EqualTo(port));
                Assert.That(clientParameters["CommandArguments"].ToString(), Does.Contain($":{port}/json"));
                Assert.That(clientParameters["CommandArguments"].ToString(), Does.Not.Contain("{ServerPort}"));
            }
        }

        [Test]
        [TestCase("EXAMPLE-WORKLOAD.json")]
        public async Task ParametersAndMetadataArePassedToTheVirtualClientComponentOnExecution(string profile)
        {
            this.SetupDefaultMockBehaviors();

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                bool parametersSet = false;
                bool metadataSet = false;

                executor.ExecuteDependencies = false;
                executor.ActionBegin += (sender, args) =>
                {
                    parametersSet = args.Component.Parameters.Any();

                    IConvertible value = null;
                    metadataSet = args.Component.Metadata.Any()
                        && args.Component.Metadata.TryGetValue("ExampleMetadata1", out value) && value?.ToString() == "Value1"
                        && args.Component.Metadata.TryGetValue("ExampleMetadata2", out value) && (bool)value == true;
                };

                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(parametersSet);
                Assert.IsTrue(metadataSet);
            }
        }

        private void Executor_ActionBegin(object sender, ComponentEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SetupDefaultMockBehaviors()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupPackage("exampleworkload", expectedFiles: @"win-x64\exampleworkload.exe");
            this.mockFixture.SetupDisks(withRemoteDisks: false);
        }
    }
}
