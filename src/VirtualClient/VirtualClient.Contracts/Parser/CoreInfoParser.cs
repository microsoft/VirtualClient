// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Used to parse the output of the CoreInfo.exe toolset.
    /// </summary>
    internal class CoreInfoParser : TextParser<CpuInfo>
    {
        private static readonly Regex HyperthreadingExpression = new Regex("HTT", RegexOptions.Compiled);
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
            // *---Physical Processor 0
            // -*--Physical Processor 1
            // --*-Physical Processor 2
            // ---*Physical Processor 3
            // 
            // Logical Processor to Socket Map:
            // ****Socket 0
            //
            // Logical Processor to NUMA Node Map:
            // ****NUMA Node 0
            //
            // No NUMA nodes.
            MatchCollection physicalProcessors = CoreInfoParser.PhysicalProcessorExpression.Matches(this.RawText);
            MatchCollection sockets = CoreInfoParser.SocketExpression.Matches(this.RawText);
            Match hyperthreading = CoreInfoParser.HyperthreadingExpression.Match(this.RawText);

            string[] lines = this.RawText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines?.Any() != true || physicalProcessors.Count <= 0 || sockets.Count <= 0)
            {
                throw new WorkloadException(
                    $"The system CPU information could not be parsed from the CoreInfo.exe toolset output.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            int numaNodeCount = 0;
            if (!CoreInfoParser.NoNumaNodeExpression.IsMatch(this.RawText))
            {
                MatchCollection numaNodes = CoreInfoParser.NumaNodeExpression.Matches(this.RawText);
                numaNodeCount = numaNodes.Count;
            }

            bool hyperthreadingEnabled = hyperthreading.Success;

            // The CoreInfo.exe toolset will return NUMA Node 0 if there are no NUMA nodes on the system.
            return new CpuInfo(
                lines[0]?.Trim(),
                lines[1]?.Trim(),
                physicalProcessors.Count,
                Environment.ProcessorCount,
                sockets.Count,
                numaNodeCount,
                hyperthreadingEnabled);
        }
    }
}
