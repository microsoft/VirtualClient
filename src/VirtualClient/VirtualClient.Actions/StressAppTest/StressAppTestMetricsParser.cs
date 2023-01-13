namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for StressAppTest Workload.
    /// </summary>
    public class StressAppTestMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="StressAppTestMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public StressAppTestMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            List<Metric> metrics = new List<Metric>();
            string hardwareErrors = string.Empty;

            // Add count of Hardware Failure Error Incidents
            int hardwareErrorCount = -1;
            foreach (string line in this.PreprocessedText.Split("\n"))
            {
                if (line == "Killed")
                {
                    throw new WorkloadResultsException($"The StressAppTest Workload did not generate valid metrics! " +
                        $"The process got killed, possibly due to low memory exception.");
                }

                if (Regex.IsMatch(line, @".*Hardware Error:.*"))
                {
                    hardwareErrors += line + "\n";
                }

                if (Regex.IsMatch(line, @".*Stats: Found .* hardware incidents.*"))
                {
                    try
                    {
                        hardwareErrorCount = Convert.ToInt16(
                        line.Substring(
                            line.IndexOf("Found") + 6, line.IndexOf("hardware incident") - line.IndexOf("Found") - 7));
                    }
                    catch
                    {
                        throw new WorkloadResultsException($"Error while parsing the hardware error count in StressAppTest Workload logs.");
                    }
                }
            }            

            if (hardwareErrorCount == -1)
            {
                throw new WorkloadResultsException($"The StressAppTest Workload did not generate valid metrics. " +
                    $"No data on hardware failures captured. ");
            }

            Metric metric = new Metric("hardwareErrorCount", hardwareErrorCount, MetricRelativity.LowerIsBetter);
            metric.Tags.Add(hardwareErrors);

            metrics.Add(metric);
            return metrics;
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            // Converting all CRLF(Windows EOL) to LF(Unix EOL).
            this.PreprocessedText = Regex.Replace(this.RawText, "\r\n", "\n");

            // Removing unnecessary starting and ending space.
            this.PreprocessedText = this.PreprocessedText.Trim();
        }        
    }
}
