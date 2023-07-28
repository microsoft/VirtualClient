// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents information about a particular hardware component on
    /// the system.
    /// </summary>
    public class HardwareInfo : Dictionary<string, IConvertible>
    {
        /// <summary>
        /// Hardware Component Type = Cache
        /// </summary>
        public const string ComponentTypeCache = "Cache";

        /// <summary>
        /// Hardware Component Type = MemoryChip
        /// </summary>
        public const string ComponentTypeMemoryChip = "MemoryChip";

        /// <summary>
        /// Hardware Component Type = NetworkInterface
        /// </summary>
        public const string ComponentNetworkInterface = "NetworkInterface";

        /// <summary>
        /// Initializes a new instance of the <see cref="HardwareInfo"/> class.
        /// </summary>
        protected HardwareInfo(string componentType, string name, string description, IDictionary<string, IConvertible> properties = null)
        {
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            this[nameof(this.ComponentType)] = componentType;
            this[nameof(this.Name)] = name;
            this[nameof(this.Description)] = description;

            if (properties?.Any() == true)
            {
                this.AddRange(properties);
            }
        }

        /// <summary>
        /// The type of hardware component (e.g. CPU cache, memory chip/DIMM).
        /// </summary>
        public string ComponentType
        {
            get
            {
                return this.GetValue<string>(nameof(this.ComponentType));
            }
        }

        /// <summary>
        /// A description for the hardware component.
        /// </summary>
        public string Description
        {
            get
            {
                this.TryGetValue(nameof(this.Description), out IConvertible description);
                return description?.ToString();
            }
        }

        /// <summary>
        /// A name/identifier for the hardware component.
        /// </summary>
        public string Name
        {
            get
            {
                return this.GetValue<string>(nameof(this.Name));
            }
        }
    }
}
