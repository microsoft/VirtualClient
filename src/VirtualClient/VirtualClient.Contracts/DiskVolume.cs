// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a single disk volume.
    /// </summary>
    public class DiskVolume : Device
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
            : base(index, devicePath, properties)
        {
            this.AccessPaths = accessPaths ?? new List<string>();
        }

        /// <summary>
        /// Gets the set of access paths (if any) associated with the volume/device. These paths 
        /// can be used to access the file system on the disk.
        /// </summary>
        [JsonProperty(PropertyName = "accessPaths", Required = Required.Default, Order = 5)]
        public IEnumerable<string> AccessPaths { get; set; }
    }
}
