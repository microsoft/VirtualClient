// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.ServiceProcess;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Org.BouncyCastle.Bcpg.OpenPgp;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Logging;

    /// <summary>
    /// The main entry point for the program
    /// </summary>
    public sealed class Program
    {
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

                        CommandLineBuilder commandBuilder = Program.SetupCommandLine(args, cancellationSource);
                        ParseResult parseResult = commandBuilder.Build().Parse(args);
                        parseResult.ThrowOnUsageError();

                        Task<int> executionTask = parseResult.InvokeAsync();

                        // On Windows systems, this is required when running Virtual Client as a service.
                        // Certain notifications have to be sent to the Windows service control manager (SCM)
                        // in order to ensure the service is recognized as running.
                        Task serviceHostTask = Task.Run(() =>
                        {
                            if (Program.IsRunningAsService())
                            {
                                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                                {
                                    Program.RunAsWindowsService(cancellationSource);
                                }
                            }
                        });

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
            catch (ObjectDisposedException exc)
            {
                // Expected when the CancellationTokenSource is cancelled followed by being disposed.
                Console.WriteLine(exc.Message);
            }
            catch (OperationCanceledException exc)
            {
                // Expected when the Ctrl-C is pressed to cancel operation.
                Console.WriteLine(exc.Message);
            }
            catch (Exception exc)
            {
                exitCode = 1;
                Console.Error.WriteLine(exc.Message);
                Console.Error.WriteLine(exc.StackTrace);
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
        /// Sets up the application command line options.
        /// </summary>
        internal static CommandLineBuilder SetupCommandLine(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            RootCommand rootCommand = new RootCommand("Executes workload and monitoring profiles on the system.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                 // --profile
                OptionFactory.CreateProfileOption(required: false),

                // --api-port
                OptionFactory.CreateApiPortOption(required: false),

                // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path-template
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --timeout
                OptionFactory.CreateTimeoutOption(required: false),

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString()),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --fail-fast
                OptionFactory.CreateFailFastFlag(required: false),

                // --dependencies
                OptionFactory.CreateDependenciesFlag(required: false),

                // --iterations
                OptionFactory.CreateIterationsOption(required: false),

                // --key-vault
                OptionFactory.CreateKeyVaultOption(required: false),

                // --layout-path
                OptionFactory.CreateLayoutPathOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --log-to-file
                OptionFactory.CreateLogToFileFlag(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --package-store
                OptionFactory.CreatePackageStoreOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --proxy-api
                OptionFactory.CreateProxyApiOption(required: false),

                // --scenarios
                OptionFactory.CreateScenariosOption(required: false),

                // --seed
                OptionFactory.CreateSeedOption(required: false, 777),

                // --state-dir
                OptionFactory.CreateStateDirectoryOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            // Single command execution is also supported. Behind the scenes this uses a
            // profile execution flow to allow the user to execute the command (e.g. pwsh S:\Invoke-Script.ps1)
            // while additionally having the full set of other options available for profile execution.
            rootCommand.AddArgument(OptionFactory.CreateCommandArgument(required: false, string.Empty));

            rootCommand.TreatUnmatchedTokensAsErrors = false;
            rootCommand.Handler = CommandHandler.Create<ExecuteCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command runApiCommand = new Command(
                "runapi",
                "Runs the Virtual Client API service and optionally monitors the API (local or a remote instance) for heartbeats.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                // --api-port
                OptionFactory.CreateApiPortOption(required: false),

                 // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --ip-address
                OptionFactory.CreateIPAddressOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --log-to-file
                OptionFactory.CreateLogToFileFlag(required: false),

                // --monitor
                OptionFactory.CreateMonitorFlag(required: false, false),

                // --state-dir
                OptionFactory.CreateStateDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            runApiCommand.TreatUnmatchedTokensAsErrors = true;
            runApiCommand.AddAlias("RunApi");
            runApiCommand.AddAlias("RunAPI");
            runApiCommand.Handler = CommandHandler.Create<RunApiCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command runBootstrapCommand = new Command(
                "bootstrap",
                "Bootstraps/installs a dependency package on the system.")
            {
                // Required
                // -------------------------------------------------------------------
                // --package
                OptionFactory.CreatePackageOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------
                 // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString()),

                // --iterations (for integration only. not used/always = 1)
                OptionFactory.CreateIterationsOption(required: false),

                // --key-vault
                OptionFactory.CreateKeyVaultOption(required: false),

                // --layout-path (for integration only. not used.)
                OptionFactory.CreateLayoutPathOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --name
                OptionFactory.CreateNameOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --log-to-file
                OptionFactory.CreateLogToFileFlag(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --package-store
                OptionFactory.CreatePackageStoreOption(required: false),

                // --proxy-api
                OptionFactory.CreateProxyApiOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --state-dir
                OptionFactory.CreateStateDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            runBootstrapCommand.TreatUnmatchedTokensAsErrors = true;
            runBootstrapCommand.AddAlias("Bootstrap");
            runBootstrapCommand.AddAlias("Install");
            runBootstrapCommand.AddAlias("install");
            runBootstrapCommand.Handler = CommandHandler.Create<BootstrapPackageCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command runResetCommand = new Command(
                "reset",
                "Resets the state of the Virtual Client for a 'first run' scenario.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                 // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            runResetCommand.TreatUnmatchedTokensAsErrors = true;
            runResetCommand.AddAlias("Reset");
            runResetCommand.AddAlias("Clean");
            runResetCommand.AddAlias("clean");
            runResetCommand.Handler = CommandHandler.Create<CleanArtifactsCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            Command convertCommand = new Command(
                "convert",
                "Converts execution profiles from JSON to YAML format and vice-versa.")
            {
                // Required
                // -------------------------------------------------------------------
                // --profile
                OptionFactory.CreateProfileOption(required: true),

                // --output-path
                OptionFactory.CreateOutputDirectoryOption(required: true)
            };

            convertCommand.AddAlias("Convert");
            convertCommand.TreatUnmatchedTokensAsErrors = true;
            convertCommand.Handler = CommandHandler.Create<ConvertProfileCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            rootCommand.AddCommand(runApiCommand);
            rootCommand.AddCommand(runBootstrapCommand);
            rootCommand.AddCommand(runResetCommand);
            rootCommand.AddCommand(convertCommand);

            return new CommandLineBuilder(rootCommand).WithDefaults();
        }

        private static void InitializeStartupLogging(string[] args)
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            loggerProviders.Add(new ConsoleLoggerProvider(LogLevel.Trace));

            Program.Logger = new LoggerFactory(loggerProviders).CreateLogger("VirtualClient");
        }

        private static bool IsRunningAsService()
        {
            return !Environment.UserInteractive;
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

            private static class NativeMethods
            {
                [DllImport("advapi32.dll", SetLastError = true)]
                internal static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
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
        }
    }
}