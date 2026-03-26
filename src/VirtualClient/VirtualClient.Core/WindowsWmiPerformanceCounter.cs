// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides CPU performance counter data via WMIC subprocess when the legacy
    /// <see cref="System.Diagnostics.PerformanceCounter"/> API fails on systems with
    /// more than 64 logical processors.
    /// </summary>
    /// <remarks>
    /// Uses Win32_PerfFormattedData_Counters_ProcessorInformation which supports
    /// multi-processor groups and returns all CPU cores with "Group,Core" instance naming.
    /// Data is collected by invoking wmic.exe and parsing CSV output.
    /// </remarks>
    public class WindowsWmiPerformanceCounter : WindowsPerformanceCounter
    {
        private readonly string wmiPropertyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsWmiPerformanceCounter"/> class.
        /// </summary>
        /// <param name="counterCategory">The original performance counter category name (e.g. "Processor").</param>
        /// <param name="counterName">The original performance counter name (e.g. "% Processor Time").</param>
        /// <param name="instanceName">The instance name (e.g. "_Total", "0,5").</param>
        /// <param name="captureStrategy">The capture strategy.</param>
        public WindowsWmiPerformanceCounter(string counterCategory, string counterName, string instanceName, CaptureStrategy captureStrategy)
            : base(counterCategory, counterName, instanceName, captureStrategy)
        {
            this.wmiPropertyName = WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty(counterName);
        }

        /// <summary>
        /// Returns the WMI class name that corresponds to a performance counter category.
        /// </summary>
        public static string GetWmiClassName(string counterCategory)
        {
            if (string.Equals(counterCategory, "Processor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(counterCategory, "Processor Information", StringComparison.OrdinalIgnoreCase))
            {
                return "Win32_PerfFormattedData_Counters_ProcessorInformation";
            }

            return null;
        }

        /// <summary>
        /// Maps a performance counter name to a WMI property name.
        /// </summary>
        public static string MapCounterNameToWmiProperty(string counterName)
        {
            return counterName?.Trim() switch
            {
                "% Processor Time" => "PercentProcessorTime",
                "% User Time" => "PercentUserTime",
                "% Privileged Time" => "PercentPrivilegedTime",
                "% Idle Time" => "PercentIdleTime",
                "% Interrupt Time" => "PercentInterruptTime",
                "% DPC Time" => "PercentDPCTime",
                "% C1 Time" => "PercentC1Time",
                "% C2 Time" => "PercentC2Time",
                "% C3 Time" => "PercentC3Time",
                "Interrupts/sec" => "InterruptsPersec",
                "DPCs Queued/sec" => "DPCsQueuedPersec",
                "DPC Rate" => "DPCRate",
                "C1 Transitions/sec" => "C1TransitionsPersec",
                "C2 Transitions/sec" => "C2TransitionsPersec",
                "C3 Transitions/sec" => "C3TransitionsPersec",
                "% Processor Performance" => "PercentProcessorPerformance",
                "% Processor Utility" => "PercentProcessorUtility",
                "Processor Frequency" => "ProcessorFrequency",
                "% of Maximum Frequency" => "PercentofMaximumFrequency",
                _ => counterName?.Replace(" ", string.Empty).Replace("%", "Percent").Replace("/", "Per")
            };
        }

        /// <summary>
        /// Maps a WMI property name back to the original performance counter name format.
        /// </summary>
        public static string MapWmiPropertyToCounterName(string wmiProperty)
        {
            return wmiProperty?.Trim() switch
            {
                "PercentProcessorTime" => "% Processor Time",
                "PercentUserTime" => "% User Time",
                "PercentPrivilegedTime" => "% Privileged Time",
                "PercentIdleTime" => "% Idle Time",
                "PercentInterruptTime" => "% Interrupt Time",
                "PercentDPCTime" => "% DPC Time",
                "PercentC1Time" => "% C1 Time",
                "PercentC2Time" => "% C2 Time",
                "PercentC3Time" => "% C3 Time",
                "InterruptsPersec" => "Interrupts/sec",
                "DPCsQueuedPersec" => "DPCs Queued/sec",
                "DPCRate" => "DPC Rate",
                "C1TransitionsPersec" => "C1 Transitions/sec",
                "C2TransitionsPersec" => "C2 Transitions/sec",
                "C3TransitionsPersec" => "C3 Transitions/sec",
                "PercentProcessorPerformance" => "% Processor Performance",
                "PercentProcessorUtility" => "% Processor Utility",
                "ProcessorFrequency" => "Processor Frequency",
                "PercentofMaximumFrequency" => "% of Maximum Frequency",
                _ => wmiProperty
            };
        }

        /// <summary>
        /// Queries WMIC for all instances of the Processor Information counter set.
        /// Returns a dictionary mapping instance names (e.g. "0,5", "_Total") to their metric values.
        /// </summary>
        public static Dictionary<string, Dictionary<string, float>> QueryAllInstances(string counterCategory)
        {
            var results = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
            string wmiClass = WindowsWmiPerformanceCounter.GetWmiClassName(counterCategory);
            if (wmiClass == null)
            {
                return results;
            }

            string output = WindowsWmiPerformanceCounter.RunWmic(
                $"path {wmiClass} get Name,PercentProcessorTime,PercentUserTime,PercentPrivilegedTime,PercentIdleTime,PercentDPCTime,PercentInterruptTime,PercentC1Time,PercentC2Time,PercentC3Time,InterruptsPersec,DPCsQueuedPersec,DPCRate,C1TransitionsPersec,C2TransitionsPersec,C3TransitionsPersec /format:csv");

            if (string.IsNullOrWhiteSpace(output))
            {
                return results;
            }

            string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
            {
                return results;
            }

            // Find header line (contains "Node," and "Name,")
            string[] headers = null;
            int dataStart = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Node,", StringComparison.OrdinalIgnoreCase) || line.Contains(",Name,"))
                {
                    headers = line.Split(',');
                    dataStart = i + 1;
                    break;
                }
            }

            if (headers == null)
            {
                return results;
            }

            int nameIndex = Array.FindIndex(headers, h => string.Equals(h.Trim(), "Name", StringComparison.OrdinalIgnoreCase));
            if (nameIndex < 0)
            {
                return results;
            }

            for (int i = dataStart; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                if (values.Length <= nameIndex)
                {
                    continue;
                }

                string instanceName = values[nameIndex].Trim();
                if (string.IsNullOrWhiteSpace(instanceName))
                {
                    continue;
                }

                var props = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                {
                    string header = headers[j].Trim();
                    if (j == nameIndex || string.Equals(header, "Node", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (float.TryParse(values[j].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
                    {
                        props[header] = val;
                    }
                }

                results[instanceName] = props;
            }

            return results;
        }

        /// <inheritdoc />
        protected override bool TryGetCounterValue(out float? counterValue)
        {
            counterValue = null;

            string output = WindowsWmiPerformanceCounter.RunWmic(
                $"path Win32_PerfFormattedData_Counters_ProcessorInformation WHERE \"Name='{this.InstanceName}'\" get {this.wmiPropertyName} /format:csv");

            if (string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Node,", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // CSV format: Node,PropertyValue
                string[] parts = line.Split(',');
                if (parts.Length >= 2 && float.TryParse(parts[parts.Length - 1].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float val))
                {
                    counterValue = val;
                    return true;
                }
            }

            return false;
        }

        private static string RunWmic(string arguments)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "wmic",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(30000);

                    return output;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
