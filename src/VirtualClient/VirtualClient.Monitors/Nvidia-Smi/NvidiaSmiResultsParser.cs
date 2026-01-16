// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for Nvidia SMI toolset results.
    /// </summary>
    public static class NvidiaSmiResultsParser
    {
        private static readonly Regex GpuInfoExpression = new Regex(@"GPU (?<GPU>\d+): (?<Name>.+) \(UUID: (?<UUID>.+)\)", RegexOptions.Compiled);
        private static readonly Regex C2CLinkExpression = new Regex(@"C2C Link (?<LinkNumber>\d+): (?<LinkSpeed>[\d.]+) GB/s", RegexOptions.Compiled);

        private static readonly Dictionary<string, (string metricDelimiter, string metricUnit, string metricDescription, MetricRelativity relativity)> QueryGpuMetricMappings = new ()
        {
             { "%_utilization", ("utilization.gpu [%]", null, "The current percentage utilization of the GPU.", MetricRelativity.Undefined) },
             { "%_memory_utilization", ("utilization.memory [%]", null, "The current percentage of memory utilization for the GPU.", MetricRelativity.Undefined) },
             { "temperature", ("temperature.gpu", MetricUnit.Celsius, "The current temperature reading (in celsius) for the GPU.", MetricRelativity.LowerIsBetter) },
             { "memory_temperature", ("temperature.memory", MetricUnit.Celsius, "The current temperature reading (in celsius) for the GPU memory components.", MetricRelativity.LowerIsBetter) },
             { "power_draw_average", ("power.draw.average [W]", MetricUnit.Watts, "The current power draw (in watts) for the GPU", MetricRelativity.LowerIsBetter) },
             { "graphics_clock_speed", ("clocks.current.graphics [MHz]", MetricUnit.Megahertz, "The current clock speed for GPU graphics instruction processing.", MetricRelativity.Undefined) },
             { "streaming_multiprocessor_clock_speed", ("clocks.current.sm [MHz]", MetricUnit.Megahertz, "The current clock speed for GPU streaming multiprocessor instruction processing.", MetricRelativity.Undefined) },
             { "video_clock_speed", ("clocks.current.video [MHz]", MetricUnit.Megahertz, "The current clock speed for GPU video instruction processing.", MetricRelativity.Undefined) },
             { "memory_clock_speed", ("clocks.current.memory [MHz]", MetricUnit.Megahertz, "The current clock speed for GPU memory operations.", MetricRelativity.Undefined) },
             { "memory_total", ("memory.total [MiB]", MetricUnit.Mebibytes, "The total memory (in mebibytes) for the GPU.", MetricRelativity.Undefined) },
             { "memory_free", ("memory.free [MiB]", MetricUnit.Mebibytes, "The total memory (in mebibytes) currently free/available for the GPU.", MetricRelativity.Undefined) },
             { "memory_used", ("memory.used [MiB]", MetricUnit.Mebibytes, "The total memory (in mebibytes) currently used/unavailable for the GPU.", MetricRelativity.Undefined) },
             { "instant_power_draw", ("power.draw.instant [W]", MetricUnit.Watts, "The instant power draw (in watts) for the GPU.", MetricRelativity.LowerIsBetter) },
             { "pcie_link_gen_current", ("pcie.link.gen.gpucurrent", MetricUnit.Amps, "The PCIE link generated current (in amps) for the GPU.", MetricRelativity.Undefined) },
             { "pcie_link_width_current", ("pcie.link.width.current", MetricUnit.Amps, "The PCIE link width current (in amps) for the GPU.", MetricRelativity.Undefined) },
             { "ecc_volatile_device_memory_corrected_errors", ("ecc.errors.corrected.volatile.device_memory", MetricUnit.Count, "The number of ECC volatile device corrected memory errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_volatile_dram_corrected_errors", ("ecc.errors.corrected.volatile.dram", MetricUnit.Count, "The number of ECC volatile DRAM corrected errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_volatile_sram_corrected_errors", ("ecc.errors.corrected.volatile.sram", MetricUnit.Count, "The number of ECC volatile SRAM corrected errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_total_corrected_volatile_errors", ("ecc.errors.corrected.volatile.total", MetricUnit.Count, "The total number of ECC volatile corrected errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_device_memory_corrected_errors", ("ecc.errors.corrected.aggregate.device_memory", MetricUnit.Count, "The aggregate number of corrected errors for ECC device memory.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_dram_corrected_errors", ("ecc.errors.corrected.aggregate.dram", MetricUnit.Count, "The aggregate number of corrected errors for ECC DRAM.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_sram_corrected_errors", ("ecc.errors.corrected.aggregate.sram", MetricUnit.Count, "The aggregate number of corrected errors for ECC SRAM.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_total_corrected_errors", ("ecc.errors.corrected.aggregate.total", MetricUnit.Count, "The aggregate total number of corrected errors for ECC components.", MetricRelativity.LowerIsBetter) },
             { "ecc_volatile_device_memory_uncorrected_errors", ("ecc.errors.uncorrected.volatile.device_memory", MetricUnit.Count, "The number of ECC volatile device uncorrected memory errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_volatile_dram_uncorrected_errors", ("ecc.errors.uncorrected.volatile.dram", MetricUnit.Count, "The number of ECC volatile DRAM uncorrected memory errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_volatile_sram_uncorrected_errors", ("ecc.errors.uncorrected.volatile.sram", MetricUnit.Count, "The number of ECC volatile SRAM uncorrected memory errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_total_uncorrected_volatile_errors", ("ecc.errors.uncorrected.volatile.total", MetricUnit.Count, "The total number of ECC volatile uncorrected errors.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_device_memory_uncorrected_errors", ("ecc.errors.uncorrected.aggregate.device_memory", MetricUnit.Count, "The aggregate number of corrected errors for ECC device memory.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_dram_uncorrected_errors", ("ecc.errors.uncorrected.aggregate.dram", MetricUnit.Count, "The aggregate number of corrected errors for ECC DRAM.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_sram_uncorrected_errors", ("ecc.errors.uncorrected.aggregate.sram", MetricUnit.Count, "The aggregate number of corrected errors for ECC SRAM.", MetricRelativity.LowerIsBetter) },
             { "ecc_aggregate_total_uncorrected_errors", ("ecc.errors.uncorrected.aggregate.total", MetricUnit.Count, "The aggregate total number of uncorrected errors for ECC components.", MetricRelativity.LowerIsBetter) }
        };

        /// <summary>
        /// Parses the results of the 'nvidia-smi c2c' command.
        /// </summary>
        public static IList<Metric> ParseC2CResults(string results)
        {
            List<Metric> metrics = new List<Metric>();
            string[] lines = results.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string gpuName = null;
            string gpuUuid = null;
            int gpuIndex = -1;

            foreach (string line in lines)
            {
                Match gpuMatch = GpuInfoExpression.Match(line);
                if (gpuMatch.Success)
                {
                    gpuIndex = int.Parse(gpuMatch.Groups["GPU"].Value.Trim());
                    gpuName = gpuMatch.Groups["Name"].Value.Trim();
                    gpuUuid = gpuMatch.Groups["UUID"].Value.Trim();
                    continue;
                }

                Match c2cLinkMatch = C2CLinkExpression.Match(line);
                if (c2cLinkMatch.Success)
                {
                    int linkNumber = int.Parse(c2cLinkMatch.Groups["LinkNumber"].Value.Trim());
                    double linkSpeed = double.Parse(c2cLinkMatch.Groups["LinkSpeed"].Value.Trim());
                    IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                    {
                        { "gpu_index", gpuIndex },
                        { "gpu_name", gpuName },
                        { "gpu_uuid", gpuUuid }
                    };

                    metrics.Add(new Metric(
                        $"gpu{gpuIndex}_link{linkNumber}_speed", 
                        linkSpeed, 
                        unit: MetricUnit.GigabytesPerSecond,
                        description: $"GPU {gpuIndex}, Link {linkNumber}. Defines the C2C link speed for GPU #{gpuIndex} link #{linkNumber}",
                        relativity: MetricRelativity.HigherIsBetter,
                        metadata: metadata));
                }
            }

            return metrics;
        }

        /// <summary>
        /// Parses the results of the 'nvidia-smi --query-gpu' command.
        /// </summary>
        public static IList<Metric> ParseQueryResults(string results)
        {
            List<Metric> metrics = new List<Metric>();
            DataTable dataTable = DataTableExtensions.DataTableFromCsv(results);

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
                if (NvidiaSmiResultsParser.TryGetValue(row, "index", out string index))
                {
                    NvidiaSmiResultsParser.TryGetValue(row, "name", out string name);
                    NvidiaSmiResultsParser.TryGetValue(row, "pci.bus_id", out string busId);
                    NvidiaSmiResultsParser.TryGetValue(row, "driver_version", out string driverVersion);
                    NvidiaSmiResultsParser.TryGetValue(row, "pstate", out string pstate);
                    NvidiaSmiResultsParser.TryGetValue(row, "pcie.link.gen.max", out string linkGenMax);
                    NvidiaSmiResultsParser.TryGetValue(row, "pcie.link.gen.current", out string linkGenCurrent);

                    Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                    {
                        { "gpu_name", name },
                        { "gpu_index", index },
                        { "gpu_pci_bus_id", busId },
                        { "gpu_driver_version", driverVersion },
                        { "gpu_pstate", pstate },
                        { "gpu_pcie_link_gen_max", linkGenMax },
                        { "gpu_pcie_link_gen_current", linkGenCurrent }
                    };

                    foreach (var entry in NvidiaSmiResultsParser.QueryGpuMetricMappings)
                    {
                        if (NvidiaSmiResultsParser.TryGetMetric(row, entry.Value.metricDelimiter, out double? metricValue))
                        {
                            // e.g.
                            // %_utilization_gpu0
                            // %_utilization_gpu1
                            // ecc_volatile_device_memory_corrected_errors_gpu0
                            // ecc_volatile_device_memory_corrected_errors_gpu1
                            metrics.Add(new Metric(
                                $"gpu{index}_{entry.Key}",
                                metricValue.Value,
                                unit: entry.Value.metricUnit,
                                description: $"GPU {index}. {entry.Value.metricDescription}",
                                relativity: entry.Value.relativity,
                                metadata: metadata));
                        }
                    }
                }
            }

            return metrics;
        }

        private static bool TryGetMetric(DataRow row, string columnName, out double? value)
        {
            value = null;
            if (NvidiaSmiResultsParser.TryGetValue(row, columnName, out string columnValue)
                && double.TryParse(columnValue, out double metricValue))
            {
                value = metricValue;
            }

            return value != null;
        }

        private static bool TryGetValue(DataRow row, string columnName, out string? value)
        {
            value = null;
            if (row.Table.Columns.Contains(columnName))
            {
                value = row[columnName]?.ToString();
            }

            return value != null;
        }
    }
}