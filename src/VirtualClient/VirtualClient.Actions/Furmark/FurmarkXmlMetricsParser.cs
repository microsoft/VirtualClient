// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    /// <summary>
    ///  FurmarkMonitorMetricsParser.
    /// </summary>
    public class FurmarkXmlMetricsParser : MetricsParser
    {
        /// <summary>
        /// size of dictionary 
        /// </summary>
        private static readonly string GpuPattern = "<gpu_data gpu=\"(.*?)\"";
        private static readonly string CoreTempPattern = "core_temp=\"(.*?)\"";
        private static readonly string FpsPattern = "fps=\"(.*?)\"";
        private static readonly string VddcPattern = "vddc=\"(.*?)\"";
        private static readonly string CoreLoadPattern = "core_load=\"(.*?)\"";

        private static int size = 0;

        // private static readonly Regex GpuDataRegex = new Regex(GpuDataPattern);

        /// <summary>
        /// Constructor for <see cref="FurmarkXmlMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public FurmarkXmlMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.ThrowIfInvalidOutputFormat();
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            string input = this.RawText;
            MatchCollection gpuMatches = Regex.Matches(input, GpuPattern);
            MatchCollection coreTempMatches = Regex.Matches(input, CoreTempPattern);
            MatchCollection fpsMatches = Regex.Matches(input, FpsPattern);
            MatchCollection vddcMatches = Regex.Matches(input, VddcPattern);
            MatchCollection coreLoadMatches = Regex.Matches(input, CoreLoadPattern);

            Dictionary<string, List<double>> gpuCoreTemps = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> gpuPowers = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> gpuFps = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> gpuVddcs = new Dictionary<string, List<double>>();
            Dictionary<string, List<double>> gpuCoreLoads = new Dictionary<string, List<double>>();

            for (int i = 0; i < gpuMatches.Count; i++)
            {
                string gpuId = gpuMatches[i].Groups[1].Value;
                double coreTemp = double.Parse(coreTempMatches[i].Groups[1].Value);

                double fps = double.Parse(fpsMatches[i].Groups[1].Value);
                double vddc = double.Parse(vddcMatches[i].Groups[1].Value);
                double coreLoad = double.Parse(coreLoadMatches[i].Groups[1].Value);

                // Check if gpuId already exists in the dictionary
                if (gpuCoreTemps.ContainsKey(gpuId))
                {
                    // Append coreTemp to the existing list
                    gpuCoreTemps[gpuId].Add(coreTemp);
                }
                else
                {
                    // Create a new list with coreTemp as the first element
                    gpuCoreTemps[gpuId] = new List<double> { coreTemp };
                }

                if (gpuFps.ContainsKey(gpuId))
                {
                    gpuFps[gpuId].Add(fps);
                }
                else
                {
                    gpuFps[gpuId] = new List<double> { fps };
                }

                if (gpuVddcs.ContainsKey(gpuId))
                {
                    gpuVddcs[gpuId].Add(vddc);
                }
                else
                {
                    gpuVddcs[gpuId] = new List<double> { vddc };
                }

                if (gpuCoreLoads.ContainsKey(gpuId))
                {
                    gpuCoreLoads[gpuId].Add(coreLoad);
                }
                else
                {
                    gpuCoreLoads[gpuId] = new List<double> { coreLoad };
                }
            }

            size = gpuCoreTemps.Count;

            foreach (var kvp in gpuCoreTemps)
            {
                string gpuId = kvp.Key;
                List<double> coreTemps = kvp.Value;
                double x = coreTemps.Average();
                double max = coreTemps.Max();
                metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_AvgTemperatur ", coreTemps.Average()));
                metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_MaxTemperatur ", coreTemps.Max()));

                if (gpuFps.ContainsKey(gpuId))
                {
                    List<double> gpuFpsList = gpuFps[gpuId];
                    double c = gpuFpsList.Average();
                    double d = gpuFpsList.Max();
                    metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_AvgFPS ", gpuFpsList.Average()));
                    metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_MaxFPS ", gpuFpsList.Max()));
                }

                if (gpuVddcs.ContainsKey(gpuId))
                {
                    List<double> gpuVddcList = gpuVddcs[gpuId];
                    double e = gpuVddcList.Average();
                    double f = gpuVddcList.Max();
                    
                    metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_Vddc ", gpuVddcList.Average()));
                }

                if (gpuCoreLoads.ContainsKey(gpuId))
                {
                    List<double> gpuCoreLoadList = gpuCoreLoads[gpuId];
                    double e = gpuCoreLoadList.Average();
                    double f = gpuCoreLoadList.Max();
                    metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_AvgCoreLoad ", gpuCoreLoadList.Average()));
                    metrics.Add(new Metric("GPU" + string.Join(" ", gpuId) + "_MaxCoreLoad ", gpuCoreLoadList.Max()));
                }

            }

            metrics.Add(new Metric("check", 100));

            return metrics;
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.RawText == string.Empty || this.RawText == null)
            {
                throw new SchemaException("furmark workload didn't generate results files.");
            }
        }
    }
}
