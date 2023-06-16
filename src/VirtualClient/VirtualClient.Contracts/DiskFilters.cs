// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Disk filters to filter Disk/DiskVolume
    /// </summary>
    public static class DiskFilters
    {
        /// <summary>
        /// Default disk filter that gets the biggest non-OS disks
        /// </summary>
        public const string DefaultDiskFilter = "BiggestSize";

        /// <summary>
        /// Filters the disks based on filter strings
        /// </summary>
        /// <param name="disks">Input disks</param>
        /// <param name="filterString">Filter query string</param>
        /// <param name="platform">Platform id: linux/windows</param>
        /// <returns>Filtered disks</returns>
        public static IEnumerable<Disk> FilterDisks(IEnumerable<Disk> disks, string filterString, PlatformID platform)
        {
            // Filters look like:
            // filterName1:value1&filterName2:value2&filterDoesNotRequireValue&filter4:value4
            List<string> filters = filterString.Split("&", StringSplitOptions.RemoveEmptyEntries).ToList();

            disks = DiskFilters.FilterStoragePathByPrefix(disks, platform);
            disks = DiskFilters.FilterOfflineDisksOnWindows(disks, platform);
            disks = DiskFilters.FilterReadOnlyDisksOnWindows(disks, platform);

            foreach (string filter in filters)
            {
                string filterName = filter.Trim();
                string filterValue = string.Empty;

                if (filter.Contains(":"))
                {
                    // Split on the first column, because disk path could contain column in windows.
                    int columnIndex = filter.IndexOf(':');
                    filterName = filter.Substring(0, columnIndex).Trim();
                    filterValue = filter.Substring(columnIndex + 1, filter.Length - columnIndex - 1).Trim();
                }

                switch (filterName.ToLowerInvariant())
                {
                    case Filters.None:
                        break;

                    case Filters.BiggestSize:
                        disks = DiskFilters.BiggestSizeFilter(disks, platform);
                        break;

                    case Filters.SmallestSize:
                        disks = DiskFilters.SmallestSizeFilter(disks, platform);
                        break;

                    case Filters.SizeGreaterThan:
                        disks = DiskFilters.SizeGreaterThanFilter(disks, platform, Convert.ToInt64(TextParsingExtensions.TranslateByteUnit(filterValue)));
                        break;

                    case Filters.SizeLessThan:
                        disks = DiskFilters.SizeLessThanFilter(disks, platform, Convert.ToInt64(TextParsingExtensions.TranslateByteUnit(filterValue)));
                        break;

                    case Filters.SizeEqualTo:
                        disks = DiskFilters.SizeEqualToFilter(disks, platform, Convert.ToInt64(TextParsingExtensions.TranslateByteUnit(filterValue)));
                        break;

                    case Filters.OsDisk:
                        // If OsDisk is specified, default to true.
                        bool includeOs = string.IsNullOrWhiteSpace(filterValue) ? true : Convert.ToBoolean(filterValue);
                        disks = DiskFilters.OsDiskFilter(disks, includeOs);
                        break;

                    case Filters.DiskPath:
                        // Disk Path can be multiple delimited by comma
                        // C:,D:,
                        // /dev/sda, /dev/sdb
                        disks = DiskFilters.DiskPathFilter(disks, filterValue);
                        break;

                    default:
                        throw new EnvironmentSetupException($"Disk filter '{filter}' is not supported.", ErrorReason.DiskInformationNotAvailable);
                }
            }

            return disks;
        }

        private static IEnumerable<Disk> BiggestSizeFilter(IEnumerable<Disk> disks, PlatformID platform)
        {
            long biggestSize = disks.Max(d => d.SizeInBytes(platform));
            disks = disks.Where(d => d.SizeInBytes(platform) == biggestSize);
            return disks;
        }

        private static IEnumerable<Disk> SmallestSizeFilter(IEnumerable<Disk> disks, PlatformID platform)
        {
            // 0 could mean not partitioned and is not considered a valid size.
            long smallestSize = disks.Where(d => d.SizeInBytes(platform) != 0).Min(d => d.SizeInBytes(platform));
            disks = disks.Where(d => d.SizeInBytes(platform) == smallestSize);
            return disks;
        }

        private static IEnumerable<Disk> SizeGreaterThanFilter(IEnumerable<Disk> disks, PlatformID platform, long size)
        {
            disks = disks.Where(d => d.SizeInBytes(platform) >= size);
            return disks;
        }

        private static IEnumerable<Disk> SizeEqualToFilter(IEnumerable<Disk> disks, PlatformID platform, long size)
        {
            // Due to disks are not always sized exactly as defined, due to reserved partitions and disk headers, etc.
            // We are leaving a 1% buffer.
            disks = disks.Where(d => d.SizeInBytes(platform) >= size * 0.99 && d.SizeInBytes(platform) <= size * 1.01);
            return disks;
        }

        private static IEnumerable<Disk> SizeLessThanFilter(IEnumerable<Disk> disks, PlatformID platform, long size)
        {
            disks = disks.Where(d => d.SizeInBytes(platform) <= size);
            return disks;
        }

        private static IEnumerable<Disk> OsDiskFilter(IEnumerable<Disk> disks, bool includeOs)
        {
            disks = disks.Where(d => d.IsOperatingSystem() == includeOs);
            return disks;
        }

        private static IEnumerable<Disk> DiskPathFilter(IEnumerable<Disk> disks, string diskPaths)
        {
            List<string> pathList = diskPaths.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

            // Find Disks where either devicepath or accessPath is exact match of one of the path provided in diskPaths.
            disks = disks.Where(d => pathList.Any(p => d.PathEquals(p)));
            return disks;
        }

        private static IEnumerable<Disk> FilterStoragePathByPrefix(IEnumerable<Disk> disks, PlatformID platform)
        {
            if (platform == PlatformID.Unix)
            {
                // There are NVMe disks that show up in lshw output, that are not really storage devices. This filter filters by common prefixes.
                List<string> validPrefixes = new List<string> { "/dev/hd", "/dev/sd", "/dev/nvme", "/dev/xvd" };

                // Match for either accessPath or devicePath.
                disks = disks.Where(d => validPrefixes.Any(vp => d.DevicePath?.Trim().StartsWith(vp, StringComparison.OrdinalIgnoreCase) == true));
            }

            return disks;
        }

        private static IEnumerable<Disk> FilterOfflineDisksOnWindows(IEnumerable<Disk> disks, PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                // Remove offline disks.
                disks = disks.Where(d => d.Properties.ContainsKey("Status") ? !d.Properties.GetValue<string>("Status").Contains("offline", StringComparison.OrdinalIgnoreCase) : true);
            }

            return disks;
        }

        private static IEnumerable<Disk> FilterReadOnlyDisksOnWindows(IEnumerable<Disk> disks, PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                // Remove read only disks.
                disks = disks.Where(d => d.Properties.ContainsKey("Read-only") ? !d.Properties.GetValue<string>("Read-only").Contains("Yes", StringComparison.OrdinalIgnoreCase) : true);
                disks = disks.Where(d => d.Properties.ContainsKey("Current Read-only State") ? !d.Properties.GetValue<string>("Current Read-only State").Contains("Yes", StringComparison.OrdinalIgnoreCase) : true);
            }

            return disks;
        }

        private static IEnumerable<Disk> RemoveCdromFilter(IEnumerable<Disk> disks, PlatformID platform)
        {
            if (platform == PlatformID.Unix)
            {
                // This is an implicit filter that VC removes CD ROM disks that show up in lshw outputs.
                // This method is not used because there is FilterStoragePathByPrefix that supersedes this. This method might still be applicable if the 
                // FilterStoragePathByPrefix methods is changed to accept broader prefixes.

                List<string> cdromPaths = new List<string>
                {
                    "/dev/cdrom",
                    "/dev/dvd",
                    "/dev/sr0",
                    "/dev/cdrw",
                    "/dev/dvdrw"
                };

                disks = disks.Where(d => !cdromPaths.Any(cd => cd.Equals(d.DevicePath, StringComparison.OrdinalIgnoreCase)));
            }

            return disks;
        }

        /// <summary>
        /// String const for supported filters
        /// </summary>
        private static class Filters
        {
            /// <summary>
            /// None filter, does not filter anything.
            /// </summary>
            public const string None = "none";

            /// <summary>
            /// Biggest size disk filter.
            /// </summary>
            public const string BiggestSize = "biggestsize";

            /// <summary>
            /// Smallest size disk filter
            /// </summary>
            public const string SmallestSize = "smallestsize";

            /// <summary>
            /// OS disk filter
            /// </summary>
            public const string OsDisk = "osdisk";

            /// <summary>
            /// Size greater than filter.
            /// </summary>
            public const string SizeGreaterThan = "sizegreaterthan";

            /// <summary>
            /// Size less than filter
            /// </summary>
            public const string SizeLessThan = "sizelessthan";

            /// <summary>
            /// Size equal to filter
            /// </summary>
            public const string SizeEqualTo = "sizeequalto";

            /// <summary>
            /// Disk path filter.
            /// </summary>
            public const string DiskPath = "diskpath";
        }
    }
}
