// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensions;

    /// <summary>
    /// Virtual Client REST API controller for managing application-level eventing.
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
    /// 
    /// Implementing Event-based Asynchronous Pattern
    /// https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/implementing-the-event-based-asynchronous-pattern
    /// </remarks>
    [ApiController]
    [Route("/api/application")]
    public class ApplicationController : ControllerBase
    {
        private const string ApiName = "ApplicationApi";

        /// <summary>
        /// Initializes a new instance of the <see cref="StateController"/> class.
        /// </summary>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        public ApplicationController(ILogger logger = null)
        {
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// The logger to use for capturing API telemetry.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Returns a response of 'OK' after instructing the application to exit.
        /// </summary>
        /// <response code="200">OK. The application is signalled to exit.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpPost("exit")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Instructs the Virtual Client application to exit.")]
        [ApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> ExitAsync()
        {
            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted();

            return this.Logger.LogMessageAsync($"{ApplicationController.ApiName}.ExitNotificationReceived", telemetryContext, () =>
            {
                try
                {
                    VirtualClientRuntime.CancellationSource.Cancel();
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
