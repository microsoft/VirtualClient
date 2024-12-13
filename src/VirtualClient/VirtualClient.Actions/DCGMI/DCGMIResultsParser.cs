// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for DCGMI output document
    /// </summary>
    public class DCGMIResultsParser : MetricsParser
    {
        /// <summary>
        /// Name of Diag (Diagnostics) subsystem.
        /// </summary>
        protected const string Diagnostics = "Diagnostics";

        /// <summary>
        /// Name of Discovery subsystem.
        /// </summary>
        protected const string Discovery = "Discovery";

        /// <summary>
        /// Name of FieldGroup subsystem.
        /// </summary>
        protected const string FieldGroup = "FieldGroup";

        /// <summary>
        /// Name of Group subsystem.
        /// </summary>
        protected const string Group = "Group";

        /// <summary>
        /// Name of Health subsystem.
        /// </summary>
        protected const string Health = "Health";

        /// <summary>
        /// Name of Modules subsystem.
        /// </summary>
        protected const string Modules = "Modules";

        /// <summary>
        /// Name of CUDATestGenerator(proftester) subsystem.
        /// </summary>
        protected const string CUDATestGenerator = "CUDATestGenerator";

        /// <summary>
        /// Name of CUDATestGenerator(proftester) subsystem.
        /// </summary>
        protected const string CUDATestGeneratorDmon = "CUDATestGeneratorDmon";

        /// <summary>
        /// To match NvSwitch line of the Discovery result.
        /// </summary>
        private const string GetDiscoveryNvSwitchLines = @"(\d+)\s*NvSwitch(.*?)\s*found.\s*";

        /// <summary>
        /// To match GPU line of the Discovery result.
        /// </summary>
        private const string GetDiscoveryGPULines = @"(\d+)\s*GPU(.*?)\s*found.\s*";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SpaceDelimiter = @"\s{1,}";

        /// <summary>
        /// To match ID line of the FieldGroup result.
        /// </summary>
        private const string GetFieldGroupIDLines = @"\|\s*ID\s*\|\s*(\d+)\s*\|";

        /// <summary>
        /// To match Name line of the FieldGroup result.
        /// </summary>
        private const string GetFieldGroupNameLines = @"\|\s*Name\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// To match FieldIDs line of the FieldGroup result.
        /// </summary>
        private const string GetFieldGroupFieldIDsLines = @"\|\s*Field IDs\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// Split string at '|' char.
        /// </summary>
        private const string FieldGroupDelimiter = @"\|";

        /// <summary>
        /// To match Groups line of the result.
        /// </summary>
        private const string GetGroups = @"(\d+)\s*group(.*?)\s*found.\s*";

        /// <summary>
        /// To match group ID line of the Group result.
        /// </summary>
        private const string GetGroupIDLines = @"\s*Group ID\s*\|\s*(\d+)\s*";

        /// <summary>
        /// To match group Name line of the Group result.
        /// </summary>
        private const string GetGroupNameLines = @"\|\s*Group Name\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// To match group entities line of the Group result.
        /// </summary>
        private const string GetGroupEntitiesLines = @"\|\s*Entities\s*\|\s*(.*?)\s*\|";

        /// <summary>
        /// Split string at '|' char.
        /// </summary>
        private const string GroupDelimiter = @"\|";

        /// <summary>
        /// To match status line of the Modules result.
        /// </summary>
        private const string GetModulesStatus = @"\s*Status:\s*(.*?)\s*(\r\n|\n)";

        /// <summary>
        /// To match status line of the Proftester result.
        /// </summary>
        private const string GetProftesterTestFields = @"GPU (\d+) TestField (\d+) test (\w+)";

        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex SectionDelimiter = new Regex(@"==*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex DataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);      

        private List<Metric> metrics; 

        /// <summary>
        /// Constructor for <see cref="DCGMIResultsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="subsystem">Subsystem Name</param>
        public DCGMIResultsParser(string rawText, string subsystem)
            : base(rawText)
        {
            this.Subsystem = subsystem;
        }

        /// <summary>
        /// list of modules.
        /// </summary>
        public DataTable ModulesListResult { get; set; }

        /// <summary>
        /// results for proftester.
        /// </summary>
        public DataTable ProftesterResult { get; set; }

        /// <summary>
        /// results from dmon while running proftester.
        /// </summary>
        public DataTable DmonResult { get; set; }

        private string Subsystem { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.metrics = new List<Metric>();
            if (this.Subsystem == DCGMIResultsParser.Diagnostics) 
            {
                this.metrics = this.ParseDiagnosticsResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.Discovery)
            {
                this.metrics = this.ParseDiscoveryResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.FieldGroup)
            {
                this.metrics = this.ParseFieldGroupResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.Group)
            {
                this.metrics = this.ParseGroupResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.Health)
            {
                this.metrics = this.ParseHealthResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.Modules)
            {
                this.metrics = this.ParseModulesResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.CUDATestGenerator)
            {
                this.metrics = this.ParseCUDATestGeneratorResults();
            }
            else if (this.Subsystem == DCGMIResultsParser.CUDATestGeneratorDmon)
            {
                this.metrics = this.ParseCUDATestGeneratorDmonResults();
            }

            return this.metrics;
        }

        private List<Metric> ParseDiagnosticsResults()
        {
            List<Metric> metrics = new List<Metric>();
            double metricValue = 0;
            try
            {
                DCGMDiagCommandResult dcgm = JsonConvert.DeserializeObject<DCGMDiagCommandResult>(this.PreprocessedText);
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

                            metrics.Add(new Metric(categoryName + "_" + testName, metricValue, metadata: metadata));
                        }
                    }
                }
            }
            catch
            {
                throw new SchemaException("The DCGMI Diagnostics output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseDiscoveryResults()
        {
            List<Metric> metrics = new List<Metric>();

            double gpuCount;
            double nvSwitchCount;
            try
            {
                var nvSwitchMatches = Regex.Matches(this.RawText, GetDiscoveryNvSwitchLines);
                var gpuMatches = Regex.Matches(this.RawText, GetDiscoveryGPULines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                var nvSwitchLine = Regex.Split(nvSwitchMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);
                var gpuLine = Regex.Split(gpuMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);

                nvSwitchCount = double.Parse(nvSwitchLine[0].Trim());
                gpuCount = double.Parse(gpuLine[0].Trim());

                metadata.Add($"output", this.PreprocessedText);

                metrics.Add(new Metric("GPUCount", gpuCount, metadata: metadata));
                metrics.Add(new Metric("NvSwitchCount", nvSwitchCount, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI Discovery output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseFieldGroupResults()
        {
            List<Metric> metrics = new List<Metric>();

            string id = string.Empty;
            string name = string.Empty;
            string fieldIDs = string.Empty;
            try
            {
                double metricValue = double.Parse(this.PreprocessedText.Split()[0]);
                var idMatches = Regex.Matches(this.RawText, GetFieldGroupIDLines);
                var nameMatches = Regex.Matches(this.RawText, GetFieldGroupNameLines);
                var fieldIDsMatches = Regex.Matches(this.RawText, GetFieldGroupFieldIDsLines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                for (int i = 0; i < idMatches.Count; i++)
                {
                    var idLine = Regex.Split(idMatches.ElementAt(i).Value.Trim(), FieldGroupDelimiter);
                    var nameLine = Regex.Split(nameMatches.ElementAt(i).Value.Trim(), FieldGroupDelimiter);
                    var fieldIDline = Regex.Split(fieldIDsMatches.ElementAt(i).Value.Trim(), FieldGroupDelimiter);

                    id = idLine[2].Trim();
                    name = nameLine[2].Trim();
                    fieldIDs = fieldIDline[2].Trim();

                    metadata.Add($"id_name_fieldIDs_{i}", id + "_" + name + "_" + fieldIDs);
                }

                metadata.Add("Fieldgroup output", this.RawText);
                metrics.Add(new Metric("fieldCount", metricValue, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI FieldGroup output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseGroupResults()
        {
            List<Metric> metrics = new List<Metric>();
            this.PreprocessGroupResults();
            double groupCount;
            string groupID;
            string groupName;
            string entities;

            try
            {
                var groupMatches = Regex.Matches(this.PreprocessedText, GetGroups);
                var groupIDMatches = Regex.Matches(this.PreprocessedText, GetGroupIDLines);
                var groupNameMatches = Regex.Matches(this.PreprocessedText, GetGroupNameLines);
                var entitiesMatches = Regex.Matches(this.PreprocessedText, GetGroupEntitiesLines);
                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                var groupLine = Regex.Split(groupMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);

                groupCount = double.Parse(groupLine[0].Trim());

                for (int i = 0; i < groupIDMatches.Count; i++)
                {
                    var groupIDLine = Regex.Split(groupIDMatches.ElementAt(i).Value.Trim(), GroupDelimiter);
                    var groupNameLine = Regex.Split(groupNameMatches.ElementAt(i).Value.Trim(), GroupDelimiter);
                    var entitiesLine = Regex.Split(entitiesMatches.ElementAt(i).Value.Trim(), GroupDelimiter);

                    groupID = groupIDLine[1].Trim();
                    groupName = groupNameLine[1].Trim();
                    entities = entitiesLine[1].Trim();

                    metadata.Add($"groupid_groupname_entities_{i}", groupID + "_" + groupName + "_" + entities);
                }

                metadata.Add($"output", this.RawText);

                metrics.Add(new Metric("GroupCount", groupCount, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI Group output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseHealthResults()
        {
            List<Metric> metrics = new List<Metric>();
            double metricValue = 0;

            var jsonDocument = JsonDocument.Parse(this.PreprocessedText);
            try
            {
                // Get the "Overall Health" value
                string overallHealthValue = jsonDocument.RootElement
                    .GetProperty("body")
                    .GetProperty("Overall Health")
                    .GetProperty("value")
                    .GetString();

                // Get the "Health Monitor Report" header
                string headerValue = jsonDocument.RootElement
                    .GetProperty("header")
                    .EnumerateArray()
                    .First()
                    .GetString();
                if (overallHealthValue == "Healthy")
                {
                    metricValue = 1;
                }
                else
                {
                    metricValue = 0;
                }

                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { "overallHealthValue", overallHealthValue },
                             { "headerValue", headerValue }
                         };
                metrics.Add(new Metric(headerValue + "_" + "overallHealthValue", metricValue, metadata: metadata));
            }
            catch
            {
                throw new SchemaException("The DCGMI Health output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseModulesResults()
        {
            List<Metric> metrics = new List<Metric>();
            this.PreprocessModulesResults();
            double status;
            try
            {
                var statusMatches = Regex.Matches(this.PreprocessedText, GetModulesStatus);
                var statusLine = Regex.Split(statusMatches.ElementAt(0).Value.Trim(), SpaceDelimiter);
                if (statusLine[1].Trim() == "Success")
                {
                    status = 1;
                }
                else
                {
                    status = 0;
                }

                this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"Status:\s*(.*?)\s*(\r\n|\n)", string.Empty);
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, SectionDelimiter);
                this.CalculateModulesList();
                int rows = this.ModulesListResult.Rows.Count;
                metrics.AddRange(this.ModulesListResult.GetMetrics(nameIndex: 1, valueIndex: 2));
            }
            catch
            {
                throw new SchemaException("The DCGMI Modules output file has incorrect format for parsing");
            }

            metrics.Add(new Metric("Status", status));
            return metrics;
        }

        private List<Metric> ParseCUDATestGeneratorResults()
        {
            List<Metric> metrics = new List<Metric>();
            string gpuID;
            string testFieldID;
            double testStatus;
            try
            {
                this.PreprocessProftesterResults();
                var testFieldMatches = Regex.Matches(this.PreprocessedText, GetProftesterTestFields);
                this.ThrowIfInvalidProftesterOutputFormat(testFieldMatches.Count);

                Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

                for (int i = 0; i < testFieldMatches.Count; i++)
                {
                    var testFieldLine = Regex.Split(testFieldMatches.ElementAt(i).Value.Trim(), SpaceDelimiter);
                    gpuID = testFieldLine[1].Trim();
                    testFieldID = testFieldLine[3].Trim();
                    string status = testFieldLine[5].Trim();
                    if (status == "PASSED")
                    {
                        testStatus = 1;
                    }
                    else
                    {
                        testStatus = 0;
                    }

                    metrics.Add(new Metric($"GPU{gpuID}_TestField{testFieldID}_TestStatus", testStatus, metadata: metadata));
                }

                metadata.Add("output", this.RawText);
            }
            catch
            {
                throw new SchemaException("The DCGMI Proftester output file has incorrect format for parsing");
            }

            return metrics;
        }

        private List<Metric> ParseCUDATestGeneratorDmonResults()
        {
            List<Metric> metrics = new List<Metric>();

            try
            {
                this.PreprocessProftesterDmonResults();
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, SectionDelimiter);
                this.ThrowIfInvalidProftesterDmonOutputFormat();
                this.CalculateProftesterDmonResults();

                foreach (DataRow row in this.DmonResult.Rows)
                {
                    int columnIndex = 0;
                    foreach (DataColumn column in this.DmonResult.Columns)
                    {
                        string metricName = $"{row[0]}_{column.ColumnName}";
                        double metricValue;
                        string value = row[columnIndex].ToString().Trim();
                        if (double.TryParse(value, out metricValue))
                        {
                            metrics.Add(new Metric(metricName, metricValue));
                        }

                        columnIndex++;
                    }
                }
            }
            catch
            {
                throw new SchemaException("The DCGMI dmon output file has incorrect format for parsing");
            }

            return metrics;
        }

        private void PreprocessGroupResults()
        {
            this.PreprocessedText = this.RawText.Replace("->", string.Empty);
        }

        /// <inheritdoc/>
        private void PreprocessModulesResults()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[=+]", "-");
            this.PreprocessedText = this.PreprocessedText.Replace("Loaded", "1");
            this.PreprocessedText = this.PreprocessedText.Replace("Not loaded", "0");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, $@"--*{Environment.NewLine}", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"\|", string.Empty);
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(\r\n|\n)", $"{Environment.NewLine}");
        }

        /// <inheritdoc/>
        private void PreprocessProftesterResults()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, $",", string.Empty);
        }

        /// <inheritdoc/>
        private void PreprocessProftesterDmonResults()
        {
            this.PreprocessedText = $"CUDA Generator metrics{Environment.NewLine}" + this.RawText;
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @$"ID\s*", string.Empty);
            this.PreprocessedText = this.PreprocessedText.Replace("#Entity", "Entity_ID");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(\r\n|\n)", $"{Environment.NewLine}");            // this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, new Regex(@"--*", RegexOptions.ExplicitCapture));
        }

        private void CalculateModulesList()
        {
            string sectionName = "List Modules";
            this.ModulesListResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DCGMIResultsParser.DataTableDelimiter, sectionName);
        }

        private void CalculateProftesterDmonResults()
        {
            string sectionName = "CUDA Generator metrics";
            this.DmonResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], DCGMIResultsParser.DataTableDelimiter, sectionName);
        }

        private void ThrowIfInvalidProftesterOutputFormat(int testCount)
        {
            if (testCount < 1)
            {
                throw new SchemaException("The DCGMI Proftester output file has incorrect format for parsing");
            }
        }

        private void ThrowIfInvalidProftesterDmonOutputFormat()
        {
            if (this.Sections.Count < 1 || !this.Sections.ContainsKey("CUDA Generator metrics"))
            {
                throw new SchemaException("The DCGMI dmon output file has incorrect format for parsing");
            }

            if (this.Sections.ContainsKey("CUDA Generator metrics") && !this.Sections["CUDA Generator metrics"].ToString().Contains("Entity_ID"))
            {
                throw new SchemaException("The DCGMI dmon output file has incorrect format for parsing");

            }
        }
    }

    /// <summary>
    /// DiagCommandResult Json Object
    /// </summary>
    [JsonObject]
    public class DCGMDiagCommandResult
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
        /// Gpu_Ids if any
        /// </summary>
        [JsonProperty(PropertyName = "gpu_ids", Required = Required.Default)]
        public string Gpu_Ids { get; set; }
    }
}
