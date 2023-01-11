// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for NvidiaSmi output document.
    /// </summary>
    public class NvidiaSmiQueryGpuParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="NvidiaSmiQueryGpuParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public NvidiaSmiQueryGpuParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            DataTable dataTable = DataTableExtensions.DataTableFromCsv(this.PreprocessedText);

            // timestamp, name, pci.bus_id, driver_version, pstate, pcie.link.gen.max, pcie.link.gen.current, temperature.gpu, utilization.gpu [%], utilization.memory [%],
            // memory.total [MiB], memory.free [MiB], memory.used [MiB]
            foreach (DataRow row in dataTable.Rows)
            {
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                {
                    { "name", Convert.ToString(row[1]) },
                    { "pci.bus_id", Convert.ToString(row[2]) },
                    { "driver_version", Convert.ToString(row[3]) },
                    { "pstate", Convert.ToString(row[4]) },
                    { "pcie.link.gen.max", Convert.ToString(row[5]) },
                    { "pcie.link.gen.current", Convert.ToString(row[6]) }
                };
                metrics.Add(new Metric("temperature.gpu", Convert.ToDouble(row[7]), unit: "celsuis", metadata: metadata));
                metrics.Add(new Metric("utilization.gpu [%]", Convert.ToDouble(row[8]), unit: "%", metadata: metadata));
                metrics.Add(new Metric("utilization.memory [%]", Convert.ToDouble(row[9]), unit: "%", metadata: metadata));
                metrics.Add(new Metric("memory.total [MiB]", Convert.ToDouble(row[10]), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.free [MiB]", Convert.ToDouble(row[11]), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.used [MiB]", Convert.ToDouble(row[12]), unit: "MiB", metadata: metadata));
            }
            
            return metrics;
        }
    }
}
