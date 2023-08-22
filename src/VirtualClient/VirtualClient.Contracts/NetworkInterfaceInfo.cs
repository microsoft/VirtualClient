// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an individual network interface (controller, card etc...).
    /// </summary>
    public class NetworkInterfaceInfo : HardwareInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CpuCacheInfo" /> class.
        /// </summary>
        public NetworkInterfaceInfo(string name, string description, IDictionary<string, IConvertible> properties = null)
            : base(HardwareInfo.ComponentNetworkInterface, name, description, properties)
        {
        }
    }
}
