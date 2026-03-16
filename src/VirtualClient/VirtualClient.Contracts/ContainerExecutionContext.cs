// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Tracks whether execution is happening inside a container context
    /// and provides the effective platform for component execution.
    /// </summary>
    public class ContainerExecutionContext
    {
        private static ContainerExecutionContext current;

        /// <summary>
        /// Gets or sets the current container execution context.
        /// </summary>
        public static ContainerExecutionContext Current
        {
            get => current ??= new ContainerExecutionContext();
            set => current = value;
        }

        /// <summary>
        /// True if running with container mode enabled (--image was passed).
        /// </summary>
        public bool IsContainerMode { get; set; }

        /// <summary>
        /// The container image being used.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The platform inside the container (typically Linux).
        /// </summary>
        public PlatformID ContainerPlatform { get; set; } = PlatformID.Unix;

        /// <summary>
        /// The CPU architecture inside the container.
        /// </summary>
        public Architecture ContainerArchitecture { get; set; } = Architecture.X64;

        /// <summary>
        /// Gets the effective platform - container platform if in container mode,
        /// otherwise the host platform.
        /// </summary>
        public PlatformID EffectivePlatform => this.IsContainerMode
            ? this.ContainerPlatform
            : Environment.OSVersion.Platform;

        /// <summary>
        /// Gets the effective architecture.
        /// </summary>
        public Architecture EffectiveArchitecture => this.IsContainerMode
            ? this.ContainerArchitecture
            : RuntimeInformation.ProcessArchitecture;

        /// <summary>
        /// Container configuration from profile.
        /// </summary>
        public ContainerConfiguration Configuration { get; set; }
    }

    /// <summary>
    /// Container configuration from profile's Container section.
    /// </summary>
    public class ContainerConfiguration
    {
        /// <summary>
        /// Default image (can be overridden by --image CLI).
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Standard mount configuration.
        /// </summary>
        public ContainerMountConfig Mounts { get; set; } = new ContainerMountConfig();

        /// <summary>
        /// Working directory inside container.
        /// </summary>
        public string WorkingDirectory { get; set; } = "/vc";

        /// <summary>
        /// Environment variables to pass to container.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; set; }

        /// <summary>
        /// Additional mount paths beyond the defaults.
        /// </summary>
        public IList<string> AdditionalMounts { get; set; }

        /// <summary>
        /// Pull policy: Always, IfNotPresent, Never.
        /// </summary>
        public string PullPolicy { get; set; } = "IfNotPresent";
    }

    /// <summary>
    /// Standard VC directory mounts configuration.
    /// </summary>
    public class ContainerMountConfig
    {
        /// <summary>
        /// Mount the packages directory (/vc/packages).
        /// </summary>
        public bool Packages { get; set; } = true;

        /// <summary>
        /// Mount the logs directory (/vc/logs).
        /// </summary>
        public bool Logs { get; set; } = true;

        /// <summary>
        /// Mount the state directory (/vc/state).
        /// </summary>
        public bool State { get; set; } = true;

        /// <summary>
        /// Mount the temp directory (/vc/temp).
        /// </summary>
        public bool Temp { get; set; } = true;
    }
}