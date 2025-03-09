// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors.Amd_Smi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for AMD SMI power and usage metrics, supporting multiple GPUs.
    /// </summary>
    public class AmdSmiMetricsParser : MetricsParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmdSmiMetricsParser"/> class.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public AmdSmiMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            List<Metric> metrics = new List<Metric>();
            var gpuSections = this.ExtractGpuSections(this.PreprocessedText);

            metrics.Add(new Metric("TOTAL_GPUS", gpuSections.Count, "count"));

            foreach (var (gpuId, section) in gpuSections)
            {
                this.ExtractMetrics(metrics, section, gpuId);
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = this.RawText.Trim();
        }

        /// <summary>
        /// Extracts GPU sections from the raw text based on whitespace separation.
        /// </summary>
        private List<(string GpuId, string Section)> ExtractGpuSections(string rawText)
        {
            var gpuSections = new List<(string, string)>();
            var sections = Regex.Split(rawText, "\n\\s*\n").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            for (int i = 0; i < sections.Count; i++)
            {
                string gpuId = i.ToString(); // Assigning zero-based IDs to GPUs
                gpuSections.Add((gpuId, sections[i].Trim()));
            }

            return gpuSections;
        }

        /// <summary>
        /// Extracts and adds metrics from the section for a specific GPU.
        /// </summary>
        private void ExtractMetrics(List<Metric> metrics, string section, string gpuId)
        {
            var metricDefinitions = new List<(string Name, string Pattern, string Unit, double DivideBy)>
            {
                ("GFX_ACTIVITY", "GFX_ACTIVITY:\\s+(?<value>\\d+) %", "%", 1),
                ("UMC_ACTIVITY", "UMC_ACTIVITY:\\s+(?<value>\\d+) %", "%", 1),
                ("MM_ACTIVITY", "MM_ACTIVITY:\\s+(?<value>\\w+)", string.Empty, 1),
                ("SOCKET_POWER", "SOCKET_POWER:\\s+(?<value>\\d+) W", "W", 1),
                ("GFX_VOLTAGE", "GFX_VOLTAGE:\\s+(?<value>N/A|\\d+(\\.\\d+)?)", "V", 1),
                ("SOC_VOLTAGE", "SOC_VOLTAGE:\\s+(?<value>N/A|\\d+(\\.\\d+)?)", "V", 1),
                ("MEM_VOLTAGE", "MEM_VOLTAGE:\\s+(?<value>N/A|\\d+(\\.\\d+)?)", "V", 1),
                ("POWER_MANAGEMENT", "POWER_MANAGEMENT:\\s+(?<value>\\w+)", string.Empty, 1),
                ("TEMPERATURE_EDGE", "EDGE:\\s+(?<value>N/A|\\d+(\\.\\d+)?)(?:\\s+°C)?", "C", 1),
                ("TEMPERATURE_HOTSPOT", "HOTSPOT:\\s+(?<value>N/A|\\d+(\\.\\d+)?)(?:\\s+°C)?", "C", 1),
                ("TEMPERATURE_MEM", "MEM:\\s+(?<value>N/A|\\d+(\\.\\d+)?)(?:\\s+°C)?", "C", 1)
            };

            foreach (var metric in metricDefinitions)
            {
                this.AddMetric(metrics, section, $"{metric.Name}_GPU{gpuId}", metric.Pattern, metric.Unit, metric.DivideBy);
            }
        }

        /// <summary>
        /// Adds a metric to the list if a match is found in the section, attaching GPU ID.
        /// </summary>
        private void AddMetric(List<Metric> metrics, string section, string name, string pattern, string unit, double divideBy = 1)
        {
            var match = Regex.Match(section, pattern);
            if (match.Success)
            {
                double value = ParseDoubleSafely(match.Groups["value"].Value) / divideBy;
                metrics.Add(new Metric(name, value, unit));
            }
        }

        /// <summary>
        /// Converts a value to double safely, replacing non-numeric values with -1.
        /// </summary>
        private static double ParseDoubleSafely(string value)
        {
            return double.TryParse(value, out double result) ? result : -1;
        }
    }
}