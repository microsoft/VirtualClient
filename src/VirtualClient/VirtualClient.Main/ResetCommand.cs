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
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command resets the Virtual Client environment for "first time" run scenarios.
    /// </summary>
    public class ResetCommand : CommandBase
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

            await ResetCommand.CleanLogsDirectoryAsync(systemManagement, cancellationToken);
            await ResetCommand.CleanStateDirectoryAsync(systemManagement, cancellationToken);
            await ResetCommand.CleanPackagesAsync(systemManagement, cancellationToken);

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

        private static async Task CleanLogsDirectoryAsync(ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string logsDirectory = systemManagement.PlatformSpecifics.GetLogsPath();

            IEnumerable<string> logFiles = fileSystem.Directory.EnumerateFiles(logsDirectory, "*.*", System.IO.SearchOption.AllDirectories);
            await ResetCommand.DeleteFiles(fileSystem, logFiles, cancellationToken);

            IEnumerable<string> logDirectories = fileSystem.Directory.EnumerateDirectories(logsDirectory, "*.*", System.IO.SearchOption.AllDirectories);
            await ResetCommand.DeleteDirectories(fileSystem, logDirectories, cancellationToken);
        }

        private static async Task CleanPackagesAsync(ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string packagesDirectory = systemManagement.PlatformSpecifics.GetPackagePath();

            IEnumerable<string> packageRegistrations = fileSystem.Directory.EnumerateFiles(packagesDirectory, "*.vcpkgreg", System.IO.SearchOption.AllDirectories);

            if (packageRegistrations?.Any() == true)
            {
                foreach (string file in packageRegistrations)
                {
                    DependencyPath packageInfo = (await fileSystem.File.ReadAllTextAsync(file)).FromJson<DependencyPath>();
                    bool isBuiltIn = packageInfo.Metadata.GetValue<bool>("built-in", false);

                    if (!isBuiltIn)
                    {
                        if (fileSystem.Directory.Exists(packageInfo.Path) && packageInfo.Path.StartsWith(packagesDirectory))
                        {
                            IEnumerable<string> packageFiles = fileSystem.Directory.EnumerateFiles(packageInfo.Path, "*.*", System.IO.SearchOption.AllDirectories);

                            await DeleteFiles(fileSystem, packageFiles, cancellationToken);
                            await fileSystem.Directory.DeleteAsync(packageInfo.Path, true);
                        }

                        await fileSystem.File.DeleteAsync(file);
                    }
                }
            }
        }

        private static async Task CleanStateDirectoryAsync(ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string stateDirectory = systemManagement.PlatformSpecifics.GetStatePath();

            IEnumerable<string> stateFiles = fileSystem.Directory.EnumerateFiles(stateDirectory, "*.*", System.IO.SearchOption.AllDirectories);
            await ResetCommand.DeleteFiles(fileSystem, stateFiles, cancellationToken);

            IEnumerable<string> stateDirectories = fileSystem.Directory.EnumerateDirectories(stateDirectory, "*.*", System.IO.SearchOption.AllDirectories);
            await ResetCommand.DeleteDirectories(fileSystem, stateDirectories, cancellationToken);
        }

        private static async Task DeleteDirectories(IFileSystem fileSystem, IEnumerable<string> directories, CancellationToken cancellationToken)
        {
            if (directories?.Any() == true)
            {
                foreach (string directory in directories)
                {
                    await fileSystem.Directory.DeleteAsync(directory, true);
                }
            }
        }

        private static async Task DeleteFiles(IFileSystem fileSystem, IEnumerable<string> files, CancellationToken cancellationToken)
        {
            if (files?.Any() == true)
            {
                foreach (string file in files)
                {
                    await fileSystem.File.DeleteAsync(file);
                }
            }
        }
    }
}