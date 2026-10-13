// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for 'ipmiutil' output to extract SEL metrics.
    /// </summary>
    public static class IpmiUtilSelMetricsParser
    {
        // e.g.
        // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail
        internal static readonly Regex SelHeaderPattern = new Regex(
            @"RecId|Evt_Type|Sensor|Evt_detail",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // 0002 | 12/18/23 20:16:51
        internal static readonly Regex SelRecordIdPattern = new Regex(
            @"\s*[a-z0-9]+\s*\|\s*\d{2}/\d{2}/\d{4}\s*\d{2}:\d{2}:\d{2}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // 0002 | 12/18/23 20:16:51 | INF | Sms| OS Critical Stop | na  | OS Graceful Shutdown
        internal static readonly Regex SelRecordPattern = new Regex(
            @"\s*(?<RecordId>[0-9a-z]+)\s*\|\s*(?<Timestamp>\d{2}/\d{2}/\d{2}\s*\d{2}:\d{2}:\d{2})\s*\|\s*(?<Severity>[^|]+)\s*\|\s*(?<Source>[^|]+)\s*\|\s*(?<EventType>[^|]+)\s*\|\s*(?<Sensor>[^|]+)\s*\|\s*(?<EventDetail>[^|]+)\s*\|*\s*(?<EventRawDetail>[^|]+)*",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // 0002 | 12/18/23 20:16:51 | INF | Sms| OS Critical Stop | na  | OS Graceful Shutdown
        internal static readonly Regex SelRawRecordPattern = new Regex(
            @"^([0-9a-f]{2}\s*)+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // e.g.
        // Predictive failure 6f [02 02 18] -> Predictive failure
        private static readonly Regex SelRecordEventDetailPattern = new Regex(
            @"([a-z0-9]{3,})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly object LockObject = new object();

        /// <summary>
        /// Combines the raw SEL results with the decoded results.
        /// </summary>
        /// <param name="rawResults">The raw SEL results (e.g. 02 00 02 33 19 81 65 41 00 04 20 00 6f 03 ff ff).</param>
        /// <param name="decodedResults">The decoded SEL results (e.g. 0002 | 12/18/23 20:16:51 | INF | Sms| OS Critical Stop | na  | OS Graceful Shutdown).</param>
        /// <returns>SEL records with raw and decoded results combined.</returns>
        public static string CombineRecords(string rawResults, string decodedResults)
        {
            lock (IpmiUtilSelMetricsParser.LockObject)
            {
                string combinedResults = decodedResults;
                if (!string.IsNullOrWhiteSpace(rawResults) && !string.IsNullOrWhiteSpace(decodedResults))
                {
                    if (IpmiUtilSelMetricsParser.TryGetDecodedFormatRecords(decodedResults, out IList<string> decodedRecords, out IList<string> headers))
                    {
                        List<ExtendedRecord> combinedRecords = new List<ExtendedRecord>();

                        // 1) Index the decoded results records.
                        foreach (string record in decodedRecords)
                        {
                            // Format:
                            // Decoded Record      = 0002 | 12/18/23 20:16:51 | INF | Sms| OS Critical Stop | na  | OS Graceful Shutdown
                            // Matching Raw Record = 02 00 02 33 19 81 65 41 00 04 20 00 6f 03 ff ff
                            //
                            // Record ID 0002 == 02 00
                            Match recordMatch = IpmiUtilSelMetricsParser.SelRecordPattern.Match(record);
                            if (recordMatch.Success)
                            {
                                // Convert the record ID in the decoded format (e.g. 0002) to the format that it
                                // will be in the raw results (e.g. 02 00).
                                string recordId = recordMatch.Groups["RecordId"].Value;
                                string rawRecordId = string.Join(string.Empty, Regex.Split(recordId, "([0-9a-f]{2})", RegexOptions.IgnoreCase).Reverse());

                                // e.g.
                                // ["0200"] = 0002 | 12/18/23 20:16:51 | INF | Sms| OS Critical Stop | na  | OS Graceful Shutdown
                                combinedRecords.Add(new ExtendedRecord(rawRecordId, record));
                            }
                        }

                        if (IpmiUtilSelMetricsParser.TryGetRawFormatRecords(rawResults, out IList<string> rawResultsRecords))
                        {
                            // 2) Step through indexed decoded results and match the decoded result
                            //    with the raw result.
                            foreach (ExtendedRecord record in combinedRecords)
                            {
                                for (int i = 0; i < rawResultsRecords.Count; i++)
                                {
                                    string rawDetails = rawResultsRecords[i];
                                    if (IpmiUtilSelMetricsParser.SelRawRecordPattern.IsMatch(rawDetails))
                                    {
                                        // Format:
                                        // 02 00 02 33 19 81 65 41 00 04 20 00 6f 03 ff ff
                                        //
                                        // The ipmitool represents the record ID in 2 different formats between the 
                                        // raw results (e.g. 02 00) and the decoded results (e.g. 0002). The keys in the
                                        // decoded record mapping dictionary have been previously converted to the raw record ID
                                        // format.
                                        if (rawDetails.RemoveWhitespace().StartsWith(record.Id, StringComparison.OrdinalIgnoreCase))
                                        {
                                            record.CombineWith(rawResultsRecords[i]);

                                            // Shrinking the set for increasing efficiency (less records) to consider
                                            // each time we match a record.
                                            rawResultsRecords.RemoveAt(i);
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (combinedRecords?.Any() == true)
                        {
                            StringBuilder finalResults = new StringBuilder();
                            foreach (string header in headers)
                            {
                                if (!string.IsNullOrWhiteSpace(header))
                                {
                                    finalResults.AppendLine(header);
                                }

                                if (IpmiUtilSelMetricsParser.SelHeaderPattern.IsMatch(header))
                                {
                                    // Break as soon as we have the column headers row so that the
                                    // SEL records immediately follow.
                                    break;
                                }
                            }

                            foreach (ExtendedRecord record in combinedRecords)
                            {
                                finalResults.AppendLine(record.Details);
                            }

                            combinedResults = finalResults.ToString().Trim();
                        }
                    }
                }

                return combinedResults;
            }
        }

        /// <summary>
        /// Identifies the set of records that are new or unprocessed since the last record
        /// processed.
        /// </summary>
        /// <param name="results">Text containing the ipmiutil SEL record results.</param>
        /// <param name="lastRecordProcessed">The last record processed. This should be include the record ID, date and timestamp from the record (e.g. 1a | 08/07/24 11:55:50).</param>
        /// <param name="newRecords">New records to process.</param>
        /// <returns>True if new records are found.</returns>
        public static bool TryGetNewRecords(string results, string lastRecordProcessed, out IList<string> newRecords)
        {
            lock (IpmiUtilSelMetricsParser.LockObject)
            {
                newRecords = null;

                if (!string.IsNullOrWhiteSpace(results))
                {
                    if (IpmiUtilSelMetricsParser.TryGetDecodedFormatRecords(results, out IList<string> records, out IList<string> headers))
                    {
                        string recordsToProcess = results;

                        if (!string.IsNullOrWhiteSpace(lastRecordProcessed))
                        {
                            // e.g.
                            // last record process = 000f | 12/18/23 20:16:51
                            string[] parts = lastRecordProcessed.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                            if (parts?.Count() >= 2)
                            {
                                string expression = $@"\s*{parts[0]}\s*\|\s*{parts[1]}[\x20-\x7E]+";
                                string[] sections = Regex.Split(results, expression, RegexOptions.IgnoreCase);

                                if (sections?.Length == 1)
                                {
                                    // If the set of results DOES NOT contain a matching record ID, then the full set of records
                                    // will be returned. This happens for example when the SEL log is cleared at some point.
                                    recordsToProcess = sections[0]?.Trim();
                                }
                                else if (sections?.Length == 2)
                                {
                                    // If the results DOES contain a matching record ID, then the section after that record ID
                                    // represents the new records.
                                    recordsToProcess = sections[1]?.Trim();
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(recordsToProcess))
                        {
                            IpmiUtilSelMetricsParser.TryGetDecodedFormatRecords(recordsToProcess, out newRecords, out headers);
                        }
                    }
                }

                return newRecords?.Any() == true;
            }
        }

        /// <summary>
        /// Parses metrics from the ipmiutil SEL results/records.
        /// </summary>
        /// <param name="results">The results of the 'ipmiutil sel' command execution.</param>
        /// <param name="lastRecordProcessed">The last record processed in previous 'ipmiutil sel' command results.</param>
        /// <param name="eventsFilter">A filter to apply that determines which events will be processed for metrics.</param>
        public static ParsingResult Parse(string results, string lastRecordProcessed = null, Regex eventsFilter = null)
        {
            try
            {
                lock (IpmiUtilSelMetricsParser.LockObject)
                {
                    string currentRecordProcessed = null;
                    IList<Metric> metrics = new List<Metric>();

                    if (!string.IsNullOrWhiteSpace(results))
                    {
                        if (IpmiUtilSelMetricsParser.TryGetNewRecords(results, lastRecordProcessed, out IList<string> newRecords))
                        {
                            foreach (string record in newRecords)
                            {
                                Match recordMatch = IpmiUtilSelMetricsParser.SelRecordPattern.Match(record);
                                if (recordMatch.Success && recordMatch.Groups.TryGetValue("RecordId", out Group recordId))
                                {
                                    currentRecordProcessed = recordId.Value;
                                    if (eventsFilter != null && !eventsFilter.IsMatch(record))
                                    {
                                        continue;
                                    }

                                    if (recordMatch.Groups.TryGetValue("EventDetail", out Group eventDetail)
                                        && recordMatch.Groups.TryGetValue("EventType", out Group eventType)
                                        && recordMatch.Groups.TryGetValue("Source", out Group eventSource)
                                        && recordMatch.Groups.TryGetValue("Severity", out Group severity))
                                    {
                                        recordMatch.Groups.TryGetValue("Sensor", out Group sensor);
                                        recordMatch.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail);
                                        recordMatch.Groups.TryGetValue("Timestamp", out Group timestamp);
                                        DateTime.TryParseExact(timestamp.Value?.Trim(), "MM/dd/yy HH:mm:ss", null, DateTimeStyles.None, out DateTime eventTimestamp);

                                        string metricName = IpmiUtilSelMetricsParser.GetMetricName(
                                            eventType.Value?.Trim(), 
                                            eventSource.Value?.Trim(), 
                                            eventDetail.Value?.Trim(),
                                            sensor?.Value?.Trim(),
                                            eventRawDetail?.Value?.Trim());

                                        if (!string.IsNullOrWhiteSpace(metricName))
                                        {
                                            IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                                            {
                                                ["record"] = record?.Trim(),
                                                ["recordId"] = recordId?.Value?.Trim(),
                                                ["severity"] = severity?.Value?.Trim(),
                                                ["sensor"] = sensor?.Value?.Trim(),
                                                ["eventTimestamp"] = eventTimestamp,
                                                ["eventType"] = eventType?.Value?.Trim(),
                                                ["eventSource"] = eventSource?.Value?.Trim(),
                                                ["eventDetail"] = eventDetail?.Value?.Trim(),
                                                ["eventRawDetail"] = eventRawDetail?.Value?.Trim()
                                            };

                                            metrics.Add(new Metric(
                                                metricName,
                                                1,
                                                MetricUnit.Count,
                                                MetricRelativity.Undefined,
                                                tags: new List<string>
                                                {
                                                    "IPMI",
                                                    eventSource?.Value.Trim(),
                                                    severity?.Value?.Trim()
                                                },
                                                description: $"{severity?.Value} level IPMI SEL event ({record?.Trim()}).",
                                                metadata: metadata));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return new ParsingResult(metrics, currentRecordProcessed);
                }
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse SEL metrics from ipmiutil results.", exc, ErrorReason.InvalidResults);
            }
        }

        private static string GetMetricName(string eventType, string source, string eventDetail = null, string sensor = null, string eventRawDetail = null)
        {
            // e.g.
            // 00ad | 01/19/24 08:48:37 | CRT | EFI  | Power Supply | #b3  | Predictive failure 6f [02 02 18] | 02 00 02 33 19 81 65 41 00 04 20 00 6f 03 ff ff
            // EFI_Power_Supply_#b3_Predictive_failure_6f_[02_02_18] 02_00_02_33_19_81_65_41_00_04_20_00_6f_03_ff_ff
            //
            // Format: <source>_<eventType>_<sensor>_<eventDetail> <hex_bytes_joined_with_underscores>
            // Rules:
            // - Only replace whitespace with underscores (keep /, -, [, ], etc.)
            // - Include sensor if not "na" or empty
            // - Space separator between text and hex
            // - Hex bytes joined with underscores

            List<string> nameParts = new List<string>();

            // Add source and eventType
            if (!string.IsNullOrWhiteSpace(source))
            {
                nameParts.Add(source);
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                nameParts.Add(eventType);
            }

            // Add sensor if it's not "na" or empty
            if (!string.IsNullOrWhiteSpace(sensor) && !sensor.Equals("na", StringComparison.OrdinalIgnoreCase))
            {
                nameParts.Add(sensor);
            }

            // Add event detail if available
            if (!string.IsNullOrWhiteSpace(eventDetail))
            {
                nameParts.Add(eventDetail);
            }

            // Join all parts and replace only whitespace with underscores
            string textPart = string.Join("_", nameParts);
            textPart = Regex.Replace(textPart, @"\s+", "_");

            // Add hex part if available
            if (!string.IsNullOrWhiteSpace(eventRawDetail))
            {
                // Replace spaces in hex with underscores
                string hexPart = eventRawDetail.Replace(" ", "_");
                return $"{textPart} {hexPart}";
            }

            return textPart;
        }

        private static bool TryGetDecodedFormatRecords(string results, out IList<string> records, out IList<string> headers)
        {
            records = null;
            headers = null;

            string[] lines = Regex.Split(results, @"\r?\n");
            if (lines?.Any() == true)
            {
                records = new List<string>();
                headers = new List<string>();

                foreach (string line in lines)
                {
                    if (IpmiUtilSelMetricsParser.SelRecordPattern.IsMatch(line))
                    {
                        records.Add(line);
                    }
                    else if (!string.IsNullOrWhiteSpace(line))
                    {
                        headers.Add(line);
                    }
                }
            }

            return records?.Any() == true;
        }

        private static bool TryGetRawFormatRecords(string results, out IList<string> records)
        {
            records = null;

            string[] lines = Regex.Split(results, @"\r?\n");
            if (lines?.Any() == true)
            {
                records = new List<string>();

                foreach (string line in lines)
                {
                    if (IpmiUtilSelMetricsParser.SelRawRecordPattern.IsMatch(line))
                    {
                        records.Add(line);
                    }
                }
            }

            return records?.Any() == true;
        }

        /// <summary>
        /// Represents the result of the metrics parsing.
        /// </summary>
        public class ParsingResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ParsingResult"/> class.
            /// </summary>
            public ParsingResult(IEnumerable<Metric> metrics, string lastRecordProcessed = null)
            {
                this.LastRecordProcessed = lastRecordProcessed;

                if (metrics?.Any() == true)
                {
                    this.Metrics = new List<Metric>(metrics);
                }
            }

            /// <summary>
            /// The last SEL record processed (e.g. 00e8 | 02/19/24 18:42:00).
            /// </summary>
            public string LastRecordProcessed { get; }

            /// <summary>
            /// Metrics parsed from the SEL results.
            /// </summary>
            public IList<Metric> Metrics { get; }
        }

        [DebuggerDisplay("{Id} = {Details}")]
        private class ExtendedRecord
        {
            public ExtendedRecord(string id, string details)
            {
                this.Id = id;
                this.Details = details;
            }

            public string Id { get; }

            public string Details { get; set; }

            public void CombineWith(string rawData)
            {
                this.Details = $"{this.Details} | {rawData}";
            }
        }
    }
}