// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensions;

    /// <summary>
    /// Virtual Client REST API controller for providing heartbeats to clients.
    /// </summary>
    /// <remarks>
    /// Introduction to ASP.NET Core
    /// https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.1
    ///
    /// ASP.NET Core MVC Controllers
    /// https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-2.1
    ///
    /// Kestrel Web Server (Self-Hosting)
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-2.1
    /// 
    /// Async/Await/ConfigureAwait Overview
    /// https://www.skylinetechnologies.com/Blog/Skyline-Blog/December_2018/async-await-configureawait
    /// </remarks>
    [ApiController]
    [ApiVersion(1.0)]
    [ApiVersion(2.0)]
    [Route("/api/heartbeat")]
    public class HeartbeatController : ControllerBase
    {
        private const string ApiName = "HeartbeatApi";

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartbeatController"/> class.
        /// </summary>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        public HeartbeatController(ILogger logger = null)
        {
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// The logger to use for capturing API telemetry.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Returns a heartbeat response to the caller to indicate the API is online.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="200">OK. The API is online.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpGet]
        [Produces("application/json")]
        [Description("Returns a heartbeat response to the caller to indicate the API is online.")]
        [ProducesResponseType(typeof(JObject), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetHeartbeatAsync(CancellationToken cancellationToken)
        {
            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted();

            return this.Logger.LogMessageAsync($"{HeartbeatController.ApiName}.GetHeartbeat", telemetryContext, () =>
            {
                try
                {
                    response = this.Ok();
                }
                catch (Exception exc)
                {
                    response = new ObjectResult(this.CreateErrorDetails(
                        StatusCodes.Status500InternalServerError,
                        exc.Message,
                        exc.ToDisplayFriendlyString(true)));
                }
                finally
                {
                    telemetryContext.AddResponseContext(response);
                }

                return Task.FromResult(response);
            });
        }
    }
}
