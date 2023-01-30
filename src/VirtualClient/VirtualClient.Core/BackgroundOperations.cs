// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Enables background profiling to be easily integrated into existing logic where on-demand
    /// profiling is desirable (e.g. workload execution).
    /// </summary>
    public class BackgroundOperations : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundOperations"/> class.
        /// </summary>
        /// <param name="profilingTask">The background task that handles the execution of profiler operations.</param>
        /// <param name="cancellationTokenSource">
        /// The cancellation token source used to manage cancellation of the background profiling
        /// operations.
        /// </param>
        private BackgroundOperations(Task profilingTask, CancellationTokenSource cancellationTokenSource)
        {
            this.BackgroundTask = profilingTask;
            this.BackgroundCancellationSource = cancellationTokenSource;
        }

        /// <summary>
        /// The cancellation token source used to manage cancellation of the background profiling
        /// operations.
        /// </summary>
        public CancellationTokenSource BackgroundCancellationSource { get; }

        /// <summary>
        /// The background task that handles the execution of profiler operations.
        /// </summary>
        public Task BackgroundTask { get; }

        /// <summary>
        /// Creates a <see cref="BackgroundOperations"/> instance and begins the background profiling operations.
        /// </summary>
        /// <param name="component">The component requesting the background profiling (e.g. a workload executor).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the background profiling operations.</param>
        /// <returns>
        /// A <see cref="BackgroundOperations"/> running any background profiling operations.
        /// </returns>
        public static BackgroundOperations BeginProfiling(VirtualClientComponent component, CancellationToken cancellationToken)
        {
            Task profilingTask = Task.CompletedTask;
            CancellationTokenSource profilingCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (component.ProfilingEnabled && component.ProfilingMode == ProfilingMode.OnDemand)
            {
                profilingTask = Task.Run(() =>
                {
                    // Invokes the SendReceiveInstructions event to enable any subscribers of the event (1 or more, e.g. Azure Profiler)
                    // to begin profiling.
                    Instructions instructions = new ProfilerInstructions(
                        InstructionsType.Profiling,
                        component.Parameters);

                    VirtualClientEventing.OnSendReceiveInstructions(component, new InstructionsEventArgs(instructions, cancellationToken));
                });
            }

            return new BackgroundOperations(profilingTask, profilingCancellationSource);
        }

        /// <summary>
        /// Disposes of resources used by the class.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cancels the profiling process and waits for any background profilers
        /// to exit.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    try
                    {
                        this.BackgroundCancellationSource.Cancel();
                        this.BackgroundTask.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        this.BackgroundCancellationSource.Dispose();
                        this.disposed = true;
                    }
                }
            }
        }
    }
}
