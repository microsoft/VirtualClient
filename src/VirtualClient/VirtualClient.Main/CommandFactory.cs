// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.Threading;
    using System.Windows.Input;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Configuration;

    /// <summary>
    /// Provides command option setup features to support Virtual Client 
    /// command lines.
    /// </summary>
    internal partial class CommandFactory
    {
        /// <summary>
        /// Creates the set of supported commands/subcommands for the application.
        /// </summary>
        /// <param name="args">Command line arguments supplied to the application.</param>
        /// <param name="cancellationTokenSource">The application cancellation token source.</param>
        public static CommandLineBuilder CreateCommandBuilder(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            RootCommand rootCommand = new RootCommand("Executes workloads and monitors on the system.")
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
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                // --command
                OptionFactory.CreateCommandOption(required: false),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),
                
                // --dependencies
                OptionFactory.CreateDependenciesFlag(required: false),

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --fail-fast
                OptionFactory.CreateFailFastFlag(required: false),

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

                // --temp-dir
                OptionFactory.CreateTempDirectoryOption(required: false),

                // --timeout
                OptionFactory.CreateTimeoutOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            rootCommand.WithOptionValidation(args);
            rootCommand.Handler = CommandHandler.Create<ExecuteProfileCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            IEnumerable<Command> subcommands = new List<Command>
            {
                CommandFactory.CreateApiSubcommand(args, cancellationTokenSource),
                CommandFactory.CreateBootstrapSubcommand(args, cancellationTokenSource),
                CommandFactory.CreateCleanSubcommand(args, cancellationTokenSource),
                CommandFactory.CreateConvertSubcommand(args, cancellationTokenSource),
                CommandFactory.CreateProcessTelemetrySubcommand(args, cancellationTokenSource),
                CommandFactory.CreateUploadFilesSubcommand(args, cancellationTokenSource)
            };

            foreach (Command command in subcommands)
            {
                rootCommand.AddCommand(command);
            }

            return new CommandLineBuilder(rootCommand).WithDefaults();
        }

        private static Command CreateApiSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command apiCommand = new Command(
                "api",
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

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

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

                // --temp-dir
                OptionFactory.CreateTempDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            apiCommand.WithOptionValidation(args);
            apiCommand.Handler = CommandHandler.Create<RunApiCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return apiCommand;
        }

        private static Command CreateBootstrapSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command bootstrapCommand = new Command(
                "bootstrap",
                "Bootstraps/installs a dependency package on the system.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --package
                OptionFactory.CreatePackageOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------
                // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

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

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

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

                // --temp-dir
                OptionFactory.CreateTempDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            bootstrapCommand.AddAlias("install-package");
            bootstrapCommand.WithOptionValidation(args);
            bootstrapCommand.Handler = CommandHandler.Create<BootstrapPackageCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return bootstrapCommand;
        }

        private static Command CreateCleanSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command cleanCommand = new Command(
                "clean",
                "Deletes log, state and temp files as well as previously downloaded packages from the system.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                 // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                 // --state-dir
                OptionFactory.CreateStateDirectoryOption(required: false),

                // --temp-dir
                OptionFactory.CreateTempDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            cleanCommand.WithOptionValidation(args);
            cleanCommand.Handler = CommandHandler.Create<CleanArtifactsCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return cleanCommand;
        }

        private static Command CreateConvertSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command convertCommand = new Command(
                "convert",
                "Converts execution profiles from JSON to YAML format and vice-versa.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --profile
                OptionFactory.CreateProfileOption(required: true),

                // --output-path
                OptionFactory.CreateOutputDirectoryOption(required: true)
            };

            convertCommand.WithOptionValidation(args);
            convertCommand.Handler = CommandHandler.Create<ConvertProfileCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return convertCommand;
        }

        private static Command CreateProcessTelemetrySubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command processTelemetryCommand = new Command(
               "process-telemetry",
               "Processes telemetry (e.g. events, metrics) from data point files on the system through the set of loggers provided.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --format
                OptionFactory.CreateDataFormatOption(required: true),

                // --schema
                OptionFactory.CreateDataSchemaOption(required: true),

                // --logger
                OptionFactory.CreateLoggerOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------
                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --intrinsic
                OptionFactory.CreateIntrinsicFlag(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --match
                OptionFactory.CreateMatchExpressionOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --recursive
                OptionFactory.CreateRecursiveFlag(required: false, false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --directory
                OptionFactory.CreateTargetDirectoryOption(required: false),

                // --files
                OptionFactory.CreateTargetFilesOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            processTelemetryCommand.WithOptionValidation(args);
            processTelemetryCommand.Handler = CommandHandler.Create<ProcessTelemetryCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return processTelemetryCommand;
        }

        private static Command CreateUploadFilesSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command uploadFilesCommand = new Command(
               "upload-files",
               "Uploads files from a directory on the system to a target content store.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --content-store
                OptionFactory.CreateContentStoreOption(required: true),

                // --directory
                OptionFactory.CreateTargetDirectoryOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------
                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            uploadFilesCommand.WithOptionValidation(args);
            uploadFilesCommand.Handler = CommandHandler.Create<UploadFilesCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return uploadFilesCommand;
        }
    }
}
