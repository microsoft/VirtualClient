// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Data.Common;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.ServiceProcess;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Logging;

    /// <summary>
    /// The main entry point for the program
    /// </summary>
    public sealed class Program
    {
        internal static IEnumerable<Token> CommandLineTokens { get; private set; }

        internal static IConfiguration Configuration { get; private set; }

        internal static ILogger Logger { get; set; }

        /// <summary>
        /// The application entry point/method.
        /// </summary>
        /// <param name="args">Arguments passed into the application on the command line.</param>
        [SuppressMessage("AsyncUsage", "AsyncFixer04:Fire-and-forget async call inside a using block", Justification = "CancellationTokenSource managed")]
        public static int Main(string[] args)
        {
            int exitCode = 0;

            try
            {
                var commandline = Environment.CommandLine;
                VirtualClientRuntime.CommandLineArguments = args;

                // We want to ensure that the platform on which we are running is actually supported.
                PlatformSpecifics.ThrowIfNotSupported(Environment.OSVersion.Platform);
                PlatformSpecifics.ThrowIfNotSupported(RuntimeInformation.ProcessArchitecture);

                // This helps ensure that relative paths (paths relative to the Virtual Client application) are
                // handled correctly. This is required for response file support where relative paths are used. For example
                // when a user defines the profile by name only (no full or relative path, --profile=ANY-PROFILE.json),
                // the working directory will matter.
                Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // We do not want to miss any startup errors that happen before we can get the rest of
                // the loggers setup.
                Program.InitializeStartupLogging(args);
                Program.Configuration = Program.LoadAppSettings();

                using (CancellationTokenSource cancellationSource = VirtualClientRuntime.CancellationSource)
                {
                    try
                    {
                        CancellationToken cancellationToken = cancellationSource.Token;
                        Console.CancelKeyPress += (sender, e) =>
                        {
                            cancellationSource.Cancel();
                            e.Cancel = true;
                        };


                        ParseResult parseResults = Program.ParseArguments(args, cancellationSource);
                        Program.CommandLineTokens = parseResults.Tokens;
                        Task<int> executionTask = parseResults.InvokeAsync();

                        // On Windows systems, this is required when running Virtual Client as a service.
                        // Certain notifications have to be sent to the Windows service control manager (SCM)
                        // in order to ensure the service is recognized as running.
                        if (Environment.OSVersion.Platform == PlatformID.Win32NT && Program.IsRunningAsService(Environment.OSVersion.Platform))
                        {
                            Task serviceHostTask = Task.Run(() =>
                            {
                                Program.RunAsWindowsService(cancellationSource);
                            });
                        }

                        exitCode = executionTask.GetAwaiter().GetResult();
                    }
                    catch (Exception)
                    {
                        // Occasionally some of the workloads throw exceptions right as VC receives a
                        // cancellation/exit request.
                        if (!cancellationSource.IsCancellationRequested)
                        {
                            throw;
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Expected when the CancellationTokenSource is cancelled followed by being disposed.
            }
            catch (OperationCanceledException)
            {
                // Expected when the Ctrl-C is pressed to cancel operation.
            }
            catch (CryptographicException exc)
            {
                // Certificate-related issues.
                exitCode = (int)ErrorReason.InvalidCertificate;
                Console.Error.WriteLine(exc.ToString(withCallStack: false, withErrorTypes: false));
                Program.WriteCrashLog(exc);
            }
            catch (NotSupportedException exc)
            {
                // Various usages that are not supported.
                exitCode = (int)ErrorReason.NotSupported;
                Console.Error.WriteLine(exc.ToString(withCallStack: false, withErrorTypes: false));
                Program.WriteCrashLog(exc);
            }
            catch (Exception exc)
            {
                exitCode = 1;
                Console.Error.WriteLine(exc.ToString(withCallStack: true, withErrorTypes: false));
                Program.WriteCrashLog(exc);
            }

            if (exitCode != 0)
            {
                int statusCode = StatusCodeRegistry.GetStatusCode(exitCode);
                Console.Error.WriteLine($"Status Code: Virtual Client = {statusCode}");
            }

            return exitCode;
         }

        /// <summary>
        /// Loads the settings from the appsettings.json file for the application.
        /// </summary>
        internal static IConfiguration LoadAppSettings()
        {
            return new ConfigurationBuilder()
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: false)
                .Build();
        }

        /// <summary>
        /// Logs the error to the logger provided or to the default console logger. This is a safety
        /// mechanism to ensure we capture some amount of output in the case that we fail before the
        /// logger is initialized.
        /// </summary>
        internal static void LogMessage(ILogger logger, string message, EventContext telemetryContext)
        {
            if (logger != null)
            {
                logger.LogMessage(message, telemetryContext);
            }
            else
            {
                ConsoleLogger.Default.LogMessage(message, telemetryContext);
            }
        }

        /// <summary>
        /// Logs the error to the logger provided or to the default console logger. This is a safety
        /// mechanism to ensure we capture some amount of output in the case that we fail before the
        /// logger is initialized.
        /// </summary>
        internal static void LogErrorMessage(ILogger logger, Exception exc, EventContext telemetryContext)
        {
            if (logger != null)
            {
                logger.LogErrorMessage(exc, telemetryContext);
            }
            else
            {
                ConsoleLogger.Default.LogErrorMessage(exc, telemetryContext);
            }
        }

        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        /// <param name="args">The command line arguments provided to the application.</param>
        /// <param name="cancellationSource">Provides an application-wide cancellation token.</param>
        /// <returns>The result of the command line parsing.</returns>
        internal static ParseResult ParseArguments(string[] args, CancellationTokenSource cancellationSource)
        {
#if !SDK_AGENT
            CommandLineBuilder commandBuilder = CommandFactory.CreateCommandBuilder(args, cancellationSource);
#else
            CommandLineBuilder commandBuilder = CommandFactory.SdkAgent.CreateCommandBuilder(args, cancellationSource);
#endif

            string[] preprocessedArgs = OptionFactory.PreprocessArguments(args);
            ParseResult parseResult = commandBuilder.Build().Parse(preprocessedArgs);
            parseResult.ThrowOnUsageError();

            return parseResult;
        }

        private static void InitializeStartupLogging(string[] args)
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            loggerProviders.Add(new ConsoleLoggerProvider(LogLevel.Trace));

            Program.Logger = new LoggerFactory(loggerProviders).CreateLogger("VirtualClient");
        }

        private static bool IsRunningAsService(PlatformID platform)
        {
            bool isService = false;
            if (platform == PlatformID.Win32NT)
            {
                // Windows services run as child processes of the "services.exe" module.
                int currentProcessId = Process.GetCurrentProcess().Id;
                int parentProcessId = WindowsServiceHost.GetParentProcessId(currentProcessId);
                isService = currentProcessId != parentProcessId;
            }

            return isService;
        }

        private static void RunAsWindowsService(CancellationTokenSource cancellationTokenSource)
        {
            Program.Logger?.LogTraceMessage("Running as service...");
            using (var host = new WindowsServiceHost(Program.Logger, cancellationTokenSource))
            {
                host.CanHandlePowerEvent = false;
                host.CanHandleSessionChangeEvent = false;
                host.CanPauseAndContinue = false;
                host.CanShutdown = true;
                host.CanStop = true;

                ServiceBase.Run(host);
            }
        }

        private static void WriteCrashLog(Exception exc)
        {
            try
            {
                string crashLogDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
                if (!Directory.Exists(crashLogDirectory))
                {
                    Directory.CreateDirectory(crashLogDirectory);
                }

                File.AppendAllText(
                    Path.Combine(crashLogDirectory, "crash.log"),
                    $"[{DateTime.Now.ToString("yyyy-MM-ddThh:mm:ss")}] Unhandled/Unexpected Error:{Environment.NewLine}" +
                    $"-----------------------------------------------------------------------------{Environment.NewLine}" +
                    $"{exc.ToString(withCallStack: true, withErrorTypes: true)}{Environment.NewLine}{Environment.NewLine}");
            }
            catch
            {
                // Best effort
            }
        }

        private class WindowsServiceHost : ServiceBase
        {
            private readonly CancellationTokenSource cancellationTokenSource;
            private readonly ILogger logger;

            /// <summary>
            /// Initializes a new instance of the <see cref="WindowsServiceHost"/> class.
            /// </summary>
            /// <param name="logger">Logger to use for capturing telemetry.</param>
            /// <param name="cancellationTokenSource">Cancellation token to notify cancel/stop events</param>
            public WindowsServiceHost(ILogger logger, CancellationTokenSource cancellationTokenSource)
            {
                this.cancellationTokenSource = cancellationTokenSource;
                this.logger = logger ?? NullLogger.Instance;
            }

            internal static int GetParentProcessId(int processId)
            {
                IntPtr hProcess = NativeMethods.OpenProcess(ProcessAccessFlags.QueryLimitedInformation, false, processId);
                if (hProcess == IntPtr.Zero) return -1;

                try
                {
                    PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                    int returnLength;
                    int status = NativeMethods.NtQueryInformationProcess(hProcess, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);

                    return (status == 0) ? pbi.ParentProcessId : -1;
                }
                finally
                {
                    NativeMethods.CloseHandle(hProcess);
                }
            }

            /// <inheritdoc/>
            protected override void OnStart(string[] args)
            {
                this.logger.LogTraceMessage($"Service Starting");
                this.SetStatus(ServiceState.Started);
            }

            /// <inheritdoc />
            protected override void OnStop()
            {
                this.logger.LogTraceMessage($"Service Stopping");
                this.SetStatus(ServiceState.StopPending);
                this.cancellationTokenSource.Cancel();
                this.SetStatus(ServiceState.Stopped);
            }

            /// <inheritdoc />
            protected override void OnShutdown()
            {
                this.logger.LogTraceMessage($"Service Shutdown");
                this.SetStatus(ServiceState.StopPending);
                this.cancellationTokenSource.Cancel();
                this.SetStatus(ServiceState.Stopped);
            }

            [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "This class is only used on Windows platforms.")]
            private void SetStatus(ServiceState state)
            {
                ServiceStatus serviceStatus = new ServiceStatus
                {
                    CurrentState = state,
                    WaitHint = (long)TimeSpan.FromMinutes(2).TotalMilliseconds,
                };

                NativeMethods.SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            }

            internal static class NativeMethods
            {
                [DllImport("advapi32.dll", SetLastError = true)]
                internal static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

                [DllImport("ntdll.dll")]
                internal static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
                    ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

                [DllImport("kernel32.dll")]
                internal static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

                [DllImport("kernel32.dll")]
                internal static extern bool CloseHandle(IntPtr hObject);
            }

            internal enum ServiceState
            {
                Stopped = 0x00000001,
                StartPending = 0x00000002,
                StopPending = 0x00000003,
                Started = 0x00000004,
                ContinuePending = 0x00000005,
                PausePending = 0x00000006,
                Paused = 0x00000007
            }

            /// <summary>
            /// Struct required by service functions
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            internal struct ServiceStatus
            {
                public long ServiceType;
                public ServiceState CurrentState;
                public long ControlsAccepted;
                public long Win32ExitCode;
                public long ServiceSpecificExitCode;
                public long CheckPoint;
                public long WaitHint;
            }

            internal struct PROCESS_BASIC_INFORMATION
            {
                public IntPtr Reserved1;
                public IntPtr PebBaseAddress;
                public IntPtr Reserved2;
                public IntPtr Reserved3;
                public int ParentProcessId;
                public IntPtr Reserved4;
            }

            [Flags]
            internal enum ProcessAccessFlags : uint
            {
                QueryLimitedInformation = 0x1000
            }
        }
    }
}