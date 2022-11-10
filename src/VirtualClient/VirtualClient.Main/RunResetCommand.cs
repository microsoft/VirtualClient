// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command resets the Virtual Client environment for "first time" run scenarios.
    /// </summary>
    public class RunResetCommand : CommandBase
    {
        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IServiceCollection dependencies = this.InitializeDependencies(args);
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();

            await RunResetCommand.CleanStateDirectoryAsync(systemManagement, cancellationToken)
                .ConfigureAwait(false);

            return 0;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            IServiceCollection dependencies = base.InitializeDependencies(args);
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            ILogger logger = dependencies.GetService<ILogger>();

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                this.AgentId,
                Guid.NewGuid().ToString(),
                platformSpecifics.Platform,
                platformSpecifics.CpuArchitecture,
                logger);

            IApiManager apiManager = new ApiManager(systemManagement.FirewallManager);

            dependencies.AddSingleton<ISystemInfo>(systemManagement);
            dependencies.AddSingleton<ISystemManagement>(systemManagement);
            dependencies.AddSingleton<IApiManager>(apiManager);
            dependencies.AddSingleton<ProcessManager>(systemManagement.ProcessManager);
            dependencies.AddSingleton<IDiskManager>(systemManagement.DiskManager);
            dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            dependencies.AddSingleton<IFirewallManager>(systemManagement.FirewallManager);
            dependencies.AddSingleton<IPackageManager>(systemManagement.PackageManager);
            dependencies.AddSingleton<IStateManager>(systemManagement.StateManager);

            return dependencies;
        }

        private static async Task CleanStateDirectoryAsync(ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string stateDirectory = systemManagement.PlatformSpecifics.GetStatePath();

            IEnumerable<string> stateFiles = fileSystem.Directory.EnumerateFiles(stateDirectory, "*.*", System.IO.SearchOption.AllDirectories);

            if (stateFiles?.Any() == true)
            {
                foreach (string file in stateFiles)
                {
                    await fileSystem.File.DeleteAsync(file).ConfigureAwait(false);
                }
            }

            IEnumerable<string> stateDirectories = fileSystem.Directory.EnumerateDirectories(stateDirectory, "*.*", System.IO.SearchOption.AllDirectories);

            if (stateDirectories?.Any() == true)
            {
                foreach (string directory in stateDirectories)
                {
                    await fileSystem.Directory.DeleteAsync(directory, true).ConfigureAwait(false);
                }
            }
        }
    }
}