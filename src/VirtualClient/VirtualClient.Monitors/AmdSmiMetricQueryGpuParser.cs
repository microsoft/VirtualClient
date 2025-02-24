// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for AmdSmi output document.
    /// </summary>
    public class AmdSmiMetricQueryGpuParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="AmdSmiMetricQueryGpuParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public AmdSmiMetricQueryGpuParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            List<Metric> metrics = new List<Metric>();
            DataTable dataTable = DataTableExtensions.DataTableFromCsv(this.PreprocessedText);

            foreach (DataRow row in dataTable.Rows)
            {
                string gpuId = Convert.ToString(SafeGet(row, "gpu"));
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                {
                    { "gpu.id", gpuId }
                };

                metrics.Add(new Metric($"utilization.gpu [%] (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "gfx_usage")), unit: "%", metadata: metadata));
                metrics.Add(new Metric($"framebuffer.total [MB] (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "fb_total")), unit: "MB", metadata: metadata));
                metrics.Add(new Metric($"framebuffer.used [MB] (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "fb_used")), unit: "MB", metadata: metadata));

                // AMD MI300X
                metrics.Add(new Metric($"utilization.gpu (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "gfx_activity")), unit: "%", metadata: metadata));
                double value = 100 * Convert.ToDouble(SafeGet(row, "used_vram")) / Convert.ToDouble(SafeGet(row, "total_vram"));
                int roundedValue = Convert.ToInt32(Math.Round(value));
                metrics.Add(new Metric($"utilization.memory (GPU {gpuId})", roundedValue, unit: "%", metadata: metadata));
                metrics.Add(new Metric($"temperature.gpu (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "hotspot")), unit: "celsius", metadata: metadata));
                metrics.Add(new Metric($"temperature.memory (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "mem")), unit: "celsius", metadata: metadata));
                metrics.Add(new Metric($"power.draw.average (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "socket_power")), unit: "W", metadata: metadata));

                double gfx_clk_avg = (Convert.ToDouble(SafeGet(row, "gfx_0_clk")) + Convert.ToDouble(SafeGet(row, "gfx_1_clk")) +
                    Convert.ToDouble(SafeGet(row, "gfx_2_clk")) + Convert.ToDouble(SafeGet(row, "gfx_3_clk")) +
                    Convert.ToDouble(SafeGet(row, "gfx_4_clk")) + Convert.ToDouble(SafeGet(row, "gfx_5_clk")) +
                    Convert.ToDouble(SafeGet(row, "gfx_6_clk")) + Convert.ToDouble(SafeGet(row, "gfx_7_clk"))) / 8;

                metrics.Add(new Metric($"gfx_clk_avg (GPU {gpuId})", gfx_clk_avg, unit: "MHz", metadata: metadata));
                metrics.Add(new Metric($"mem_clk (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "mem_0_clk")), unit: "MHz", metadata: metadata));

                double video_vclk_avg = (Convert.ToDouble(SafeGet(row, "vclk_0_clk")) + Convert.ToDouble(SafeGet(row, "vclk_1_clk")) +
                    Convert.ToDouble(SafeGet(row, "vclk_2_clk")) + Convert.ToDouble(SafeGet(row, "vclk_3_clk"))) / 4;

                metrics.Add(new Metric($"video_vclk_avg (GPU {gpuId})", video_vclk_avg, unit: "MHz", metadata: metadata));

                double video_dclk_avg = (Convert.ToDouble(SafeGet(row, "dclk_0_clk")) + Convert.ToDouble(SafeGet(row, "dclk_1_clk")) +
                    Convert.ToDouble(SafeGet(row, "dclk_2_clk")) + Convert.ToDouble(SafeGet(row, "dclk_3_clk"))) / 4;

                metrics.Add(new Metric($"video_dclk_avg (GPU {gpuId})", video_dclk_avg, unit: "MHz", metadata: metadata));
                metrics.Add(new Metric($"pcie_bw (GPU {gpuId})", Convert.ToDouble(SafeGet(row, "bandwidth")) / 8, unit: "MB/s", metadata: metadata));
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = this.RawText.Replace("\r\n", Environment.NewLine);
            Regex quotedPattern = new Regex("\"([^\"]*)\"");
            this.PreprocessedText = quotedPattern.Replace(this.PreprocessedText, "N/A");
            Regex quotedPattern2 = new Regex("\\[.*?\\]");
            this.PreprocessedText = quotedPattern2.Replace(this.PreprocessedText, "N/A");
        }

        private static IConvertible SafeGet(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? Convert.ToString(row[columnName]) : "-1";
        }
    }
}
