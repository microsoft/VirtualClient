// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Used to parse CPU information from the output of the 'lscpu' toolset.
    /// </summary>
    internal class LscpuParser : TextParser<CpuInfo>
    {
        private static readonly Regex CoresPerSocketExpression = new Regex(@"Core\(s\)\s+per\s+socket\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CpusExpression = new Regex(@"CPU\(s\)\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ModelNameExpression = new Regex(@"Model\s+name\:([\x20-\x7E]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex NumaNodeExpression = new Regex(@"NUMA\s+node\(s\)\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SocketExpression = new Regex(@"Socket\(s\)\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ThreadsPerCoreExpression = new Regex(@"Thread\(s\)\s+per\s+core\s*\:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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

            Match modelName = LscpuParser.ModelNameExpression.Match(this.RawText);
            Match coresPerSocket = LscpuParser.CoresPerSocketExpression.Match(this.RawText);
            Match cpus = LscpuParser.CpusExpression.Match(this.RawText);
            Match sockets = LscpuParser.SocketExpression.Match(this.RawText);
            Match threadsPerCore = LscpuParser.ThreadsPerCoreExpression.Match(this.RawText);
            Match numaNodes = LscpuParser.NumaNodeExpression.Match(this.RawText);

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
            string modelname = string.Empty;

            if (numaNodes.Success)
            {
                numaNodeCount = int.Parse(numaNodes.Groups[1].Value.Trim());
            }

            if (modelName.Success)
            {
                modelname = modelName.Groups[1].Value.Trim();
            }

            return new CpuInfo(
                modelname,
                null,
                socketCount * socketCores,
                logicalProcessorCount,
                socketCount,
                numaNodeCount,
                hyperthreadingEnabled);
        }
    }
}
