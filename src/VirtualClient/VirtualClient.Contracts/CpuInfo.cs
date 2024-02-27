// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information about the CPU(s) on the system.
    /// </summary>
    public class CpuInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CpuInfo"/> class.
        /// </summary>
        /// <param name="name">The name of the CPU (e.g. Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz, Ampere(R) Altra(R) Processor).</param>
        /// <param name="description">A description of the CPU (e.g. Intel64 Family 6 Model 106 Stepping 6, GenuineIntel).</param>
        /// <param name="physicalCoreCount">The number of physical cores/CPUs on the system (or allocated to it).</param>
        /// <param name="logicalCoreCount">The number of logical cores/vCPUs on the system.</param>
        /// <param name="socketCount">The number of CPU sockets on the system.</param>
        /// <param name="numaNodeCount">The number of NUMA nodes on the system.</param>
        /// <param name="hyperThreadingEnabled">True/false whether CPU hyperthreading is enabled on the system.</param>
        /// <param name="caches">Memory caches for the CPU (e.g. L1, L2, L3).</param>
        /// <param name="flags">List of other information about CPU</param>
        /// <param name="maxFrequency">Maximum CPU frequency in MHz.</param>
        /// <param name="minFrequency">Minimum CPU frequency in MHz.</param>
        /// <param name="frequency">Currrent frequency of CPU in MHz.</param>
        public CpuInfo(string name, string description, int physicalCoreCount, int logicalCoreCount, int socketCount, int numaNodeCount, bool hyperThreadingEnabled, IEnumerable<CpuCacheInfo> caches = null,  Dictionary<string, string> flags = null, double maxFrequency = double.NaN, double minFrequency = double.NaN, double frequency = double.NaN)
            : base()
        {
            name.ThrowIfNull(nameof(name));
            physicalCoreCount.ThrowIfInvalid(nameof(physicalCoreCount), (count) => count > 0);
            logicalCoreCount.ThrowIfInvalid(nameof(logicalCoreCount), (count) => count > 0);
            socketCount.ThrowIfInvalid(nameof(socketCount), (count) => count > 0);
            numaNodeCount.ThrowIfInvalid(nameof(numaNodeCount), (count) => count >= 0);

            this.Name = name;
            this.Description = description;
            this.LogicalProcessorCount = logicalCoreCount;
            this.PhysicalCoreCount = physicalCoreCount;
            this.NumaNodeCount = numaNodeCount;
            this.SocketCount = socketCount;
            this.IsHyperthreadingEnabled = hyperThreadingEnabled;

            if (caches?.Any() == true)
            {
                this.Caches = new List<CpuCacheInfo>(caches);
            }

            if (flags != null)
            {
                this.Flags = flags;
            }

            if (!double.IsNaN(maxFrequency))
            {
                this.MaxFrequencyMHz = maxFrequency;
            }

            if (!double.IsNaN(minFrequency))
            {
                this.MinFrequencyMHz = minFrequency;
            }

            if (!double.IsNaN(frequency))
            {
                this.FrequencyMHz = frequency;
            }
        }

        /// <summary>
        /// A description of the CPU (e.g. Intel64 Family 6 Model 106 Stepping 6, GenuineIntel).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The set of memory cache components associated with the CPU (e.g. L1, L2, L3).
        /// </summary>
        public IEnumerable<CpuCacheInfo> Caches { get; }

        /// <summary>
        /// True/false whether CPU hyperthreading is enabled on the system.
        /// </summary>
        public bool IsHyperthreadingEnabled { get; }

        /// <summary>
        /// The number of logical processors/vCPUs on the system.
        /// </summary>
        public int LogicalProcessorCount { get; }

        /// <summary>
        /// The number of logical processors/vCPUs per physical core/CPU.
        /// </summary>
        public int LogicalProcessorCountPerPhysicalCore
        {
            get
            {
                int processorsPerCore = this.LogicalProcessorCount / this.PhysicalCoreCount;
                return processorsPerCore <= 0 ? 1 : processorsPerCore;
            }
        }

        /// <summary>
        /// The name of the CPU (e.g. Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz, Ampere(R) Altra(R) Processor).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The number of NUMA nodes on the system.
        /// </summary>
        public int NumaNodeCount { get; }

        /// <summary>
        /// The number of physical cores/CPUs on the system (or allocated to it).
        /// </summary>
        public int PhysicalCoreCount { get; }

        /// <summary>
        /// The number of CPU sockets on the system.
        /// </summary>
        public int SocketCount { get; }

        /// <summary>
        /// Dictionary of other information about CPU.
        /// </summary>
        public Dictionary<string, string> Flags { get; }

        /// <summary>
        /// Maximum CPU frequency in MHz.
        /// </summary>
        public double MaxFrequencyMHz { get; set; } = double.NaN;

        /// <summary>
        /// Minimum CPU frequency in MHz.
        /// </summary>
        public double MinFrequencyMHz { get; set; } = double.NaN;

        /// <summary>
        /// Currrent frequency of CPU in MHz.
        /// </summary>
        public double FrequencyMHz { get; set; } = double.NaN;
    }
}