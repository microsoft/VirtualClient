// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Abstractions;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Virtual Client REST API controller for managing state data.
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
    [Route(StateController.ApiRoute)]
    public class StateController : ControllerBase
    {
        private const string ApiName = "StateApi";
        private const string ApiRoute = "/api/State";

        private static readonly Assembly ControllerAssembly = Assembly.GetAssembly(typeof(StateController));
        private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1);
        private static readonly JsonSerializerSettings StateSerializationSettings = new JsonSerializerSettings
        {
            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Error,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="StateController"/> class.
        /// </summary>
        /// <param name="stateManager">Provides methods for managing local state.</param>
        /// <param name="fileSystem">Provides methods for managing state files on the file system.</param>
        /// <param name="platformSpecifics">Provides platform-specific information.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        public StateController(IStateManager stateManager, IFileSystem fileSystem, PlatformSpecifics platformSpecifics, ILogger logger = null)
        {
            stateManager.ThrowIfNull(nameof(stateManager));
            fileSystem.ThrowIfNull(nameof(fileSystem));
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));

            this.StateManager = stateManager;
            this.FileSystem = fileSystem;
            this.Logger = logger ?? NullLogger.Instance;
            this.PlatformSpecifics = platformSpecifics;
        }

        /// <summary>
        /// TODO: Use state manager. Provides methods for managing local state.
        /// </summary>
        public IStateManager StateManager { get; }

        /// <summary>
        /// Provides methods for managing state data/files on the local file
        /// system.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// The logger to use for capturing API telemetry.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Creates a state object/definition on the local Virtual Client system.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="201">Created. The state object/definition was successfully created.</response>
        /// <response code="404">Not Found. The state object/definition was not found on the system.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpPost("{stateId}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Creates a state object/definition on the local Virtual Client system.")]
        [ProducesResponseType(typeof(Item<JObject>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> CreateStateAsync(string stateId, [FromBody] JObject state, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext(nameof(stateId), stateId)
                .AddContext(nameof(state), state);

            return this.Logger.LogMessageAsync($"{StateController.ApiName}.CreateState", telemetryContext, async () =>
            {
                try
                {
                    await StateController.Semaphore.WaitAsync(TimeSpan.FromSeconds(30));

                    Item<JObject> stateInstance = new Item<JObject>(stateId, state);

                    string stateFilePath = this.GetStateFilePath(stateId);
                    string stateFileDirectory = Path.GetDirectoryName(stateFilePath);

                    telemetryContext.AddContext(nameof(stateFilePath), stateFilePath);

                    if (!this.FileSystem.Directory.Exists(stateFileDirectory))
                    {
                        this.FileSystem.Directory.CreateDirectory(stateFileDirectory).Create();
                    }

                    // The file cannot be access by anything else during the time it is being written to. This is purposeful
                    // to ensure consistency with read/write operations.
                    using (Stream stream = this.FileSystem.FileStream.Create(stateFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        byte[] fileContent = Encoding.UTF8.GetBytes(stateInstance.ToJson(StateController.StateSerializationSettings));
                        await stream.WriteAsync(fileContent, 0, fileContent.Length);

                        string stateObjectUri = this.Request != null
                            ? $"{this.Request?.Scheme}://{this.Request?.Host}{this.Request?.Path}"
                            : string.Empty;

                        response = this.Created(stateObjectUri, stateInstance);
                    }
                }
                catch (IOException exc) when (exc.Message.Contains("already exists") || exc.Message.Contains("used by another process"))
                {
                    response = this.Conflict(this.CreateErrorResponse(
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        $"A state object/definition with ID '{stateId}' already exists."));
                }
                finally
                {
                    StateController.Semaphore.Release();
                    telemetryContext.AddResponseContext(response);
                }

                return response;
            });
        }

        /// <summary>
        /// Deletes a state object/definition on the local Virtual Client system.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="204">NoContent. The state object/definition was successfully deleted.</response>
        /// <response code="404">Not Found. The state object/definition was not found on the system.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpDelete("{stateId}")]
        [Description("Deletes a state object/definition from the local Virtual Client system.")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> DeleteStateAsync(string stateId, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            IActionResult response = null;
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext(nameof(stateId), stateId);

            return this.Logger.LogMessageAsync($"{StateController.ApiName}.DeleteState", telemetryContext, async () =>
            {
                try
                {
                    await StateController.Semaphore.WaitAsync(TimeSpan.FromSeconds(30));
                    string stateFilePath = this.GetStateFilePath(stateId);

                    // We want to handle the case where the state file is being created or updated. During these points
                    // we want to block the file from being read to ensure optimistic concurrency.
                    await Policy.Handle<IOException>((exc) => exc.Message.Contains("used by another process"))
                        .WaitAndRetryAsync(20, (retries) => TimeSpan.FromSeconds(2 * retries)).ExecuteAsync(async () =>
                        {
                            try
                            {
                                await this.FileSystem.File.DeleteAsync(stateFilePath);
                            }
                            catch (FileNotFoundException)
                            {
                                // This is ok. So long as the file no longer exists at the end of this API
                                // call, we've accomplished the desired outcome.
                            }
                        });

                    response = this.NoContent();
                }
                catch (IOException exc) when (exc.Message.Contains("used by another process"))
                {
                    response = this.Conflict(this.CreateErrorResponse(
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        $"The state object/definition with ID '{stateId}' cannot be accessed."));
                }
                finally
                {
                    StateController.Semaphore.Release();
                    telemetryContext.AddResponseContext(response);
                }

                return response;
            });
        }

        /// <summary>
        /// Gets an experiment instance from the system.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="200">OK. The state object/instance was found on the system.</response>
        /// <response code="404">Not Found. The state object/instance was not found on the system.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpGet("{stateId}")]
        [Produces("application/json")]
        [Description("Gets a state object/definition from the local Virtual Client system.")]
        [ProducesResponseType(typeof(JObject), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> GetStateAsync(string stateId, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            IActionResult response = null;
            EventContext telemetryContext = new EventContext(Guid.NewGuid())
               .AddContext(nameof(stateId), stateId);

            return this.Logger.LogMessageAsync($"{StateController.ApiName}.GetState", telemetryContext, async () =>
            {
                try
                {
                    // We want to handle the case where the state file is being created or updated. During these points
                    // we want to block the file from being read to ensure optimistic concurrency.
                    await Policy.Handle<IOException>((exc) => exc.Message.Contains("used by another process"))
                        .WaitAndRetryAsync(20, (retries) => TimeSpan.FromSeconds(2 * retries)).ExecuteAsync(async () =>
                        {
                            string stateFilePath = this.GetStateFilePath(stateId);
                            string stateInstance = await this.FileSystem.File.ReadAllTextAsync(stateFilePath, cancellationToken);

                            response = this.Ok(JObject.Parse(stateInstance));
                        });
                }
                catch (DirectoryNotFoundException)
                {
                    response = this.NotFound(this.CreateErrorResponse(
                        StatusCodes.Status404NotFound,
                        "Not Found",
                        $"A state object/definition with ID '{stateId}' does not exist."));
                }
                catch (FileNotFoundException)
                {
                    response = this.NotFound(this.CreateErrorResponse(
                       StatusCodes.Status404NotFound,
                       "Not Found",
                       $"A state object/definition with ID '{stateId}' does not exist."));
                }
                catch (IOException exc) when (exc.Message.Contains("used by another process"))
                {
                    // Given the retries we have above, this is expected to be a very rare case. If it does happen
                    // though, then we will simply force the client to wait and retry on its side.
                    response = this.NotFound(this.CreateErrorResponse(
                       StatusCodes.Status404NotFound,
                       "Not Found",
                       $"A state object/definition with ID '{stateId}' does not exist."));
                }
                finally
                {
                    telemetryContext.AddResponseContext(response);
                }

                return response;
            });
        }

        /// <summary>
        /// Creates a state object/definition on the local Virtual Client system.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <response code="201">Created. The state object/definition was successfully created.</response>
        /// <response code="404">Not Found. The state object/definition was not found on the system.</response>
        /// <response code="500">Internal Server Error. An unexpected error occurred on the server.</response>
        [HttpPut("{stateId}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [Description("Creates a state object/definition on the local Virtual Client system.")]
        [ProducesResponseType(typeof(Item<JObject>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public Task<IActionResult> UpdateStateAsync(string stateId, [FromBody] Item<JObject> state, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            IActionResult response = null;
            EventContext telemetryContext = new EventContext(Guid.NewGuid())
              .AddContext(nameof(stateId), stateId)
              .AddContext(nameof(state), state);

            return this.Logger.LogMessageAsync($"{StateController.ApiName}.UpdateState", telemetryContext, async () =>
            {
                try
                {
                    await StateController.Semaphore.WaitAsync(TimeSpan.FromSeconds(30));

                    if (stateId != state.Id)
                    {
                        response = this.BadRequest(this.CreateErrorResponse(
                        StatusCodes.Status400BadRequest,
                        "Bad Request",
                        $"Invalid schema. The state ID provided does not match with the ID defined in the state object."));
                    }
                    else
                    {
                        string stateFilePath = this.GetStateFilePath(stateId);
                        string stateFileDirectory = Path.GetDirectoryName(stateFilePath);

                        if (!this.FileSystem.Directory.Exists(stateFileDirectory))
                        {
                            this.FileSystem.Directory.CreateDirectory(stateFileDirectory).Create();
                        }

                        state.LastModified = DateTime.UtcNow;

                        // The file cannot be access by anything else during the time it is being written to. This is purposeful
                        // to ensure consistency with read/write operations.
                        using (Stream stream = this.FileSystem.FileStream.Create(stateFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                        {
                            byte[] fileContent = Encoding.UTF8.GetBytes(state.ToJson(StateController.StateSerializationSettings));
                            stream.SetLength(0);
                            await stream.WriteAsync(fileContent, 0, fileContent.Length);
                            response = this.Ok(state);
                        }
                    }

                    telemetryContext.AddResponseContext(response);
                }
                catch (IOException exc) when (exc.Message.Contains("used by another process"))
                {
                    response = this.Conflict(this.CreateErrorResponse(
                        StatusCodes.Status409Conflict,
                        "Conflict",
                        $"The state object/definition with ID '{stateId}' was updated concurrently by another process."));

                    telemetryContext.AddResponseContext(response);
                }
                finally
                {
                    StateController.Semaphore.Release();
                }

                return response;
            });
        }

        private string GetStateFilePath(string stateId)
        {
            return this.PlatformSpecifics.GetStatePath($"{stateId.ToLowerInvariant()}.json");
        }

        private ProblemDetails CreateErrorResponse(int statusCode, string title, string details)
        {
            return new ProblemDetails
            {
                Detail = details,
                Instance = $"{this.Request?.Method} {this.Request?.Path.Value}",
                Status = statusCode,
                Title = title
            };
        }
    }
}
