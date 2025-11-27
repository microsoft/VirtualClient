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

    /// <summary>
    /// Provides command option setup features to support SDK Agent 
    /// command lines.
    /// </summary>
    internal static class SdkAgentCommandFactory
    {
        /// <summary>
        /// Creates the set of supported commands/subcommands for the application.
        /// </summary>
        /// <param name="args">Command line arguments supplied to the application.</param>
        /// <param name="cancellationTokenSource">The application cancellation token source.</param>
        public static CommandLineBuilder CreateCommandLine(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            RootCommand rootCommand = new RootCommand("Executes SDK scripts, workloads and monitors on the system.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                // --profile
                OptionFactory.CreateProfileOption(required: false),

                // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --command
                OptionFactory.CreateCommandOption(required: false),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),
                
                // --dependencies
                OptionFactory.CreateDependenciesFlag(required: false),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString()),

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

                // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --package-store
                OptionFactory.CreatePackageStoreOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --scenarios
                OptionFactory.CreateScenariosOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --target
                OptionFactory.CreateTargetAgentOption(required: false),

                // --timeout
                OptionFactory.CreateTimeoutOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            rootCommand.Handler = CommandHandler.Create<DefaultCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            IEnumerable<Command> subcommands = new List<Command>
            {
                SdkAgentCommandFactory.CreateApiSubcommand(args, cancellationTokenSource),
                SdkAgentCommandFactory.CreateBootstrapSubcommand(args, cancellationTokenSource),
                SdkAgentCommandFactory.CreateCleanSubcommand(args, cancellationTokenSource),
                SdkAgentCommandFactory.CreateProcessTelemetrySubcommand(args, cancellationTokenSource),
                SdkAgentCommandFactory.CreateUploadFilesSubcommand(args, cancellationTokenSource)
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
                "Runs the SDK Agent API service and optionally monitors the API (local or a remote instance) for heartbeats.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                // --api-port
                OptionFactory.CreateApiPortOption(required: false),

                 // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --ip-address
                OptionFactory.CreateIPAddressOption(required: false),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --monitor
                OptionFactory.CreateMonitorFlag(required: false, false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            apiCommand.Handler = CommandHandler.Create<RunApiCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return apiCommand;
        }

        private static Command CreateBootstrapSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command bootstrapCommand = new Command(
                "bootstrap",
                "Installs a package on the system in the local SDK Agent 'packages' folder.")
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
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString()),

                // --key-vault
                OptionFactory.CreateKeyVaultOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --name
                OptionFactory.CreateNameOption(required: false),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --package-store
                OptionFactory.CreatePackageStoreOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            bootstrapCommand.AddAlias("install");

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

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            return cleanCommand;
        }

        private static Command CreateProcessTelemetrySubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command uploadTelemetryCommand = new Command(
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
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --intrinsic
                OptionFactory.CreateIntrinsicFlag(required: false),

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

            return uploadTelemetryCommand;
        }

        private static Command CreateRemoteSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command remoteExecuteCommand = new Command(
                "remote",
                "Executes scripts, workloads and monitors via an SDK Agent on a remote system.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --target
                OptionFactory.CreateTargetAgentOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------
                // --profile
                OptionFactory.CreateProfileOption(required: false),

                // --clean
                OptionFactory.CreateCleanOption(required: false),

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --command
                OptionFactory.CreateCommandOption(required: false),

                // --content-store
                OptionFactory.CreateContentStoreOption(required: false),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),
                
                // --dependencies
                OptionFactory.CreateDependenciesFlag(required: false),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString()),

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

                // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-retention
                OptionFactory.CreateLogRetentionOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --package-store
                OptionFactory.CreatePackageStoreOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --scenarios
                OptionFactory.CreateScenariosOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --timeout
                OptionFactory.CreateTimeoutOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            return remoteExecuteCommand;
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
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            return uploadFilesCommand;
        }
    }
}
