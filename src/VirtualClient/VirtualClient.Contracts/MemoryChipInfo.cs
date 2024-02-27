// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents information about a memory chip/DIMM on the hardware
    /// system.
    /// </summary>
    public class MemoryChipInfo : HardwareInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryChipInfo" /> class.
        /// </summary>
        /// <param name="name">A name/model for the memory chip.</param>
        /// <param name="description">A description of the memory chip.</param>
        /// <param name="capacity">The memory chip size (in-bytes).</param>
        /// <param name="speed">The memory speed (in millions of transactions/sec).</param>
        /// <param name="manufacturer">The manufacturer of the memory chip (e.g. 16KTF1G64HZ-1G9P1).</param>
        /// <param name="partNumber">The part number for the memory chip.</param>
        /// <param name="properties">Additional properties associated with the memory chip.</param>
        public MemoryChipInfo(string name, string description, long capacity, long? speed = null, string manufacturer = null, string partNumber = null, IDictionary<string, IConvertible> properties = null)
            : base(HardwareInfo.ComponentTypeMemoryChip, name, description, properties)
        {
            this[nameof(this.Capacity)] = capacity;
            this[nameof(this.Speed)] = speed;
            this[nameof(this.Manufacturer)] = manufacturer;
            this[nameof(this.PartNumber)] = partNumber;
        }

        /// <summary>
        /// The manufacturer of the memory chip (e.g. Micron, HK Hynix).
        /// </summary>
        public string Manufacturer
        {
            get
            {
                this.TryGetValue(nameof(this.Manufacturer), out IConvertible manufacturer);
                return manufacturer?.ToString();
            }
        }

        /// <summary>
        /// The part number for the memory chip (e.g. 16KTF1G64HZ-1G9P1).
        /// </summary>
        public string PartNumber
        {
            get
            {
                this.TryGetValue(nameof(this.PartNumber), out IConvertible partNumber);
                return partNumber?.ToString();
            }
        }

        /// <summary>
        /// The memory chip capacity (in-bytes).
        /// </summary>
        public long Capacity
        {
            get
            {
                return this.GetValue<long>(nameof(this.Capacity));
            }
        }

        /// <summary>
        /// The memory chip speed (in millions of transactions/sec).
        /// </summary>
        public long? Speed
        {
            get
            {
                this.TryGetValue(nameof(this.Speed), out IConvertible speed);
                return speed?.ToInt64(CultureInfo.CurrentCulture) ?? null;
            }
        }
    }
}
