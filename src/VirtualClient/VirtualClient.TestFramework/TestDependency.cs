// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A test/fake dependency executor that can be used in unit and functional 
    /// testing scenarios.
    /// </summary>
    public class TestDependency : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestDependency"/> class.
        /// </summary>
        public TestDependency(MockFixture fixture)
            : base(fixture?.Dependencies, fixture?.Parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDependency"/> class.
        /// </summary>
        public TestDependency(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Delegate/anonymous function can be used to inject behaviors into the dependency executor when
        /// the <see cref="ExecuteAsync(EventContext, CancellationToken)"/> method is called.
        /// </summary>
        public Action<EventContext, CancellationToken> OnExecute { get; set; }

        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.OnExecute?.Invoke(telemetryContext, cancellationToken);
            return Task.CompletedTask;
        }
    }
}
