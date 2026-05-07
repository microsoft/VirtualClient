// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.Management.Infrastructure;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides performance counter data via WMI (CIM) when the legacy
    /// <see cref="System.Diagnostics.PerformanceCounter"/> API fails on systems with
    /// more than 64 logical processors.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="CimSession"/> to query Win32_PerfFormattedData_* WMI classes.
    /// This supports multi-processor groups and returns all CPU cores with "Group,Core"
    /// instance naming. Unlike wmic.exe (deprecated), CimSession is in-process and
    /// supported on all modern Windows versions.
    /// </remarks>
    public class WindowsWmiPerformanceCounter : WindowsPerformanceCounter
    {
        private static readonly string CimNamespace = "root/cimv2";

        /// <summary>
        /// WMI system/metadata properties to exclude from counter results.
        /// </summary>
        private static readonly HashSet<string> ExcludedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Name", "Caption", "Description", "Frequency_Object", "Frequency_PerfTime",
            "Frequency_Sys100NS", "Timestamp_Object", "Timestamp_PerfTime", "Timestamp_Sys100NS"
        };

        /// <summary>
        /// Maps performance counter category names to their corresponding WMI class names.
        /// </summary>
        private static readonly Dictionary<string, string> WmiClassMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Processor"] = "Win32_PerfFormattedData_Counters_ProcessorInformation",
            ["Processor Information"] = "Win32_PerfFormattedData_Counters_ProcessorInformation",
            ["Memory"] = "Win32_PerfFormattedData_PerfOS_Memory",
            ["PhysicalDisk"] = "Win32_PerfFormattedData_PerfDisk_PhysicalDisk",
            ["System"] = "Win32_PerfFormattedData_PerfOS_System",
            ["IPv4"] = "Win32_PerfFormattedData_Tcpip_IPv4",
            ["Hyper-V Hypervisor Logical Processor"] = "Win32_PerfFormattedData_HvStats_HyperVHypervisorLogicalProcessor",
            ["Hyper-V Hypervisor Root Virtual Processor"] = "Win32_PerfFormattedData_HvStats_HyperVHypervisorRootVirtualProcessor",
            ["Hyper-V Hypervisor Virtual Processor"] = "Win32_PerfFormattedData_HvStats_HyperVHypervisorVirtualProcessor"
        };

        /// <summary>
        /// Per-category cache of WMI query results. When the first counter in a category calls
        /// TryGetCounterValue, it queries all instances for the category in a single SELECT * and
        /// populates the cache. Subsequent counters in the same category read from cache.
        /// Cache expires after <see cref="CacheTtl"/> to ensure fresh data each capture cycle.
        /// </summary>
        private static readonly ConcurrentDictionary<string, CategoryCache> CategoryCacheMap
            = new ConcurrentDictionary<string, CategoryCache>(StringComparer.OrdinalIgnoreCase);

        private static readonly TimeSpan CacheTtl = TimeSpan.FromMilliseconds(800);

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
            if (counterCategory != null && WmiClassMap.TryGetValue(counterCategory.Trim(), out string wmiClass))
            {
                return wmiClass;
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
                _ => WindowsWmiPerformanceCounter.PascalCaseToCounterName(wmiProperty)
            };
        }

        /// <summary>
        /// Queries WMI via CimSession for all instances of a counter category.
        /// Returns a dictionary mapping instance names to their numeric property values.
        /// For singleton categories (e.g. Memory, System, IPv4) that have no Name property,
        /// a single empty-string key is used.
        /// </summary>
        public static Dictionary<string, Dictionary<string, float>> QueryAllInstances(string counterCategory)
        {
            var results = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
            string wmiClass = WindowsWmiPerformanceCounter.GetWmiClassName(counterCategory);
            if (wmiClass == null)
            {
                return results;
            }

            using (CimSession session = CimSession.Create("localhost"))
            {
                foreach (CimInstance instance in session.QueryInstances(CimNamespace, "WQL", $"SELECT * FROM {wmiClass}"))
                {
                    using (instance)
                    {
                        // Multi-instance categories (Processor, PhysicalDisk, Hyper-V) have a Name property.
                        // Singleton categories (Memory, System, IPv4) do not.
                        string instanceName = instance.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;

                        var props = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                        foreach (CimProperty prop in instance.CimInstanceProperties)
                        {
                            if (ExcludedProperties.Contains(prop.Name))
                            {
                                continue;
                            }

                            if (prop.Value != null
                                && float.TryParse(prop.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float numVal))
                            {
                                props[prop.Name] = numVal;
                            }
                        }

                        if (props.Count > 0)
                        {
                            results[instanceName] = props;
                        }
                    }
                }
            }

            return results;
        }

        /// <inheritdoc />
        protected override bool TryGetCounterValue(out float? counterValue)
        {
            counterValue = null;

            // Use per-category cache to batch WMI queries. One SELECT * per category
            // instead of one query per counter drastically reduces overhead
            // (e.g. 7 queries instead of 1188 on a 132-LP system).
            var cache = WindowsWmiPerformanceCounter.GetOrRefreshCache(this.Category);
            if (cache == null)
            {
                return false;
            }

            string instanceKey = this.InstanceName ?? string.Empty;
            if (cache.Data.TryGetValue(instanceKey, out var props)
                && props.TryGetValue(this.wmiPropertyName, out float val))
            {
                counterValue = val;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a PascalCase WMI property name to a space-separated performance counter name.
        /// Handles common WMI suffixes: "Persec" → "/sec", "Percent" prefix → "% ".
        /// Examples: "AvailableBytes" → "Available Bytes", "PageFaultsPersec" → "Page Faults/sec",
        /// "PercentGuestRunTime" → "% Guest Run Time".
        /// </summary>
        private static string PascalCaseToCounterName(string wmiProperty)
        {
            if (string.IsNullOrWhiteSpace(wmiProperty))
            {
                return wmiProperty;
            }

            // Handle "Persec" suffix → "/sec"
            string working = wmiProperty;
            bool hasPersec = working.EndsWith("Persec", StringComparison.Ordinal);
            if (hasPersec)
            {
                working = working.Substring(0, working.Length - 6);
            }

            // Handle "Percent" prefix → "% "
            bool hasPercent = working.StartsWith("Percent", StringComparison.Ordinal);
            if (hasPercent)
            {
                working = working.Substring(7);
            }

            // Insert spaces before uppercase letters (but not consecutive ones or first char)
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < working.Length; i++)
            {
                if (i > 0 && char.IsUpper(working[i]) && !char.IsUpper(working[i - 1]))
                {
                    result.Append(' ');
                }

                result.Append(working[i]);
            }

            string name = result.ToString();

            if (hasPercent)
            {
                name = "% " + name;
            }

            if (hasPersec)
            {
                name += "/sec";
            }

            return name;
        }

        /// <summary>
        /// Returns a cached WMI query result for the category, refreshing if expired.
        /// </summary>
        private static CategoryCache GetOrRefreshCache(string category)
        {
            string wmiClass = WindowsWmiPerformanceCounter.GetWmiClassName(category);
            if (wmiClass == null)
            {
                return null;
            }

            if (CategoryCacheMap.TryGetValue(category, out var existing) && !existing.IsExpired)
            {
                return existing;
            }

            try
            {
                var data = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase);
                using (CimSession session = CimSession.Create("localhost"))
                {
                    foreach (CimInstance instance in session.QueryInstances(CimNamespace, "WQL", $"SELECT * FROM {wmiClass}"))
                    {
                        using (instance)
                        {
                            string instanceName = instance.CimInstanceProperties["Name"]?.Value?.ToString() ?? string.Empty;
                            var props = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

                            foreach (CimProperty prop in instance.CimInstanceProperties)
                            {
                                if (ExcludedProperties.Contains(prop.Name))
                                {
                                    continue;
                                }

                                if (prop.Value != null
                                    && float.TryParse(prop.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out float numVal))
                                {
                                    props[prop.Name] = numVal;
                                }
                            }

                            if (props.Count > 0)
                            {
                                data[instanceName] = props;
                            }
                        }
                    }
                }

                var cache = new CategoryCache(data);
                CategoryCacheMap[category] = cache;
                return cache;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Holds cached WMI query results for a single counter category with a time-based expiry.
        /// </summary>
        private sealed class CategoryCache
        {
            private readonly DateTime createdUtc;

            public CategoryCache(Dictionary<string, Dictionary<string, float>> data)
            {
                this.Data = data;
                this.createdUtc = DateTime.UtcNow;
            }

            public Dictionary<string, Dictionary<string, float>> Data { get; }

            public bool IsExpired => DateTime.UtcNow - this.createdUtc > CacheTtl;
        }
    }
}
