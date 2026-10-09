// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;

    [TestFixture]
    [Category("Unit")]
    public class IpmiUtilSelMetricsParserTests : MockFixture
    {
        private string examplesDirectory;
        private string exampleRawResults;
        private string exampleDecodedResults;

        [SetUp]
        public void SetupTest()
        {
            this.examplesDirectory = MockFixture.GetDirectory(typeof(IpmiUtilSelMetricsParserTests), "test_examples", "ipmiutil");
            this.exampleRawResults = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "ipmiutil_sel_example_1.txt"));
            this.exampleDecodedResults = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "ipmiutil_sel_decoded_example_1.txt"));
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat()
        {
            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);

            string[] lines = Regex.Split(combinedResults, "\r?\n");

            Assert.IsNotNull(lines);
            Assert.AreEqual(238, lines.Length);

            // Expectation:
            // Every record will have the expected column value. Additionally the record ID of the
            // decoded data should match the record ID of the raw detail/data (e.g. 0001 == 01 00).
            // 
            // e.g. 
            // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail | Evt_raw_detail
            // 0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff
            foreach (var line in lines)
            {
                Match match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
                if (match.Success)
                {
                    Assert.IsTrue(match.Groups.TryGetValue("RecordId", out Group recordId));
                    Assert.IsTrue(match.Groups.TryGetValue("Timestamp", out Group timestamp));
                    Assert.IsTrue(match.Groups.TryGetValue("Severity", out Group severity));
                    Assert.IsTrue(match.Groups.TryGetValue("Source", out Group source));
                    Assert.IsTrue(match.Groups.TryGetValue("EventType", out Group eventType));
                    Assert.IsTrue(match.Groups.TryGetValue("Sensor", out Group sensor));
                    Assert.IsTrue(match.Groups.TryGetValue("EventDetail", out Group eventDetail));
                    Assert.IsTrue(match.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail));

                    string decodedRecordId = recordId.Value;
                    string rawRecordId = string.Join(string.Empty, eventRawDetail.Value.Substring(0, 5).Split(" ").Reverse()); // 01 00 -> 0001

                    Assert.AreEqual(decodedRecordId, rawRecordId);
                }
            }
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat_WhenRawResultsAreNotProvided()
        {
            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(null, this.exampleDecodedResults);

            Assert.AreEqual(this.exampleDecodedResults, combinedResults);
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat_Record_Order_Does_Not_Matter_1()
        {
            string[] rawResults = Regex.Split(this.exampleRawResults, @"\r?\n");

            // Setup:
            // The raw results are in reverse order from the decoded results.
            List<string> reversedRecords = new List<string>(rawResults.Take(4));
            reversedRecords.AddRange(rawResults.Skip(4).Reverse());
            this.exampleRawResults = string.Join(Environment.NewLine, reversedRecords);

            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);
            string[] lines = Regex.Split(combinedResults, "\r?\n");

            Assert.IsNotNull(lines);
            Assert.AreEqual(238, lines.Length);

            // Expectation:
            // Every record will have the expected column value. Additionally the record ID of the
            // decoded data should match the record ID of the raw detail/data (e.g. 0001 == 01 00).
            // 
            // e.g. 
            // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail | Evt_raw_detail
            // 0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff
            foreach (var line in lines)
            {
                Match match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
                if (match.Success)
                {
                    Assert.IsTrue(match.Groups.TryGetValue("RecordId", out Group recordId));
                    Assert.IsTrue(match.Groups.TryGetValue("Timestamp", out Group timestamp));
                    Assert.IsTrue(match.Groups.TryGetValue("Severity", out Group severity));
                    Assert.IsTrue(match.Groups.TryGetValue("Source", out Group source));
                    Assert.IsTrue(match.Groups.TryGetValue("EventType", out Group eventType));
                    Assert.IsTrue(match.Groups.TryGetValue("Sensor", out Group sensor));
                    Assert.IsTrue(match.Groups.TryGetValue("EventDetail", out Group eventDetail));
                    Assert.IsTrue(match.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail));

                    string decodedRecordId = recordId.Value;
                    string rawRecordId = string.Join(string.Empty, eventRawDetail.Value.Substring(0, 5).Split(" ").Reverse()); // 01 00 -> 0001

                    Assert.AreEqual(decodedRecordId, rawRecordId);
                }
            }
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat_Record_Order_Does_Not_Matter_2()
        {
            string[] rawResults = Regex.Split(this.exampleRawResults, @"\r?\n");
            
            // Setup:
            // The raw results are in random order from the decoded results.
            List<string> randomizedRecords = new List<string>(rawResults.Take(4));
            randomizedRecords.AddRange(rawResults.Skip(4).Shuffle());
            this.exampleRawResults = string.Join(Environment.NewLine, randomizedRecords);

            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);
            string[] lines = Regex.Split(combinedResults, "\r?\n");

            Assert.IsNotNull(lines);
            Assert.AreEqual(238, lines.Length);

            // Expectation:
            // Every record will have the expected column value. Additionally the record ID of the
            // decoded data should match the record ID of the raw detail/data (e.g. 0001 == 01 00).
            // 
            // e.g. 
            // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail | Evt_raw_detail
            // 0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff
            foreach (var line in lines)
            {
                Match match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
                if (match.Success)
                {
                    Assert.IsTrue(match.Groups.TryGetValue("RecordId", out Group recordId));
                    Assert.IsTrue(match.Groups.TryGetValue("Timestamp", out Group timestamp));
                    Assert.IsTrue(match.Groups.TryGetValue("Severity", out Group severity));
                    Assert.IsTrue(match.Groups.TryGetValue("Source", out Group source));
                    Assert.IsTrue(match.Groups.TryGetValue("EventType", out Group eventType));
                    Assert.IsTrue(match.Groups.TryGetValue("Sensor", out Group sensor));
                    Assert.IsTrue(match.Groups.TryGetValue("EventDetail", out Group eventDetail));
                    Assert.IsTrue(match.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail));

                    string decodedRecordId = recordId.Value;
                    string rawRecordId = string.Join(string.Empty, eventRawDetail.Value.Substring(0, 5).Split(" ").Reverse()); // 01 00 -> 0001

                    Assert.AreEqual(decodedRecordId, rawRecordId);
                }
            }
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat_Record_Order_Does_Not_Matter_3()
        {
            string[] decodedResults = Regex.Split(this.exampleDecodedResults, @"\r?\n");

            // Setup:
            // The decoded results are in reverse order from the raw results.
            List<string> reversedRecords = new List<string>(decodedResults.Take(4));
            reversedRecords.AddRange(decodedResults.Skip(4).Reverse());
            this.exampleDecodedResults = string.Join(Environment.NewLine, reversedRecords);

            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);
            string[] lines = Regex.Split(combinedResults, "\r?\n");

            Assert.IsNotNull(lines);
            Assert.AreEqual(238, lines.Length);

            // Expectation:
            // Every record will have the expected column value. Additionally the record ID of the
            // decoded data should match the record ID of the raw detail/data (e.g. 0001 == 01 00).
            // 
            // e.g. 
            // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail | Evt_raw_detail
            // 0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff
            foreach (var line in lines)
            {
                Match match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
                if (match.Success)
                {
                    Assert.IsTrue(match.Groups.TryGetValue("RecordId", out Group recordId));
                    Assert.IsTrue(match.Groups.TryGetValue("Timestamp", out Group timestamp));
                    Assert.IsTrue(match.Groups.TryGetValue("Severity", out Group severity));
                    Assert.IsTrue(match.Groups.TryGetValue("Source", out Group source));
                    Assert.IsTrue(match.Groups.TryGetValue("EventType", out Group eventType));
                    Assert.IsTrue(match.Groups.TryGetValue("Sensor", out Group sensor));
                    Assert.IsTrue(match.Groups.TryGetValue("EventDetail", out Group eventDetail));
                    Assert.IsTrue(match.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail));

                    string decodedRecordId = recordId.Value;
                    string rawRecordId = string.Join(string.Empty, eventRawDetail.Value.Substring(0, 5).Split(" ").Reverse()); // 01 00 -> 0001

                    Assert.AreEqual(decodedRecordId, rawRecordId);
                }
            }
        }

        [Test]
        public void IpmiUtilSelMetricsParserCombinesRawSelEventsWithDecodedSelEventsIntoExpectedFormat_Record_Order_Does_Not_Matter_4()
        {
            string[] decodedResults = Regex.Split(this.exampleDecodedResults, @"\r?\n");

            // Setup:
            // The decoded results are in random order from the raw results.
            List<string> randomizedRecords = new List<string>(decodedResults.Take(4));
            randomizedRecords.AddRange(decodedResults.Skip(4).Shuffle());
            this.exampleDecodedResults = string.Join(Environment.NewLine, randomizedRecords);

            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);
            string[] lines = Regex.Split(combinedResults, "\r?\n");

            Assert.IsNotNull(lines);
            Assert.AreEqual(238, lines.Length);

            // Expectation:
            // Every record will have the expected column value. Additionally the record ID of the
            // decoded data should match the record ID of the raw detail/data (e.g. 0001 == 01 00).
            // 
            // e.g. 
            // RecId | Date/Time        | SEV | Src| Evt_Type | Sensor | Evt_detail | Evt_raw_detail
            // 0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff
            foreach (var line in lines)
            {
                Match match = IpmiUtilSelMetricsParser.SelRecordPattern.Match(line);
                if (match.Success)
                {
                    Assert.IsTrue(match.Groups.TryGetValue("RecordId", out Group recordId));
                    Assert.IsTrue(match.Groups.TryGetValue("Timestamp", out Group timestamp));
                    Assert.IsTrue(match.Groups.TryGetValue("Severity", out Group severity));
                    Assert.IsTrue(match.Groups.TryGetValue("Source", out Group source));
                    Assert.IsTrue(match.Groups.TryGetValue("EventType", out Group eventType));
                    Assert.IsTrue(match.Groups.TryGetValue("Sensor", out Group sensor));
                    Assert.IsTrue(match.Groups.TryGetValue("EventDetail", out Group eventDetail));
                    Assert.IsTrue(match.Groups.TryGetValue("EventRawDetail", out Group eventRawDetail));

                    string decodedRecordId = recordId.Value;
                    string rawRecordId = string.Join(string.Empty, eventRawDetail.Value.Substring(0, 5).Split(" ").Reverse()); // 01 00 -> 0001

                    Assert.AreEqual(decodedRecordId, rawRecordId);
                }
            }
        }

        [Test]
        public void IpmiUtilSelMetricsParserFindsExpectedRecordsToProcess_1()
        {
            Assert.IsTrue(IpmiUtilSelMetricsParser.TryGetNewRecords(this.exampleDecodedResults, null, out IList<string> newRecords));
            Assert.IsNotEmpty(newRecords);
            Assert.IsTrue(newRecords.Count == 234);
        }

        [Test]
        [TestCase(" 00e8 | 02/19/24 18:42:00")]
        [TestCase("00e8 | 02/19/24 18:42:00")]
        [TestCase("00e8|    02/19/24 18:42:00")]
        [TestCase("00e8|02/19/24 18:42:00")]
        [TestCase(" 00e8 | 02/19/24 18:42:00 |")]
        [TestCase("00e8 | 02/19/24 18:42:00 | CRT | BMC | Processor | #8b | FRB2 timeout 6f [03 ff ff]")]
        public void IpmiUtilSelMetricsParserFindsExpectedRecordsToProcess_2(string lastRecordProcessed)
        {
            Assert.IsTrue(IpmiUtilSelMetricsParser.TryGetNewRecords(this.exampleDecodedResults, lastRecordProcessed, out IList<string> newRecords));
            Assert.IsNotEmpty(newRecords);
            Assert.IsTrue(newRecords.Count == 2);
            Assert.IsTrue(newRecords[0].StartsWith("00e9 | 02/19/24 18:42:00"));
            Assert.IsTrue(newRecords[1].StartsWith("00ea | 02/19/24 18:42:00"));
        }

        [Test]
        [TestCase(" 00ea | 02/19/24 18:42:00")]
        [TestCase("00ea | 02/19/24 18:42:00")]
        [TestCase("00ea|   02/19/24 18:42:00")]
        [TestCase("00ea|02/19/24 18:42:00")]
        [TestCase("00ea | 02/19/24 18:42:00 | CRT | EFI | Critical Interrupt | #a1 | Bus Warn  6f [a7 00 21]")]
        public void IpmiUtilSelMetricsParserFindsExpectedRecordsToProcess_3(string lastRecordProcessed)
        {
            Assert.IsFalse(IpmiUtilSelMetricsParser.TryGetNewRecords(this.exampleDecodedResults, lastRecordProcessed, out IList<string> newRecords));
            Assert.IsEmpty(newRecords);
        }

        [Test]
        public void IpmiUtilSelMetricsParserParsesTheExpectedMetricsFromTheResults_1()
        {
            IpmiUtilSelMetricsParser.ParsingResult result = IpmiUtilSelMetricsParser.Parse(this.exampleDecodedResults);

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Metrics);
            Assert.IsTrue(result.Metrics.Count == 234);

            // Note: Metric names now include sensor fields in format: <source>_<eventType>_<sensor>_<eventDetail> <hex>
            // Verify a few key metric patterns exist

            // Assert at least one "fully-qualified" metric that includes the sensor segment added by this change.
            Assert.IsTrue(
                result.Metrics.Any(m => Regex.IsMatch(m.Name, @"\bBMC_Event_Log_SEL_Log_Cleared\b", RegexOptions.IgnoreCase)),
                "Should contain a full metric name including sensor field (e.g. BMC_Event_Log_SEL_Log_Cleared ...)");

            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("ACPI_Power_State")), "Should contain BMC ACPI Power State events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("Battery")), "Should contain BMC Battery events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("Event_Log")), "Should contain BMC Event Log events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("Power_Supply")), "Should contain BMC Power Supply events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("Processor")), "Should contain BMC Processor events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC") && m.Name.Contains("Watchdog")), "Should contain BMC Watchdog events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("EFI") && m.Name.Contains("Critical_Interrupt")), "Should contain EFI Critical Interrupt events");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("Sms") && m.Name.Contains("OS")), "Should contain Sms OS events");
        }

        [Test]
        public void IpmiUtilSelMetricsParserParsesTheExpectedMetricsFromTheResults_2()
        {
            IpmiUtilSelMetricsParser.ParsingResult result = IpmiUtilSelMetricsParser.Parse(
                this.exampleDecodedResults, 
                eventsFilter: new Regex("MAJ|CRT", RegexOptions.IgnoreCase));

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Metrics);
            Assert.IsTrue(result.Metrics.Count == 8);

            // Note: Metric names now include sensor fields, so we check for substrings
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC_Power_Supply") && m.Name.Contains("Failure_detected")), "Should contain Power Supply Failure event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC_Processor") && m.Name.Contains("FRB2")), "Should contain Processor FRB2 event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC_Processor") && m.Name.Contains("FRB3")), "Should contain Processor FRB3 event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("BMC_Processor") && m.Name.Contains("IERR")), "Should contain Processor IERR event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("EFI_Critical_Interrupt") && m.Name.Contains("Bus_Warn")), "Should contain Critical Interrupt Bus Warn event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("EFI_Critical_Interrupt") && m.Name.Contains("Fatal_NMI")), "Should contain Critical Interrupt Fatal NMI event");
            Assert.IsTrue(result.Metrics.Any(m => m.Name.Contains("EFI_Power_Supply") && m.Name.Contains("Predictive_failure")), "Should contain Power Supply Predictive failure event");
        }

        [Test]
        public void IpmiUtilSelMetricsParserIncludesRawHexInMetricNameWhenCombinedRecordsProvided()
        {
            // Combine raw and decoded results to get records with hex
            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);

            // Parse the combined results
            IpmiUtilSelMetricsParser.ParsingResult result = IpmiUtilSelMetricsParser.Parse(combinedResults);

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result.Metrics);

            // Verify that metrics contain the hex part with underscores
            // Format should be: <text_part> <hex_with_underscores>
            var metricsWithHex = result.Metrics.Where(m => m.Name.Contains(" ")).ToList();
            Assert.IsNotEmpty(metricsWithHex, "Should have metrics with space-separated hex portion");

            // Check specific hex patterns - spaces should be replaced with underscores
            var metricWithHex = metricsWithHex.FirstOrDefault();
            Assert.IsNotNull(metricWithHex, "Should find at least one metric with hex");

            // Verify the hex portion format: should have underscores between bytes (e.g., "02_00_02")
            string[] parts = metricWithHex.Name.Split(' ');
            Assert.AreEqual(2, parts.Length, "Metric name should have text part and hex part separated by space");

            string hexPart = parts[1];
            Assert.IsTrue(hexPart.Contains("_"), "Hex part should contain underscores between bytes");
            Assert.IsFalse(hexPart.Contains(" "), "Hex part should not contain spaces");

            // Verify hex pattern: should be pairs of hex digits separated by underscores
            Assert.IsTrue(Regex.IsMatch(hexPart, @"^[0-9a-f]{2}(_[0-9a-f]{2})+$", RegexOptions.IgnoreCase), 
                "Hex part should be in format: XX_XX_XX_... where X is hex digit");
        }

        [Test]
        public void IpmiUtilSelMetricsParserMetricNameFormatWithSensorAndHex()
        {
            // Combine raw and decoded to get full records
            string combinedResults = IpmiUtilSelMetricsParser.CombineRecords(this.exampleRawResults, this.exampleDecodedResults);
            IpmiUtilSelMetricsParser.ParsingResult result = IpmiUtilSelMetricsParser.Parse(combinedResults);

            // Find a metric that should have all components: source, eventType, sensor, eventDetail, hex
            // Looking for records like: "0001 | 12/13/23 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared | 01 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff"
            var eventLogMetric = result.Metrics.FirstOrDefault(m => m.Name.Contains("BMC") && m.Name.Contains("Event_Log"));
            Assert.IsNotNull(eventLogMetric, "Should find Event Log metric");

            // Verify format: BMC_Event_Log_SEL_Log_Cleared 01_00_02_87_32_7a_65_20_00_04_10_8a_6f_02_ff_ff
            Assert.IsTrue(eventLogMetric.Name.Contains("BMC"), "Should contain source");
            Assert.IsTrue(eventLogMetric.Name.Contains("Event_Log"), "Should contain event type");
            Assert.IsTrue(eventLogMetric.Name.Contains("SEL"), "Should contain sensor");
            Assert.IsTrue(eventLogMetric.Name.Contains("Log_Cleared"), "Should contain event detail");

            // Verify it has the space separator and hex part
            Assert.IsTrue(eventLogMetric.Name.Contains(" "), "Should have space separator between text and hex");
            string[] parts = eventLogMetric.Name.Split(new[] { ' ' }, 2);
            Assert.AreEqual(2, parts.Length, "Should have text part and hex part");

            string hexPart = parts[1];
            // Hex for record 0001 should be: 01_00_02_87_32_7a_65_20_00_04_10_8a_6f_02_ff_ff
            Assert.IsTrue(hexPart.StartsWith("01_00"), "Hex part should start with record ID bytes");
            Assert.IsTrue(hexPart.Contains("_"), "Hex bytes should be joined with underscores");
        }
    }
}