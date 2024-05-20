// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using YamlDotNet.Core.Tokens;

    /// <summary>
    /// Extensions for parsing test documents.
    /// </summary>
    public static class TextParsingExtensions
    {
        /// <summary>
        /// Regex expression for capturing version with three or four parts.
        /// Valid: 1.2.3, 0111.01.10.015
        /// Invalid: 1.2, 1.2., .2.3
        /// </summary>
        public static readonly string VersionRegex = @"((?:\d+\.){2,3}(?:\d+))";

        /// <summary>
        /// Regex expression for capturing directory file.
        /// Invalid directory signs: \ / : * ? " and greater/smaller sign (can't put them here without xml complaining).
        /// Valid: /abc.exe,  abc/, /abc, /a/b/c, /a/b/c/, a/b/c
        /// Invalid: a, a.exe
        /// </summary>
        public static readonly string DirectoryRegex = @"((?:[^/\\:\*\?""<>|]*\/)*[^/\\:\*\?""<>|]*)";

        /// <summary>
        /// Regex expression for capturing double.
        /// Valid: -123, -1.5, -0.01, -0, 0, 0.01, 1, 2.5, 123.123, 321
        /// Invalid: 01, 1.1.1
        /// </summary>
        public static readonly string DoubleTypeRegex = @"(-?(?:0|(?:[1-9]\d*))(?:\.\d+)?)";

        /// <summary>
        /// Regex expression for capturing scientific notations. Normal double without the notation is also supported..
        /// Valid: 143221e4, 143221.1e4, other normal double.
        /// Invalid: 143221.e4
        /// </summary>
        public static readonly string ScientificNotationRegex = @"(((-?)(0|([1-9]\d*))(\.\d+)?)([Ee][+-]?[0-9]+)?)";

        /// <summary>
        /// Regex expression for capturing GUIDS.
        /// Valid: 324DFKWJFNSFie-nfjeKFNKLJNF332-enfeljkwenf3, 08f12e24-629b-45f7-a1a3-7e843b6d1840
        /// Invalid: abC3-1dD3, lp@gx-45d1E-apdD2
        /// </summary>
        public static readonly string GUIDRegex = @"(?i)[{(]?[0-9A-Z]+[-]+(?:[0-9A-Z]+[-]+)+[0-9A-Z]+[)}]?";

        /// <summary>
        /// Regex expression for capturing emails.
        /// Valid: johndoe@gmail.com, john3.doe@gmail.com, john.joe53@apple.co.id
        /// Invalid: johndoe_at_gmail.com, jogndoe@gmail
        /// </summary>
        public static readonly string EmailRegex = @"[\w\-\.]+@([\w -]+\.)+[\w-]{2,}";

        /// <summary>
        /// Remove rows that matches the regex.
        /// </summary>
        /// <param name="text">Raw text.</param>
        /// <param name="delimiter">Regex for the rows that should be removed.</param>
        public static string RemoveRows(string text, Regex delimiter)
        {
            List<string> result = new List<string>();
            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.None).ToList();

            foreach (string row in rows)
            {
                if (!Regex.IsMatch(row, delimiter.ToString(), delimiter.Options))
                {
                    result.Add(row);
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// Sectionize raw text into sections based on regex. First line of each section will become section key!
        /// </summary>
        /// <param name="text">Raw text.</param>
        /// <param name="delimiter">Regex for the section Delimiter.</param>
        public static IDictionary<string, string> Sectionize(string text, Regex delimiter)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            string[] sections = Regex.Split(text, delimiter.ToString(), delimiter.Options);

            foreach (string section in sections)
            {
                if (!string.IsNullOrWhiteSpace(section))
                {
                    List<string> rows = section.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
                    string sectionName = rows.FirstOrDefault().Trim();
                    rows.RemoveAt(0);

                    result.Add(sectionName, string.Join(Environment.NewLine, rows.Select(r => r.Trim())));
                }
            }

            return result;
        }

        /// <summary>
        /// Sectionize raw text into sections based on regex. First line of each section will become section key!
        /// </summary>
        /// <param name="text">Raw text.</param>
        public static IDictionary<string, IConvertible> ParseVcDelimiteredParameters(string text)
        {
            IDictionary<string, IConvertible> delimitedValues = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if (text.Contains(",,,"))
            {
                // If the list contains three comma",,,", use this as delimeter
                string[] delimitedProperties = text.Split(",,,", StringSplitOptions.RemoveEmptyEntries);

                if (delimitedProperties?.Any() == true)
                {
                    foreach (string property in delimitedProperties)
                    {
                        if (property.Contains("=", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string key = property.Substring(0, property.IndexOf("=", StringComparison.Ordinal));
                            string value = property.Substring(property.IndexOf("=", StringComparison.Ordinal) + 1);
                            delimitedValues[key.Trim()] = value.Trim();
                        }
                    }
                }
            }
            else
            {
                string[] segments = text.Split('=', StringSplitOptions.TrimEntries);
                // Only start at second segment and end at second to last segment
                // Because first segment is the key for first pair, and last segment is the value for last pair.
                string key = segments[0];
                for (int i = 1; i < segments.Length - 1; i++)
                {
                    // This is just to 
                    int lastCommaIndex = segments[i].LastIndexOf(",,,");
                    int lastSemicolonIndex = segments[i].LastIndexOf(';');
                    int splitIndex = Math.Max(lastCommaIndex, lastSemicolonIndex);

                    string value = segments[i].Substring(0, splitIndex);
                    delimitedValues.Add(key, value);
                    key = segments[i].Substring(splitIndex).Trim(';').Trim(',');
                }

                delimitedValues.Add(key, segments[segments.Length - 1]);
            }

            return delimitedValues;
        }

        /// <summary>
        /// Translate the unit of time to second. Example: 1m->60 and 1h->3,600
        /// </summary>
        /// <param name="text">Original text.</param>
        public static string TranslateToSecondUnit(string text)
        {
            if (text.Trim().StartsWith('-'))
            {
                throw new NotSupportedException($"{nameof(TranslateToSecondUnit)} does not support negative time.");
            }

            // Unit: h, hr, hrs, hour, hours
            Regex hourRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(h|hr|hrs|hours|hour)$", RegexOptions.IgnoreCase);
            Match hourMatch = Regex.Match(text, hourRegex.ToString(), hourRegex.Options);
            if (hourMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(hourMatch.Groups[1].Value) * (60 * 60));
            }

            // Unit: ms, millisecond, milliseconds
            Regex millisecondRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(ms|milliseconds|millisecond)", RegexOptions.IgnoreCase);
            Match millisecondMatch = Regex.Match(text, millisecondRegex.ToString(), millisecondRegex.Options);
            if (millisecondMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(millisecondMatch.Groups[1].Value) * Math.Pow(10, -3));
            }

            // Unit: us, microsecond, microseconds
            Regex microsecondsRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(us|microseconds|microsecond)", RegexOptions.IgnoreCase);
            Match microsecondsMatch = Regex.Match(text, microsecondsRegex.ToString(), microsecondsRegex.Options);
            if (microsecondsMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(microsecondsMatch.Groups[1].Value) * Math.Pow(10, -6));
            }

            // Unit: ns, nanosecond, nanoseconds
            Regex nanosecondsRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(ns|nanoseconds|nanosecond)", RegexOptions.IgnoreCase);
            Match nanosecondMatch = Regex.Match(text, nanosecondsRegex.ToString(), nanosecondsRegex.Options);
            if (nanosecondMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(nanosecondMatch.Groups[1].Value) * Math.Pow(10, -9));
            }

            // Unit: m, minute, minutes
            Regex minuteRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(m|minutes|minute)", RegexOptions.IgnoreCase);
            Match minuteMatch = Regex.Match(text, minuteRegex.ToString(), minuteRegex.Options);
            if (minuteMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(minuteMatch.Groups[1].Value) * 60);
            }

            // Unit: s, second, seconds
            Regex secondsRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(s|seconds|second)", RegexOptions.IgnoreCase);
            Match secondsMatch = Regex.Match(text, secondsRegex.ToString(), secondsRegex.Options);
            if (secondsMatch.Success)
            {
                return Convert.ToString(Convert.ToDouble(secondsMatch.Groups[1].Value) * 1);
            }

            return text;
        }

        /// <summary>
        /// Translate time by unit provided.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="metricUnit">time unit for eg seconds,milliseconds, etc.</param>
        public static string TranslateTimeByUnit(string text, string metricUnit)
        {
            double secondUnitTime = Convert.ToDouble(TranslateToSecondUnit(text));

            switch (metricUnit)
            {
                case MetricUnit.Seconds:
                    return Convert.ToString(secondUnitTime);

                case MetricUnit.Milliseconds:
                    return Convert.ToString(secondUnitTime * Math.Pow(10, 3));

                case MetricUnit.Microseconds:
                    return Convert.ToString(secondUnitTime * Math.Pow(10, 6));

                case MetricUnit.Nanoseconds:
                    return Convert.ToString(secondUnitTime / Math.Pow(10, 9));

                case MetricUnit.Minutes:
                    return Convert.ToString(secondUnitTime / 60);

                default:
                    throw new NotSupportedException($"Metric unit: {metricUnit} is not supported. Metric units supported {MetricUnit.Seconds}, " +
                        $"{MetricUnit.Milliseconds}, {MetricUnit.Microseconds}, {MetricUnit.Nanoseconds}, {MetricUnit.Minutes}.");
            }
        }

        /// <summary>
        /// Translate the unit of number in a text. Example: 1K->1000 and 1M->1000000.
        /// </summary>
        /// <param name="text">Original text.</param>
        public static string TranslateNumericUnit(string text)
        {
            // Unit thousand: K, k
            Regex thousandRegex = new Regex("((?:[0-9]*[.])?[0-9]*)[Kk]$", RegexOptions.IgnoreCase);
            Match thousandMatch = Regex.Match(text, thousandRegex.ToString(), thousandRegex.Options);
            if (thousandMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(thousandMatch.Groups[1].Value) * 1000);
            }

            // Unit million: M,m
            Regex millionRegex = new Regex("((?:[0-9]*[.])?[0-9]*)[mM]$", RegexOptions.IgnoreCase);
            Match millionMatch = Regex.Match(text, millionRegex.ToString(), millionRegex.Options);
            if (millionMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(millionMatch.Groups[1].Value) * 1000000);
            }

            return text;
        }

        /// <summary>
        /// Translate the unit of number in a text. Example: 1K->1024 and 1M->1,048,576.
        /// </summary>
        /// <param name="text">Original text.</param>
        public static string TranslateByteUnit(string text)
        {
            // Unit thousand: K, k, kb, KB, KiB
            Regex thousandRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(k|kb|kib)$", RegexOptions.IgnoreCase);
            Match thousandMatch = Regex.Match(text, thousandRegex.ToString(), thousandRegex.Options);
            if (thousandMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(thousandMatch.Groups[1].Value) * 1024);
            }

            // Unit million: M, m, MB, mb, MiB
            Regex millionRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(m|mb|mib)$", RegexOptions.IgnoreCase);
            Match millionMatch = Regex.Match(text, millionRegex.ToString(), millionRegex.Options);
            if (millionMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(millionMatch.Groups[1].Value) * 1024 * 1024);
            }

            // Unit giga: G, g, GB, gb, GiB
            Regex gigaRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(g|gb|gib)", RegexOptions.IgnoreCase);
            Match gigaMatch = Regex.Match(text, gigaRegex.ToString(), gigaRegex.Options);
            if (gigaMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(gigaMatch.Groups[1].Value) * 1024 * 1024 * 1024);
            }

            // Unit tera: T, t, TB, tb, TiB
            Regex teraRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(t|tb|tib)", RegexOptions.IgnoreCase);
            Match teraMatch = Regex.Match(text, teraRegex.ToString(), teraRegex.Options);
            if (teraMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(teraMatch.Groups[1].Value) * 1024 * 1024 * 1024 * 1024);
            }

            // Unit peta: P, p, PB, pb, PiB
            Regex petaRegex = new Regex(@"((?:[0-9]*[.])?[0-9]*)\s?(p|pb|pib)", RegexOptions.IgnoreCase);
            Match petaMatch = Regex.Match(text, petaRegex.ToString(), petaRegex.Options);
            if (petaMatch.Success)
            {
                text = Convert.ToString(Convert.ToDouble(petaMatch.Groups[1].Value) * 1024 * 1024 * 1024 * 1024 * 1024);
            }

            return text;
        }

        /// <summary>
        /// Translate storage by unit provided.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="metricUnit">storage unit for eg bytes,kilobytes,megabytes,etc.</param>
        public static string TranslateStorageByUnit(string text, string metricUnit)
        {
            double byteUnitStorage = Convert.ToDouble(TranslateByteUnit(text));
            string result = null;

            switch (metricUnit)
            {
                case MetricUnit.Bytes:
                    result = Convert.ToString(byteUnitStorage);
                    break;

                case MetricUnit.Kilobytes:
                    result = Convert.ToString(byteUnitStorage / Math.Pow(1024, 1));
                    break;

                case MetricUnit.Megabytes:
                    result = Convert.ToString(byteUnitStorage / Math.Pow(1024, 2));
                    break;

                case MetricUnit.Gigabytes:
                    result = Convert.ToString(byteUnitStorage / Math.Pow(1024, 3));
                    break;

                case MetricUnit.Terabytes:
                    result = Convert.ToString(byteUnitStorage / Math.Pow(1024, 4));
                    break;

                case MetricUnit.Petabytes:
                    result = Convert.ToString(byteUnitStorage / Math.Pow(1024, 5));
                    break;

                default:
                    throw new NotSupportedException($"Metric unit: {metricUnit} is not supported. Metric units supported {MetricUnit.Bytes}, " +
                        $"{MetricUnit.Kilobytes}, {MetricUnit.Megabytes}, {MetricUnit.Gigabytes}, {MetricUnit.Terabytes}, {MetricUnit.Petabytes}.");
            }

            return result;
        }

        /// <summary>
        /// Sectionize raw text into sections based on regex matches. Rows that matches any regex will be assigned to that corresponding section.
        /// </summary>
        /// <param name="text">Raw input text</param>
        /// <param name="matchingRegexes">Key is the name of the section, value is the regex to match in order to be put in the section.</param>
        public static IDictionary<string, string> Sectionize(string text, IDictionary<string, Regex> matchingRegexes)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (KeyValuePair<string, Regex> section in matchingRegexes)
            {
                List<string> matchRows = new List<string>();
                foreach (string row in rows)
                {
                    if (Regex.IsMatch(row, section.Value.ToString(), section.Value.Options))
                    {
                        matchRows.Add(row.Trim());
                    }
                }

                result.Add(section.Key, string.Join(Environment.NewLine, matchRows));
            }

            return result;
        }

        /// <summary>
        /// Parse metric with regex, and then use group to identify names, unit and value.
        /// </summary>
        /// <param name="text">Raw text</param>
        /// <param name="capturingRegex">The capturing regex.</param>
        /// <param name="valueGroup">Regex group of the value.</param>
        /// <param name="value">Override the value, if regex group of value is not provided.</param>
        /// <param name="nameGroup">Regex group of the metric name.</param>
        /// <param name="name">Override the name, if regex group of name is not provided.</param>
        /// <param name="unitGroup">Regex group of the unit.</param>
        /// <param name="unit">Override the unit, if regex group of unit is not provided.</param>
        /// <returns>Parsed Metrics from raw text using Regex.</returns>
        public static IList<Metric> ParseMetricsByRegexCaptureGroups(
            string text, Regex capturingRegex, int valueGroup = -1, double value = double.NaN, int nameGroup = -1, string name = null, int unitGroup = -1, string unit = null)
        {
            IList<Metric> result = new List<Metric>();
            List<string> rows = text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string row in rows)
            {
                Match match = Regex.Match(row, capturingRegex.ToString(), capturingRegex.Options);
                if (match.Success)
                {
                    if (double.IsNaN(value) && valueGroup >= 0)
                    {
                        value = Convert.ToDouble(match.Groups[valueGroup].Value);
                    }

                    if (unit == null && unitGroup >= 0)
                    {
                        unit = match.Groups[unitGroup].Value;
                    }

                    if (name == null && nameGroup >= 0)
                    {
                        name = match.Groups[nameGroup].Value;
                    }

                    result.Add(new Metric(name, value, unit));
                }
            }

            return result;
        }
    }
}
