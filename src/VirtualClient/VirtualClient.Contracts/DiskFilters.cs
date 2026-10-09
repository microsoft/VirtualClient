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
        /// Attempts to parse a "DiskIndex:" filter string and extract explicit physical disk indexes.
        /// </summary>
        /// <remarks>
        /// Supported forms:
        /// <list type="bullet">
        ///   <item><c>DiskIndex:6-180</c> — inclusive range, populates <paramref name="indexes"/> with 6..180.</item>
        ///   <item><c>DiskIndex:6,10,15</c> — comma-separated list, populates <paramref name="indexes"/> with {6, 10, 15}.</item>
        ///   <item>
        ///     <c>DiskIndex:hdd</c> — auto-discover sentinel; returns <c>true</c> with <paramref name="indexes"/> set to
        ///     <c>null</c>, signalling the caller to perform OS-based HDD discovery (e.g. <c>Get-PhysicalDisk</c> on Windows).
        ///   </item>
        /// </list>
        /// </remarks>
        /// <param name="diskFilter">The disk filter string (e.g. the value of the <c>DiskFilter</c> parameter).</param>
        /// <param name="indexes">
        ///   The parsed disk indexes when an explicit range or list is provided; <c>null</c> when the value is "hdd"
        ///   (indicating OS-based auto-discovery should be used instead).
        /// </param>
        /// <returns>
        ///   <c>true</c> if <paramref name="diskFilter"/> begins with "DiskIndex:" (whether explicit or the "hdd" sentinel);
        ///   <c>false</c> if the filter is not a DiskIndex filter at all.
        /// </returns>
        public static bool TryGetDiskIndexes(string diskFilter, out IEnumerable<int> indexes)
        {
            indexes = null;

            if (string.IsNullOrWhiteSpace(diskFilter))
            {
                return false;
            }

            const string prefix = "DiskIndex:";

            if (!diskFilter.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string value = diskFilter.Substring(prefix.Length).Trim();

            // "DiskIndex:hdd" is a sentinel meaning "discover HDD disks at runtime via Get-PhysicalDisk".
            if (value.Equals("hdd", StringComparison.OrdinalIgnoreCase))
            {
                // indexes remains null — caller should perform OS-based auto-discovery.
                return true;
            }

            List<int> parsed = new List<int>();

            if (value.Contains('-'))
            {
                string[] parts = value.Split('-', 2);
                if (int.TryParse(parts[0].Trim(), out int start) && int.TryParse(parts[1].Trim(), out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        parsed.Add(i);
                    }
                }
            }
            else
            {
                foreach (string token in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(token.Trim(), out int idx))
                    {
                        parsed.Add(idx);
                    }
                }
            }

            indexes = parsed;
            return true;
        }

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

            // Allow callers to opt into keeping offline disks (e.g. bare/unformatted disks for raw disk I/O).
            bool includeOffline = filters.Any(f => f.Trim().Equals(Filters.IncludeOffline, StringComparison.OrdinalIgnoreCase));

            IEnumerable<Disk> filteredDisks = disks;
            filteredDisks = DiskFilters.FilterByStoragePathPrefix(filteredDisks, platform);

            if (!includeOffline)
            {
                filteredDisks = DiskFilters.FilterOutOfflineDisks(filteredDisks, platform);
            }

            filteredDisks = DiskFilters.FilterOutReadOnlyDisks(filteredDisks, platform);

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
                        filteredDisks = DiskFilters.FilterByBiggestSize(filteredDisks, platform);
                        break;

                    case Filters.SmallestSize:
                        filteredDisks = DiskFilters.FilterBySmallestSize(filteredDisks, platform);
                        break;

                    case Filters.SizeGreaterThan:
                        filteredDisks = DiskFilters.FilterBySizeGreaterThan(filteredDisks, platform, DiskFilters.ParseDiskSize(filterName, filterValue));
                        break;

                    case Filters.SizeLessThan:
                        filteredDisks = DiskFilters.FilterBySizeLessThan(filteredDisks, platform, DiskFilters.ParseDiskSize(filterName, filterValue));
                        break;

                    case Filters.SizeEqualTo:
                        filteredDisks = DiskFilters.FilterBySizeEqualTo(filteredDisks, platform, DiskFilters.ParseDiskSize(filterName, filterValue));
                        break;

                    case Filters.OsDisk:
                        // If OsDisk is specified, default to true.
                        bool includeOs = string.IsNullOrWhiteSpace(filterValue) ? true : Convert.ToBoolean(filterValue);
                        filteredDisks = DiskFilters.FilterByOperatingSystemDisk(filteredDisks, includeOs);
                        break;

                    case Filters.DiskPath:
                        // Disk Path can be multiple delimited by comma
                        // C:,D:,
                        // /dev/sda, /dev/sdb
                        filteredDisks = DiskFilters.FilterByDiskPath(filteredDisks, filterValue);
                        break;

                    case Filters.IncludeOffline:
                        // Already handled before the filter loop; treated as a no-op here.
                        break;

                    case Filters.AccessPath:
                        filteredDisks = DiskFilters.FilterByAccessPath(filteredDisks, filterValue);
                        break;

                    case Filters.Logical:
                        filteredDisks = DiskFilters.FilterByLogicalDisk(filteredDisks);
                        break;

                    default:
                        throw new EnvironmentSetupException($"Disk filter '{filter}' is not supported.", ErrorReason.DiskInformationNotAvailable);
                }
            }

            return filteredDisks;
        }

        private static decimal ParseDiskSize(string filterName, string filterValue)
        {
            // Sizes are kept as decimal because values like 3.7TB do not land on a whole number of bytes.
            if (!TextParsingExtensions.TryTranslateByteUnit(filterValue, out decimal sizeInBytes) || sizeInBytes < 0)
            {
                throw new EnvironmentSetupException(
                    $"Invalid disk filter. The value '{filterValue}' supplied for the '{filterName}' disk filter is not a valid disk size. " +
                    $"Supply a size in bytes or a size with a unit (e.g. 1024, 100KB, 1.5GB, 3.7TB).",
                    ErrorReason.DiskInformationNotAvailable);
            }

            return sizeInBytes;
        }

        private static IEnumerable<Disk> FilterByBiggestSize(IEnumerable<Disk> disks, PlatformID platform)
        {
            long biggestSize = disks.Max(d => d.SizeInBytes(platform));
            return disks.Where(d => d.SizeInBytes(platform) == biggestSize);
        }

        private static IEnumerable<Disk> FilterBySmallestSize(IEnumerable<Disk> disks, PlatformID platform)
        {
            // 0 could mean not partitioned and is not considered a valid size.
            long smallestSize = disks.Where(d => d.SizeInBytes(platform) != 0).Min(d => d.SizeInBytes(platform));
            return disks.Where(d => d.SizeInBytes(platform) == smallestSize);
        }

        private static IEnumerable<Disk> FilterBySizeGreaterThan(IEnumerable<Disk> disks, PlatformID platform, decimal size)
        {
            return disks.Where(d => d.SizeInBytes(platform) >= size);
        }

        private static IEnumerable<Disk> FilterBySizeEqualTo(IEnumerable<Disk> disks, PlatformID platform, decimal size)
        {
            // Due to disks are not always sized exactly as defined, due to reserved partitions and disk headers, etc.
            // We are leaving a 1% buffer.
            return disks.Where(d => d.SizeInBytes(platform) >= size * 0.99m && d.SizeInBytes(platform) <= size * 1.01m);
        }

        private static IEnumerable<Disk> FilterBySizeLessThan(IEnumerable<Disk> disks, PlatformID platform, decimal size)
        {
            return disks.Where(d => d.SizeInBytes(platform) <= size);
        }

        private static IEnumerable<Disk> FilterByOperatingSystemDisk(IEnumerable<Disk> disks, bool includeOs)
        {
            return disks.Where(d => d.IsOperatingSystem() == includeOs);
        }

        private static IEnumerable<Disk> FilterByDiskPath(IEnumerable<Disk> disks, string diskPaths)
        {
            List<string> pathList = diskPaths.Split(",", StringSplitOptions.RemoveEmptyEntries).ToList();

            // Find Disks where either devicepath or accessPath is exact match of one of the path provided in diskPaths.
            return disks.Where(d => pathList.Any(p => d.PathEquals(p)));
        }

        private static IEnumerable<Disk> FilterByAccessPath(IEnumerable<Disk> disks, string accessPathPattern)
        {
            // Find disks where any volume has an access path containing the given pattern.
            return disks.Where(d => d.Volumes.Any(v => v.AccessPaths.Any(p => p.Contains(accessPathPattern, StringComparison.OrdinalIgnoreCase))));
        }

        private static IEnumerable<Disk> FilterByLogicalDisk(IEnumerable<Disk> disks)
        {
            // LVM device mapper paths: /dev/dm-N or /dev/mapper/*
            return disks.Where(d =>
                d.DevicePath?.StartsWith("/dev/dm", StringComparison.OrdinalIgnoreCase) == true
                || d.DevicePath?.StartsWith("/dev/mapper", StringComparison.OrdinalIgnoreCase) == true);
        }

        private static IEnumerable<Disk> FilterByStoragePathPrefix(IEnumerable<Disk> disks, PlatformID platform)
        {
            var filteredDisks = disks;
            if (platform == PlatformID.Unix)
            {
                // There are NVMe disks that show up in lshw output, that are not really storage devices. This filter filters by common prefixes.
                List<string> validPrefixes = new List<string> { "/dev/hd", "/dev/sd", "/dev/nvme", "/dev/xvd", "/dev/dm", "/dev/mapper" };

                // Match for either accessPath or devicePath.
                filteredDisks = disks.Where(d => validPrefixes.Any(vp => d.DevicePath?.Trim().StartsWith(vp, StringComparison.OrdinalIgnoreCase) == true));
            }

            return filteredDisks;
        }

        private static IEnumerable<Disk> FilterOutOfflineDisks(IEnumerable<Disk> disks, PlatformID platform)
        {
            var filteredDisks = disks;
            if (platform == PlatformID.Win32NT)
            {
                // Remove offline disks.
                filteredDisks = disks.Where(d => d.Properties.ContainsKey("Status") ? !d.Properties.GetValue<string>("Status").Contains("offline", StringComparison.OrdinalIgnoreCase) : true);
            }

            return filteredDisks;
        }

        private static IEnumerable<Disk> FilterOutReadOnlyDisks(IEnumerable<Disk> disks, PlatformID platform)
        {
            var filteredDisks = disks;
            if (platform == PlatformID.Win32NT)
            {
                // Remove read only disks.
                filteredDisks = disks.Where(d => d.Properties.ContainsKey("Read-only") ? !d.Properties.GetValue<string>("Read-only").Contains("Yes", StringComparison.OrdinalIgnoreCase) : true);
                filteredDisks = filteredDisks?.Where(d => d.Properties.ContainsKey("Current Read-only State") ? !d.Properties.GetValue<string>("Current Read-only State").Contains("Yes", StringComparison.OrdinalIgnoreCase) : true);
            }

            return filteredDisks;
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

            /// <summary>
            /// Include offline disks filter. By default offline disks are excluded on Windows.
            /// Use this filter to include them (e.g. bare/unformatted disks for raw disk I/O).
            /// </summary>
            public const string IncludeOffline = "includeoffline";

            /// <summary>
            /// Access path filter. Matches disks with a volume access path containing the given value.
            /// </summary>
            public const string AccessPath = "accesspath";

            /// <summary>
            /// Logical disk filter. Matches LVM device mapper disks (/dev/dm-*, /dev/mapper/*).
            /// </summary>
            public const string Logical = "logical";
        }
    }
}
