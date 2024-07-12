// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for AmdSmi output document.
    /// </summary>
    public class AmdSmiXGMIQueryGpuParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="AmdSmiXGMIQueryGpuParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public AmdSmiXGMIQueryGpuParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            List<Metric> metrics = new List<Metric>();
            List<dynamic> gpuDataList = JsonConvert.DeserializeObject<List<dynamic>>(this.PreprocessedText);

            DataTable dt = new DataTable();
            dt.Columns.Add("gpu", typeof(int));

            int numGPUs = gpuDataList.Count;

            for (int i = 0; i < numGPUs; i++)
            {
                dt.Columns.Add($"gpu{i}_read_value", typeof(double));
                dt.Columns.Add($"gpu{i}_write_value", typeof(double));
            }

            foreach (dynamic gpuData in gpuDataList)
            {
                DataRow row = dt.NewRow();
                row["gpu"] = gpuData.gpu;

                foreach (var link in gpuData.link_metrics.links)
                {
                    int gpuIndex = link.gpu;
                    row[$"gpu{gpuIndex}_read_value"] = link.read.value.Value;
                    row[$"gpu{gpuIndex}_write_value"] = link.write.value.Value;
                }

                dt.Rows.Add(row);
            }

            foreach (DataRow row in dt.Rows)
            {
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                {
                    { "gpu.id", Convert.ToString(SafeGet(row, "gpu")) },
                };

                for (int i = 0; i < numGPUs; i++)
                {
                    // metrics.Add(new Metric($"xgmi.readData.to{i}", Convert.ToDouble(SafeGet(row, $"gpu{i}_read_value")), unit: "KB", metadata: metadata));
                    // metrics.Add(new Metric($"xgmi.writeData.to{i}", Convert.ToDouble(SafeGet(row, $"gpu{i}_read_value")), unit: "KB", metadata: metadata));

                    // Bidirectional Data Transferred
                    metrics.Add(new Metric($"xgmi.bidata.{i}", (Convert.ToDouble(SafeGet(row, $"gpu{i}_read_value")) + Convert.ToDouble(SafeGet(row, $"gpu{i}_read_value"))), unit: "KB", metadata: metadata));
                }
            }

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            Regex quotedPattern = new Regex("\"N/A\"");
            this.PreprocessedText = quotedPattern.Replace(this.RawText, "{\r\n\"value\": 0,\r\n\"unit\": \"KB\"\r\n}");
        }

        private static IConvertible SafeGet(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName) ? Convert.ToString(row[columnName]) : "-1";
        }
    }
}
