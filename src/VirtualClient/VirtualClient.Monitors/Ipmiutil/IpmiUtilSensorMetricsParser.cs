// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;

    /// <summary>
    /// Parser for ipmiutil sensor output
    /// </summary>
    public class IpmiUtilSensorMetricsParser
    {
        // e.g.
        // 0022 | Full    | Temperature     | 22 | HSC0_TMP | OK   | 29.00 C
        // 0023 | Full    | Power Supply    | 23 | HSC1_INPUT_PWR | OK   | 594.00 W
        // 0020 | Full    | Voltage         | 20 | HSC0_INPUT_VOLT | OK   | 51.03 V
        // Also captures non-numerical readings like: NA, N/A, ERR, ---, etc.
        internal static readonly Regex SelRecordPattern = new Regex(
            @"\s*(?<RecordId>[0-9a-z]+)\s*\|\s*(?<SDRType>[^|]+)\s*\|\s*(?<Type>[^|]+)\s*\|\s*(?<SNum>[^|]+)\s*\|\s*(?<SensorName>[^|]+)\s*\|\s*(?<SensorStatus>[^|]+)\s*\|\s*(?<SensorReading>[^\s|]+)\s*(?<SensorReadingUnit>[^\s|]*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        /// <summary>
        /// Gets the list of invalid IPMI sensor entries that could not be parsed as metrics
        /// </summary>
        public IList<IDictionary<string, object>> InvalidIpmiSensorEntries { get; private set; }

        /// <inheritdoc/>
        public IList<Metric> Parse(string results)
        {
            IList<Metric> metrics = new List<Metric>();
            this.InvalidIpmiSensorEntries = new List<IDictionary<string, object>>();

            string[] lines = Regex.Split(results, @"\r?\n");
            if (lines?.Any() == true)
            {
                foreach (string line in lines)
                {
                    Match recordMatch = IpmiUtilSensorMetricsParser.SelRecordPattern.Match(line);
                    if (recordMatch.Success)
                    {
                        string recordId = recordMatch.Groups[1].Value?.Trim();
                        string sdrType = recordMatch.Groups[2].Value?.Trim();
                        string sensorType = recordMatch.Groups[3].Value?.Trim();
                        string sNum = recordMatch.Groups[4].Value?.Trim();
                        string sensorName = recordMatch.Groups[5].Value?.Trim();
                        string sensorStatus = recordMatch.Groups[6].Value?.Trim();
                        string sensorReading = recordMatch.Groups[7].Value?.Trim();
                        string sensorReadingUnit = recordMatch.Groups[8].Value?.Trim();

                        // Skip header lines that contain literal field names like "ID", "Name", "Reading"
                        bool hasHeaderFieldNames = string.Equals(recordId, "ID", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(sensorName, "Name", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(sensorStatus, "Status", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(sensorReading, "Reading", StringComparison.OrdinalIgnoreCase);

                        if (hasHeaderFieldNames)
                        {
                            continue;
                        }

                        if (!string.IsNullOrWhiteSpace(sensorType) 
                            && !string.IsNullOrWhiteSpace(sensorName) 
                            && !string.IsNullOrWhiteSpace(sensorReading)
                            && !string.IsNullOrWhiteSpace(sensorReadingUnit)
                            && double.TryParse(sensorReading, out double metricValue))
                        {
                            string metricUnit = IpmiUtilSensorMetricsParser.GetMetricUnit(sensorReadingUnit, out MetricRelativity metricRelativity);

                            var metadata = new Dictionary<string, IConvertible>
                            {
                                ["id"] = recordId,
                                ["sdrType"] = sdrType,
                                ["type"] = sensorType,
                                ["snum"] = sNum,
                                ["name"] = sensorName,
                                ["status"] = sensorStatus,
                                ["reading"] = sensorReading,
                                ["readingUnit"] = sensorReadingUnit
                            };

                            var tags = new List<string>() { sensorStatus };

                            metrics.Add(new Metric(
                                name: sensorName, 
                                value: metricValue, 
                                unit: metricUnit, 
                                relativity: metricRelativity,
                                tags: tags,
                                description: line.Trim(),
                                metadata: metadata));
                        }
                        else if (!string.IsNullOrWhiteSpace(sensorType) 
                            && !string.IsNullOrWhiteSpace(sensorName)
                            && !double.TryParse(sensorReading, out _))
                        {
                            // Capture non-numerical sensor readings as events
                            var eventInfo = new Dictionary<string, object>
                            {
                                ["id"] = recordId,
                                ["sdrType"] = sdrType,
                                ["type"] = sensorType,
                                ["snum"] = sNum,
                                ["name"] = sensorName,
                                ["status"] = sensorStatus,
                                ["reading"] = sensorReading,
                                ["readingUnit"] = sensorReadingUnit,
                                ["rawLine"] = line.Trim()
                            };

                            this.InvalidIpmiSensorEntries.Add(eventInfo);
                        }
                    }
                }
            }

            return metrics;
        }

        private static string GetMetricUnit(string sensorReadingUnit, out MetricRelativity relativity)
        {
            string unit = null;
            relativity = MetricRelativity.Undefined;

            if (!string.IsNullOrWhiteSpace(sensorReadingUnit))
            {
                switch (sensorReadingUnit.ToLowerInvariant())
                {
                    case "%":
                        unit = "percentage";
                        break;

                    case "a":
                        unit = MetricUnit.Amps;
                        break;

                    case "c":
                        unit = MetricUnit.Celcius;
                        relativity = MetricRelativity.LowerIsBetter;
                        break;

                    case "v":
                        unit = "volts";
                        break;

                    case "w":
                        unit = MetricUnit.Watts;
                        relativity = MetricRelativity.LowerIsBetter;
                        break;

                    default:
                        unit = sensorReadingUnit.ToLowerInvariant();
                        break;
                }
            }

            return unit;
        }
    }
}