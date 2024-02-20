// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// 
    /// </summary>
    public static class DiskExtensions
    {
        /// <summary>
        /// The assembly containing the component base class and types.
        /// </summary>
        private static readonly Assembly DllAssembly = Assembly.GetAssembly(typeof(DiskExtensions));

        /// <summary>
        /// Extension returns the default test path for the disk.
        /// </summary>
        /// <param name="disk">The disk.</param>
        /// <param name="platform">OS Platform</param>
        /// <returns>
        /// A mount point/path that can be created and used to access the disk.
        /// </returns>
        public static string GetPreferredAccessPath(this Disk disk, PlatformID platform)
        {
            string path = string.Empty;
            List<string> forbiddenPaths = new List<string>()
            {
                "/boot/efi"
            };

            if (platform == PlatformID.Unix && disk.IsOperatingSystem())
            {
                // If it is OS disk, test on OS partition
                path = "/";

            }
            else if (platform == PlatformID.Unix && disk.IsOperatingSystem())
            {
                // If it is OS disk, test on OS partition
                path = disk.Volumes.Where(v => v.IsOperatingSystem()).FirstOrDefault().AccessPaths.FirstOrDefault();

            }
            else
            {
                // If it is not OS disk, test on biggest partition that's not in forbidden path.
                IEnumerable<DiskVolume> eligibleVolumes = disk.Volumes.Where(v => !v.AccessPaths.Any(p => forbiddenPaths.Contains(p.ToLower())));

                if (eligibleVolumes.Count() == 0)
                {
                    throw new WorkloadException($"There is no eligible volume in {disk.DevicePath} that can run IO workloads.", ErrorReason.EnvironmentIsInsufficent);
                }

                long biggestSize = eligibleVolumes.Max(v => v.SizeInBytes(platform));
                path = eligibleVolumes.Where(v => v.SizeInBytes(platform) == biggestSize).FirstOrDefault().AccessPaths.FirstOrDefault();
            }

            return path;
        }

        /// <summary>
        /// Extension returns the default mount point/path for the disk.
        /// </summary>
        /// <param name="volume">The disk.</param>
        /// <param name="prefix">Prefix name.</param>
        /// <returns>
        /// A mount point/path that can be created and used to access the disk.
        /// </returns>
        public static string GetDefaultMountPoint(this DiskVolume volume, string prefix = null)
        {
            // Example:
            // /home/azureuser/VirtualClient.1.0.1585.119/linux-x64/vcmnt_dev_sda
            //
            // C:\Users\azureuser\VirtualClient.1.0.1585.119\win-x64\vcmt_c
            // C:\Users\azureuser\VirtualClient.1.0.1585.119\win-x64\vcmt_d

            prefix = string.IsNullOrEmpty(prefix) ? $"vcmnt_{prefix}" : $"vcmnt";
            string relativePath = $"{prefix}_{volume.DevicePath.ToLowerInvariant().Replace("/", "_").Replace(":", string.Empty).Replace("\\", string.Empty)}";

            relativePath = Regex.Replace(relativePath, @"_+", "_");

            string path = Path.Combine(
                Path.GetDirectoryName(DiskExtensions.DllAssembly.Location),
                relativePath);

            return path;
        }

        /// <summary>
        /// Returns if a partition is OS partition.
        /// </summary>
        /// <param name="volume">Specificed volume/partition.</param>
        /// <returns>True/False a volume is operating system.</returns>
        public static bool IsOperatingSystem(this DiskVolume volume)
        {
            bool isWindowsBoot = false;
            if (volume.Properties.TryGetValue(Disk.WindowsDiskProperties.Info, out IConvertible bootDisk))
            {
                isWindowsBoot = string.Equals(bootDisk?.ToString().Trim(), "Boot", StringComparison.OrdinalIgnoreCase);
            }

            bool isLinuxRoot = false;
            isLinuxRoot = volume.AccessPaths.Any(p => p == "/");

            return (isWindowsBoot || isLinuxRoot);
        }

        /// <summary>
        /// If a Disk contains an OS partition.
        /// </summary>
        /// <param name="disk">Specified Disk.</param>
        /// <returns>True/Fals a disk contains an operating system volume.</returns>
        public static bool IsOperatingSystem(this Disk disk)
        {
            return disk.Volumes.Any(v => v.IsOperatingSystem());
        }

        /// <summary>
        /// Return the size for a disk in bytes.
        /// </summary>
        /// <param name="volume">Input disk/volume</param>
        /// <param name="platform">PlatformId is needed to read the correct property.</param>
        /// <returns>Disk size in bytes.</returns>
        public static long SizeInBytes(this DiskVolume volume, PlatformID platform)
        {
            long result = 0;
            if (platform == PlatformID.Win32NT)
            {
                volume.Properties.TryGetValue(Disk.WindowsDiskProperties.Size, out IConvertible windowsSize);
                // Default to 0.
                windowsSize = (windowsSize == null) ? "0" : windowsSize;
                result = Convert.ToInt64(TextParsingExtensions.TranslateByteUnit(windowsSize.ToString()));
            }
            else if (platform == PlatformID.Unix)
            {
                volume.Properties.TryGetValue(Disk.UnixDiskProperties.Size, out IConvertible unixSize);
                // Default to 0.
                unixSize = (unixSize == null) ? "0" : unixSize;
                result = Convert.ToInt64(unixSize);
            }

            return result;
        }

        /// <summary>
        /// Return the size for a disk in bytes.
        /// </summary>
        /// <param name="disk">Input disk/volume</param>
        /// <param name="platform">PlatformId is needed to read the correct property.</param>
        /// <returns>Disk size in bytes.</returns>
        public static long SizeInBytes(this Disk disk, PlatformID platform)
        {
            long result = 0;
            if (platform == PlatformID.Win32NT)
            {
                if (disk.Properties.TryGetValue(Disk.WindowsDiskProperties.Size, out IConvertible windowsSize))
                {
                    result = Convert.ToInt64(TextParsingExtensions.TranslateByteUnit(windowsSize.ToString()));
                }
                else if (disk.Volumes.Any())
                {
                    result = disk.Volumes.Sum(v => v.SizeInBytes(platform));
                }
            }
            else if (platform == PlatformID.Unix)
            {
                if (disk.Properties.TryGetValue(Disk.UnixDiskProperties.Size, out IConvertible unixSize))
                {
                    result = Convert.ToInt64(unixSize);
                }
                else
                {
                    result = disk.Volumes.Sum(v => v.SizeInBytes(platform));
                }               
            }

            return result;
        }

        /// <summary>
        /// Return if any of the disk path is equal to provided path.
        /// </summary>
        /// <param name="disk">Input disk/volume</param>
        /// <param name="path">Path to disk.</param>
        /// <returns>Disk size in bytes.</returns>
        public static bool PathEquals(this Disk disk, string path)
        {
            // trim end slashes, C:\ and C: should match. /dev/sda and /dev/sda/ should match.
            path = path.TrimEnd('/', '\\', ':');
            bool matchDevicePath = string.Equals(disk.DevicePath.TrimEnd('/', '\\', ':'), path, StringComparison.OrdinalIgnoreCase);
            bool volumeMatchDevicePath = false;
            bool volumeMatchAccessPath = false;

            if (disk.Volumes?.Any() == true)
            {
                volumeMatchDevicePath = disk.Volumes.Any(v => string.Equals(v.DevicePath?.TrimEnd('/', '\\', ':'), path, StringComparison.OrdinalIgnoreCase));
                volumeMatchAccessPath = disk.Volumes.Any(v => v.AccessPaths.Any(ap => string.Equals(ap.TrimEnd('/', '\\', ':'), path, StringComparison.OrdinalIgnoreCase)));
            }

            return matchDevicePath || volumeMatchDevicePath || volumeMatchAccessPath;
        }
    }
}
