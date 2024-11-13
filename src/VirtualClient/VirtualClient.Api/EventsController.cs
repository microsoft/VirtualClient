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
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensions;

    /// <summary>
    /// Virtual Client REST API controller for managing client/server eventing.
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
    [Route("/api/events")]
    public class EventsController : ControllerBase
    {
        private const string ApiName = "EventsApi";

        /// <summary>
        /// Initializes a new instance of the <see cref="StateController"/> class.
        /// </summary>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        public EventsController(ILogger logger = null)
        {
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// The logger to use for capturing API telemetry.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Returns a response of 'OK' if the eventing API is online/ready or 'Locked' if it is not.
        /// </summary>
        /// <response code="200">OK. The eventing API is online and ready to service requests.</response>
        /// <response code="423">Locked. The eventing API is awaiting signal before servicing requests.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpHead]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Receives an instruction and passes it along to subscribers.")]
        [ApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> ConfirmOnlineAsync()
        {
            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted();

            return this.Logger.LogMessageAsync($"{EventsController.ApiName}.ConfirmOnline", telemetryContext, () =>
            {
                try
                {
                    if (VirtualClientRuntime.IsApiOnline)
                    {
                        response = this.Ok();
                    }
                    else
                    {
                        response = this.StatusCode(StatusCodes.Status423Locked);
                    }
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

        /// <summary>
        /// Creates a state object/definition on the local Virtual Client system.
        /// </summary>
        /// <param name="instructions">Defines the instructions.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="201">Created. The state object/definition was successfully created.</response>
        /// <response code="404">Not Found. The state object/definition was not found on the system.</response>
        /// <response code="423">Locked. The eventing API is awaiting signal before servicing requests.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Receives an instruction and passes it along to subscribers.")]
        [ApiVersion("1.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> ReceiveInstructionsAsync([FromBody] JObject instructions, CancellationToken cancellationToken)
        {
            instructions.ThrowIfNull(nameof(instructions));

            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext(nameof(instructions), instructions);

            return this.Logger.LogMessageAsync($"{EventsController.ApiName}.ReceiveInstructions", telemetryContext, () =>
            {
                try
                {
                    if (!VirtualClientRuntime.IsApiOnline)
                    {
                        response = this.StatusCode(StatusCodes.Status423Locked);
                    }
                    else
                    {
                        VirtualClientRuntime.OnReceiveInstructions(this, instructions);
                        response = this.Ok();
                    }
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

        /// <summary>
        /// Creates a state object/definition on the local Virtual Client system.
        /// </summary>
        /// <param name="instructions">Defines the instructions.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="201">Created. The state object/definition was successfully created.</response>
        /// <response code="404">Not Found. The state object/definition was not found on the system.</response>
        /// <response code="423">Locked. The eventing API is awaiting signal before servicing requests.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpPost]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Receives an instruction and passes it along to subscribers.")]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status423Locked)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> ReceiveInstructionsAsync([FromBody] Instructions instructions, CancellationToken cancellationToken)
        {
            instructions.ThrowIfNull(nameof(instructions));

            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext(nameof(instructions), instructions);

            return this.Logger.LogMessageAsync($"{EventsController.ApiName}.ReceiveInstructions", telemetryContext, () =>
            {
                try
                {
                    if (!VirtualClientRuntime.IsApiOnline)
                    {
                        response = this.StatusCode(StatusCodes.Status423Locked);
                    }
                    else
                    {
                        Guid instructionsId = Guid.NewGuid();
                        VirtualClientRuntime.OnSendReceiveInstructions(this, new InstructionsEventArgs(instructionsId, instructions, CancellationToken.None));

                        response = this.Ok(new Item<Instructions>(instructionsId.ToString(), instructions));
                    }
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
