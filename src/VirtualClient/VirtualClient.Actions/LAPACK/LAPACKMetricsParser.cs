// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for LAPACK test results file.
    /// </summary>
    public class LAPACKMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex LAPACKSectionDelimiter = new Regex(@"===*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex LAPACKDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Identifies string starting with digits and ending with digits.
        /// </summary>
        private static readonly Regex ValueUnitSplitRegex = new Regex(@"(?<=\d)( )(?=\w)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// constructor for <see cref="LAPACKMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public LAPACKMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Results for Linear equations single-precision .
        /// </summary>
        public DataTable LINSingleResult { get; set; }

        /// <summary>
        /// Results for Linear equations double-precision .
        /// </summary>
        public DataTable LINDoubleResult { get; set; }

        /// <summary>
        /// Results for Linear equations complex .
        /// </summary>
        public DataTable LINComplexResult { get; set; }

        /// <summary>
        /// Results for Linear equations complex double .
        /// </summary>
        public DataTable LINComplexDoubleResult { get; set; }

        /// <summary>
        /// Results for Eigen problems single-precision .
        /// </summary>
        public DataTable EIGSingleResult { get; set; }

        /// <summary>
        /// Results for Eigen problems double-precision .
        /// </summary>
        public DataTable EIGDoubleResult { get; set; }

        /// <summary>
        /// Results for Eigen problems complex .
        /// </summary>
        public DataTable EIGComplexResult { get; set; }

        /// <summary>
        /// Results for Eigen problems complex double .
        /// </summary>
        public DataTable EIGComplexDoubleResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, LAPACKSectionDelimiter);
            this.ThrowIfInvalidOutputFormat();
            this.CalculateLAPACKResult();

            List<Metric> metrics = new List<Metric>();
            metrics.AddRange(this.LINSingleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.LINDoubleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.LINComplexResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.LINComplexDoubleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.EIGSingleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.EIGDoubleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.EIGComplexResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));
            metrics.AddRange(this.EIGComplexDoubleResult.GetMetric(nameIndex: 0, valueIndex: 2, unitIndex: 3, metricRelativity: MetricRelativity.LowerIsBetter));

            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[\r\n|\n]+", $"{Environment.NewLine}");
            this.PreprocessedText = this.PreprocessedText.Replace("START LIN SINGLE", $"LIN_Single_Precision");
            this.PreprocessedText = this.PreprocessedText.Replace("START LIN DOUBLE", $"LIN_Double_Precision");
            this.PreprocessedText = this.PreprocessedText.Replace("START LIN COMPLEX DOUBLE", $"LIN_Complex_Double");
            this.PreprocessedText = this.PreprocessedText.Replace("START LIN COMPLEX", $"LIN_Complex");
            this.PreprocessedText = this.PreprocessedText.Replace("START EIG SINGLE", $"EIG_Single_Precision");
            this.PreprocessedText = this.PreprocessedText.Replace("START EIG DOUBLE", $"EIG_Double_Precision");
            this.PreprocessedText = this.PreprocessedText.Replace("START EIG COMPLEX DOUBLE", $"EIG_Complex_Double");
            this.PreprocessedText = this.PreprocessedText.Replace("START EIG COMPLEX", $"EIG_Complex");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @" Total time used =(\s){2,}", $"Total time used =  ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"END[A-Z \\n]*", $"===========================");
        }

        private void CalculateLAPACKResult()
        {
            foreach (KeyValuePair<string, string> section in this.Sections)
            {
                this.Sections[section.Key] = this.PreprocessSection(section.Value);
                double timeResultForEachTest = FindTimeResult(this.Sections[section.Key]);
                this.Sections[section.Key] = "compute_time_" + section.Key + "  " + timeResultForEachTest + " " + "seconds";
            }

            this.ParseLINSingleResult();
            this.ParseLINDoubleResult();
            this.ParseLINComplexResult();
            this.ParseLINComplexDoubleResult();
            this.ParseEIGSingleResult();
            this.ParseEIGDoubleResult();
            this.ParseEIGComplexResult();
            this.ParseEIGComplexDoubleResult();
        }

        private void ParseLINSingleResult()
        {
            string sectionName = "LIN_Single_Precision";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.LINSingleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.LINSingleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseLINDoubleResult()
        {
            string sectionName = "LIN_Double_Precision";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.LINDoubleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.LINDoubleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseLINComplexResult()
        {
            string sectionName = "LIN_Complex";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.LINComplexResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.LINComplexResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseLINComplexDoubleResult()
        {
            string sectionName = "LIN_Complex_Double";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.LINComplexDoubleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.LINComplexDoubleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseEIGSingleResult()
        {
            string sectionName = "EIG_Single_Precision";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.EIGSingleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.EIGSingleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseEIGDoubleResult()
        {
            string sectionName = "EIG_Double_Precision";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.EIGDoubleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.EIGDoubleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseEIGComplexResult()
        {
            string sectionName = "EIG_Complex";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.EIGComplexResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.EIGComplexResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private void ParseEIGComplexDoubleResult()
        {
            string sectionName = "EIG_Complex_Double";
            IList<string> columnNames = new List<string> { "Name", "Measurement" };
            this.EIGComplexDoubleResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], LAPACKMetricsParser.LAPACKDataTableDelimiter, sectionName, columnNames);

            IList<string> splitColumnNames = new List<string> { "Value", "Unit" };
            this.EIGComplexDoubleResult.SplitDataColumn(columnIndex: 1, LAPACKMetricsParser.ValueUnitSplitRegex, splitColumnNames);
        }

        private static double FindTimeResult(string sectionContent)
        {
            double timeResult = 0;
            Regex regexToMatchNumbers = new Regex(@"[+-]?([0-9]*[.])?[0-9]+", RegexOptions.ExplicitCapture);
            MatchCollection numbers = regexToMatchNumbers.Matches(sectionContent);
            if (numbers.Count == 0)
            {
                throw new SchemaException("A test in LAPACK has no output time metrics");
            }

            foreach (Match number in numbers)
            {
                timeResult += Convert.ToDouble(number.Value);
            }

            return timeResult;
        }

        private string PreprocessSection(string sectionContent)
        {
            List<string> result = new List<string>();
            List<string> rows = sectionContent.Split(Environment.NewLine, StringSplitOptions.None).ToList();
            foreach (string row in rows)
            {
                if (row.Contains("Total time used ="))
                {
                    result.Add(row);
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count != 8 || !this.Sections.ContainsKey("LIN_Single_Precision") || !this.Sections.ContainsKey("LIN_Double_Precision") ||
               !this.Sections.ContainsKey("LIN_Complex") || !this.Sections.ContainsKey("LIN_Complex_Double") || !this.Sections.ContainsKey("EIG_Single_Precision") ||
               !this.Sections.ContainsKey("EIG_Double_Precision") || !this.Sections.ContainsKey("EIG_Complex") || !this.Sections.ContainsKey("EIG_Complex_Double"))
            {
                throw new SchemaException("The LAPACK output file has incorrect format for parsing");
            }
        }
    }
}
