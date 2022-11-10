// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extensions for <see cref="UnixDiskManager"/> instances.
    /// </summary>
    public static class UnixDiskManagerExtensions
    {
        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="diskManager">Manage disks on a Unix/Linux system.</param>
        /// <param name="disks">This disks for which mount points need to be created.</param>
        /// <param name="systemManager">System manager providing system information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task<bool> CreateMountPointsAsync(this IDiskManager diskManager, IEnumerable<Disk> disks, ISystemManagement systemManager, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                {
                    await Task.Run(async () =>
                    {
                        string newMountPoint = volume.GetDefaultMountPoint();
                        if (!systemManager.FileSystem.Directory.Exists(newMountPoint))
                        {
                            systemManager.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                        }

                        await diskManager.CreateMountPointAsync(volume, newMountPoint, cancellationToken)
                            .ConfigureAwait(false);

                        mountPointsCreated = true;

                    }).ConfigureAwait(false);
                }
            }

            return mountPointsCreated;
        }
    }
}
