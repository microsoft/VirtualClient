// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Methods for extending the functionality of the 
    /// disk manager class, and related classes.
    /// </summary>
    public static class DiskManagerExtensions
    {
        /// <summary>
        /// Gets a filtered set of physical disks that exist on the system.
        /// </summary>
        /// <param name="diskManager"></param>
        /// <param name="platform"></param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="diskFilter">The filter to apply to the disks.</param>
        public static async Task<IEnumerable<Disk>> GetFilteredDisksAsync(this IDiskManager diskManager, PlatformID platform, string diskFilter, CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await diskManager.GetDisksAsync(cancellationToken)
                .ConfigureAwait(false);

            IEnumerable<Disk> filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, platform).ToList();

            return filteredDisks;
        }
    }
}
