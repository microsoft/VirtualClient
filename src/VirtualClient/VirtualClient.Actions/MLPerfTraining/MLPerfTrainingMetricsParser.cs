// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Extreme.DataAnalysis.Models;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for MLPerf output document
    /// </summary>
    public class MLPerfTrainingMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="MLPerfTrainingMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public MLPerfTrainingMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            string[] stringsAccuracy = this.CleanDataAccuracy();
            string[] stringsOthers = this.CleanDataOthers();
            this.Metrics = new List<Metric>();

            // eval_accuracy
            Metric metricAccuracy = this.AccuracyParsing(stringsAccuracy);
            this.Metrics.Add(metricAccuracy);

            string patternDelimiterE2E = @"'e2e_time':\s*([^,}]+)";
            string patternDelimiterTrainingPerSec = @"'training_sequences_per_second':\s*([^,}]+)";
            string patternDelimiterFinalLoss = @"'final_loss':\s*([^,}]+)";
            string patternDelimiterRawTrainTime = @"'raw_train_time':\s*([^,}]+)";

            string metricName = string.Empty;
            double metricValue = 0;
            string metricUnit = string.Empty;
            MetricRelativity relativity = MetricRelativity.LowerIsBetter;

            // e2e_time
            metricName = "e2e_time";
            metricValue = OthersParsing(this.PreprocessedText, patternDelimiterE2E);
            metricUnit = "s";
            relativity = MetricRelativity.LowerIsBetter;

            Metric metricE2E = new Metric(metricName, metricValue, metricUnit, relativity);
            this.Metrics.Add(metricE2E);

            // training_sequences_per_second
            metricName = "training_sequences_per_second";
            metricValue = OthersParsing(this.PreprocessedText, patternDelimiterTrainingPerSec);
            metricUnit = string.Empty;
            relativity = MetricRelativity.HigherIsBetter;

            Metric metricTrainPerSec = new Metric(metricName, metricValue, metricUnit, relativity);
            this.Metrics.Add(metricTrainPerSec);

            // final_loss
            metricName = "final_loss";
            metricValue = OthersParsing(this.PreprocessedText, patternDelimiterFinalLoss);
            metricUnit = string.Empty;
            relativity = MetricRelativity.LowerIsBetter;

            Metric metricFinalLoss = new Metric(metricName, metricValue, metricUnit, relativity);
            this.Metrics.Add(metricFinalLoss);

            // raw_train_time
            metricName = "raw_train_time";
            metricValue = OthersParsing(this.PreprocessedText, patternDelimiterRawTrainTime);
            metricUnit = "s";
            relativity = MetricRelativity.LowerIsBetter;

            Metric metricRawTrainTime = new Metric(metricName, metricValue, metricUnit, relativity);
            this.Metrics.Add(metricRawTrainTime);

            return this.Metrics;
        }

        /// <summary>
        /// Extract only lines with eval_accuracy
        /// </summary>
        /// <returns></returns>
        private string[] CleanDataAccuracy()
        {
            this.Preprocess();

            string[] stringsSplitAccuracy = this.PreprocessedText.Split("\\n");

            string pattern = @"^\{'global.*";
            Regex regex = new Regex(pattern, RegexOptions.Multiline);

            string[] lines = new string[0];

            foreach (string line in stringsSplitAccuracy)
            {
                MatchCollection matches = regex.Matches(line);

                // Append the matched lines to the string array
                string[] temp = new string[lines.Length + matches.Count];
                Array.Copy(lines, temp, lines.Length);
                
                for (int i = 0; i < matches.Count; i++)
                {
                    temp[lines.Length + i] = matches[i].Value;
                }

                lines = temp;
            }

            // Return the string array
            return lines;
        }

        /// <summary>
        /// Extract only lines with e2e_time, training_sequences_per_second, final_loss, raw_train_time
        /// </summary>
        /// <returns></returns>
        private string[] CleanDataOthers()
        {
            string[] stringsSplitOthers = this.PreprocessedText.Split("\\n");

            string pattern = @"^\{'e2e.*";
            Regex regex = new Regex(pattern, RegexOptions.Multiline);

            string[] lines = new string[0];

            foreach (string line in stringsSplitOthers)
            {
                MatchCollection matches = regex.Matches(line);

                // Append the matched lines to the string array
                string[] temp = new string[lines.Length + matches.Count];
                Array.Copy(lines, temp, lines.Length);

                for (int i = 0; i < matches.Count; i++)
                {
                    temp[lines.Length + i] = matches[i].Value;
                }

                lines = temp;
            }

            // Return the string array
            return lines;
        }

        /// <summary>
        /// Used to parse for eval_accuracy
        /// </summary>
        /// <param name="stringsAccuracy"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadResultsException"></exception>
        private Metric AccuracyParsing(string[] stringsAccuracy)
        {
            string patternDelimiterAccuracy = @"'eval_mlm_accuracy':\s*([^,}]+)";
            string patternDelimiterGlobal = @"'global_steps':\s*([^,}]+)";

            double mlmAccuracy = 0;
            double mlMAccuracyTemp = 0;
            double globalStepNumber = 0;
            double counter = 0;

            foreach (string line in stringsAccuracy)
            {
                Regex regexCheck = new Regex(patternDelimiterGlobal);
                Match matchGlobal = regexCheck.Match(line);

                if (matchGlobal.Success)
                {
                    string globalStepsValue = matchGlobal.Groups[1].Value.Trim();
                    if (double.TryParse(globalStepsValue, out double number))
                    {
                        if (number < globalStepNumber)
                        {
                            mlmAccuracy += mlMAccuracyTemp;
                            counter++;
                        }

                        globalStepNumber = number;
                    }
                }
                else
                {
                    throw new WorkloadResultsException($"The MLPerf Training Workload did not generate accuracy metrics! ");
                }

                Regex regex = new Regex(patternDelimiterAccuracy);
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string evalMlmAccuracyValue = match.Groups[1].Value.Trim();
                    if (double.TryParse(evalMlmAccuracyValue, out double number))
                    {
                        mlMAccuracyTemp = number;
                    }
                }
                else
                {
                    throw new WorkloadResultsException($"The MLPerf Training Workload did not generate accuracy metrics! ");
                }
            }

            mlmAccuracy /= counter;

            string metricName = "Accuracy";
            double metricValue = mlmAccuracy;
            string metricUnit = "%";
            MetricRelativity relativity = MetricRelativity.HigherIsBetter;

            Metric metric = new Metric(metricName, metricValue, metricUnit, relativity);

            return metric;
        }

        /// <summary>
        /// Used for parsing e2e_time, training_sequences_per_second, final_loss, raw_train_time
        /// </summary>
        /// <param name="stringsOthers"></param>
        /// <param name="patternDelimiter"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadResultsException"></exception>
        private static double OthersParsing(string stringsOthers, string patternDelimiter)
        {
            double metricValue = 0;
            double counter = 0;

            string line = stringsOthers;
                
            // For e2e_time
            Regex regex = new Regex(patternDelimiter);
            Match match = regex.Match(line);
    
            if (match.Success)
            {
                string value = match.Groups[1].Value.Trim();
                if (double.TryParse(value, out double number))
                {
                    metricValue += number;
                    counter++;
                }
            }
            else
            {
                // throw new WorkloadResultsException($"The MLPerf Training Workload did not generate proper metrics! ");
            }

            metricValue /= counter;

            return metricValue;
        }
    }
}
