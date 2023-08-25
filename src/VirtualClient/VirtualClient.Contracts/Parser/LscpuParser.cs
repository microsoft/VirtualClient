// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Used to parse CPU information from the output of the 'lscpu' toolset.
    /// </summary>
    internal class LscpuParser : TextParser<CpuInfo>
    {
        private static readonly Regex CacheExpression = new Regex(@"L([0-9]+)([a-z]*)\s+cache\s*\:\s*([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CoresPerSocketExpression = new Regex(@"Core\(s\)\s+per\s+socket\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CpusExpression = new Regex(@"CPU\(s\)\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CpuFamilyExpression = new Regex(@"CPU\s+family\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ModelExpression = new Regex(@"Model\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ModelNameExpression = new Regex(@"Model\s+name\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumaNodeExpression = new Regex(@"NUMA\s+node\(s\)\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SocketExpression = new Regex(@"Socket\(s\)\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SteppingExpression = new Regex(@"Stepping\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ThreadsPerCoreExpression = new Regex(@"Thread\(s\)\s+per\s+core\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex VendorIdExpression = new Regex(@"Vendor\s+ID\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LscpuParser(string text)
            : base(text)
        {
        }

        /// <summary>
        /// Parses the output of the CoreInfo.exe toolset and returns a information
        /// about the system.
        /// </summary>
        /// <returns></returns>
        public override CpuInfo Parse()
        {
            // Example format:
            // CPU(s):                          16
            // On-line CPU(s) list:             0 - 15
            // Thread(s) per core:              2
            // Core(s) per socket:              2
            // Socket(s):                       1
            // NUMA node(s):                    1
            // Vendor ID:                       GenuineIntel
            // CPU family:                      6
            // Model:                           106
            // Model name:                      Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz
            // Stepping:                        6
            // CPU MHz:                         2793.438
            // BogoMIPS:                        5586.87
            // Virtualization:                  VT-x
            // Hypervisor vendor:               Microsoft
            // Virtualization type:             full
            // L1d cache:                       96 KiB
            // L1i cache:                       64 KiB
            // L2 cache:                        2.5 MiB
            // L3 cache:                        48 MiB

            Match modelName = LscpuParser.ModelNameExpression.Match(this.RawText);
            Match coresPerSocket = LscpuParser.CoresPerSocketExpression.Match(this.RawText);
            Match cpus = LscpuParser.CpusExpression.Match(this.RawText);
            Match cpuModel = LscpuParser.ModelExpression.Match(this.RawText);
            Match cpuFamily = LscpuParser.CpuFamilyExpression.Match(this.RawText);
            Match cpuStepping = LscpuParser.SteppingExpression.Match(this.RawText);
            Match sockets = LscpuParser.SocketExpression.Match(this.RawText);
            Match threadsPerCore = LscpuParser.ThreadsPerCoreExpression.Match(this.RawText);
            Match numaNodes = LscpuParser.NumaNodeExpression.Match(this.RawText);
            Match vendor = LscpuParser.VendorIdExpression.Match(this.RawText);

            if (!coresPerSocket.Success || !cpus.Success || !sockets.Success || !threadsPerCore.Success)
            {
                throw new WorkloadException(
                    $"The system CPU information could not be parsed from the 'lscpu' toolset output.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            int logicalProcessorCount = int.Parse(cpus.Groups[1].Value.Trim());
            int socketCount = int.Parse(sockets.Groups[1].Value.Trim());
            int socketCores = int.Parse(coresPerSocket.Groups[1].Value.Trim());
            bool hyperthreadingEnabled = int.Parse(threadsPerCore.Groups[1].Value.Trim()) > 1;
            int numaNodeCount = 0;

            string name = null;
            string description = string.Empty;

            if (modelName.Success)
            {
                name = modelName.Groups[1].Value.Trim();
                description = name;
            }

            if (numaNodes.Success)
            {
                numaNodeCount = int.Parse(numaNodes.Groups[1].Value.Trim());
            }

            if (cpuFamily.Success)
            {
                description += $" Family {cpuFamily.Groups[1].Value.Trim()}";
            }

            if (cpuModel.Success)
            {
                description += $" Model {cpuModel.Groups[1].Value.Trim()}";
            }

            if (cpuStepping.Success)
            {
                description += $" Stepping {cpuStepping.Groups[1].Value.Trim()}";
            }

            if (vendor.Success)
            {
                description += $", {vendor.Groups[1].Value.Trim()}";
            }

            IEnumerable<CpuCacheInfo> caches = LscpuParser.ParseCacheInfo(this.RawText);

            return new CpuInfo(
                name ?? description.Trim(),
                description.Trim(),
                socketCount * socketCores,
                logicalProcessorCount,
                socketCount,
                numaNodeCount,
                hyperthreadingEnabled,
                caches);
        }

        private static IEnumerable<CpuCacheInfo> ParseCacheInfo(string text)
        {
            List<CpuCacheInfo> caches = null;
            
            try
            {
                MatchCollection matches = LscpuParser.CacheExpression.Matches(text);
                if (matches?.Any() == true)
                {
                    caches = new List<CpuCacheInfo>();
                    IDictionary<string, long> cacheInfo = new Dictionary<string, long>();

                    foreach (Match cacheMatch in matches)
                    {
                        string cacheType = cacheMatch.Groups[2].Value.Trim();
                        int.TryParse(cacheMatch.Groups[1].Value.Trim(), out int cacheLevel);
                        string cacheSize = cacheMatch.Groups[3].Value.Trim();
                        long.TryParse(TextParsingExtensions.TranslateByteUnit(cacheSize), out long cacheSizeBytes);

                        // Account for the unlikely possibility of a toolset or parsing issue. If we cannot
                        // determine the cache level, we do not include the info in the cache output. This is
                        // not something we've seen before but is theoretically possible.
                        if (cacheLevel <= 0 || cacheSizeBytes <= 0)
                        {
                            continue;
                        }

                        // lscpu output provides a breakdown of the CPU memory caches between
                        // data caches, instruction caches and unified caches. We capture the total
                        // cache size (e.g. L1 data + L1 instruction = total L1 bytes) as well as
                        // the individuals. Note that we have to compute to total cache size when
                        // the cache is not unified (e.g. L1d + L1i below).
                        // e.g.
                        // L1d
                        // L1i
                        // L2
                        // L3
                        string cacheName = $"L{cacheLevel}";
                        string cacheName2 = null;
                        if (cacheType.Contains("d", StringComparison.OrdinalIgnoreCase))
                        {
                            cacheName2 = $"{cacheName}d";
                        }
                        else if (cacheType.Contains("i", StringComparison.OrdinalIgnoreCase))
                        {
                            cacheName2 = $"{cacheName}i";
                        }

                        // Cache Total Size (e.g. L1, L2, L3)
                        long currentTotalCacheSize;
                        if (cacheInfo.TryGetValue(cacheName, out currentTotalCacheSize))
                        {
                            cacheInfo[cacheName] += cacheSizeBytes;
                        }
                        else
                        {
                            cacheInfo[cacheName] = cacheSizeBytes;
                        }

                        // Cache Subset Size (e.g. L1d (data cache), L1i (instructions cache)).
                        if (!string.IsNullOrWhiteSpace(cacheName2))
                        {
                            long currentIndividualCacheSize;
                            if (cacheInfo.TryGetValue(cacheName2, out currentIndividualCacheSize))
                            {
                                cacheInfo[cacheName2] += cacheSizeBytes;
                            }
                            else
                            {
                                cacheInfo[cacheName2] = cacheSizeBytes;
                            }
                        }
                    }

                    foreach (var entry in cacheInfo)
                    {
                        caches.Add(new CpuCacheInfo(entry.Key, $"{entry.Key} CPU cache", entry.Value));
                    }
                }
            }
            catch
            {
                // Best effort only.
            }

            return caches;
        }
    }
}
