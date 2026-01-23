// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Linux-specific CPU affinity configuration using numactl.
    /// </summary>
    public class LinuxProcessAffinityConfiguration : ProcessAffinityConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxProcessAffinityConfiguration"/> class.
        /// </summary>
        /// <param name="cores">The list of core indices to bind to.</param>
        public LinuxProcessAffinityConfiguration(IEnumerable<int> cores)
            : base(cores)
        {
        }

        /// <summary>
        /// Gets the numactl core specification string (e.g., "0,1,2" or "0-3").
        /// </summary>
        public string NumactlCoreSpec
        {
            get
            {
                return this.OptimizeCoreListForNumactl();
            }
        }

        /// <summary>
        /// Wraps a command with numactl to apply CPU affinity.
        /// Returns the full bash command string ready for execution.
        /// </summary>
        /// <param name="command">The command to wrap.</param>
        /// <param name="arguments">Optional arguments for the command.</param>
        /// <returns>The complete command string with numactl wrapper (e.g., "bash -c \"numactl -C 0,1 redis-server --port 6379\"").</returns>
        public string GetCommandWithAffinity(string command, string arguments = null)
        {
            return string.IsNullOrEmpty(command) ? $"\"numactl -C {this.NumactlCoreSpec} {arguments}\"" : $"{command} \"numactl -C {this.NumactlCoreSpec} {arguments}\"";
        }

        /// <summary>
        /// Gets a string representation including the numactl specification.
        /// </summary>
        public override string ToString()
        {
            return $"{base.ToString()} (numactl: -C {this.NumactlCoreSpec})";
        }

        /// <summary>
        /// Optimizes the core list for numactl by converting consecutive cores to range notation.
        /// Example: [0, 1, 2, 5, 6, 7, 8] ? "0-2,5-8"
        /// </summary>
        private string OptimizeCoreListForNumactl()
        {
            if (!this.Cores.Any())
            {
                return string.Empty;
            }

            List<int> sortedCores = this.Cores.OrderBy(c => c).ToList();
            List<string> ranges = new List<string>();

            int rangeStart = sortedCores[0];
            int rangeEnd = sortedCores[0];

            for (int i = 1; i < sortedCores.Count; i++)
            {
                if (sortedCores[i] == rangeEnd + 1)
                {
                    // Continue the range
                    rangeEnd = sortedCores[i];
                }
                else
                {
                    // End current range and start a new one
                    ranges.Add(FormatRange(rangeStart, rangeEnd));
                    rangeStart = sortedCores[i];
                    rangeEnd = sortedCores[i];
                }
            }

            // Add the final range
            ranges.Add(FormatRange(rangeStart, rangeEnd));

            return string.Join(",", ranges);
        }

        private static string FormatRange(int start, int end)
        {
            // Use range notation only if there are 3 or more consecutive cores
            // This keeps the output concise: "0-2" instead of "0,1,2"
            // but keeps "0,1" as-is since "0-1" isn't much shorter
            if (end - start >= 2)
            {
                return $"{start}-{end}";
            }
            else if (start == end)
            {
                return start.ToString();
            }
            else
            {
                return $"{start},{end}";
            }
        }
    }
}
