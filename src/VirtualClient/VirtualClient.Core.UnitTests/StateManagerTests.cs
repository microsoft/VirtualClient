// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class StateManagerTests
    {
        private MockFixture mockFixture;
        private StateManager stateManager;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
           
        }

        [Test]
        public void StateManagerConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new StateManager(null, this.mockFixture.PlatformSpecifics));
            Assert.Throws<ArgumentException>(() => new StateManager(new Mock<IFileSystem>().Object, null));
        }

        [Test]
        public void StateManagerConstructorsSetPropertiesToExpectedValues()
        {
            StateManager manager = new StateManager(this.mockFixture.FileSystem.Object, this.mockFixture.PlatformSpecifics);
            Assert.IsTrue(object.ReferenceEquals(this.mockFixture.FileSystem.Object, manager.FileSystem));
            Assert.IsTrue(object.ReferenceEquals(this.mockFixture.PlatformSpecifics, manager.PlatformSpecifics));
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task StateManagerSavesStateObjectsToTheExpectedLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "ExampleState";
            string expectedStatePath = this.mockFixture.PlatformSpecifics.GetStatePath("examplestate.json");

            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(expectedStatePath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await this.stateManager.SaveStateAsync(expectedStateId, JObject.Parse("{ \"any\": \"state\" }"), CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            this.mockFixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task StateManagerSavesTheExpectedStateObjectsToTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "ExampleState";
            JObject expectedState = JObject.Parse("{ \"any\": \"state\" }");

            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(
                    It.IsAny<string>(),
                    It.Is<string>((state) => state.ToString().RemoveWhitespace() == expectedState.ToString().RemoveWhitespace()),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await this.stateManager.SaveStateAsync(expectedStateId, expectedState, CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            this.mockFixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task StateManagerGetsStateObjectsFromTheExpectedLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "ExampleState";
            string expectedStatePath = this.mockFixture.PlatformSpecifics.GetStatePath("examplestate.json");

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(expectedStatePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{ \"any\": \"state\" }")
                .Verifiable();

            await this.stateManager.GetStateAsync(expectedStateId, CancellationToken.None, retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);
            this.mockFixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task StateManagerGetsTheExpectedStateObjectFromTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            JObject expectedState = JObject.Parse("{ \"any\": \"state\" }");

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedState.ToString());

            JObject actualState = await this.stateManager.GetStateAsync("AnyState", CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(expectedState, actualState);
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task StateManagerReturnsNothingIfTheStateObjectDoesNotExistInTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);

            JObject actualState = await this.stateManager.GetStateAsync("StateDoesNotExist", CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(actualState);
        }

        private void SetupBehaviors(PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            this.stateManager = new StateManager(this.mockFixture.FileSystem.Object, this.mockFixture.PlatformSpecifics);
        }
    }
}
