// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Server side execution
    /// </summary>
    public class NetworkingWorkloadProxy : VirtualClientComponent
    {
        private static readonly object LockObject = new object();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadProxy"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NetworkingWorkloadProxy(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        public CancellationTokenSource ServerCancellationSource { get; internal set; }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(NetworkingWorkloadProxy)}.ExecuteServer", telemetryContext, async () =>
            {
                // The current model uses an event handler to subscribe to events that are processed by the 
                // Events API. Event handlers have a signature that is may be too strict to 
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        // Subscribe to notifications from the Events API. The client passes instructions
                        // to the server via this API.
                        VirtualClientRuntime.ReceiveInstructions += this.OnInstructionsReceived;
                        this.SetServerOnline(true);

                        await this.WaitAsync(this.ServerCancellationSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    }
                    finally
                    {
                        // Cleanup the event subscription to avoid any issues with memory leaks.
                        VirtualClientRuntime.ReceiveInstructions -= this.OnInstructionsReceived;
                        this.SetServerOnline(false);
                    }
                }
            });
        }

        /// <summary>
        /// Disposes of resources used by the class instance.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!this.disposed)
                {
                    VirtualClientRuntime.ReceiveInstructions -= this.OnInstructionsReceived;
                    this.ServerCancellationSource?.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// On executes receiving after instructions from client.  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="instructions"></param>
        protected void OnInstructionsReceived(object sender, JObject instructions)
        {
            lock (NetworkingWorkloadProxy.LockObject)
            {
                try
                {
                    EventContext telemetryContext = EventContext.Persisted()
                        .AddContext("instructions", instructions.ToString());

                    if (VirtualClientRuntime.IsApiOnline)
                    {
                        this.Logger.LogMessageAsync($"{nameof(NetworkingWorkloadProxy)}.InstructionsReceived", telemetryContext, async () =>
                        {
                            this.Logger.LogTraceMessage($"{nameof(NetworkingWorkloadProxy)}.Notification = {instructions}");

                            CancellationToken cancellationToken = this.ServerCancellationSource.Token;

                            Item<Instructions> notification = instructions.ToObject<Item<Instructions>>();
                            Instructions workloadInstructions = notification.Definition;

                            workloadInstructions.Properties["TypeOfInstructions"] = workloadInstructions.Type;

                            // Create WorkloadServerExecutor
                            VirtualClientComponent serverComponent = ComponentFactory.CreateComponent(workloadInstructions.Properties, workloadInstructions.Properties["Type"].ToString(), this.Dependencies);
                            serverComponent.ClientRequestId = workloadInstructions.ClientRequestId;

                            await serverComponent.ExecuteAsync(cancellationToken)
                                .ConfigureAwait(false);
                        });
                    }
                }
                catch
                {
                    // We should not surface exceptions that cause the eventing system
                    // issues.
                }
            }
        }
    }
}
