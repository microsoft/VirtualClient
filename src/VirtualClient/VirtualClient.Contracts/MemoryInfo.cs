// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information on the system memory.
    /// </summary>
    public class MemoryInfo : List<HardwareInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryInfo"/> class.
        /// </summary>
        /// <param name="totalSystemMemoryKb">The total system memory (in kilobytes).</param>
        /// <param name="chips">Physical memory chips on the system.</param>
        public MemoryInfo(long totalSystemMemoryKb, IEnumerable<MemoryChipInfo> chips = null)
            : base()
        {
            totalSystemMemoryKb.ThrowIfInvalid(nameof(totalSystemMemoryKb), kb => kb > 0);
            this.TotalMemory = totalSystemMemoryKb;

            if (chips?.Any() == true)
            {
                this.Chips = new List<MemoryChipInfo>(chips);
            }
        }

        /// <summary>
        /// Memory chips on the system.
        /// </summary>
        public IEnumerable<MemoryChipInfo> Chips { get; }

        /// <summary>
        /// The total system memory (in kilobytes).
        /// </summary>
        public long TotalMemory { get; }
    }
}
