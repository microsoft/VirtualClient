// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// A base disk class that could contain partitions..
    /// </summary>
    public class Disk : Device
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Disk"/> class.
        /// </summary>
        /// <param name="index">The index of the physical disk on the system bus.</param>
        /// <param name="devicePath">Physical device path. Typically look like /dev/sdx, /dev/nvme in Linux or PHYSICAL0 in Windows.</param>
        /// <param name="volumes">Volumes that exist on the disk.</param>
        /// <param name="properties">Properties associated with the disk.</param>
        [JsonConstructor]
        public Disk(
            int? index,
            string devicePath = null,
            IEnumerable<DiskVolume> volumes = null,
            IDictionary<string, IConvertible> properties = null)
            : base(index, devicePath, properties)
        {
            this.Volumes = volumes ?? new List<DiskVolume>();
        }

        /// <summary>
        /// The partitions that exist on the physical disk.
        /// </summary>
        [JsonProperty(PropertyName = "volumes", Required = Required.Default, Order = 20)]
        public IEnumerable<DiskVolume> Volumes { get; }

        /// <summary>
        /// Common Disk properties in Lshw output.
        /// </summary>
        public static class UnixDiskProperties
        {
            /// <summary>
            /// businfo
            /// </summary>
            public const string BusInfo = "businfo";

            /// <summary>
            /// Capacity
            /// </summary>
            public const string Capacity = "capacity";

            /// <summary>
            /// Capabilities section
            /// </summary>
            public const string Capabilities = "capabilities";

            /// <summary>
            /// Claimed
            /// </summary>
            public const string Claimed = "claimed";

            /// <summary>
            /// Class
            /// </summary>
            public const string Class = "class";

            /// <summary>
            /// Device
            /// </summary>
            public const string Device = "dev";

            /// <summary>
            /// Description
            /// </summary>
            public const string Description = "description";

            /// <summary>
            /// FileSystem.
            /// </summary>
            public const string FileSystem = "filesystem";

            /// <summary>
            /// Handle
            /// </summary>
            public const string Handle = "handle";

            /// <summary>
            /// Id
            /// </summary>
            public const string Id = "id";

            /// <summary>
            /// LogicalName
            /// </summary>
            public const string LogicalName = "logicalname";

            /// <summary>
            /// PhysicalId
            /// </summary>
            public const string PhysicalId = "physid";

            /// <summary>
            /// Product
            /// </summary>
            public const string Product = "product";

            /// <summary>
            /// Serial
            /// </summary>
            public const string Serial = "serial";

            /// <summary>
            /// Size in bytes.
            /// </summary>
            public const string Size = "size";

            /// <summary>
            /// Vendor
            /// </summary>
            public const string Vendor = "vendor";

            /// <summary>
            /// Version
            /// </summary>
            public const string Version = "version";
        }

        /// <summary>
        /// Common Disk properties in Lshw output.
        /// </summary>
        public static class WindowsDiskProperties
        {
            // These are all disk or volume properties referenced at multiple locations
            // in this disk manager.

            /// <summary>
            /// BootDisk
            /// </summary>
            public const string BootDisk = "Boot Disk";

            /// <summary>
            /// FileSystem
            /// </summary>
            public const string FileSystem = "Fs";

            /// <summary>
            /// Info
            /// </summary>
            public const string Info = "Info";

            /// <summary>
            /// Index
            /// </summary>
            public const string Index = "Index";

            /// <summary>
            /// Size
            /// </summary>
            public const string Size = "Size";

            /// <summary>
            /// Letter
            /// </summary>
            public const string Letter = "Ltr";

            /// <summary>
            /// LogicalUnitId
            /// </summary>
            public const string LogicalUnitId = "LUN ID";

            /// <summary>
            /// Model
            /// </summary>
            public const string Model = "Model";

            /// <summary>
            /// PartitionIndex
            /// </summary>
            public const string PartitionIndex = "PartitionIndex";
        }
    }
}
