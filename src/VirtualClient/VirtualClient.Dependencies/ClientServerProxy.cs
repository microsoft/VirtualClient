// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Acts as a proxy for a client to pass instructions to the server to execute actions
    /// defined in a profile.
    /// </summary>
    public class ClientServerProxy : VirtualClientComponent
    {
        private static readonly JsonSerializerSettings SerializerSettingsForOutput = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            FloatParseHandling = FloatParseHandling.Decimal,
        };

        private static Task proxyTask;

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
        private IDictionary<Guid, Tuple<Task, CancellationTokenSource>> backgroundTasks;
        private IStateManager stateManager;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientServerProxy"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public ClientServerProxy(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.backgroundTasks = new Dictionary<Guid, Tuple<Task, CancellationTokenSource>>();
            this.stateManager = dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource CancellationSource { get; private set; }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ClientServerProxy.proxyTask = Task.Run(async () =>
            {
                // The current model uses an event handler to subscribe to events that are processed by the 
                // Events API. Event handlers have a signature that is may be too strict to 
                using (this.CancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        // Subscribe to notifications from the Events API. The client passes instructions
                        // to the server via this API.
                        VirtualClientEventing.SendReceiveInstructions += this.OnInstructionsReceived;
                        VirtualClientEventing.SetEventingApiOnline(true);

                        await this.WaitAsync(this.CancellationSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    }
                    finally
                    {
                        // Cleanup the event subscription to avoid any issues with memory leaks.
                        VirtualClientEventing.SendReceiveInstructions -= this.OnInstructionsReceived;
                        VirtualClientEventing.SetEventingApiOnline(false);

                        await this.StopBackgroundTasksAsync(CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }
            });

            await Task.Delay(5000).ConfigureAwait(false);

            if (ClientServerProxy.proxyTask.IsFaulted)
            {
                throw new DependencyException($"Client/server proxy failed to start.", proxyTask.Exception, ErrorReason.ApiStartupFailed);
            }
        }

        /// <summary>
        /// Disposes of resources used by the class instance.
        /// </summary>
        /// <param name="disposing">Whether to force dispose the cancallation source.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!this.disposed)
                {
                    VirtualClientEventing.SendReceiveInstructions -= this.OnInstructionsReceived;
                    this.CancellationSource?.Dispose();
                    this.semaphore.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Executes on receiving instructions from client.  
        /// </summary>
        protected void OnInstructionsReceived(object sender, InstructionsEventArgs args)
        {
            try
            {
                if (!this.CancellationSource.IsCancellationRequested)
                {
                    this.semaphore.Wait();

                    try
                    {
                        Instructions instructions = args.Instructions;
                        EventContext telemetryContext = EventContext.Persisted().AddContext("instructions", instructions.ToString());

                        this.Logger.LogMessageAsync($"{this.TypeName}.InstructionsReceived", telemetryContext, async () =>
                        {
                            this.Logger.LogTraceMessage($"Instructions:{Environment.NewLine}{instructions.ToJson(ClientServerProxy.SerializerSettingsForOutput)}");

                            if (instructions.Type == InstructionsType.ClientServerExit)
                            {
                                // The client requested the client/server proxy shut down entirely. This can happen when the application is shutting down.
                                await this.StopBackgroundTasksAsync(this.CancellationSource.Token)
                                    .ConfigureAwait(false);

                                this.CancellationSource.Cancel();
                                return;
                            }
                            else if (instructions.Type == InstructionsType.ClientServerReset || instructions.Type == InstructionsType.ClientServerStartStopExecution)
                            {
                                // The client requested that the client/server proxy stop all background operations.
                                await this.StopBackgroundTasksAsync(args.Id, this.CancellationSource.Token)
                                    .ConfigureAwait(false);

                                return;
                            }
                            else if (instructions.Type == InstructionsType.ClientServerStartExecution)
                            {
                                VirtualClientComponent component = null;
                                if (instructions.TryGetComponent(out ExecutionProfileElement profileElement))
                                {
                                    component = ComponentFactory.CreateComponent(profileElement, this.Dependencies, this.ExecutionSeed);
                                }
                                else if (instructions.Properties.TryGetValue("type", out IConvertible componentType))
                                {
                                    component = ComponentFactory.CreateComponent(instructions.Properties, componentType.ToString(), this.Dependencies, this.ExecutionSeed);
                                }

                                if (component != null)
                                {
                                    CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationSource.Token);
                                    Task backgroundTask = this.ExecuteBackgroundTaskAsync(args.Id, component, cancellationSource);

                                    this.backgroundTasks.Add(args.Id, new Tuple<Task, CancellationTokenSource>(backgroundTask, cancellationSource));
                                }
                            }
                        });
                    }
                    finally
                    {
                        this.semaphore.Release();
                    }
                }
            }
            catch
            {
                // We should not surface exceptions that cause the eventing system
                // issues.
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1116:Split parameters should start on line after declaration", Justification = "Format is fine for a Task.Run case.")]
        private Task ExecuteBackgroundTaskAsync(Guid id, VirtualClientComponent component, CancellationTokenSource cancellationSource)
        {
            return Task.Run(async () =>
            {
                CancellationToken cancellationToken = cancellationSource.Token;

                try
                {
                    Task componentTask = component.ExecuteAsync(cancellationToken);

                    await this.SaveRequestStateAsync(id, ClientServerStatus.ExecutionStarted, cancellationToken)
                        .ConfigureAwait(false);

                    await componentTask.ConfigureAwait(false);

                    await this.SaveRequestStateAsync(id, ClientServerStatus.ExecutionCompleted, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception exc)
                {
                    await this.SaveRequestStateAsync(id, ClientServerStatus.Failed, cancellationToken, exc)
                        .ConfigureAwait(false);
                }
                finally
                {
                    this.backgroundTasks.Remove(id);
                    cancellationSource.Dispose();
                }
            },
            cancellationSource.Token);
        }

        private async Task DeleteRequestStateAsync(Guid requestId, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                await this.stateManager.DeleteStateAsync(requestId.ToString(), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task SaveRequestStateAsync(Guid requestId, ClientServerStatus status, CancellationToken cancellationToken, Exception error = null)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                State state = new State(new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                {
                    ["status"] = status.ToString()
                });

                if (error != null)
                {
                    state.Properties.Add("errorType", error.GetType().FullName);
                    state.Properties.Add("errorMessage", error.ToDisplayFriendlyString(withErrorType: false, withCallStack: false));

                    VirtualClientException vcException = error as VirtualClientException;
                    if (vcException != null)
                    {
                        state.Properties.Add("errorReason", (int)vcException.Reason);
                        state.Properties.Add("errorReasonName", vcException.Reason.ToString());
                    }
                }

                await this.stateManager.SaveStateAsync(requestId.ToString(), new Item<State>(requestId.ToString(), state), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task StopBackgroundTasksAsync(CancellationToken cancellationToken)
        {
            if (this.backgroundTasks.Any())
            {
                foreach (var entry in this.backgroundTasks)
                {
                    try
                    {
                        entry.Value.Item2.Cancel();
                        await this.DeleteRequestStateAsync(entry.Key, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch
                    {
                        // Don't let any exceptions thrown by the cancellation request cause issues.
                    }
                }

                try
                {
                    await Task.WhenAll(this.backgroundTasks.Values.Select(entry => entry.Item1))
                        .ConfigureAwait(false);
                }
                catch
                {
                    // Don't let any exceptions thrown by the cancellation request cause issues.
                }
                finally
                {
                    this.backgroundTasks.Clear();
                }
            }
        }

        private async Task StopBackgroundTasksAsync(Guid requestId, CancellationToken cancellationToken)
        {
            if (this.backgroundTasks.Any())
            {
                await this.SaveRequestStateAsync(requestId, ClientServerStatus.ResetInProgress, cancellationToken)
                    .ConfigureAwait(false);

                await this.StopBackgroundTasksAsync(cancellationToken)
                    .ConfigureAwait(false);

                await this.SaveRequestStateAsync(requestId, ClientServerStatus.ResetCompleted, cancellationToken)
                        .ConfigureAwait(false);
            }
        }
    }
}
