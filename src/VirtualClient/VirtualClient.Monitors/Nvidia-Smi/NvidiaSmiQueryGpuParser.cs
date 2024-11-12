// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for NvidiaSmi output document.
    /// </summary>
    public class NvidiaSmiQueryGpuParser : MetricsParser
    {
        private readonly Dictionary<string, (string metricDelimiter, string metricUnit)> metricInfo = new ()
        {
            { "utilization.gpu", ("utilization.gpu [%]", "%") },
            { "utilization.memory", ("utilization.memory [%]", "%") },
            { "temperature.gpu", ("temperature.gpu", "celsius") },
            { "temperature.memory", ("temperature.memory", "celsius") },
            { "power.draw.average", ("power.draw.average [W]", "W") },
            { "clocks.gr", ("clocks.current.graphics [MHz]", "MHz") },
            { "clocks.sm", ("clocks.current.sm [MHz]", "MHz") },
            { "clocks.video", ("clocks.current.video [MHz]", "MHz") },
            { "clocks.mem", ("clocks.current.memory [MHz]", "MHz") },
            { "memory.total", ("memory.total [MiB]", "MiB") },
            { "memory.free", ("memory.free [MiB]", "MiB") },
            { "memory.used", ("memory.used [MiB]", "MiB") },
            { "power.draw.instant", ("power.draw.instant [W]", "W") },
            { "pcie.link.gen.gpucurrent", ("pcie.link.gen.gpucurrent", string.Empty) },
            { "pcie.link.width.current", ("pcie.link.width.current", string.Empty) },
            { "ecc.errors.corrected.volatile.device_memory", ("ecc.errors.corrected.volatile.device_memory", string.Empty) },
            { "ecc.errors.corrected.volatile.dram", ("ecc.errors.corrected.volatile.dram", string.Empty) },
            { "ecc.errors.corrected.volatile.sram", ("ecc.errors.corrected.volatile.sram", string.Empty) },
            { "ecc.errors.corrected.volatile.total", ("ecc.errors.corrected.volatile.total", string.Empty) },
            { "ecc.errors.corrected.aggregate.device_memory", ("ecc.errors.corrected.aggregate.device_memory", string.Empty) },
            { "ecc.errors.corrected.aggregate.dram", ("ecc.errors.corrected.aggregate.dram", string.Empty) },
            { "ecc.errors.corrected.aggregate.sram", ("ecc.errors.corrected.aggregate.sram", string.Empty) },
            { "ecc.errors.corrected.aggregate.total", ("ecc.errors.corrected.aggregate.total", string.Empty) },
            { "ecc.errors.uncorrected.volatile.device_memory", ("ecc.errors.uncorrected.volatile.device_memory", string.Empty) },
            { "ecc.errors.uncorrected.volatile.dram", ("ecc.errors.uncorrected.volatile.dram", string.Empty) },
            { "ecc.errors.uncorrected.volatile.sram", ("ecc.errors.uncorrected.volatile.sram", string.Empty) },
            { "ecc.errors.uncorrected.volatile.total", ("ecc.errors.uncorrected.volatile.total", string.Empty) },
            { "ecc.errors.uncorrected.aggregate.device_memory", ("ecc.errors.uncorrected.aggregate.device_memory", string.Empty) },
            { "ecc.errors.uncorrected.aggregate.dram", ("ecc.errors.uncorrected.aggregate.dram", string.Empty) },
            { "ecc.errors.uncorrected.aggregate.sram", ("ecc.errors.uncorrected.aggregate.sram", string.Empty) },
            { "ecc.errors.uncorrected.aggregate.total", ("ecc.errors.uncorrected.aggregate.total", string.Empty) }
        };

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
                    { "name", SafeGet(row, "name") },
                    { "pci.bus_id", SafeGet(row, "pci.bus_id") },
                    { "driver_version", SafeGet(row, "driver_version") },
                    { "pstate", SafeGet(row, "pstate") },
                    { "pcie.link.gen.max", SafeGet(row, "pcie.link.gen.max") },
                    { "pcie.link.gen.current", SafeGet(row, "pcie.link.gen.current") }
                };

                foreach (var entry in this.metricInfo)
                {
                    if (double.TryParse(SafeGet(row, entry.Value.metricDelimiter), out _))
                    {
                        metrics.Add(new Metric(entry.Key, Convert.ToDouble(SafeGet(row, entry.Value.metricDelimiter)), unit: entry.Value.metricUnit, description: "Nvidia-smi gpu", metadata: metadata));
                    }
                }
            }

            return metrics;
        }

        private static string SafeGet(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? Convert.ToString(row[columnName]) : "-1";
        }
    }
}
