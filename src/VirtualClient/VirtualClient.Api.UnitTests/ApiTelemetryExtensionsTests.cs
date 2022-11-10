// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Formatters;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.TestExtensions;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class ApiTelemetryExtensionsTests
    {
        [Test]
        public void AddResponseContextExtensionHandlesAllExpectedStatusCodeResultTypes()
        {
            Dictionary<IActionResult, int> statusCodeResults = new Dictionary<IActionResult, int>
            {
                { new StatusCodeResult(StatusCodes.Status200OK), StatusCodes.Status200OK },
                { new OkResult(), StatusCodes.Status200OK },
                { new BadRequestResult(), StatusCodes.Status400BadRequest },
                { new ConflictResult(), StatusCodes.Status409Conflict },
                { new NoContentResult(), StatusCodes.Status204NoContent },
                { new UnauthorizedResult(), StatusCodes.Status401Unauthorized }
            };

            foreach (var entry in statusCodeResults)
            {
                EventContext telemetryContext = new EventContext(Guid.NewGuid());
                IActionResult response = entry.Key;
                telemetryContext.AddResponseContext(response);

                Assert.IsTrue(telemetryContext.Properties.ContainsKey("response"));

                object expectedResponse = new
                {
                    statusCode = entry.Value
                };

                object actualResponse = telemetryContext.Properties["response"];

                SerializationAssert.JsonEquals(expectedResponse.ToJson(), actualResponse.ToJson());
            }
        }

        [Test]
        public void AddResponseContextExtensionHandlesAllExpectedObjectResultTypes()
        {
            Item<string> resultValue = new Item<string>("anyId", DateTime.UtcNow, DateTime.UtcNow, "anyValue");

            Dictionary<IActionResult, int> statusCodeResults = new Dictionary<IActionResult, int>
            {
                {
                    new ObjectResult(resultValue)
                    {
                        StatusCode = 200,
                        ContentTypes = new MediaTypeCollection { "application/json" },
                        DeclaredType = resultValue.GetType()
                    },
                    StatusCodes.Status200OK
                },
                {
                    new OkObjectResult(resultValue)
                    {
                        ContentTypes = new MediaTypeCollection { "application/json" },
                        DeclaredType = resultValue.GetType()
                    },
                    StatusCodes.Status200OK
                },
                {
                    new BadRequestObjectResult(resultValue)
                    {
                        ContentTypes = new MediaTypeCollection { "application/json" },
                        DeclaredType = resultValue.GetType()
                    },
                    StatusCodes.Status400BadRequest
                },
                {
                    new ConflictObjectResult(resultValue)
                    {
                        ContentTypes = new MediaTypeCollection { "application/json" },
                        DeclaredType = resultValue.GetType()
                    },
                    StatusCodes.Status409Conflict
                },
                {
                    new CreatedAtActionResult("/anyAction", "anyController", new { id = Guid.NewGuid() }, resultValue)
                    {
                        ContentTypes = new MediaTypeCollection { "application/json" },
                        DeclaredType = resultValue.GetType()
                    },
                    StatusCodes.Status201Created
                }
            };

            foreach (var entry in statusCodeResults)
            {
                EventContext telemetryContext = new EventContext(Guid.NewGuid());
                IActionResult response = entry.Key;
                telemetryContext.AddResponseContext(response);

                Assert.IsTrue(telemetryContext.Properties.ContainsKey("response"));

                object expectedResponse = new
                {
                    statusCode = entry.Value,
                    contentType = new string[] { "application/json" },
                    declaredType = resultValue.GetType().FullName,
                    result = resultValue
                };

                object actualResponse = telemetryContext.Properties["response"];

                SerializationAssert.JsonEquals(expectedResponse.ToJson(), actualResponse.ToJson());
            }
        }
    }
}