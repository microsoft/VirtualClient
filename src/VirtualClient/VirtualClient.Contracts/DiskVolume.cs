// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;

    /// <summary>
    /// Represents a single disk volume.
    /// </summary>
    public class DiskVolume
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiskVolume"/> class.
        /// </summary>
        [JsonConstructor]
        public DiskVolume(
            int? index,
            string devicePath = null,
            IEnumerable<string> accessPaths = null,
            IDictionary<string, IConvertible> properties = null)
        {
            this.Index = index;
            this.DevicePath = devicePath;
            this.Properties = properties ?? new Dictionary<string, IConvertible>();
            this.AccessPaths = accessPaths ?? new List<string>();
        }

        /// <summary>
        /// The disk index on the bus.
        /// </summary>
        [JsonProperty(PropertyName = "index", Required = Required.AllowNull, Order = 0)]
        public int? Index { get; }

        /// <summary>
        /// The path to the disk device.
        /// </summary>
        [JsonProperty(PropertyName = "devicePath", Required = Required.AllowNull, Order = 2)]
        public string DevicePath { get; set; }

        /// <summary>
        /// Gets the set of access paths (if any) associated with the volume/device. These paths 
        /// can be used to access the file system on the disk.
        /// </summary>
        [JsonProperty(PropertyName = "accessPaths", Required = Required.Default, Order = 3)]
        public IEnumerable<string> AccessPaths { get; set; }

        /// <summary>
        /// Properties associated with the volume/partition.
        /// </summary>
        [JsonProperty(PropertyName = "properties", Required = Required.Default, Order = 101)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Properties { get; }
    }
}
