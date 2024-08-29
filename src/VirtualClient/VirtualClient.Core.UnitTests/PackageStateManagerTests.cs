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
    using VirtualClient.TestExtensions;
    using VirtualClient.Common.Extensions;

    [TestFixture]
    [Category("Unit")]
    public class PackageStateManagerTests
    {
        private MockFixture fixture;
        private PackageStateManager stateManager;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
           
        }

        [Test]
        public void PackageStateManagerConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new PackageStateManager(null, this.fixture.PlatformSpecifics));
            Assert.Throws<ArgumentException>(() => new PackageStateManager(new Mock<IFileSystem>().Object, null));
        }

        [Test]
        public void PackageStateManagerConstructorsSetPropertiesToExpectedValues()
        {
            PackageStateManager manager = new PackageStateManager(this.fixture.FileSystem.Object, this.fixture.PlatformSpecifics);
            Assert.IsTrue(object.ReferenceEquals(this.fixture.FileSystem.Object, manager.FileSystem));
            Assert.IsTrue(object.ReferenceEquals(this.fixture.PlatformSpecifics, manager.PlatformSpecifics));
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task PackageStateManagerSavesStateObjectsToTheExpectedLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "examplepackage";
            string expectedStatePath = this.fixture.PlatformSpecifics.GetPackagePath("examplepackage.vcpkgreg");

            this.fixture.File.Setup(file => file.WriteAllTextAsync(expectedStatePath, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await this.stateManager.SaveStateAsync(expectedStateId, JObject.Parse("{ \"name\": \"examplepackage\" }"), CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            this.fixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task PackageStateManagerSavesTheExpectedStateObjectsToTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "examplepackage";
            JObject expectedState = JObject.Parse("{ \"name\": \"examplepackage\" }");

            this.fixture.File
                .Setup(file => file.WriteAllTextAsync(
                    It.IsAny<string>(),
                    It.Is<string>((state) => state.ToString().RemoveWhitespace() == expectedState.ToString().RemoveWhitespace()),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            await this.stateManager.SaveStateAsync(expectedStateId, expectedState, CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            this.fixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task PackageStateManagerGetsStateObjectsFromTheExpectedLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            string expectedStateId = "examplepackage";
            string expectedStatePath = this.fixture.PlatformSpecifics.GetPackagePath("examplepackage.vcpkgreg");

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(expectedStatePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync("{ \"name\": \"examplepackage\" }")
                .Verifiable();

            await this.stateManager.GetStateAsync(expectedStateId, CancellationToken.None, retryPolicy: Policy.NoOpAsync()).ConfigureAwait(false);
            this.fixture.File.VerifyAll();
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task PackageStateManagerGetsTheExpectedStateObjectFromTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            JObject expectedState = JObject.Parse("{ \"name\": \"examplepackage\" }");

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File
                .Setup(file => file.ReadAllTextAsync(
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedState.ToString());

            JObject actualState = await this.stateManager.GetStateAsync("AnyPackage", CancellationToken.None, retryPolicy: Policy.NoOpAsync())
                .ConfigureAwait(false);

            Assert.AreEqual(expectedState, actualState);
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public async Task PackageStateManagerReturnsNothingIfTheStateObjectDoesNotExistInTheBackingLocation(PlatformID platform)
        {
            this.SetupBehaviors(platform);

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(false);

            JObject actualState = await this.stateManager.GetStateAsync("StateDoesNotExist", CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsNull(actualState);
        }

        private void SetupBehaviors(PlatformID platform)
        {
            this.fixture.Setup(platform);
            this.stateManager = new PackageStateManager(this.fixture.FileSystem.Object, this.fixture.PlatformSpecifics);
        }
    }
}
