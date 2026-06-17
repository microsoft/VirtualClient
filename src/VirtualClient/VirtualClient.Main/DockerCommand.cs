// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes a workload profile inside a Docker container.
    /// </summary>
    internal class DockerCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// The Docker image to use for container execution (e.g. ubuntu:noble, redis:7.0-alpine).
        /// </summary>
        public string DockerImage { get; set; }

        /// <summary>
        /// Initializes the command runtime before dependency initialization and execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            // Validate docker image is provided
            if (string.IsNullOrWhiteSpace(this.DockerImage))
            {
                throw new ArgumentException("Docker image (--image) is required for docker command execution.");
            }

            // Validate that at least one profile is specified
            if (this.Profiles == null || !this.Profiles.Any())
            {
                throw new ArgumentException("At least one profile (--profile) is required for docker command execution.");
            }

            // Set timeout to reasonable default if not specified
            if (this.Timeout == null)
            {
                this.Timeout = new ProfileTiming(TimeSpan.FromMinutes(30));
            }

            // Call parent initialization
            base.Initialize(args, platformSpecifics);
        }
    }
}

