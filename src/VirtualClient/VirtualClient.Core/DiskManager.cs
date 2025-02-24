// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Polly;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods to manage disks on the system.
    /// </summary>
    public abstract class DiskManager : IDiskManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiskManager"/> class.
        /// </summary>
        protected DiskManager()
            : this(NullLogger.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskManager"/> class.
        /// </summary>
        protected DiskManager(ILogger logger)
        {
            this.Logger = logger ?? NullLogger.Instance;
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries + 1));
        }

        /// <summary>
        /// The logger for capturing disk management telemetry.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// A retry policy to apply to disk management commands to allow for transient
        /// error handling.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <inheritdoc/>
        public abstract Task CreateMountPointAsync(DiskVolume volume, string mountPoint, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task FormatDiskAsync(Disk disk, PartitionType partitionType, FileSystemType fileSystemType, CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken);

        /// <inheritdoc/>
        public abstract Task<IEnumerable<Disk>> GetDisksAsync(string diskFilter, CancellationToken cancellationToken);
    }
}
