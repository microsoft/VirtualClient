// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Process manager that wraps commands in Docker when container mode is active.
    /// </summary>
    public class ContainerAwareProcessManager : ProcessManager
    {
        private readonly DockerRuntime dockerRuntime;
        private readonly PlatformSpecifics platformSpecifics;
        private readonly ProcessManager innerProcessManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dockerRuntime"></param>
        /// <param name="platformSpecifics"></param>
        /// <param name="innerProcessManager"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ContainerAwareProcessManager(
            DockerRuntime dockerRuntime,
            PlatformSpecifics platformSpecifics,
            ProcessManager innerProcessManager)
        {
            this.dockerRuntime = dockerRuntime ?? throw new ArgumentNullException(nameof(dockerRuntime));
            this.platformSpecifics = platformSpecifics ?? throw new ArgumentNullException(nameof(platformSpecifics));
            this.innerProcessManager = innerProcessManager ?? throw new ArgumentNullException(nameof(innerProcessManager));
        }

        /// <inheritdoc/>
        public override PlatformID Platform => this.innerProcessManager.Platform;

        /// <summary>
        /// Creates a process. If in container mode, the process runs inside Docker.
        /// </summary>
        public override IProcessProxy CreateProcess(string command, string arguments = null, string workingDirectory = null)
        {
            if (ContainerExecutionContext.Current.IsContainerMode)
            {
                // Wrap in Docker execution
                return new ContainerProcessProxy(
                    this.dockerRuntime,
                    ContainerExecutionContext.Current.Image,
                    command,
                    arguments,
                    workingDirectory,
                    ContainerExecutionContext.Current.Configuration,
                    this.platformSpecifics);
            }

            // Normal host execution - delegate to inner manager
            return this.innerProcessManager.CreateProcess(command, arguments, workingDirectory);
        }
    }

    /// <summary>
    /// Process proxy that executes inside a container.
    /// </summary>
    public class ContainerProcessProxy : IProcessProxy
    {
        private readonly DockerRuntime runtime;
        private readonly string image;
        private readonly string command;
        private readonly string arguments;
        private readonly string workingDirectory;
        private readonly ContainerConfiguration config;
        private readonly PlatformSpecifics platformSpecifics;
        
        private DockerRunResult result;
        private bool hasStarted;
        private DateTime startTime;
        private DateTime exitTime;
        private bool disposed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="runtime"></param>
        /// <param name="image"></param>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <param name="workingDirectory"></param>
        /// <param name="config"></param>
        /// <param name="platformSpecifics"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ContainerProcessProxy(
            DockerRuntime runtime,
            string image,
            string command,
            string arguments,
            string workingDirectory,
            ContainerConfiguration config,
            PlatformSpecifics platformSpecifics)
        {
            this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            this.image = image ?? throw new ArgumentNullException(nameof(image));
            this.command = command;
            this.arguments = arguments;
            this.workingDirectory = workingDirectory;
            this.config = config;
            this.platformSpecifics = platformSpecifics;
            
            this.StandardOutput = new ConcurrentBuffer();
            this.StandardError = new ConcurrentBuffer();
        }

        /// <inheritdoc/>
        public int Id => -1; // Container processes don't have a host PID

        /// <inheritdoc/>
        public string Name => $"docker:{this.image}";

        /// <inheritdoc/>
        public StringDictionary EnvironmentVariables => null;

        /// <inheritdoc/>
        public int ExitCode => this.result?.ExitCode ?? -1;

        /// <inheritdoc/>
        public DateTime ExitTime
        {
            get => this.exitTime;
            set => this.exitTime = value;
        }

        /// <inheritdoc/>
        public IntPtr? Handle => null;

        /// <inheritdoc/>
        public bool HasExited => this.result != null;

        /// <inheritdoc/>
        public bool RedirectStandardError { get; set; } = true;

        /// <inheritdoc/>
        public bool RedirectStandardInput { get; set; } = false;

        /// <inheritdoc/>
        public bool RedirectStandardOutput { get; set; } = true;

        /// <inheritdoc/>
        public ConcurrentBuffer StandardOutput { get; }

        /// <inheritdoc/>
        public ConcurrentBuffer StandardError { get; }

        /// <inheritdoc/>
        public StreamWriter StandardInput => null;

        /// <inheritdoc/>
        public ProcessStartInfo StartInfo => new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run {this.image} {this.command} {this.arguments}".Trim()
        };

        /// <inheritdoc/>
        public DateTime StartTime
        {
            get => this.startTime;
            set => this.startTime = value;
        }

        /// <inheritdoc/>
        public void Close()
        {
            // Container is already removed with --rm flag
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Kill()
        {
            // TODO: Implement docker stop/kill if needed
        }

        /// <inheritdoc/>
        public void Kill(bool entireProcessTree)
        {
            this.Kill();
        }

        /// <inheritdoc/>
        public bool Start()
        {
            this.hasStarted = true;
            this.startTime = DateTime.UtcNow;
            return true;
        }

        /// <inheritdoc/>
        public async Task WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            if (!this.hasStarted)
            {
                this.Start();
            }

            var fullCommand = string.IsNullOrWhiteSpace(this.arguments)
                ? this.command
                : $"{this.command} {this.arguments}";

            this.result = await this.runtime.RunAsync(
                this.image,
                fullCommand,
                this.config,
                this.platformSpecifics,
                cancellationToken);

            this.exitTime = DateTime.UtcNow;
            this.StandardOutput.Append(this.result.StandardOutput ?? string.Empty);
            this.StandardError.Append(this.result.StandardError ?? string.Empty);
        }

        /// <inheritdoc/>
        public IProcessProxy WriteInput(string input)
        {
            // Container stdin not supported in this implementation
            return this;
        }

        /// <summary>
        /// Disposes of resources used by the proxy.
        /// </summary>
        /// <param name="disposing">True to dispose of unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    // no underlying process defined yet.
                    ////this.UnderlyingProcess.Close();
                    ////this.UnderlyingProcess.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}