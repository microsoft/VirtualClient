// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Acts as a proxy for a <see cref="Process"/> running on the local
    /// system.
    /// </summary>
    public class ProcessProxy : IProcessProxy
    {
        /// <summary>
        /// The list of exit codes that indicate success by default.
        /// </summary>
        public static readonly IEnumerable<int> DefaultSuccessCodes = new int[] { 0 };

        private DateTime startTime;
        private DateTime exitTime;
        private bool disposed;

        private ProcessDetails processDetails;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessProxy"/> class.
        /// </summary>
        /// <param name="process">The process associated with the proxy.</param>
        public ProcessProxy(Process process)
        {
            process.ThrowIfNull(nameof(process));
            this.UnderlyingProcess = process;
            this.StandardError = new ConcurrentBuffer();
            this.StandardOutput = new ConcurrentBuffer();
            this.processDetails = new ProcessDetails();
            this.processDetails.GeneratedResults = new List<string>();
        }

        /// <inheritdoc />
        public int Id => this.UnderlyingProcess.Id;

        /// <inheritdoc />
        public virtual string Name => this.UnderlyingProcess.ProcessName;

        /// <inheritdoc />
        public virtual StringDictionary EnvironmentVariables => this.UnderlyingProcess.StartInfo?.EnvironmentVariables;

        /// <inheritdoc />
        public virtual int ExitCode => this.UnderlyingProcess.ExitCode;

        /// <inheritdoc />
        public DateTime ExitTime
        {
            get
            {
                return this.exitTime;
            }

            set
            {
                this.exitTime = value;
            }
        }

        /// <inheritdoc />
        public IntPtr? Handle => this.UnderlyingProcess?.Handle;

        /// <inheritdoc />
        public virtual bool HasExited
        {
            get
            {
                try
                {
                    return this.UnderlyingProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    // No process is associated with this object.
                    return true;
                }
            }
        }

        /// <inheritdoc />
        public bool RedirectStandardError
        {
            get
            {
                return this.StartInfo.RedirectStandardError;
            }

            set
            {
                this.StartInfo.RedirectStandardError = value;
            }
        }

        /// <inheritdoc />
        public bool RedirectStandardInput
        {
            get
            {
                return this.StartInfo.RedirectStandardInput;
            }

            set
            {
                this.StartInfo.RedirectStandardInput = value;
            }
        }

        /// <inheritdoc />
        public bool RedirectStandardOutput
        {
            get
            {
                return this.StartInfo.RedirectStandardOutput;
            }

            set
            {
                this.StartInfo.RedirectStandardOutput = value;
            }
        }

        /// <inheritdoc />
        public ProcessDetails ProcessDetails
        {
            get
            {
                this.processDetails.Id = this.Id;
                this.processDetails.CommandLine = SensitiveData.ObscureSecrets($"{this.StartInfo?.FileName} {this.StartInfo?.Arguments}".Trim());
                this.processDetails.ExitCode = this.ExitCode;
                this.processDetails.StandardError = this.StandardError?.Length > 0 ? this.StandardError.ToString() : string.Empty;
                this.processDetails.StandardOutput = this.StandardOutput?.Length > 0 ? this.StandardOutput.ToString() : string.Empty;
                this.processDetails.WorkingDirectory = this.StartInfo?.WorkingDirectory;

                return this.processDetails;
            }
        }

        /// <inheritdoc />
        public ConcurrentBuffer StandardError { get; }

        /// <inheritdoc />
        public ConcurrentBuffer StandardOutput { get; }

        /// <inheritdoc />
        public StreamWriter StandardInput => this.UnderlyingProcess.StandardInput;

        /// <inheritdoc />
        public ProcessStartInfo StartInfo => this.UnderlyingProcess.StartInfo;

        /// <inheritdoc />
        public DateTime StartTime
        {
            get
            {
                return this.startTime;
            }

            set
            {
                this.startTime = value;
            }
        }

        /// <summary>
        /// Gets the underlying process itself.
        /// </summary>
        protected Process UnderlyingProcess { get; }

        /// <summary>
        /// Returns true if the underlying process is found running on the local system and outputs
        /// the <see cref="Process"/> itself.
        /// </summary>
        /// <param name="processId">The ID of the process to find.</param>
        /// <param name="process">The matching process (by process ID) if found.</param>
        /// <returns>
        /// True if the process is found on the local system, false if it not.
        /// </returns>
        public static bool TryGetProcess(int processId, out IProcessProxy process)
        {
            process = null;

            try
            {
                Process existingProcess = Process.GetProcessById(processId);
                if (existingProcess?.ProcessName != null)
                {
                    process = new ProcessProxy(existingProcess);
                }
            }
            catch (ArgumentException)
            {
                // Process is not running and does not exist.
            }
            catch (InvalidOperationException)
            {
                // Process is not running and does not exist.
            }

            return process != null;
        }

        /// <summary>
        /// Returns true if the exit code can be determined from the process.
        /// </summary>
        /// <param name="process">The process itself.</param>
        /// <param name="exitCode">The exit code of the process.</param>
        /// <returns>True if the process exit code is available, false if not.</returns>
        public static bool TryGetProcessExitCode(IProcessProxy process, out int? exitCode)
        {
            process.ThrowIfNull(nameof(process));
            exitCode = null;

            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {
                // Process exit code cannot be determined.
            }

            return exitCode != null;
        }

        /// <summary>
        /// Gracefully closes the process and main window.
        /// </summary>
        public void Close()
        {
            try
            {
                this.UnderlyingProcess.Close();
            }
            catch
            {
                // Best Effort
            }
            finally
            {
                this.exitTime = DateTime.UtcNow;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public virtual void Kill()
        {
            try
            {
                this.UnderlyingProcess.Kill(true);
            }
            catch
            {
                // Best effort.
            }
            finally
            {
                this.exitTime = DateTime.UtcNow;
            }
        }

        /// <inheritdoc />
        public virtual void Kill(bool entireProcessTree)
        {
            try
            {
                this.UnderlyingProcess.Kill(entireProcessTree);
            }
            catch
            {
                // Best effort.
            }
            finally
            {
                this.exitTime = DateTime.UtcNow;
            }
        }

        /// <inheritdoc />
        public virtual bool Start()
        {
            if (this.RedirectStandardError)
            {
                this.UnderlyingProcess.ErrorDataReceived += this.OnStandardErrorReceived;
            }

            if (this.RedirectStandardOutput)
            {
                this.UnderlyingProcess.OutputDataReceived += this.OnStandardOutputReceived;
            }

            bool processStarted = this.UnderlyingProcess.Start();

            if (processStarted)
            {
                this.startTime = DateTime.UtcNow;
                if (this.RedirectStandardError)
                {
                    this.UnderlyingProcess.BeginErrorReadLine();
                }

                if (this.RedirectStandardOutput)
                {
                    this.UnderlyingProcess.BeginOutputReadLine();
                }
            }

            return processStarted;
        }

        /// <summary>
        /// Writes input/command to standard input.
        /// </summary>
        /// <param name="input">The input/command to send to standard input.</param>
        public virtual IProcessProxy WriteInput(string input)
        {
            if (!this.RedirectStandardInput)
            {
                throw new InvalidOperationException(
                    $"The process is not redirecting standard input and thus cannot write input text " +
                    $"(command={this.StartInfo.FileName} {this.StartInfo.Arguments}).");
            }

            this.StandardInput.WriteLine(input);
            return this;
        }

        /// <summary>
        /// Wait for process to exit
        /// </summary>
        public virtual async Task WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            try
            {
                if (timeout == null)
                {
                    await this.UnderlyingProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    this.exitTime = DateTime.UtcNow;
                }
                else
                {
                    await this.UnderlyingProcess.WaitForExitAsync(cancellationToken).WaitAsync(timeout.Value).ConfigureAwait(false);
                    this.exitTime = DateTime.UtcNow;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected whenever the CancellationToken receives a cancellation request.
            }
            catch (TimeoutException) when (timeout != null)
            {
                // Expected timeout when timeout was specified.
            }
            finally
            {
                this.exitTime = DateTime.UtcNow;
            }
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
                    this.UnderlyingProcess.Close();
                    this.UnderlyingProcess.Dispose();
                }

                this.disposed = true;
            }
        }

        private void OnStandardErrorReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null)
            {
                this.StandardError.AppendLine(e.Data);
            }
        }

        private void OnStandardOutputReceived(object sender, DataReceivedEventArgs e)
        {
            if (e?.Data != null)
            {
                this.StandardOutput.AppendLine(e.Data);
            }
        }
    }
}