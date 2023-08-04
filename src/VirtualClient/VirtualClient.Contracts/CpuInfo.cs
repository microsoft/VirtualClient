// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
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
        public CpuInfo(string name, string description, int physicalCoreCount, int logicalCoreCount, int socketCount, int numaNodeCount, bool hyperThreadingEnabled)
        {
            name.ThrowIfNull(nameof(name));
            physicalCoreCount.ThrowIfInvalid(nameof(physicalCoreCount), (count) => count > 0);
            logicalCoreCount.ThrowIfInvalid(nameof(logicalCoreCount), (count) => count > 0);
            socketCount.ThrowIfInvalid(nameof(socketCount), (count) => count > 0);
            numaNodeCount.ThrowIfInvalid(nameof(numaNodeCount), (count) => count >= 0);

            this.Name = name;
            this.Description = description;
            this.LogicalCoreCount = logicalCoreCount;
            this.PhysicalCoreCount = physicalCoreCount;
            this.NumaNodeCount = numaNodeCount;
            this.SocketCount = socketCount;
            this.IsHyperthreadingEnabled = hyperThreadingEnabled;
        }

        /// <summary>
        /// A description of the CPU (e.g. Intel64 Family 6 Model 106 Stepping 6, GenuineIntel).
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// True/false whether CPU hyperthreading is enabled on the system.
        /// </summary>
        public bool IsHyperthreadingEnabled { get; }

        /// <summary>
        /// The number of logical cores/vCPUs on the system.
        /// </summary>
        public int LogicalCoreCount { get; }

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
    }
}