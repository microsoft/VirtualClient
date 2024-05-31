// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The HammerDB Server workload executor.
    /// </summary>
    public class HammerDBServerExecutor : HammerDBExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDBServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public HammerDBServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(HammerDBServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        this.SetServerOnline(true);

                        if (this.IsMultiRoleLayout())
                        {
                            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                            {
                                await this.WaitAsync(cancellationToken);
                            }
                        }
                    }
                    finally
                    {
                        this.SetServerOnline(false);
                    }
                }
            });
        }
    }
}
