// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents an individual CPU memory cache (e.g. L1, L2, L3).
    /// </summary>
    public class CpuCacheInfo : HardwareInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CpuCacheInfo" /> class.
        /// </summary>
        public CpuCacheInfo(string name, string description, long sizeInBytes, IDictionary<string, IConvertible> properties = null)
            : base(HardwareInfo.ComponentTypeCache, name, description, properties)
        {
            this[nameof(this.SizeInBytes)] = sizeInBytes;
        }

        /// <summary>
        /// The size of the memory cache (in-bytes).
        /// </summary>
        public long SizeInBytes
        {
            get
            {
                return this.GetValue<long>(nameof(this.SizeInBytes), 0);
            }
        }
    }
}
