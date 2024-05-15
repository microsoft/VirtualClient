namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Dependency class to set environment variable on the system
    /// </summary>
    public class SetEnvironmentVariable : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MsmpiInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public SetEnvironmentVariable(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Delimitered environment varaible
        /// Example: Varaible1=A;Variable2=B
        /// </summary>
        public string EnvironmentVariables
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SetEnvironmentVariable.EnvironmentVariables), string.Empty);
            }
        }

        /// <summary>
        /// Sets environment variable
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var variablePairs = TextParsingExtensions.ParseVcDelimiteredParameters(this.EnvironmentVariables);
            foreach (var parameter in variablePairs)
            {
                this.SetEnvironmentVariable(parameter.Key, (string)parameter.Value);
            }

            return Task.CompletedTask;
        }
    }
}
