// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides support for package manager operations with cross-process isolation
    /// and protection.
    /// </summary>
    public class IsolatedPackageManager : IPackageManager
    {
        private static readonly Mutex CrossProcessLock = new Mutex(false, "Global\\VCPackageManagerLock");
        private IPackageManager underlyingPackageManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsolatedPackageManager"/> class.
        /// </summary>
        /// <param name="packageManager">The package manager to use with isolations in place.</param>
        public IsolatedPackageManager(IPackageManager packageManager)
        {
            packageManager.ThrowIfNull(nameof(packageManager));
            this.underlyingPackageManager = packageManager;
        }

        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics
        {
            get
            {
                return this.underlyingPackageManager.PlatformSpecifics;
            }
        }

        /// <summary>
        /// Performs extensions package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task<PlatformExtensions> DiscoverExtensionsAsync(CancellationToken cancellationToken)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            return Task.Run(() =>
            {
                bool acquired = false;
                try
                {
                    if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                    {
                        acquired = true;
                    }
                }
                catch (AbandonedMutexException)
                {
                    // This happens if the Mutex was not properly signalled on a previous run. For example,
                    // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                    // state.
                }

                PlatformExtensions extensions = null;
                if (acquired)
                {
                    try
                    {
                        extensions = this.underlyingPackageManager.DiscoverExtensionsAsync(cancellationToken)
                            .GetAwaiter().GetResult();
                    }
                    finally
                    {
                        IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                    }
                }

                return extensions;
            });
        }

        /// <summary>
        /// Performs package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(CancellationToken cancellationToken)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            bool acquired = false;
            try
            {
                if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                {
                    acquired = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // This happens if the Mutex was not properly signalled on a previous run. For example,
                // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                // state. We take ownership once again.
                acquired = true;
            }

            IEnumerable<DependencyPath> packages = null;
            if (acquired)
            {
                try
                {
                    packages = Task.Run(() => this.underlyingPackageManager.DiscoverPackagesAsync(cancellationToken))
                        .GetAwaiter().GetResult();
                }
                finally
                {
                    IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                }
            }

            return Task.FromResult(packages);
        }

        /// <summary>
        /// Extracts/unzips the package at the file path provided. This supports standard .zip and .nupkg
        /// file formats.
        /// </summary>
        /// <param name="archiveFilePath">The path to the package zip file.</param>
        /// <param name="destinationPath">The path to the directory where the files should be extracted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        /// <param name="archiveType">The type of archive format the file is in.</param>
        public Task ExtractPackageAsync(string archiveFilePath, string destinationPath, CancellationToken cancellationToken, ArchiveType archiveType = ArchiveType.Zip)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            bool acquired = false;
            try
            {
                if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                {
                    acquired = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // This happens if the Mutex was not properly signalled on a previous run. For example,
                // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                // state. We take ownership once again.
                acquired = true;
            }

            if (acquired)
            {
                try
                {
                    Task.Run(async () => await this.underlyingPackageManager.ExtractPackageAsync(archiveFilePath, destinationPath, cancellationToken, archiveType))
                        .GetAwaiter().GetResult();
                }
                finally
                {
                    IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        /// <param name="packageName">The name of the package dependency.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task<DependencyPath> GetPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            bool acquired = false;
            try
            {
                if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                {
                    acquired = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // This happens if the Mutex was not properly signalled on a previous run. For example,
                // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                // state. We take ownership once again.
                acquired = true;
            }

            DependencyPath package = null;
            if (acquired)
            {
                try
                {
                    package = Task.Run(() => this.underlyingPackageManager.GetPackageAsync(packageName, cancellationToken))
                        .GetAwaiter().GetResult();
                }
                finally
                {
                    IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                }
            }

            return Task.FromResult(package);
        }

        /// <summary>
        /// Performs package initialization on the system including extraction of package archives.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public Task InitializePackagesAsync(CancellationToken cancellationToken)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            bool acquired = false;
            try
            {
                if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                {
                    acquired = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // This happens if the Mutex was not properly signalled on a previous run. For example,
                // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                // state. We take ownership once again.
                acquired = true;
            }

            if (acquired)
            {
                try
                {
                    Task.Run(() => this.underlyingPackageManager.InitializePackagesAsync(cancellationToken))
                        .GetAwaiter().GetResult();
                }
                finally
                {
                    IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Installs the package from the Azure storage account blob store.
        /// </summary>
        /// <param name="packageStoreManager">The blob manager to use for downloading the package to the file system.</param>
        /// <param name="description">Provides information about the target package.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="installationPath">Optional installation path to be used to override the default installation path.</param>
        /// <param name="retryPolicy">A retry policy to apply to the blob download and installation to allow for transient error handling.</param>
        /// <returns>The path where the Blob package was installed.</returns>
        public Task<string> InstallPackageAsync(IBlobManager packageStoreManager, DependencyDescriptor description, CancellationToken cancellationToken, string installationPath = null, IAsyncPolicy retryPolicy = null)
        {
            // Note:
            // In order to use a Mutex (system-wide lock), the logic between the WaitOne() and ReleaseMutex()
            // method calls MUST be on the same thread. We are allowing the logic to all run asynchronous using a
            // Task.Run() while also ensuring the logic within that block is synchronous.
            //
            // Otherwise the following error will happen: "Object synchronization method was called from an unsynchronized block of code"
            bool acquired = false;
            try
            {
                if (IsolatedPackageManager.CrossProcessLock.WaitOne())
                {
                    acquired = true;
                }
            }
            catch (AbandonedMutexException)
            {
                // This happens if the Mutex was not properly signalled on a previous run. For example,
                // the application may have crashed leaving the kernel-layer mutex construct in an abandoned
                // state. We take ownership once again.
                acquired = true;
            }

            string packagePath = null;
            if (acquired)
            {
                try
                {
                    packagePath = Task.Run(() => this.underlyingPackageManager.InstallPackageAsync(packageStoreManager, description, cancellationToken, installationPath, retryPolicy))
                        .GetAwaiter().GetResult();
                }
                finally
                {
                    IsolatedPackageManager.CrossProcessLock.ReleaseMutex();
                }
            }

            return Task.FromResult(packagePath);
        }

        /// <summary>
        /// Registers/saves the path so that it can be used by dependencies, workloads and monitors. Paths registered
        /// follow a strict format
        /// </summary>
        /// <param name="package">Describes a package dependency to register with the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task RegisterPackageAsync(DependencyPath package, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
