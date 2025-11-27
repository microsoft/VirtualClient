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
    using VirtualClient.Agent;

    internal partial class CommandFactory
    {
        /// <summary>
        /// Provides command option setup features to support SDK Agent 
        /// command lines.
        /// </summary>
        internal static class SdkAgent
        {
            /// <summary>
            /// Creates the set of supported commands/subcommands for the application.
            /// </summary>
            /// <param name="args">Command line arguments supplied to the application.</param>
            /// <param name="cancellationTokenSource">The application cancellation token source.</param>
            public static CommandLineBuilder CreateCommandBuilder(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                RootCommand rootCommand = new RootCommand("Executes SDK scripts, workloads and monitors on the system.")
                {
                    // OPTIONAL
                    // -------------------------------------------------------------------
                    // --api-port
                    OptionFactory.CreateApiPortOption(required: false),

                    // --profile
                    OptionFactory.CreateProfileOption(required: false),

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

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

                    // --experiment-name -> --metadata="experimentName=DC_Cycle,,,cycle=cycle1"
                    OptionFactory.CreateExperimentNameOption(required: false),

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

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

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

                    // --proxy-api
                    OptionFactory.CreateProxyApiOption(required: false),

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

                rootCommand.WithOptionValidation(args);
                rootCommand.Handler = CommandHandler.Create<ExecuteProfileCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                IEnumerable<Command> subcommands = new List<Command>
                {
                    CommandFactory.SdkAgent.CreateApiSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateBootstrapSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateCleanSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateCopyLogsSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateInstallAgentSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateInstallPackagesSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateProcessTelemetrySubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateRemoteSubcommand(args, cancellationTokenSource),
                    CommandFactory.SdkAgent.CreateUploadFilesSubcommand(args, cancellationTokenSource)
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

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --monitor
                    OptionFactory.CreateMonitorFlag(required: false, false),

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
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                    // --content-store
                    OptionFactory.CreateContentStoreOption(required: false),

                    // --content-path
                    OptionFactory.CreateContentPathTemplateOption(required: false),

                    // --exit-wait
                    OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

                    // --key-vault
                    OptionFactory.CreateKeyVaultOption(required: false),

                    // --metadata
                    OptionFactory.CreateMetadataOption(required: false),

                    // --name
                    OptionFactory.CreateNameOption(required: false),

                     // --logger
                    OptionFactory.CreateLoggerOption(required: false),

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --log-retention
                    OptionFactory.CreateLogRetentionOption(required: false),

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

                     // --logger
                    OptionFactory.CreateLoggerOption(required: false),

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --log-retention
                    OptionFactory.CreateLogRetentionOption(required: false),

                    // --package-dir
                    OptionFactory.CreatePackageDirectoryOption(required: false),

                    // --verbose
                    OptionFactory.CreateVerboseFlag(required: false, false)
                };

                cleanCommand.WithOptionValidation(args);
                cleanCommand.Handler = CommandHandler.Create<CleanArtifactsCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return cleanCommand;
            }

            private static Command CreateCopyLogsSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                Command copyLogsCommand = new Command(
                    "copy-logs",
                    "Copies logs from a target system to the SDK Agent directory on the local system.")
                {
                    // REQUIRED
                    // -------------------------------------------------------------------
                    // --target
                    OptionFactory.CreateTargetAgentOption(required: true),

                    // OPTIONAL
                    // -------------------------------------------------------------------
                    // --client-id
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

                    // --experiment-name -> --metadata="experimentName=DC_Cycle,,,cycle=cycle1"
                    OptionFactory.CreateExperimentNameOption(required: false),

                    // --exit-wait
                    OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                    // --logger
                    OptionFactory.CreateLoggerOption(required: false),

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --metadata
                    OptionFactory.CreateMetadataOption(required: false),

                    // --system
                    OptionFactory.CreateSystemOption(required: false),

                    // --verbose
                    OptionFactory.CreateVerboseFlag(required: false, false)
                };

                copyLogsCommand.WithOptionValidation(args);
                copyLogsCommand.Handler = CommandHandler.Create<ExecuteAgentCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return copyLogsCommand;
            }

            private static Command CreateInstallAgentSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                Command installAgentCommand = new Command(
                    "install-agent",
                    "Installs the SDK Agent on a set of target systems.")
                {
                    // REQUIRED
                    // -------------------------------------------------------------------
                    // --target
                    OptionFactory.CreateTargetAgentOption(required: true),

                    // OPTIONAL
                    // -------------------------------------------------------------------
                    // --client-id
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

                    // --exit-wait
                    OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                    // --logger
                    OptionFactory.CreateLoggerOption(required: false),

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --metadata
                    OptionFactory.CreateMetadataOption(required: false),

                    // --system
                    OptionFactory.CreateSystemOption(required: false),

                    // --verbose
                    OptionFactory.CreateVerboseFlag(required: false, false)
                };

                installAgentCommand.WithOptionValidation(args);
                installAgentCommand.Handler = CommandHandler.Create<ExecuteAgentCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return installAgentCommand;
            }

            private static Command CreateInstallPackagesSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
            {
                Command installPackagesCommand = new Command(
                    "install-packages",
                    "Installs packages in the SDK Agent directory on a set of target systems.")
                {
                    // REQUIRED
                    // -------------------------------------------------------------------
                    // --target
                    OptionFactory.CreateTargetAgentOption(required: true),

                    // OPTIONAL
                    // -------------------------------------------------------------------
                    // --client-id
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

                    // --exit-wait
                    OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                    // --logger
                    OptionFactory.CreateLoggerOption(required: false),

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

                    // --metadata
                    OptionFactory.CreateMetadataOption(required: false),

                    // --package-dir
                    OptionFactory.CreatePackageDirectoryOption(required: false),

                    // --system
                    OptionFactory.CreateSystemOption(required: false),

                    // --verbose
                    OptionFactory.CreateVerboseFlag(required: false, false)
                };

                installPackagesCommand.WithOptionValidation(args);
                installPackagesCommand.Handler = CommandHandler.Create<ExecuteAgentCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return installPackagesCommand;
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
                    OptionFactory.CreateExperimentIdOption(required: false),

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

                processTelemetryCommand.WithOptionValidation(args);
                processTelemetryCommand.Handler = CommandHandler.Create<ProcessTelemetryCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return processTelemetryCommand;
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
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

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

                    // --experiment-name -> --metadata="experimentName=DC_Cycle,,,cycle=cycle1"
                    OptionFactory.CreateExperimentNameOption(required: false),

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

                    // --log-dir (e.g. folder1, folder1/folder2)
                    OptionFactory.CreateLogSubdirectoryOption(required: false),

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

                remoteExecuteCommand.WithOptionValidation(args);
                remoteExecuteCommand.Handler = CommandHandler.Create<ExecuteRemoteAgentCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

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
                    OptionFactory.CreateClientIdOption(required: false, Environment.MachineName.ToLowerInvariant()),

                    // --content-path
                    OptionFactory.CreateContentPathTemplateOption(required: false),

                    // --exit-wait
                    OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                    // --experiment-id
                    OptionFactory.CreateExperimentIdOption(required: false),

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

                uploadFilesCommand.WithOptionValidation(args);
                uploadFilesCommand.Handler = CommandHandler.Create<UploadFilesCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

                return uploadFilesCommand;
            }
        }
    }
}
