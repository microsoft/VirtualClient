// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Used to parse the output of the CoreInfo.exe toolset.
    /// </summary>
    internal class CoreInfoParser : TextParser<CpuInfo>
    {
        private static readonly Regex AsteriskExpression = new Regex(@"\*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CacheExpression = new Regex(@"(Data\s*Cache|Instruction\s*Cache|Unified\s*Cache)\s*\d+,\s*Level\s*([0-9]+),\s*([0-9]+\s*[a-z]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NoNumaNodeExpression = new Regex("No NUMA nodes", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumaNodeExpression = new Regex(@"NUMA\s+Node\s+\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PhysicalProcessorExpression = new Regex(@"Physical\s+Processor\s+\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SocketExpression = new Regex(@"Socket\s+\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public CoreInfoParser(string text)
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
            // Logical to Physical Processor Map:
            // *--- Physical Processor 0
            // -*-- Physical Processor 1
            // --*- Physical Processor 2
            // ---* Physical Processor 3
            // 
            // Logical Processor to Socket Map:
            // ****Socket 0
            //
            // Logical Processor to NUMA Node Map:
            // ****NUMA Node 0
            //
            // No NUMA nodes.
            string logicalProcessorMapSection = CoreInfoParser.ParseLogicalToPhysicalProcessorSection(this.RawText);

            MatchCollection logicalProcessors = CoreInfoParser.AsteriskExpression.Matches(logicalProcessorMapSection);
            MatchCollection physicalProcessors = CoreInfoParser.PhysicalProcessorExpression.Matches(this.RawText);
            MatchCollection sockets = CoreInfoParser.SocketExpression.Matches(this.RawText);

            string[] lines = this.RawText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines?.Any() != true || logicalProcessors.Count <= 0 || physicalProcessors.Count <= 0 || sockets.Count <= 0)
            {
                throw new WorkloadException(
                    $"The system CPU information could not be parsed from the CoreInfo.exe toolset output.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            // **NOTE ON LOGICAL CORES**:
            // The number of logical processors can be determined by counting the number of asterisks in the
            // 'Logical to Physical Processor Map' section. Each physical processor will have 1 asterisk per
            // logical processor associated with the physical core.
            //
            // [Example 1]:
            // In the example below there are 4 physical cores and 8 logical cores (noting each physical core
            // shows 2 asterisks and thus 4 x 2 = 8).
            //
            // Logical to Physical Processor Map:
            // **------ Physical Processor 0(Hyperthreaded)
            // --**---- Physical Processor 1(Hyperthreaded)
            // ----**-- Physical Processor 2(Hyperthreaded)
            // ------** Physical Processor 3(Hyperthreaded)
            //
            // [Example 2]:
            // This is from an ARM64 system where it is very common for each 1 physical core to also be 1 logical core. ARM processors
            // are a simpler model where there is not necessarily a separation between physical and logical. In the example below there
            // are 16 physical cores and 16 logical cores (noting each physical core shows 1 asterisk and thus 16 x 1 = 16).
            //
            // Logical to Physical Processor Map:
            // *--------------- Physical Processor 0
            // -*-------------- Physical Processor 1
            // --*------------- Physical Processor 2
            // ---*------------ Physical Processor 3
            // ----*----------- Physical Processor 4
            // -----*---------- Physical Processor 5
            // ------*--------- Physical Processor 6
            // -------*-------- Physical Processor 7
            // --------*------- Physical Processor 8
            // ---------*------ Physical Processor 9
            // ----------*----- Physical Processor 10
            // -----------*---- Physical Processor 11
            // ------------*--- Physical Processor 12
            // -------------*-- Physical Processor 13
            // --------------*- Physical Processor 14
            // ---------------* Physical Processor 15
            int logicalProcessorCount = CoreInfoParser.AsteriskExpression.Matches(string.Join(string.Empty, logicalProcessors.Select(m => m.Value))).Count;

            int numaNodeCount = 0;
            if (!CoreInfoParser.NoNumaNodeExpression.IsMatch(this.RawText))
            {
                MatchCollection numaNodes = CoreInfoParser.NumaNodeExpression.Matches(this.RawText);
                numaNodeCount = numaNodes.Count;
            }

            bool hyperthreadingEnabled = logicalProcessorCount > physicalProcessors.Count;

            IEnumerable<CpuCacheInfo> caches = CoreInfoParser.ParseCacheInfo(this.RawText);

            // The CoreInfo.exe toolset will return NUMA Node 0 if there are no NUMA nodes on the system.
            return new CpuInfo(
                lines[0]?.Trim(),
                lines[1]?.Trim(),
                physicalProcessors.Count,
                logicalProcessorCount,
                sockets.Count,
                numaNodeCount,
                hyperthreadingEnabled,
                caches);
        }

        private static IEnumerable<CpuCacheInfo> ParseCacheInfo(string text)
        {
            List<CpuCacheInfo> caches = null;

            try
            {
                MatchCollection matches = CoreInfoParser.CacheExpression.Matches(text);
                if (matches?.Any() == true)
                {
                    caches = new List<CpuCacheInfo>();
                    IDictionary<string, long> cacheInfo = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

                    foreach (Match cacheMatch in matches)
                    {
                        string cacheType = cacheMatch.Groups[1].Value.Trim();
                        int.TryParse(cacheMatch.Groups[2].Value.Trim(), out int cacheLevel);
                        string cacheSize = cacheMatch.Groups[3].Value.Trim();
                        long.TryParse(TextParsingExtensions.TranslateByteUnit(cacheSize), out long cacheSizeBytes);

                        // Account for the unlikely possibility of a toolset or parsing issue. If we cannot
                        // determine the cache level, we do not include the info in the cache output. This is
                        // not something we've seen before but is theoretically possible.
                        if (cacheLevel <= 0 || cacheSizeBytes <= 0)
                        {
                            continue;
                        }

                        // CoreInfo output provides a breakdown of the CPU memory caches between
                        // data caches, instruction caches and unified caches. We capture the total
                        // cache size (e.g. L1 data + L1 instruction = total L1 bytes) as well as
                        // the individuals.
                        // e.g.
                        // L1
                        // L1d
                        // L1i
                        // L2
                        // L3
                        string cacheName = $"L{cacheLevel}";
                        string cacheName2 = null;
                        if (cacheType.Contains("Data", StringComparison.OrdinalIgnoreCase))
                        {
                            cacheName2 = $"{cacheName}d";
                        }
                        else if (cacheType.Contains("Instruction", StringComparison.OrdinalIgnoreCase))
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

        private static string ParseLogicalToPhysicalProcessorSection(string text)
        {
            // Depending upon the number of physical cores on the system, the output of Coreinfo is slightly
            // different. To simplify the parsing of the logical cores, we use a sub-section of the entire output.
            //
            // e.g.
            // 1) For a system with up to 64 physical cores, the section looks like this:
            //
            // Logical to Physical Processor Map:
            // **------  Physical Processor 0 (Hyperthreaded)
            // --**----  Physical Processor 1 (Hyperthreaded)
            // ---- **-- Physical Processor 2 (Hyperthreaded)
            // ------**  Physical Processor 3 (Hyperthreaded)
            //
            // Logical Processor to Socket Map:
            //
            // 2) For a system with greater than 64 physical cores, the section looks like this:
            //
            // Logical to Physical Processor Map:
            // Physical Processor 0:
            // *---------------------------------------------------------------
            // ----------------------------------------------------------------
            // Physical Processor 1:
            // -*--------------------------------------------------------------
            // ----------------------------------------------------------------
            //
            // ...
            // Physical Processor 63:
            // ---------------------------------------------------------------*
            // ----------------------------------------------------------------
            //
            // Logical Processor to Socket Map:

            string section = null;
            if (!string.IsNullOrWhiteSpace(text))
            {
                int indexBegin = text.LastIndexOf("Logical to Physical Processor Map:", StringComparison.OrdinalIgnoreCase);
                int indexEnd = text.IndexOf("Logical Processor to Socket Map:", StringComparison.OrdinalIgnoreCase);
                int sectionLength = indexEnd - indexBegin;

                if (indexBegin >= 0 && indexEnd >= 0 && sectionLength > 0)
                {
                    section = text.Substring(indexBegin, sectionLength)?.Trim();
                }
            }

            return section;
        }
    }
}
