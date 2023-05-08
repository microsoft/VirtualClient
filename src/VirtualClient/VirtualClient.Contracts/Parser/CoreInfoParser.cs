// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Used to parse the output of the CoreInfo.exe toolset.
    /// </summary>
    internal class CoreInfoParser : TextParser<CpuInfo>
    {
        private static readonly Regex AsteriskExpression = new Regex(@"\*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LogicalProcessorExpression = new Regex(@"\*+-*\s*Physical\s*Processor", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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

            MatchCollection logicalProcessors = CoreInfoParser.LogicalProcessorExpression.Matches(this.RawText);
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

            // The CoreInfo.exe toolset will return NUMA Node 0 if there are no NUMA nodes on the system.
            return new CpuInfo(
                lines[0]?.Trim(),
                lines[1]?.Trim(),
                physicalProcessors.Count,
                logicalProcessorCount,
                sockets.Count,
                numaNodeCount,
                hyperthreadingEnabled);
        }
    }
}
