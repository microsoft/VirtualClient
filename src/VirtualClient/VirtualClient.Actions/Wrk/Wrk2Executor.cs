// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Wrk Client executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class Wrk2Executor : WrkExecutor
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Wrk2Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Initializes the executor dependencies, package locations, server api, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.SystemManagement.PackageManager.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            DependencyPath scriptPackage = await this.SystemManagement.PackageManager.GetPackageAsync(Wrk2Executor.WrkConfiguration, cancellationToken).ConfigureAwait(false);

            if (workloadPackage == null || this.PackageName != "wrk2")
            {
                throw new DependencyException($"{this.TypeName} did not find correct package in the directory. Supported Package: wrk2. Package Provided: {this.PackageName}", ErrorReason.WorkloadDependencyMissing);
            }

            if (scriptPackage == null)
            {
                throw new DependencyException($"{this.TypeName} did not find package ({WrkExecutor.WrkConfiguration}) in the packages directory.", ErrorReason.WorkloadDependencyMissing);
            }

            this.PackageDirectory = workloadPackage.Path;
            this.ScriptDirectory = this.PlatformSpecifics.ToPlatformSpecificPath(scriptPackage, this.Platform, this.CpuArchitecture).Path;

            this.InitializeApiClients();
            await this.SetupWrkClient(telemetryContext, cancellationToken).ConfigureAwait(false);
        }
    }
}