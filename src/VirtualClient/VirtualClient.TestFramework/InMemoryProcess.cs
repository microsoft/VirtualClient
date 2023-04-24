// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;

    /// <summary>
    /// Represents a fake process.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
    public class InMemoryProcess : Dictionary<string, IConvertible>, IProcessProxy
    {
        private ProcessDetails processDetails;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryProcess"/>
        /// </summary>
        public InMemoryProcess()
            : this(new MemoryStream())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryProcess"/>
        /// </summary>
        public InMemoryProcess(Stream standardInput)
        {
            this.StandardError = new ConcurrentBuffer();
            this.StandardOutput = new ConcurrentBuffer();
            this.StandardInput = new StreamWriter(standardInput);
            this.StartInfo = new ProcessStartInfo
            {
                FileName = @"C:\any\path\command.exe",
                Arguments = "--argument1=123 --argument2=value"
            };
            this.processDetails = new ProcessDetails();
        }

        /// <summary>
        /// The exit time for the process.
        /// </summary>
        public DateTime ExitTime
        {
            get
            {
                if (this.HasExited)
                {
                    return DateTime.UtcNow;
                }

                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// The fake process ID.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The fake process name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Fake environment variables.
        /// </summary>
        public StringDictionary EnvironmentVariables => this.StartInfo?.EnvironmentVariables;

        /// <summary>
        /// Returns true if the process was started/executed.
        /// </summary>
        public bool Executed { get; private set; }

        /// <summary>
        /// The fake process exit code.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// A fake process handle.
        /// </summary>
        public IntPtr? Handle => IntPtr.Zero;

        /// <summary>
        /// Has the fake process exited?
        /// </summary>
        public bool HasExited => this.OnHasExited?.Invoke() ?? false;

        /// <summary>
        /// Redirect standard error for the fake process.
        /// </summary>
        public bool RedirectStandardError { get; set; }

        /// <summary>
        /// Redirect standard input for the fake process.
        /// </summary>
        public bool RedirectStandardInput { get; set; }

        /// <summary>
        /// Redirect standard output for the fake process.
        /// </summary>
        public bool RedirectStandardOutput { get; set; }

        /// <inheritdoc />
        public ConcurrentBuffer StandardError { get; set; }

        /// <inheritdoc />
        public ConcurrentBuffer StandardOutput { get; set; }

        /// <inheritdoc />
        public StreamWriter StandardInput { get; }

        /// <summary>
        /// The start info for the fake process.
        /// </summary>
        public ProcessStartInfo StartInfo { get; set; }

        /// <summary>
        /// The start time for the process.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Dispose' method is called.
        /// </summary>
        public Action OnDispose { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'HasExited' property is called.
        /// </summary>
        public Func<bool> OnHasExited { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Kill' method is called.
        /// </summary>
        public Action OnKill { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Start' method is called.
        /// </summary>
        public Func<bool> OnStart { get; set; }

        /// <inheritdoc />
        public ProcessDetails ProcessDetails
        {
            get
            {
                this.processDetails.CommandLine = SensitiveData.ObscureSecrets($"{this.StartInfo?.FileName} {this.StartInfo?.Arguments}".Trim());
                this.processDetails.ExitCode = this.ExitCode;
                this.processDetails.StandardError = this.StandardError?.Length > 0 ? this.StandardError.ToString() : string.Empty;
                this.processDetails.StandardOutput = this.StandardOutput?.Length > 0 ? this.StandardOutput.ToString() : string.Empty;
                this.processDetails.WorkingDirectory = this.StartInfo?.WorkingDirectory;

                return this.processDetails;
            }
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
        public void Dispose()
        {
            this.OnDispose?.Invoke();
        }

        /// <summary>
        /// Kills the fake process.
        /// </summary>
        public void Kill()
        {
            this.OnKill?.Invoke();
        }

        /// <summary>
        /// Kills the fake process, and associated child processes.
        /// </summary>
        public void Kill(bool entireProcessTree)
        {
            this.OnKill?.Invoke();
        }

        /// <summary>
        /// Starts the fake process.
        /// </summary>
        public bool Start()
        {
            this.Executed = true;
            this.StartTime = DateTime.UtcNow;
            return this.OnStart?.Invoke() ?? true;
        }

        public Task WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Writes input to the fake process standard input.
        /// </summary>
        public IProcessProxy WriteInput(string input)
        {
            this.StandardInput.WriteLine(input);
            this.StandardInput.Flush();

            return this;
        }
    }
}
