// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Base class for platform-specific CPU affinity configuration.
    /// Provides abstraction for binding processes to specific CPU cores on different platforms.
    /// </summary>
    public abstract class ProcessAffinityConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessAffinityConfiguration"/> class.
        /// </summary>
        /// <param name="cores">The list of core indices to bind to (e.g., [0, 1, 2]).</param>
        protected ProcessAffinityConfiguration(IEnumerable<int> cores)
        {
            cores.ThrowIfNull(nameof(cores));
            if (!cores.Any())
            {
                throw new ArgumentException("At least one core must be specified.", nameof(cores));
            }

            // Remove duplicates and sort cores for consistency
            this.Cores = cores.Distinct().OrderBy(c => c).ToList().AsReadOnly();
        }

        /// <summary>
        /// Gets the list of core indices to bind to.
        /// </summary>
        public IReadOnlyList<int> Cores { get; }

        /// <summary>
        /// Creates a platform-specific <see cref="ProcessAffinityConfiguration"/> instance.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="cores">The list of core indices to bind to.</param>
        /// <returns>A platform-specific affinity configuration instance.</returns>
        public static ProcessAffinityConfiguration Create(PlatformID platform, IEnumerable<int> cores)
        {
            cores.ThrowIfNullOrEmpty(nameof(cores));

            return platform switch
            {
                PlatformID.Unix => new LinuxProcessAffinityConfiguration(cores),
                _ => throw new NotSupportedException($"CPU affinity configuration is not supported on platform '{platform}'.")
            };
        }

        /// <summary>
        /// Creates a platform-specific <see cref="ProcessAffinityConfiguration"/> instance from a core list string.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="coreList">A comma-separated list of core indices (e.g., "0,1,2,3" or "0-3").</param>
        /// <returns>A platform-specific affinity configuration instance.</returns>
        public static ProcessAffinityConfiguration Create(PlatformID platform, string coreList)
        {
            coreList.ThrowIfNullOrWhiteSpace(nameof(coreList));
            IEnumerable<int> cores = ParseCoreList(coreList);
            return Create(platform, cores);
        }

        /// <summary>
        /// Parses a core list string into a collection of core indices.
        /// Supports comma-separated values (e.g., "0,1,2") and ranges (e.g., "0-3").
        /// </summary>
        /// <param name="coreList">The core list string to parse.</param>
        /// <returns>A collection of core indices.</returns>
        public static IEnumerable<int> ParseCoreList(string coreList)
        {
            coreList.ThrowIfNullOrWhiteSpace(nameof(coreList));

            List<int> cores = new List<int>();
            string[] parts = coreList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                string trimmed = part.Trim();

                // Handle range notation (e.g., "0-3")
                if (trimmed.Contains('-'))
                {
                    string[] range = trimmed.Split('-');
                    if (range.Length != 2)
                    {
                        throw new ArgumentException($"Invalid core range format: '{trimmed}'. Expected format: 'start-end' (e.g., '0-3').", nameof(coreList));
                    }

                    if (!int.TryParse(range[0].Trim(), out int start) || !int.TryParse(range[1].Trim(), out int end))
                    {
                        throw new ArgumentException($"Invalid core range values: '{trimmed}'. Both start and end must be valid integers.", nameof(coreList));
                    }

                    if (start > end)
                    {
                        throw new ArgumentException($"Invalid core range: '{trimmed}'. Start value cannot be greater than end value.", nameof(coreList));
                    }

                    if (start < 0 || end < 0)
                    {
                        throw new ArgumentException($"Invalid core range: '{trimmed}'. Core indices cannot be negative.", nameof(coreList));
                    }

                    for (int i = start; i <= end; i++)
                    {
                        cores.Add(i);
                    }
                }
                else
                {
                    // Handle individual core index
                    if (!int.TryParse(trimmed, out int core))
                    {
                        throw new ArgumentException($"Invalid core index: '{trimmed}'. Must be a valid integer.", nameof(coreList));
                    }

                    if (core < 0)
                    {
                        throw new ArgumentException($"Invalid core index: '{core}'. Core indices cannot be negative.", nameof(coreList));
                    }

                    cores.Add(core);
                }
            }

            if (!cores.Any())
            {
                throw new ArgumentException("Core list must contain at least one core.", nameof(coreList));
            }

            return cores.Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Gets a string representation of the core list.
        /// </summary>
        /// <returns>A comma-separated string of core indices.</returns>
        public override string ToString()
        {
            return string.Join(",", this.Cores);
        }
    }
}
