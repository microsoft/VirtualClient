// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Atop output document (https://www.systutorials.com/docs/linux/man/1-pcp-atop/).
    /// </summary>
    public class AtopParser : MetricsParser
    {
        /// <summary>
        /// Regex expression for a cell in the ATOP table
        /// </summary>
        private static readonly string Cell = @"(?:\|[^\|]+)";

        /// <summary>
        /// Regex expression for pipe sign with optional write spaces
        /// </summary>
        private static readonly string Pipe = @"\s*\|\s*";

        private HashSet<string> countersToCapture;

        /// <summary>
        /// Constructor for <see cref="AtopParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="counters">An explicit list of counters to capture. When not defined, all counters are captured.</param>
        public AtopParser(string rawText, IEnumerable<string> counters = null)
            : base(rawText)
        {
            this.countersToCapture = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (counters?.Any() == true)
            {
                this.countersToCapture.AddRange(counters);
            }
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            // Remove the first ATOP sample as it contains the system and process activity since boot.
            // Note that particular counters could have reached their maximum value (several times) and started by zero again, so do not rely on these figures.
            // If there is only one ATOP sample available we should take it, as something is better than nothing.
            
            Regex withoutBootAtopResults = new Regex(@"(?:.*?\bATOP\b.*?\bATOP\b)(.*)", RegexOptions.Singleline);
            Match match = withoutBootAtopResults.Match(this.RawText);

            if (match.Success)
            {
                this.RawText = "ATOP" + match.Groups[1].Value;
            }

            int totalSamples = this.GetTotalSamples();
            List<Metric> metrics = new List<Metric>();
            IDictionary<string, MetricAggregate> aggregates = new Dictionary<string, MetricAggregate>(StringComparer.OrdinalIgnoreCase);

            this.AddCPUSummaryCounters(aggregates);
            this.AddIndividualCPUCounters(aggregates);
            this.AddCPULoadCounters(aggregates);
            this.AddMemorySummaryCounters(aggregates);
            this.AddSwapSpaceCounters(aggregates);
            this.AddPagingCounters(aggregates);
            this.AddDiskCounters(aggregates, totalSamples);
            this.AddNetworkSummaryCounters(aggregates);
            this.AddNetworkIndividualCounters(aggregates, totalSamples);

            if (aggregates.Any())
            {
                metrics.AddRange(aggregates.Values.Select(agg => agg.ToMetric()));
            }

            return metrics;
        }

        private void AddMetric(
            IDictionary<string, MetricAggregate> metrics, string metricName, double metricValue, string metricUnit = null, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
        {
            if (this.ShouldCapture(metricName))
            {
                if (!metrics.ContainsKey(metricName))
                {
                    metrics.Add(metricName, new MetricAggregate(metricName, metricUnit, aggregateType, description));
                }

                metrics[metricName].Add(metricValue);
            }
        }

        private void AddMetric(
            IDictionary<string, MetricAggregate> metrics, string metricName, string parsedValue, string metricUnit = null, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
        {
            if (this.ShouldCapture(metricName))
            {
                if (!string.IsNullOrEmpty(parsedValue) && double.TryParse(parsedValue, out double metricValue))
                {
                    this.AddMetric(metrics, metricName, metricValue, metricUnit, aggregateType, description);
                }
            }
        }

        private void AddMetric(
            IDictionary<string, MetricAggregate> metrics, string metricName, IEnumerable<string> parsedValues, string metricUnit = null, MetricAggregateType aggregateType = MetricAggregateType.Average, string description = null)
        {
            if (this.ShouldCapture(metricName))
            {
                if (parsedValues?.Any() == true)
                {
                    foreach (string parsedValue in parsedValues)
                    {
                        if (double.TryParse(parsedValue, out double metricValue))
                        {
                            this.AddMetric(metrics, metricName, metricValue, metricUnit, aggregateType, description);
                        }
                    }
                }
            }
        }

        private void AddCPUSummaryCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are processor counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Processor", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            // % System Time
            Regex cpuSysRegex = new Regex($@"CPU {Cell}{{0}}{Pipe}sys\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            MatchCollection matches = cpuSysRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\% System Time", matches.Select(m => m.Groups[1].Value));
                this.AddMetric(metrics, @"\Processor Information(_Total)\% System Time Min", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Min);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% System Time Max", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Max);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% System Time Median", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Median);
            }

            // % User Time
            Regex cpuUserRegex = new Regex($@"CPU {Cell}{{1}}{Pipe}user\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            matches = cpuUserRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\% User Time", matches.Select(m => m.Groups[1].Value));
                this.AddMetric(metrics, @"\Processor Information(_Total)\% User Time Min", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Min);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% User Time Max", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Max);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% User Time Median", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Median);
            }

            // % IRQ/Interrupt Request Time
            Regex cpuIrqRegex = new Regex($@"CPU {Cell}{{2}}{Pipe}irq\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            matches = cpuIrqRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IRQ Time", matches.Select(m => m.Groups[1].Value));
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IRQ Time Min", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Min);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IRQ Time Max", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Max);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IRQ Time Median", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Median);
            }

            // % Idle Time
            Regex cpuIdleRegex = new Regex($@"CPU {Cell}{{3}}{Pipe}idle\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            matches = cpuIdleRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\% Idle Time", matches.Select(m => m.Groups[1].Value));
                this.AddMetric(metrics, @"\Processor Information(_Total)\% Idle Time Min", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Min);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% Idle Time Max", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Max);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% Idle Time Median", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Median);
            }

            // % IOWait Time
            Regex cpuWaitRegex = new Regex($@"CPU {Cell}{{4}}{Pipe}wait\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            matches = cpuWaitRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IOWait Time", matches.Select(m => m.Groups[1].Value));
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IOWait Time Min", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Min);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IOWait Time Max", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Max);
                this.AddMetric(metrics, @"\Processor Information(_Total)\% IOWait Time Median", matches.Select(m => m.Groups[1].Value), aggregateType: MetricAggregateType.Median);
            }
        }

        private void AddIndividualCPUCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are processor counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Processor", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            // % System Time
            Regex cpuSysRegex = new Regex($@"cpu {Cell}{{0}}{Pipe}sys\s+({TextParsingExtensions.DoubleTypeRegex})% {Cell}{{3}}{Pipe}(\w+) w\s+\S+ \|", RegexOptions.Multiline);
            MatchCollection matches = cpuSysRegex.Matches(this.RawText);

            foreach (Match match in matches)
            {
                string instance = match.Groups.Values.Last().Value;
                this.AddMetric(metrics, $@"\Processor({instance})\% System Time", match.Groups[1].Value);
            }

            // % User Time
            Regex cpuUserRegex = new Regex($@"cpu {Cell}{{1}}{Pipe}user\s+({TextParsingExtensions.DoubleTypeRegex})% {Cell}{{2}}{Pipe}(\w+) w\s+\S+ \|", RegexOptions.Multiline);
            matches = cpuUserRegex.Matches(this.RawText);

            foreach (Match match in matches)
            {
                string instance = match.Groups.Values.Last().Value;
                this.AddMetric(metrics, $@"\Processor({instance})\% User Time", match.Groups[1].Value);
            }

            // % IRQ/Interrupt Request Time
            Regex cpuIrqRegex = new Regex($@"cpu {Cell}{{2}}{Pipe}irq\s+({TextParsingExtensions.DoubleTypeRegex})% {Cell}{{1}}{Pipe}(\w+) w\s+\S+ \|", RegexOptions.Multiline);
            matches = cpuIrqRegex.Matches(this.RawText);

            foreach (Match match in matches)
            {
                string instance = match.Groups.Values.Last().Value;
                this.AddMetric(metrics, $@"\Processor({instance})\% IRQ Time", match.Groups[1].Value);
            }

            // % Idle Time
            Regex cpuIdleRegex = new Regex($@"cpu {Cell}{{3}}{Pipe}idle\s+({TextParsingExtensions.DoubleTypeRegex})% {Cell}{{0}}{Pipe}(\w+) w\s+\S+ \|", RegexOptions.Multiline);
            matches = cpuIdleRegex.Matches(this.RawText);

            foreach (Match match in matches)
            {
                string instance = match.Groups.Values.Last().Value;
                this.AddMetric(metrics, $@"\Processor({instance})\% Idle Time", match.Groups[1].Value);
            }

            // % Wait Time
            Regex cpuWaitRegex = new Regex($@"cpu {Cell}{{4}}{Pipe} (\w+) w\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.Multiline);
            matches = cpuWaitRegex.Matches(this.RawText);

            foreach (Match match in matches)
            {
                string instance = match.Groups[1].Value;
                this.AddMetric(metrics, $@"\Processor({instance})\% IOWait Time", match.Groups[2].Value);
            }
        }

        private void AddCPULoadCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are processor counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Processor", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            Regex cplAvg1Regex = new Regex($@"CPL {Cell}{{0}}{Pipe}avg1\s+({TextParsingExtensions.DoubleTypeRegex})", RegexOptions.Multiline);
            MatchCollection matches = cplAvg1Regex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\Available Threads (Avg1)", matches.Select(m => m.Groups[1].Value));
            }

            Regex cplAvg5Regex = new Regex($@"CPL {Cell}{{1}}{Pipe}avg5\s+({TextParsingExtensions.DoubleTypeRegex})", RegexOptions.Multiline);
            matches = cplAvg5Regex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\Available Threads (Avg5)", matches.Select(m => m.Groups[1].Value));
            }

            Regex cplAvg15Regex = new Regex($@"CPL {Cell}{{2}}{Pipe}avg15\s+({TextParsingExtensions.DoubleTypeRegex})", RegexOptions.Multiline);
            matches = cplAvg15Regex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\Available Threads (Avg15)", matches.Select(m => m.Groups[1].Value));
            }

            Regex cplCswRegex = new Regex($@"CPL {Cell}{{3}}{Pipe}csw\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = cplCswRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\CSwitches", matches.Select(m => m.Groups[1].Value), null, MetricAggregateType.Sum);
            }

            Regex cplIntrRegex = new Regex($@"CPL {Cell}{{4}}{Pipe}intr\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = cplIntrRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                this.AddMetric(metrics, @"\Processor Information(_Total)\Serviced Interrupts", matches.Select(m => m.Groups[1].Value), null, MetricAggregateType.Sum);
            }
        }

        private void AddMemorySummaryCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are memory counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Memory", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            Regex memTotRegex = new Regex($@"MEM {Cell}{{0}}{Pipe}tot\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            MatchCollection matches = memTotRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Total Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex memFreeRegex = new Regex($@"MEM {Cell}{{1}}{Pipe}free\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = memFreeRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Free Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex memCacheRegex = new Regex($@"MEM {Cell}{{2}}{Pipe}cache\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = memCacheRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Cached Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex memBuffRegex = new Regex($@"MEM {Cell}{{3}}{Pipe}buff\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = memBuffRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Buffer Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex memSlabRegex = new Regex($@"MEM {Cell}{{4}}{Pipe}slab\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = memSlabRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Kernel Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }
        }

        private void AddSwapSpaceCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are memory counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Memory", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            Regex swpTotRegex = new Regex($@"SWP {Cell}{{0}}{Pipe}tot\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            MatchCollection matches = swpTotRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Total Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex swpFreeRegex = new Regex($@"SWP {Cell}{{1}}{Pipe}free\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = swpFreeRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Free Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex swpVmcomRegex = new Regex($@"SWP {Cell}{{3}}{Pipe}vmcom\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = swpVmcomRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Virtual Committed Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }

            Regex swpVmlimRegex = new Regex($@"SWP {Cell}{{4}}{Pipe}vmlim\s+({TextParsingExtensions.DoubleTypeRegex}[TGMK])", RegexOptions.Multiline);
            matches = swpVmlimRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Virtual Limit Bytes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value)));
            }
        }

        private void AddPagingCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are memory counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Memory", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            Regex pagScanRegex = new Regex($@"PAG {Cell}{{0}}{Pipe}scan\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            MatchCollection matches = pagScanRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Page Scans", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex pagStealRegex = new Regex($@"PAG {Cell}{{1}}{Pipe}steal\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = pagStealRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Page Steals", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex pagStallRegex = new Regex($@"PAG {Cell}{{2}}{Pipe}stall\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = pagStallRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Page Reclaims", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex pagSwinRegex = new Regex($@"PAG {Cell}{{3}}{Pipe}swin\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = pagSwinRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Reads", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex pagSwoutRegex = new Regex($@"PAG {Cell}{{4}}{Pipe}swout\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = pagSwoutRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Memory\Swap Space Writes", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }
        }

        private void AddDiskCounters(IDictionary<string, MetricAggregate> metrics, int totalSamples)
        {
            // If explicit counters are defined to capture and none are disk counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Disk", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            // List<string> diskCounters = new List<string>() { "LVM", "MDD", "DSK" };
            List<string> diskCounters = new List<string>() { "DSK" };

            foreach (string counter in diskCounters)
            {
                // % Busy Time
                Regex diskBusyRegex = new Regex($@"{counter}{Pipe}(\S+)\s*{Cell}{{0}}{Pipe}busy\s+({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.None);
                MatchCollection matches = diskBusyRegex.Matches(this.RawText);

                if (matches.Any())
                {
                    foreach (Match match in matches)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double metricValue))
                        {
                            string instance = match.Groups[1].Value;

                            // % Busy Time for a specific disk.
                            this.AddMetric(metrics, $@"\Disk({instance})\% Busy Time", metricValue);

                            // % Busy Time for system as a whole.
                            this.AddMetric(metrics, @"\Disk\Avg. % Busy Time", metricValue);
                        }
                    }
                }

                // # Reads
                Regex diskReadRegex = new Regex($@"{counter}{Pipe}(\S+)\s*{Cell}{{1}}{Pipe}read\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.None);
                matches = diskReadRegex.Matches(this.RawText);

                if (matches.Any())
                {
                    List<double> values = new List<double>();
                    foreach (Match match in matches)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double metricValue))
                        {
                            string instance = match.Groups[1].Value;

                            // # reads for a specific disk.
                            values.Add(metricValue);
                            this.AddMetric(metrics, $@"\Disk({instance})\# Reads", metricValue, null, MetricAggregateType.Sum);
                        }
                    }

                    if (values.Any())
                    {
                        // # reads for system as a whole. Disk reads are not cumulative in atop for each sample. They represent the number of
                        // disk reads during the sample range. To determine the overall # reads we have to get the sum for each of the samples.
                        this.AddMetric(metrics, @"\Disk\# Reads", values.Sum());
                    }
                }

                // # Writes
                Regex diskWriteRegex = new Regex($@"{counter}{Pipe}(\S+)\s*{Cell}{{2}}{Pipe}write\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.None);
                matches = diskWriteRegex.Matches(this.RawText);

                if (matches.Any())
                {
                    List<double> values = new List<double>();
                    foreach (Match match in matches)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double metricValue))
                        {
                            string instance = match.Groups[1].Value;

                            // # writes for a specific disk.
                            values.Add(metricValue);
                            this.AddMetric(metrics, $@"\Disk({instance})\# Writes", metricValue, null, MetricAggregateType.Sum);
                        }
                    }

                    if (values.Any())
                    {
                        // # writes for system as a whole. Disk writes are not cumulative in atop for each sample. They represent the number of
                        // disk writes during the sample range. To determine the overall # writes we have to get the sum for each of the samples.
                        this.AddMetric(metrics, @"\Disk\# Writes", values.Sum());
                    }
                }

                // Avg. Request Time
                Regex diskAvioRegex = new Regex($@"{counter}{Pipe}(\S+)\s*{Cell}{{3}}{Pipe}avio\s+({TextParsingExtensions.DoubleTypeRegex})\s*ms", RegexOptions.None);
                matches = diskAvioRegex.Matches(this.RawText);

                if (matches.Any())
                {
                    foreach (Match match in matches)
                    {
                        if (double.TryParse(match.Groups[2].Value, out double metricValue))
                        {
                            string instance = match.Groups[1].Value;

                            // # writes for a specific disk.
                            this.AddMetric(metrics, $@"\Disk({instance})\Avg. Request Time", metricValue, MetricUnit.Milliseconds);

                            // # writes for system as a whole.
                            this.AddMetric(metrics, @"\Disk\Avg. Request Time", metricValue, MetricUnit.Milliseconds);
                        }
                    }
                }
            }
        }

        private void AddNetworkSummaryCounters(IDictionary<string, MetricAggregate> metrics)
        {
            // If explicit counters are defined to capture and none are network counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Network", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            MatchCollection matches = null;
            Regex netTcpiRegex = new Regex($@"NET {Cell}{{1}}{Pipe}tcpi\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netTcpiRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\TCP Segments Received", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netTcpoRegex = new Regex($@"NET {Cell}{{2}}{Pipe}tcpo\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netTcpoRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\TCP Segments Transmitted", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netUdpiRegex = new Regex($@"NET {Cell}{{3}}{Pipe}udpi\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netUdpiRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\UDP Segments Received", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netUdpoRegex = new Regex($@"NET {Cell}{{4}}{Pipe}udpo\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netUdpoRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\UDP Segments Transmitted", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netIpiRegex = new Regex($@"NET {Cell}{{1}}{Pipe}ipi\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netIpiRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\IP Datagrams Received", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netIpoRegex = new Regex($@"NET {Cell}{{2}}{Pipe}ipo\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netIpoRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\IP Datagrams Transmitted", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netIpfrwRegex = new Regex($@"NET {Cell}{{3}}{Pipe}ipfrw\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netIpfrwRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\IP Datagrams Forwarded", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }

            Regex netDelivRegex = new Regex($@"NET {Cell}{{4}}{Pipe}deliv\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.Multiline);
            matches = netDelivRegex.Matches(this.RawText);

            if (matches?.Any() == true)
            {
                matches.ToList().ForEach(m => this.AddMetric(metrics, @"\Network\IP Datagrams Delivered", TextParsingExtensions.TranslateByteUnit(m.Groups[1].Value), null, MetricAggregateType.Sum));
            }
        }

        private void AddNetworkIndividualCounters(IDictionary<string, MetricAggregate> metrics, int totalSamples)
        {
            // If explicit counters are defined to capture and none are network counters, avoid executing any of the
            // regular expressions.
            if (this.countersToCapture.Any() && this.countersToCapture.Any(c => c.StartsWith(@"\Network", StringComparison.OrdinalIgnoreCase)) != true)
            {
                return;
            }

            Regex netUsageRegex = new Regex($@"NET {Pipe}(\w+)\s*{Cell}{{0}}({TextParsingExtensions.DoubleTypeRegex})%", RegexOptions.None);
            MatchCollection matches = netUsageRegex.Matches(this.RawText);

            if (matches.Any())
            {
                foreach (Match match in matches)
                {
                    // Filter out the loopback interface.
                    if (match.Value.Contains("----"))
                    {
                        continue;
                    }

                    if (double.TryParse(match.Groups[2].Value, out double metricValue))
                    {
                        string instance = match.Groups[1].Value;

                        // % usage for a specific device.
                        this.AddMetric(metrics, $@"\Network({instance})\% Usage", metricValue);

                        // % usage for system as a whole.
                        this.AddMetric(metrics, @"\Network\Avg. % Usage", metricValue);
                    }
                }
            }

            // The \S* here is to eliminate the "X%" from the ethernet or the "----" from the local net.
            Regex netPckiRegex = new Regex($@"NET {Pipe}(\w+)\s*\S*\s*{Cell}{{0}}{Pipe}pcki\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.None);
            matches = netPckiRegex.Matches(this.RawText);

            if (matches.Any())
            {
                List<double> values = new List<double>();
                foreach (Match match in matches)
                {
                    // Filter out the loopback interface.
                    if (match.Value.Contains("----"))
                    {
                        continue;
                    }

                    if (double.TryParse(match.Groups[2].Value, out double metricValue))
                    {
                        // packets received for a specific device.
                        string instance = match.Groups[1].Value;
                        values.Add(metricValue);

                        this.AddMetric(metrics, $@"\Network({instance})\Packets Received", metricValue, null, MetricAggregateType.Sum);
                    }
                }

                if (values.Any())
                {
                    // packets received for system as a whole. Packets received is not cumulative in atop for each sample. They represent the number of
                    // packets received during the sample range. To determine the overall packages received we have to get the sum for each of the samples
                    // and divide by the number of distinct sample intervals (e.g. 1 second sample rate over 60 minutes = 60 distinct sample intervals).
                    this.AddMetric(metrics, @"\Network\Packets Received", values.Sum());
                }
            }

            Regex netPckoRegex = new Regex($@"NET {Pipe}(\w+)\s*\S*\s*{Cell}{{1}}{Pipe}pcko\s+({TextParsingExtensions.ScientificNotationRegex})", RegexOptions.None);
            matches = netPckoRegex.Matches(this.RawText);

            if (matches.Any())
            {
                List<double> values = new List<double>();
                foreach (Match match in matches)
                {
                    // Filter out the loopback interface.
                    if (match.Value.Contains("----"))
                    {
                        continue;
                    }

                    if (double.TryParse(match.Groups[2].Value, out double metricValue))
                    {
                        // packets transmitted for a specific device.
                        string instance = match.Groups[1].Value;
                        values.Add(metricValue);

                        this.AddMetric(metrics, $@"\Network({instance})\Packets Transmitted", metricValue, null, MetricAggregateType.Sum);
                    }
                }

                if (values.Any())
                {
                    // packets transmitted for system as a whole. Packets transmitted is not cumulative in atop for each sample. They represent the number of
                    // packets transmitted during the sample range. To determine the overall packages transmitted we have to get the sum for each of the samples
                    // and divide by the number of distinct sample intervals (e.g. 1 second sample rate over 60 minutes = 60 distinct sample intervals).
                    this.AddMetric(metrics, @"\Network\Packets Transmitted", values.Sum());
                }
            }

            Regex netSiRegex = new Regex($@"NET {Pipe}(\w+)\s*\S*\s*{Cell}{{2}}{Pipe}si\s+({TextParsingExtensions.DoubleTypeRegex})\s*Kbps", RegexOptions.None);
            matches = netSiRegex.Matches(this.RawText);

            if (matches.Any())
            {
                foreach (Match match in matches)
                {
                    // Filter out the loopback interface.
                    if (match.Value.Contains("----"))
                    {
                        continue;
                    }

                    if (double.TryParse(match.Groups[2].Value, out double metricValue))
                    {
                        string instance = match.Groups[1].Value;

                        // kilobytes/sec received for a specific device.
                        this.AddMetric(metrics, $@"\Network({instance})\KB/sec Received", metricValue);

                        // kilobytes/sec received for system as a whole.
                        this.AddMetric(metrics, @"\Network\Avg. KB/sec Received", metricValue);
                    }
                }
            }

            Regex netSoRegex = new Regex($@"NET {Pipe}(\w+)\s*\S*\s*{Cell}{{3}}{Pipe}so\s+({TextParsingExtensions.DoubleTypeRegex})\s*Kbps", RegexOptions.None);
            matches = netSoRegex.Matches(this.RawText);

            if (matches.Any())
            {
                foreach (Match match in matches)
                {
                    // Filter out the loopback interface.
                    if (match.Value.Contains("----"))
                    {
                        continue;
                    }

                    if (double.TryParse(match.Groups[2].Value, out double metricValue))
                    {
                        string instance = match.Groups[1].Value;

                        // kilobytes/sec transmitted for a specific device.
                        this.AddMetric(metrics, $@"\Network({instance})\KB/sec Transmitted", metricValue);

                        // kilobytes/sec transmitted for system as a whole.
                        this.AddMetric(metrics, @"\Network\Avg. KB/sec Transmitted", metricValue);
                    }
                }
            }
        }

        private int GetTotalSamples()
        {
            MatchCollection matches = Regex.Matches(this.RawText, "ATOP", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return matches.Count;
        }

        private bool ShouldCapture(string counterName)
        {
            return !this.countersToCapture.Any() ? true : this.countersToCapture.Contains(counterName);
        }
    }
}
