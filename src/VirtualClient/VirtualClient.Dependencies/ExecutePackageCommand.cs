// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes a command on the system with the working directory set to a 
    /// package installed.
    /// </summary>
    public class ExecutePackageCommand : ExecuteCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WgetPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public ExecutePackageCommand(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Initializes the component for execution.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.Parameters[nameof(ExecuteCommand.WorkingDirectory)] = package.Path;
        }

        /// <summary>
        /// Validates the parameters that were provided to the component.
        /// </summary>
        protected override void ValidateParameters()
        {
            base.ValidateParameters();

            if (!string.IsNullOrWhiteSpace(this.WorkingDirectory))
            {
                throw new DependencyException(
                    $"Invalid definition. The parameter '{nameof(this.WorkingDirectory)}' cannot be used. This component uses the " +
                    $"directory for the package defined by the '{nameof(this.PackageName)}' as the working directory. Use the '{nameof(ExecuteCommand)}' " +
                    $"component to define a specific working directory.",
                    ErrorReason.DependencyDescriptionInvalid);
            }
        }
    }
}
