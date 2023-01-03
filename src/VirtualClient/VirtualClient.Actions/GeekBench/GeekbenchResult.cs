// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Contracts;
    using Newtonsoft.Json;

    /// <summary>
    /// Geekbench result class.
    /// </summary>
    public class GeekbenchResult
    {
        /// <summary>
        /// Mapping workloads to subsections
        /// </summary>
        private readonly Dictionary<string, SubSection> subsections = new Dictionary<string, SubSection>()
        {
            { "AES-XTS",  SubSection.Cryptography },
            { "Text Compression", SubSection.Integer },
            { "Image Compression", SubSection.Integer },
            { "Navigation", SubSection.Integer },
            { "HTML5", SubSection.Integer },
            { "SQLite", SubSection.Integer },
            { "PDF Rendering", SubSection.Integer },
            { "Text Rendering", SubSection.Integer },
            { "Clang", SubSection.Integer },
            { "Camera", SubSection.Integer },
            { "N-Body Physics", SubSection.FloatingPoint },
            { "Rigid Body Physics", SubSection.FloatingPoint },
            { "Gaussian Blur", SubSection.FloatingPoint },
            { "Face Detection", SubSection.FloatingPoint },
            { "Horizon Detection", SubSection.FloatingPoint },
            { "Image Inpainting", SubSection.FloatingPoint },
            { "HDR", SubSection.FloatingPoint },
            { "Ray Tracing", SubSection.FloatingPoint },
            { "Structure from Motion", SubSection.FloatingPoint },
            { "Speech Recognition", SubSection.FloatingPoint },
            { "Machine Learning", SubSection.FloatingPoint }
        };

        /// <summary>
        /// Set of valid subsections
        /// </summary>
        private enum SubSection
        {
            Cryptography,
            Integer,
            FloatingPoint
        }

        /// <summary>
        ///
        /// </summary>
        public string Date { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("document_type")]
        public int DocumentType { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("document_version")]
        public int DocumentVersion { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Build { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Commit { get; set; }

        /// <summary>
        ///
        /// </summary>
        public Dictionary<string, int> Options { get; set; }

        /// <summary>
        ///
        /// </summary>
        public GeekbenchPlatform Platform { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("processor_frequency")]
        public ProcessorFrequency ProcessorFrequency { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float Runtime { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("system_uuid")]
        public string SystemUuid { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float Clock { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Uuid { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        ///
        /// </summary>
        public GeekbenchMetricSection[] Metrics { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("multicore_rate")]
        public float MulticoreRate { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("multicore_score")]
        public int MulticoreScore { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Valid { get; set; }

        /// <summary>
        ///
        /// </summary>
        public Section[] Sections { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int CompleteBenchmark { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public static bool TryParseGeekbenchResult(string json, out GeekbenchResult geekbenchResult)
        {
            geekbenchResult = null;
            bool success = false;

            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentNullException(json, "Can't parse null or empty JSON string");
            }
            else
            {
                try
                {
                    geekbenchResult = JsonConvert.DeserializeObject<GeekbenchResult>(json);
                }
                catch (Exception ex)
                {
                    geekbenchResult = null;
                    Console.WriteLine(ex.ToString());
                }

                if (geekbenchResult != null)
                {
                    success = true;
                }
            }

            return success;
        }

        /// <summary>
        /// Get overall score and the metrics from singlecore and multicore sections
        /// </summary>
        /// <returns>List of scores from differnet subbenchmarks</returns>
        public IDictionary<string, Metric> GetResults()
        {
            Dictionary<string, Metric> results = new Dictionary<string, Metric>();

            foreach (Section section in this.Sections)
            {
                results.Add(string.Format("{0} Score", section.Name), new Metric("Test Score", section.Score, "score"));

                foreach (Workload workload in section.Workloads)
                {
                    workload.GetResults().ToList().ForEach(r =>
                    {
                        string testName = string.Format("{0} ({1})", r.Key, section.Name);

                        results.Add(testName, r.Value);
                    });
                }
            }

            results.AddRange(this.GetSubsectionScores(results, this.Sections.Select(x => x.Name).ToList()));

            return results;
        }

        /// <summary>
        /// Calculate the geometric mean of the workloads in each subsection.
        /// Uses the antilog function instead of multiplying the numbers together directly.
        /// https://www.geeksforgeeks.org/geometric-mean-two-methods/
        /// </summary>
        /// <param name="workloadScores">List of workload scores</param>
        /// <param name="sections">Section(s) to calculate subsection scores for, currently single or multi core</param>
        /// <returns>Name-Metric pairs for each subsection score</returns>
        private IReadOnlyDictionary<string, Metric> GetSubsectionScores(IReadOnlyDictionary<string, Metric> workloadScores, List<string> sections)
        {
            Dictionary<string, Metric> results = new Dictionary<string, Metric>();

            foreach (string section in sections)
            {
                Dictionary<SubSection, float> subsectionSums = new Dictionary<SubSection, float>()
            {
                { SubSection.Cryptography, 0 },
                { SubSection.Integer, 0 },
                { SubSection.FloatingPoint, 0 }
            };

                // get scores from the correct section - where it includes Single-Core or Multi-Core
                workloadScores.Where(x => x.Key.Contains(section, StringComparison.OrdinalIgnoreCase) && x.Value.Unit.Equals("score", StringComparison.OrdinalIgnoreCase))
                    .ToList()
                    .ForEach(x =>
                    {
                        // strip section and score from test name
                        string name = x.Key.Replace(string.Format(" Score ({0})", section), string.Empty, StringComparison.OrdinalIgnoreCase);

                        if (this.subsections.TryGetValue(name, out SubSection ss))
                        {
                            if (subsectionSums.ContainsKey(ss))
                            {
                                subsectionSums[ss] += (float)Math.Log(x.Value.Value);
                            }
                        }
                    });

                foreach (KeyValuePair<SubSection, float> sum in subsectionSums)
                {
                    int n = this.subsections.Where(x => x.Value.Equals(sum.Key)).Count();
                    if (n > 0)
                    {
                        float score = (float)Math.Exp((sum.Value / n));
                        results.Add(string.Format("{0} Score ({1})", sum.Key, section), new Metric("Test Score", (int)Math.Round(score), "score"));
                    }
                }
            }

            return results;
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class GeekbenchPlatform
    {
        /// <summary>
        ///
        /// </summary>
        public string Os { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Architecture { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Bits { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class ProcessorFrequency
    {
        /// <summary>
        ///
        /// </summary>
        public int[] Frequencies { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class GeekbenchMetricSection
    {
        /// <summary>
        ///
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        ///
        /// </summary>
        public long IValue { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float FValue { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class Section
    {
        /// <summary>
        ///
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("graph_width")]
        public int GraphWidth { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Valid { get; set; }

        /// <summary>
        /// A single test e.g. AES-XTS, Text compression, etc
        /// </summary>
        public Workload[] Workloads { get; set; }
    }

    /// <summary>
    ///
    /// </summary>
    public class Workload
    {
        /// <summary>
        ///
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Units { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Threads { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("graph_width")]
        public int GraphWidth { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("factory_clock")]
        public float FactoryClock { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("factory_runtime")]
        public float FactoryRuntime { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("workload_clock")]
        public float WorkloadClock { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float Runtime { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("runtime_max")]
        public float RuntimeMax { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("runtime_mean")]
        public float RuntimeMean { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("runtime_median")]
        public float RuntimeMedian { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("runtime_min")]
        public float RuntimeMin { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("runtime_stddev")]
        public float RuntimeStddev { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("rumtime_warmup")]
        public float RuntimeWarmup { get; set; }

        /// <summary>
        ///
        /// </summary>
        public float[] Runtimes { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int Valid { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("workload_rate")]
        public float WorkloadRate { get; set; }

        /// <summary>
        ///
        /// </summary>
        public long Work { get; set; }

        /// <summary>
        ///
        /// </summary>
        [JsonProperty("rate_string")]
        public string RateString { get; set; }

        /// <summary>
        ///
        /// </summary>
        public object[] Kernels { get; set; }

        /// <summary>
        ///
        /// </summary>
        public object[] Temperatures { get; set; }

        /// <summary>
        /// Get each metric and the corresponding score
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<string, Metric> GetResults()
        {
            Dictionary<string, Metric> results = new Dictionary<string, Metric>();

            int rateStringIndex = this.RateString.IndexOf(' ', StringComparison.OrdinalIgnoreCase);

            results.Add(
                this.Name,
                new Metric(
                    "Raw Value",
                    Convert.ToDouble(this.RateString.Substring(0, rateStringIndex)),
                    this.RateString.Substring(rateStringIndex + 1)));

            results.Add(string.Format("{0} Score", this.Name), new Metric("Test Score", this.Score, "score"));

            return results;
        }
    }
}