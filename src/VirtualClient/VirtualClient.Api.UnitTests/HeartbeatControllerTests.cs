// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HeartbeatControllerTests
    {
        private HeartbeatController controller;

        [SetUp]
        public void SetupTests()
        {
            this.controller = new HeartbeatController(NullLogger.Instance);
        }

        [Test]
        public async Task HeartbeatControllerReturnsTheExpectedResponseForHeartbeats()
        {
            OkResult result = await this.controller.GetHeartbeatAsync(CancellationToken.None)
                as OkResult;

            Assert.IsNotNull(result);
        }
    }
}
