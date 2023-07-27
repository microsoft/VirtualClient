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
    /// Parser for AmdSmi output document.
    /// </summary>
    public class AmdSmiQueryGpuParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="AmdSmiQueryGpuParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public AmdSmiQueryGpuParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();

            // Sanatize non-standard csv tokens in output
            string replacedText = this.PreprocessedText.Replace("[0, 0]", "0");

            List<Metric> metrics = new List<Metric>();
            DataTable dataTable = DataTableExtensions.DataTableFromCsv(replacedText);

            foreach (DataRow row in dataTable.Rows)
            {
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                {
                    { "gpu.id", Convert.ToString(row[0]) },
                };

                // Ingest only the metrics which are exposed at the guest level
                metrics.Add(new Metric("utilization.gpu [%]", Convert.ToDouble(row[1]), unit: "%", metadata: metadata));
                metrics.Add(new Metric("framebuffer.total [MB]", Convert.ToDouble(row[4]), unit: "MB", metadata: metadata));
                metrics.Add(new Metric("framebuffer.used [MB]", Convert.ToDouble(row[5]), unit: "MB", metadata: metadata));
            }

            return metrics;
        }
    }
}
