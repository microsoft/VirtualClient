// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information on the system memory.
    /// </summary>
    public class MemoryInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryInfo"/> class.
        /// </summary>
        /// <param name="totalSystemMemoryKb">The total system memory (in kilobytes).</param>
        public MemoryInfo(long totalSystemMemoryKb)
        {
            totalSystemMemoryKb.ThrowIfInvalid(nameof(totalSystemMemoryKb), kb => kb > 0);
            this.TotalMemory = totalSystemMemoryKb;
        }

        /// <summary>
        /// The total system memory (in kilobytes).
        /// </summary>
        public long TotalMemory { get; }
    }
}
