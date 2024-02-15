// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a physical device (e.g. storage) on the system.
    /// </summary>
    public class Device
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiskVolume"/> class.
        /// </summary>
        [JsonConstructor]
        public Device(int? index, string devicePath = null, IDictionary<string, IConvertible> properties = null)
        {
            this.Index = index;
            this.DevicePath = devicePath;
            this.Properties = new Dictionary<string, IConvertible>();

            if (properties?.Any() == true)
            {
                this.Properties.AddRange(properties);
            }
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
        /// Properties associated with the volume/partition.
        /// </summary>
        [JsonProperty(PropertyName = "properties", Required = Required.Default, Order = 10)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Properties { get; }
    }
}
