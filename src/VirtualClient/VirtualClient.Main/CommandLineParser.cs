// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.CommandLine;
    using System.CommandLine.Builder;
    using System.CommandLine.Invocation;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Provides command option setup features to support Virtual Client 
    /// command lines.
    /// </summary>
    internal partial class CommandLineParser
    {
        private CommandLineParser(string[] args, CommandLineBuilder builder)
        {
            if (args != null)
            {
                this.Arguments = new ReadOnlyCollection<string>(args);
            }

            this.Builder = builder;
        }

        /// <summary>
        /// The arguments passed in on the command line.
        /// </summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>
        /// The command line builder used by the parser to create the command line
        /// option/argument handling.
        /// </summary>
        public CommandLineBuilder Builder { get; }

        /// <summary>
        /// The set of tokens parsed from the command line arguments.
        /// </summary>
        public IReadOnlyList<Token> Tokens { get; private set; }

        /// <summary>
        /// Creates the set of supported commands/subcommands for the application.
        /// </summary>
        /// <param name="args">Command line arguments supplied to the application.</param>
        /// <param name="cancellationTokenSource">The application cancellation token source.</param>
        public static CommandLineParser Create(IEnumerable<string> args, CancellationTokenSource cancellationTokenSource)
        {
            string[] commandLineArgs = args?.ToArray();

            RootCommand rootCommand = new RootCommand("Executes workload and monitoring profiles on the system.")
            {
                // OPTIONAL
                // -------------------------------------------------------------------
                // --profile
                OptionFactory.CreateProfileOption(required: false),

                // --api-port
                OptionFactory.CreateApiPortOption(required: false),

                // --archive-logs
                OptionFactory.CreateArchiveLogsOption(required: false),

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

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --fail-fast
                OptionFactory.CreateFailFastFlag(required: false),

                // --isolated
                OptionFactory.CreateIsolatedFlag(required: false),

                // --iterations
                OptionFactory.CreateIterationsOption(required: false),

                // --key-vault
                OptionFactory.CreateKeyVaultStoreOption(required: false),

                // --layout-path
                OptionFactory.CreateLayoutOption(required: false),

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

                // --state-dir
                OptionFactory.CreateStateDirectoryOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --target
                OptionFactory.CreateTargetOption(required: false),

                // --temp-dir
                OptionFactory.CreateTempDirectoryOption(required: false),

                // --timeout
                OptionFactory.CreateTimeoutOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            rootCommand.TreatUnmatchedTokensAsErrors = true;
            rootCommand.WithOptionValidation(commandLineArgs);
            rootCommand.Handler = CommandHandler.Create<ExecuteProfileCommand>(cmd => cmd.ExecuteAsync(commandLineArgs, cancellationTokenSource));

            IEnumerable<Command> subcommands = new List<Command>
            {
                CommandLineParser.CreateApiSubcommand(commandLineArgs, cancellationTokenSource),
                CommandLineParser.CreateBootstrapSubcommand(commandLineArgs, cancellationTokenSource),
                CommandLineParser.CreateConvertSubcommand(commandLineArgs, cancellationTokenSource),
                CommandLineParser.CreateGetTokenSubcommand(commandLineArgs, cancellationTokenSource),
                CommandLineParser.CreateUploadFilesSubcommand(commandLineArgs, cancellationTokenSource),
                CommandLineParser.CreateUploadTelemetrySubcommand(commandLineArgs, cancellationTokenSource)
            };

            foreach (Command command in subcommands)
            {
                rootCommand.AddCommand(command);
            }

            CommandLineBuilder builder = new CommandLineBuilder(rootCommand).WithDefaults();

            return new CommandLineParser(commandLineArgs, builder);
        }

        /// <summary>
        /// Parses the command line arguments.
        /// </summary>
        /// <remarks>
        /// The System.CommandLine library has quirks that causes any \" escaping characters to be removed
        /// from the end of any argument. This method handles this quirk to ensure users can reliably use escaping on
        /// the command line where required (e.g. --command="pwsh Invoke-FirmwareUpdate -Image \"/path/to/image.bin\" -LogDirectory \"/path/to/logs\"")
        /// </remarks>
        /// <returns>The result of the command line parsing.</returns>
        public ParseResult Parse()
        {
            string[] preprocessedArgs = CommandLineParser.PreprocessArguments(this.Arguments.ToArray());
            ParseResult results = this.Builder.Build().Parse(preprocessedArgs);

            if (!CommandLineParser.ContainsHelpFlag(results) && !CommandLineParser.ContainsVersionFlag(results))
            {
                // Scenario 1:
                // General parsing errors for the command line options.
                if (results.Errors?.Any() == true)
                {
                    throw new ArgumentException($"Invalid Usage. {string.Join(" ", results.Errors.Select(e => e.Message))}");
                }
            }

            return results;
        }

        private static bool ContainsHelpFlag(ParseResult parseResults)
        {
            bool containsHelpFlag = false;
            List<string> helpFlags = new List<string>
            {
                "/?",
                "-h",
                "--help"
            };

            if (parseResults?.Tokens?.Where(t => t.Type == TokenType.Option)?.Any(token => helpFlags.Contains(token.Value?.Trim())) == true)
            {
                containsHelpFlag = true;
            }

            return containsHelpFlag;
        }

        private static bool ContainsVersionFlag(ParseResult parseResults)
        {
            bool containsVersionFlag = false;
            Option versionOption = OptionFactory.CreateVersionOption();
            if (parseResults?.Tokens?.Any(token => versionOption.Aliases.Contains(token.Value.ToLower())) == true)
            {
                containsVersionFlag = true;
            }

            return containsVersionFlag;
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

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --ip-address
                OptionFactory.CreateIPAddressOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --monitor
                OptionFactory.CreateMonitorFlag(required: false, false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            apiCommand.WithOptionValidation(args);
            apiCommand.Handler = CommandHandler.Create<RunApiCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return apiCommand;
        }

        private static Command CreateBootstrapSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            // --package
            Option packageOption = OptionFactory.CreatePackageOption(required: false);

            // --package-store
            Option packageStoreOption = OptionFactory.CreatePackageStoreOption(required: false);

            // --cert-name
            Option certNameOption = OptionFactory.CreateCertificateNameOption(required: false);

            // --key-vault
            Option keyVaultOption = OptionFactory.CreateKeyVaultStoreOption(required: false);

            // --tenant-id
            Option tenantIdOption = OptionFactory.CreateTenantIdOption(required: false);

            // --token
            Option tokenOption = OptionFactory.CreateTokenOption(required: false);

            // --token-file
            Option tokenFileOption = OptionFactory.CreateTokenFileOption(required: false);

            Command bootstrapCommand = new Command(
                "bootstrap",
                "Bootstraps/installs a dependency package on the system.")
            {
                // REQUIRED
                // -------------------------------------------------------------------

                // OPTIONAL
                // -------------------------------------------------------------------     
                packageOption,
                packageStoreOption,
                certNameOption,
                keyVaultOption,
                tenantIdOption,
                tokenOption,
                tokenFileOption,

                // --client-id
                OptionFactory.CreateClientIdOption(required: false, Environment.MachineName),

                // --content-path
                OptionFactory.CreateContentPathTemplateOption(required: false),

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                // --iterations (for integration only. not used/always = 1)
                OptionFactory.CreateIterationsOption(required: false),

                // --layout-path (for integration only. not used.)
                OptionFactory.CreateLayoutOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --name
                OptionFactory.CreateNameOption(required: false),

                // --output-file
                OptionFactory.CreateOutputFileOption(required: false),

                // --package-dir
                OptionFactory.CreatePackageDirectoryOption(required: false),

                // --proxy-api
                OptionFactory.CreateProxyApiOption(required: false),

                // --system
                OptionFactory.CreateSystemOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            bootstrapCommand.AddValidator(result =>
            {
                OptionResult package = result.FindResultFor(packageOption);
                OptionResult packageStore = result.FindResultFor(packageStoreOption);
                OptionResult certName = result.FindResultFor(certNameOption);
                OptionResult accessToken = result.FindResultFor(tokenOption);
                OptionResult accessTokenFile = result.FindResultFor(tokenFileOption);
                OptionResult keyVault = result.FindResultFor(keyVaultOption);
                OptionResult tenantId = result.FindResultFor(tenantIdOption);

                // Must choose at least one operation.
                if (package == null && certName == null)
                {
                    throw new ArgumentException(
                        "Invalid usage. At least one type of target resource must be specified for the bootstrap command." +
                        "Use --package to install a package or --cert-name to install a certificate.");
                }

                if (package != null && packageStore == null)
                {
                    throw new ArgumentException("The package store URI must be provided (--package-store) when installing a package.");
                }

                // Certificate installation requires both --cert-name and --key-vault.
                if (certName != null)
                {
                    if (keyVault == null)
                    {
                        throw new ArgumentException("The Key Vault URI must be provided (--key-vault) when installing a certificate.");
                    }

                    if (accessToken == null && accessTokenFile == null)
                    {
                        // The tenant ID is required if the Microsoft Entra and certificate information is not provided
                        // in the URI for the Key Vault.
                        string keyVaultConnection = keyVault.Tokens.First().Value;

                        if ((Uri.TryCreate(keyVaultConnection, UriKind.Absolute, out Uri uri)
                            && !EndpointUtility.IsCustomUri(uri))
                            && !EndpointUtility.IsCustomConnectionString(keyVaultConnection)
                            && tenantId == null)
                        {
                            throw new ArgumentException("The Azure tenant ID must be provided (--tenant-id) when installing a certificate.");
                        }
                    }
                }

                if (accessToken != null && accessTokenFile != null)
                {
                    throw new ArgumentException("Ambiguous usage. The token (--token) and token path (--token-path) options cannot be used at the same time.");
                }

                return null;
            });

            bootstrapCommand.WithOptionValidation(args);
            bootstrapCommand.Handler = CommandHandler.Create<BootstrapCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return bootstrapCommand;
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

                // --output-dir
                OptionFactory.CreateOutputDirectoryOption(required: true)
            };

            convertCommand.WithOptionValidation(args);
            convertCommand.Handler = CommandHandler.Create<ConvertProfileCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return convertCommand;
        }

        private static Command CreateGetTokenSubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command getTokenCommand = new Command(
                "get-token",
                "Get an access token for current user for authentication with Azure resources.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --key-vault
                OptionFactory.CreateKeyVaultStoreOption(required: true),

                // --tenant-id
                OptionFactory.CreateTenantIdOption(required: true),

                // OPTIONAL
                // -------------------------------------------------------------------)
                // --output-file
                OptionFactory.CreateOutputFileOption(required: false),

                // --verbose
                OptionFactory.CreateVerboseFlag(required: false, false)
            };

            getTokenCommand.WithOptionValidation(args);
            getTokenCommand.Handler = CommandHandler.Create<GetAccessTokenCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return getTokenCommand;
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

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

                // --exit-wait
                OptionFactory.CreateExitWaitOption(required: false, TimeSpan.FromMinutes(30)),

                // --experiment-id
                OptionFactory.CreateExperimentIdOption(required: false, Guid.NewGuid().ToString().ToLowerInvariant()),

                 // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

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

        private static Command CreateUploadTelemetrySubcommand(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            Command processTelemetryCommand = new Command(
               "upload-telemetry",
               "Uploads telemetry (e.g. events, metrics) from data point files on the system to a target Event Hub.")
            {
                // REQUIRED
                // -------------------------------------------------------------------
                // --format
                OptionFactory.CreateDataFormatOption(required: true),

                // --schema
                OptionFactory.CreateDataSchemaOption(required: true),

                // --event-hub
                OptionFactory.CreateEventHubStoreOption(required: false),

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

                // --logger
                OptionFactory.CreateLoggerOption(required: false),

                // --log-dir
                OptionFactory.CreateLogDirectoryOption(required: false),

                // --log-level
                OptionFactory.CreateLogLevelOption(required: false, LogLevel.Information),

                // --match
                OptionFactory.CreateMatchExpressionOption(required: false),

                // --metadata
                OptionFactory.CreateMetadataOption(required: false),

                // --parameters
                OptionFactory.CreateParametersOption(required: false),

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
            processTelemetryCommand.Handler = CommandHandler.Create<UploadTelemetryCommand>(cmd => cmd.ExecuteAsync(args, cancellationTokenSource));

            return processTelemetryCommand;
        }

        private static string[] PreprocessArguments(params string[] args)
        {
            string[] preprocessedArgs = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains("\""))
                {
                    // System.CommandLine Quirk:
                    // The library parsing logic will strip the \" from the end of the command line
                    // vs. treating it as an explicit quotation mark to leave in place. There are no
                    // hooks in the library implementation to override this behavior.
                    //
                    // To workaround this we replace the quotes with the HTML encoding. Each option can
                    // then handle the HTML decoding as required.
                    preprocessedArgs[i] = args[i].Replace("\"", OptionFactory.HtmlQuote);
                    continue;
                }

                preprocessedArgs[i] = args[i];
            }

            return preprocessedArgs;
        }
    }
}