// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class ProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("EXAMPLE-WORKLOAD.json")]
        public async Task ParametersAndMetadataArePassedToTheVirtualClientComponentOnExecution(string profile)
        {
            this.SetupDefaultMockBehaviors();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
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
            this.mockFixture.SetupWorkloadPackage("exampleworkload", expectedFiles: @"win-x64\exampleworkload.exe");
            this.mockFixture.SetupDisks(withRemoteDisks: false);
        }
    }
}
