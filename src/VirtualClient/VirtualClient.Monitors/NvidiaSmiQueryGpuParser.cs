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

            // timestamp, name, pci.bus_id, driver_version, pstate, pcie.link.gen.max, pcie.link.gen.current, utilization.gpu [%],
            // utilization.memory [%], temperature.gpu, temperature.memory, power.draw.average [W], clocks.current.graphics [MHz],
            // clocks.current.sm [MHz], clocks.current.video [MHz], clocks.current.memory [MHz], memory.total [MiB], memory.free [MiB],
            // memory.used [MiB], power.draw.instant [W], pcie.link.gen.gpucurrent, pcie.link.width.current,
            // ecc.errors.corrected.volatile.device_memory, ecc.errors.corrected.volatile.dram, ecc.errors.corrected.volatile.sram,
            // ecc.errors.corrected.volatile.total, ecc.errors.corrected.aggregate.device_memory, ecc.errors.corrected.aggregate.dram,
            // ecc.errors.corrected.aggregate.sram, ecc.errors.corrected.aggregate.total, ecc.errors.uncorrected.volatile.device_memory,
            // ecc.errors.uncorrected.volatile.dram, ecc.errors.uncorrected.volatile.sram, ecc.errors.uncorrected.volatile.total,
            // ecc.errors.uncorrected.aggregate.device_memory, ecc.errors.uncorrected.aggregate.dram, ecc.errors.uncorrected.aggregate.sram,
            // ecc.errors.uncorrected.aggregate.total
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

                metrics.Add(new Metric("utilization.gpu", Convert.ToDouble(row[7]), unit: "%", metadata: metadata));
                metrics.Add(new Metric("utilization.memory", Convert.ToDouble(row[8]), unit: "%", metadata: metadata));
                metrics.Add(new Metric("temperature.gpu", Convert.ToDouble(row[9]), unit: "celsuis", metadata: metadata));
                metrics.Add(new Metric("temperature.memory", Convert.ToDouble(row[10]), unit: "celsuis", metadata: metadata));
                metrics.Add(new Metric("power.draw.average", Convert.ToDouble(row[11]), unit: "W", metadata: metadata));
                metrics.Add(new Metric("clocks.gr", Convert.ToDouble(row[12]), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.sm", Convert.ToDouble(row[13]), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.video", Convert.ToDouble(row[14]), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.mem", Convert.ToDouble(row[15]), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("memory.total", Convert.ToDouble(row[16]), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.free", Convert.ToDouble(row[17]), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.used", Convert.ToDouble(row[18]), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("power.draw.instant", Convert.ToDouble(row[19]), unit: "W", metadata: metadata));
                metrics.Add(new Metric("pcie.link.gen.gpucurrent", Convert.ToDouble(row[20]), metadata: metadata));
                metrics.Add(new Metric("pcie.link.width.current", Convert.ToDouble(row[21]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.device_memory", Convert.ToDouble(row[22]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.dram", Convert.ToDouble(row[23]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.sram", Convert.ToDouble(row[24]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.total", Convert.ToDouble(row[25]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.device_memory", Convert.ToDouble(row[26]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.dram", Convert.ToDouble(row[27]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.sram", Convert.ToDouble(row[28]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.total", Convert.ToDouble(row[29]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.device_memory", Convert.ToDouble(row[30]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.dram", Convert.ToDouble(row[31]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.sram", Convert.ToDouble(row[32]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.total", Convert.ToDouble(row[33]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.device_memory", Convert.ToDouble(row[34]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.dram", Convert.ToDouble(row[35]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.sram", Convert.ToDouble(row[36]), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.total", Convert.ToDouble(row[37]), metadata: metadata));
            }
            
            return metrics;
        }
    }
}
