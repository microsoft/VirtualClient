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
                    { "name", SafeGet(row, "name") },
                    { "pci.bus_id", SafeGet(row, "pci.bus_id") },
                    { "driver_version", SafeGet(row, "driver_version") },
                    { "pstate", SafeGet(row, "pstate") },
                    { "pcie.link.gen.max", SafeGet(row, "pcie.link.gen.max") },
                    { "pcie.link.gen.current", SafeGet(row, "pcie.link.gen.current") }
                };

                metrics.Add(new Metric("utilization.gpu", Convert.ToDouble(SafeGet(row, "utilization.gpu [%]")), unit: "%", metadata: metadata));
                metrics.Add(new Metric("utilization.memory", Convert.ToDouble(SafeGet(row, "utilization.memory [%]")), unit: "%", metadata: metadata));
                metrics.Add(new Metric("temperature.gpu", Convert.ToDouble(SafeGet(row, "temperature.gpu")), unit: "celsuis", metadata: metadata));
                metrics.Add(new Metric("temperature.memory", Convert.ToDouble(SafeGet(row, "temperature.memory")), unit: "celsuis", metadata: metadata));
                metrics.Add(new Metric("power.draw.average", Convert.ToDouble(SafeGet(row, "power.draw.average [W]")), unit: "W", metadata: metadata));
                metrics.Add(new Metric("clocks.gr", Convert.ToDouble(SafeGet(row, "clocks.current.graphics [MHz]")), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.sm", Convert.ToDouble(SafeGet(row, "clocks.current.sm [MHz]")), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.video", Convert.ToDouble(SafeGet(row, "clocks.current.video [MHz]")), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("clocks.mem", Convert.ToDouble(SafeGet(row, "clocks.current.memory [MHz]")), unit: "MHz", metadata: metadata));
                metrics.Add(new Metric("memory.total", Convert.ToDouble(SafeGet(row, "memory.total [MiB]")), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.free", Convert.ToDouble(SafeGet(row, "memory.free [MiB]")), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("memory.used", Convert.ToDouble(SafeGet(row, "memory.used [MiB]")), unit: "MiB", metadata: metadata));
                metrics.Add(new Metric("power.draw.instant", Convert.ToDouble(SafeGet(row, "power.draw.instant [W]")), unit: "W", metadata: metadata));
                metrics.Add(new Metric("pcie.link.gen.gpucurrent", Convert.ToDouble(SafeGet(row, "pcie.link.gen.gpucurrent")), metadata: metadata));
                metrics.Add(new Metric("pcie.link.width.current", Convert.ToDouble(SafeGet(row, "pcie.link.width.current")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.device_memory", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.volatile.device_memory")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.dram", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.volatile.dram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.sram", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.volatile.sram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.volatile.total", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.volatile.total")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.device_memory", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.aggregate.device_memory")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.dram", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.aggregate.dram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.sram", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.aggregate.sram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.corrected.aggregate.total", Convert.ToDouble(SafeGet(row, "ecc.errors.corrected.aggregate.total")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.device_memory", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.volatile.device_memory")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.dram", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.volatile.dram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.sram", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.volatile.sram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.volatile.total", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.volatile.total")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.device_memory", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.aggregate.device_memory")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.dram", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.aggregate.dram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.sram", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.aggregate.sram")), metadata: metadata));
                metrics.Add(new Metric("ecc.errors.uncorrected.aggregate.total", Convert.ToDouble(SafeGet(row, "ecc.errors.uncorrected.aggregate.total")), metadata: metadata));
            }

            return metrics;
        }

        private static IConvertible SafeGet(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? Convert.ToString(row[columnName]) : null;
        }
    }
}
