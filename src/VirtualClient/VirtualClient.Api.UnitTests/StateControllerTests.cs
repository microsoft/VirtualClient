// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class StateControllerTests
    {
        private MockFixture mockFixture;
        private IConfiguration mockConfiguration;
        private Mock<InMemoryFileSystemStream> mockFileStream;
        private State mockState;
        private Item<JObject> mockStateInstance;
        private StateController controller;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
            this.mockConfiguration = new ConfigurationBuilder().Build();
            this.mockFileStream = new Mock<InMemoryFileSystemStream>();
            this.mockState = new State(new Dictionary<string, IConvertible>
            {
                ["property1"] = 1234
            });

            this.mockStateInstance = new Item<JObject>(Guid.NewGuid().ToString(), JObject.FromObject(this.mockState));

            this.controller = new StateController(
                this.mockFixture.StateManager.Object,
                this.mockFixture.FileSystem.Object,
                new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64),
                this.mockFixture.Logger);

            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Returns(this.mockFileStream.Object);

            this.mockFixture.Directory.Setup(dir => dir.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task StateControllerCreatesTheExpectedStateInstance()
        {
            string expectedStateId = "teststate";

            HttpRequest request = this.controller.Request;

            IActionResult result = await this.controller.CreateStateAsync(
                expectedStateId,
                JObject.FromObject(this.mockState),
                CancellationToken.None);

            this.mockFixture.FileStream.Verify(f => f.New(
                It.Is<string>(filePath => Path.GetFileName(filePath) == $"{expectedStateId}.json"),
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None));
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResponseWhenTheStateObjectIsSuccessfullyCreated()
        {
            string expectedStateId = "teststate";

            CreatedResult result = await this.controller.CreateStateAsync(
                expectedStateId,
                JObject.FromObject(this.mockState),
                CancellationToken.None) as CreatedResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status201Created, result.StatusCode);
            Assert.IsNotNull(result.Value);
            Assert.IsInstanceOf<Item<JObject>>(result.Value);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResponseWhenTheStateObjectAlreadyExists()
        {
            string expectedStateId = "teststate";

            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Throws(new IOException("file already exists"));

            ConflictObjectResult result = await this.controller.CreateStateAsync(
                expectedStateId,
                JObject.FromObject(this.mockState),
                CancellationToken.None) as ConflictObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResponseWhenTheStateObjectIsInUseByAnotherProcess()
        {
            string expectedStateId = "teststate";

            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Throws(new IOException("used by another process"));

            ConflictObjectResult result = await this.controller.CreateStateAsync(
                expectedStateId,
                JObject.FromObject(this.mockState),
                CancellationToken.None) as ConflictObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Test]
        public async Task StateControllerDeletesTheExpectedStateObjectWhenItExists()
        {
            string expectedStateId = "teststate";
            NoContentResult result = await this.controller.DeleteStateAsync(expectedStateId, CancellationToken.None) as NoContentResult;

            this.mockFixture.File.Verify(f => f.Delete(It.Is<string>(path => path.Contains(expectedStateId))));
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResultWhenDeletingAStateObject()
        {
            string expectedStateId = "teststate";
            NoContentResult result = await this.controller.DeleteStateAsync(expectedStateId, CancellationToken.None) as NoContentResult;

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResultWhenAttemptingToDeleteAStateObjectThatDoesNotExist()
        {
            string expectedStateId = "teststate";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.Is<string>(path => path.Contains(expectedStateId)), It.IsAny<CancellationToken>()))
                .Throws(new FileNotFoundException());

            NoContentResult result = await this.controller.DeleteStateAsync(expectedStateId, CancellationToken.None) as NoContentResult;

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedStateInstanceWhenItExists()
        {
            string expectedStateId = "teststate";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.Is<string>(path => path.Contains(expectedStateId)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockStateInstance.ToJson())
                .Verifiable();

            await this.controller.GetStateAsync(expectedStateId, CancellationToken.None);

            this.mockFixture.File.Verify();
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResultWhenTheStateObjectExists()
        {
            string expectedStateId = "teststate";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.Is<string>(path => path.Contains(expectedStateId)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockStateInstance.ToJson());

            OkObjectResult result = await this.controller.GetStateAsync(expectedStateId, CancellationToken.None) as OkObjectResult;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Value);
            Assert.IsInstanceOf<JObject>(result.Value);
        }


        [Test]
        public async Task StateControllerReturnsTheExpectedResultWhenTheStateObjectDoesNotExist()
        {
            string expectedStateId = "teststate";

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.Is<string>(path => path.Contains(expectedStateId)), It.IsAny<CancellationToken>()))
                .Throws(new FileNotFoundException());

            NotFoundObjectResult result = await this.controller.GetStateAsync(expectedStateId, CancellationToken.None) as NotFoundObjectResult;

            Assert.IsNotNull(result);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResponseWhenTheStateObjectIsSuccessfullyUpdated()
        {
            OkObjectResult result = await this.controller.UpdateStateAsync(
                this.mockStateInstance.Id,
                this.mockStateInstance,
                CancellationToken.None) as OkObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
            Assert.IsNotNull(result.Value);
            Assert.IsInstanceOf<Item<JObject>>(result.Value);
        }

        [Test]
        public async Task StateControllerReturnsTheExpectedResponseWhenTheStateObjectIsInUseByAnotherProcessDuringUpdate()
        {
            this.mockFixture.FileStream.Setup(f => f.New(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>()))
                .Throws(new IOException("used by another process"));

            ConflictObjectResult result = await this.controller.UpdateStateAsync(
                this.mockStateInstance.Id,
                this.mockStateInstance,
                CancellationToken.None) as ConflictObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Test]
        public async Task StateControllerEnsuresTheStateObjectIdMatchesBeforeUpdating()
        {
            BadRequestObjectResult result = await this.controller.UpdateStateAsync(
                "InvalidId",
                this.mockStateInstance,
                CancellationToken.None) as BadRequestObjectResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(StatusCodes.Status400BadRequest, result.StatusCode);
        }
    }
}
