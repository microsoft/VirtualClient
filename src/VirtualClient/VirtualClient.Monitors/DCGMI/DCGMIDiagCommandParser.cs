using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtualClient.Contracts;

namespace VirtualClient.Monitors
{
    /// <summary>
    /// Parser for DCGMIDiag output document
    /// </summary>
    public class DCGMIDiagCommandParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="DCGMIDiagCommandParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DCGMIDiagCommandParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            double metricValue = 0;
            try
            {
                DCGMDia dcgm = JsonConvert.DeserializeObject<DCGMDia>(this.PreprocessedText);
                int numberOfCategories = dcgm.Diagname.Test_categories.Count;
                for (int i = 0; i < numberOfCategories; i++)
                {
                    string categoryName = dcgm.Diagname.Test_categories[i].Category;
                    int numberOfTests = dcgm.Diagname.Test_categories[i].Tests.Count;
                    for (int j = 0; j < numberOfTests; j++)
                    {
                        string testName = dcgm.Diagname.Test_categories[i].Tests[j].Name;
                        int numberOfResultsPerEachTest = dcgm.Diagname.Test_categories[i].Tests[j].Results.Count;
                        for (int k = 0; k < numberOfResultsPerEachTest; k++)
                        {
                            string resultStatus = dcgm.Diagname.Test_categories[i].Tests[j].Results[k].Status;
                            string resultsInfo = dcgm.Diagname.Test_categories[i].Tests[j].Results[k].Info;
                            string resultsWarning = dcgm.Diagname.Test_categories[i].Tests[j].Results[k].Warnings;
                            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { "resultsInfo", resultsInfo },
                             { "resultsWarning", resultsWarning }
                         };

                            if (resultStatus == "Pass")
                            {
                                metricValue = 1;
                            }
                            else if (resultStatus == "Fail")
                            {
                                metricValue = 0;
                            }
                            else
                            {
                                // this is for skip
                                metricValue = -1;
                            }

                            this.Metrics.Add(new Metric(categoryName + "_" + testName, metricValue, metadata: metadata));
                        }
                    }
                }
            }
            catch
            {
                throw new SchemaException("The DCGMI output file has incorrect format for parsing");
            }

            return this.Metrics;
        }
    }

    /// <summary>
    /// DiagJson Object
    /// </summary>
    [JsonObject]
    public class DCGMDia
    {
        /// <summary>
        /// diag name with which the json starts
        /// </summary>
        [JsonProperty(PropertyName = "DCGM GPU Diagnostic", Required = Required.Always)]
        public DCGMGPUDiagnostic Diagname { get; set; }
    }

    /// <summary>
    /// DCGMGPUDiagnostic info
    /// </summary>
    [JsonObject]
    public class DCGMGPUDiagnostic
    {
        /// <summary>
        /// All test_categories
        /// </summary>
        [JsonProperty(PropertyName = "test_categories", Required = Required.Always)]
        public List<TestCategory> Test_categories { get; set; }
    }

    /// <summary>
    /// TestCategory
    /// </summary>
    [JsonObject]
    public class TestCategory
    {
        /// <summary>
        /// Category Name
        /// </summary>
        [JsonProperty(PropertyName = "category", Required = Required.Always)]
        public string Category { get; set; }

        /// <summary>
        /// Tests
        /// </summary>
        [JsonProperty(PropertyName = "tests", Required = Required.Always)]
        public List<Test> Tests { get; set; }
    }

    /// <summary>
    /// Test name
    /// </summary>
    [JsonObject]
    public class Test
    {
        /// <summary>
        /// Name of test
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Results of the test
        /// </summary>
        [JsonProperty(PropertyName = "results", Required = Required.Always)]
        public List<Result> Results { get; set; }
    }

    /// <summary>
    /// Results of the test
    /// </summary>
    [JsonObject]
    public class Result
    {
        /// <summary>
        /// Status of the result for any test
        /// </summary>
        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        public string Status { get; set; }

        /// <summary>
        /// warning message if any
        /// </summary>
        [JsonProperty(PropertyName = "warnings", Required = Required.Default)]
        public string Warnings { get; set; }

        /// <summary>
        /// Info if any
        /// </summary>
        [JsonProperty(PropertyName = "info", Required = Required.Default)]
        public string Info { get; set; }

        /// <summary>
        /// Info if any
        /// </summary>
        [JsonProperty(PropertyName = "gpu_ids", Required = Required.Default)]
        public string Gpu_Ids { get; set; }
    }
}
