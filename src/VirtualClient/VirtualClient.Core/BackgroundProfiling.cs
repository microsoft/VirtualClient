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
    public class BackgroundProfiling : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundProfiling"/> class.
        /// </summary>
        /// <param name="profilingTask">The background task that handles the execution of profiler operations.</param>
        /// <param name="cancellationTokenSource">
        /// The cancellation token source used to manage cancellation of the background profiling
        /// operations.
        /// </param>
        private BackgroundProfiling(Task profilingTask, CancellationTokenSource cancellationTokenSource)
        {
            this.ProfilingTask = profilingTask;
            this.ProfilingCancellationSource = cancellationTokenSource;
        }

        /// <summary>
        /// The cancellation token source used to manage cancellation of the background profiling
        /// operations.
        /// </summary>
        public CancellationTokenSource ProfilingCancellationSource { get; }

        /// <summary>
        /// The background task that handles the execution of profiler operations.
        /// </summary>
        public Task ProfilingTask { get; }

        /// <summary>
        /// Creates a <see cref="BackgroundProfiling"/> instance and begins the background profiling operations.
        /// </summary>
        /// <param name="component">The component requesting the background profiling (e.g. a workload executor).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the background profiling operations.</param>
        /// <returns>
        /// A <see cref="BackgroundProfiling"/> running any background profiling operations.
        /// </returns>
        public static BackgroundProfiling Begin(VirtualClientComponent component, CancellationToken cancellationToken)
        {
            Task profilingTask = Task.CompletedTask;
            CancellationTokenSource profilingCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            if (component.ProfilingEnabled && component.ProfilingMode == ProfilingMode.OnDemand)
            {
                profilingTask = component.RequestProfilingAsync(profilingCancellationSource.Token);
            }

            return new BackgroundProfiling(profilingTask, profilingCancellationSource);
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
                        this.ProfilingCancellationSource.Cancel();
                        this.ProfilingTask.GetAwaiter().GetResult();
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    finally
                    {
                        this.ProfilingCancellationSource.Dispose();
                        this.disposed = true;
                    }
                }
            }
        }
    }
}
