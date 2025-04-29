// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;
    using VirtualClient.Monitors;

    /// <summary>
    /// Command executes the operations to bootstrap/install dependencies on the system
    /// prior to running a Virtual Client profile.
    /// </summary>
    public class UploadFilesCommand : CommandBase
    {
        /// <summary>
        /// The source directory to watch for content upload requests/notifications.
        /// </summary>
        public string RequestsDirectory { get; set; }

        /// <summary>
        /// Executes the dependency bootstrap/installation operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;
            ILogger logger = null;
            EventContext.Persist(Guid.NewGuid());

            try
            {
                IServiceCollection dependencies = this.InitializeDependencies(args);
                logger = dependencies.GetService<ILogger>();

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>(this.Parameters, StringComparer.OrdinalIgnoreCase)
                {
                    { nameof(FileUploadMonitor.Scenario), "UploadContent" },
                };

                if (!string.IsNullOrWhiteSpace(RequestsDirectory))
                {
                    parameters[nameof(FileUploadMonitor.RequestsDirectory)] = this.RequestsDirectory;
                }

                ConsoleLogger.Default.LogMessage($"Uploading Content/Files...", EventContext.Persisted());
                using (FileUploadMonitor monitor = new FileUploadMonitor(dependencies, parameters))
                {
                    await monitor.ExecuteAsync(cancellationTokenSource.Token);
                }
            }
            catch (Exception exc)
            {
                Program.LogErrorMessage(logger, exc, EventContext.Persisted());
                exitCode = 1;
            }

            return exitCode;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            IServiceCollection dependencies = base.InitializeDependencies(args);
            dependencies.AddSingleton<IFileSystem>(new FileSystem());

            return dependencies;
        }
    }
}
