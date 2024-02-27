// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System.Collections.Generic;

    /// <summary>
    /// Data contract for PciDevice parsed from lsPci.
    /// </summary>
    public class PciDevice
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 
        /// </summary>
        public List<PciDeviceCapability> Capabilities { get; set; } = new List<PciDeviceCapability> { };

        /// <summary>
        /// Data contract for capabiilities in lspci output.
        /// -------------------------------Example---------------------------------
        /// Capabilities: [80] MSI-X: Enable- Count=1 Masked-
        ///     Vector table: BAR=0 offset=00002000
        ///     PBA: BAR=0 offset=00003000
        /// -------------------------------Example---------------------------------
        /// </summary>
        public class PciDeviceCapability
        {
            /// <summary>
            /// 
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object> { };
        }
    }
}