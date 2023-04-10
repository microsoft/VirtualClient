// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Web.Services.Description;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for AmdSmi output document.
    /// </summary>
    public class DXFLOPSParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="DXFLOPSParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DXFLOPSParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override List<Metric> Parse()
        {
            List<Metric> result = new List<Metric>
            {
                new Metric("performance.gpu [TFLOPs]", double.Parse(Regex.Match(this.RawText, @"[+-]?([0-9]*[.])?[0-9]+").Value), "TFLOPs", MetricRelativity.HigherIsBetter)
            };
            return result;
        }
    }
}
