// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Test executor/component used to validate the discovery of extensions
    /// at runtime as part of the functional testing operations.
    /// </summary>
    public class TestExtensionsExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestExtensionsExecutor"/> class.
        /// </summary>
        public TestExtensionsExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
