// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class FioMetricsParserTests
    {
        private static readonly string ExamplesPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(FioMetricsParserTests)).Location),
            "Examples",
            "FIO");

        private static readonly IDictionary<string, double> ExpectedReadMetrics = new Dictionary<string, double>
        {
            // Data from the FIO_Results.txt file in the 'Examples' folder.
            { "read_bytes", 1071141824 },
            { "read_ios", 258144 },
            { "read_ios_short", 1 },
            { "read_ios_dropped", 2 },
            { "read_bandwidth", 8632 },
            { "read_bandwidth_min", 7 },
            { "read_bandwidth_max", 15195 },
            { "read_bandwidth_mean", 12030.843023 },
            { "read_bandwidth_stdev", 4056.505286 },
            { "read_iops", 2160.561601 },
            { "read_iops_min", 1 },
            { "read_iops_max", 3798 },
            { "read_iops_mean", 3007.465116 },
            { "read_iops_stdev", 1014.164654 },
            { "read_latency_min", 53500 },
            { "read_latency_max", 4292581200 },
            { "read_latency_mean", 459323.126984 },
            { "read_latency_stdev", 20701886.804844 },
            { "read_completionlatency_min", 1100 },
            { "read_completionlatency_max", 4080273600 },
            { "read_completionlatency_mean", 418273.267365 },
            { "read_completionlatency_stdev", 18641270.383323 },
            { "read_completionlatency_p50", 92648 },
            { "read_completionlatency_p70", 103960 },
            { "read_completionlatency_p90", 133144 },
            { "read_completionlatency_p99", 263192 },
            { "read_completionlatency_p99_99", 54488096 },
            { "read_submissionlatency_min", 9800 },
            { "read_submissionlatency_max", 4392478900 },
            { "read_submissionlatency_mean", 41849.859619 },
            { "read_submissionlatency_stdev", 8568471.214832 },
            { "h000000_002", 0.010000 },
            { "h000000_004", 0.010000 },
            { "h000000_010", 0.010000 },
            { "h000000_020", 0.000000 },
            { "h000000_050", 0.196075 },
            { "h000000_100", 60.919189 },
            { "h000000_250", 37.810516 },
            { "h000000_500", 0.417709 },
            { "h000000_750", 0.022888 },
            { "h000001_000", 0.010000 },
            { "h000002_000", 0.011063 },
            { "h000004_000", 0.014496 },
            { "h000010_000", 0.013351 },
            { "h000020_000", 0.114822 },
            { "h000050_000", 0.460052 },
            { "h000100_000", 0.010000 },
            { "h000250_000", 0.010000 },
            { "h000500_000", 0.010000 },
            { "h000750_000", 0.010000 },
            { "h001000_000", 0.010000 },
            { "h002000_000", 0.010000 },
            { "hgt002000_000", 0.010000 }
        };

        private static readonly IDictionary<string, double> ExpectedWriteMetrics = new Dictionary<string, double>
        {
            // Data from the FIO_Results.txt file in the 'Examples' folder.
            { "write_bytes", 1073741824 },
            { "write_ios", 262144 },
            { "write_ios_short", 4 },
            { "write_ios_dropped", 5 },
            { "write_bandwidth", 8668 },
            { "write_bandwidth_min", 8 },
            { "write_bandwidth_max", 15195 },
            { "write_bandwidth_mean", 12030.843023 },
            { "write_bandwidth_stdev", 4056.505286 },
            { "write_iops", 2167.231601 },
            { "write_iops_min", 3 },
            { "write_iops_max", 3798 },
            { "write_iops_mean", 3007.465116 },
            { "write_iops_stdev", 1014.164654 },
            { "write_latency_min", 53500 },
            { "write_latency_max", 4292581200 },
            { "write_latency_mean", 459323.126984 },
            { "write_latency_stdev", 20701886.804844 },
            { "write_completionlatency_min", 1200 },
            { "write_completionlatency_max", 4090273600 },
            { "write_completionlatency_mean", 418473.267365 },
            { "write_completionlatency_stdev", 18841270.383323 },
            { "write_completionlatency_p50", 91648 },
            { "write_completionlatency_p70", 104960 },
            { "write_completionlatency_p90", 134144 },
            { "write_completionlatency_p99", 264192 },
            { "write_completionlatency_p99_99", 54788096 },
            { "write_submissionlatency_min", 9900 },
            { "write_submissionlatency_max", 4292478900 },
            { "write_submissionlatency_mean", 40849.859619 },
            { "write_submissionlatency_stdev", 8578271.214832 },
            { "h000000_002", 0.010000 },
            { "h000000_004", 0.010000 },
            { "h000000_010", 0.010000 },
            { "h000000_020", 0.000000 },
            { "h000000_050", 0.196075 },
            { "h000000_100", 60.919189 },
            { "h000000_250", 37.810516 },
            { "h000000_500", 0.417709 },
            { "h000000_750", 0.022888 },
            { "h000001_000", 0.010000 },
            { "h000002_000", 0.011063 },
            { "h000004_000", 0.014496 },
            { "h000010_000", 0.013351 },
            { "h000020_000", 0.114822 },
            { "h000050_000", 0.460052 },
            { "h000100_000", 0.010000 },
            { "h000250_000", 0.010000 },
            { "h000500_000", 0.010000 },
            { "h000750_000", 0.010000 },
            { "h001000_000", 0.010000 },
            { "h002000_000", 0.010000 },
            { "hgt002000_000", 0.010000 }
        };

        [Test]
        public void FioResultsParserThrowsIfTheResultsProvidedAreInAnInvalidFormat()
        {
            FioMetricsParser parser = new FioMetricsParser("Not Valid JSON-formatted results", parseReadMetrics: true, parseWriteMetrics: true);
            
            WorkloadResultsException error = Assert.Throws<WorkloadResultsException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, error.Reason);
        }

        [Test]
        public void FioResultsParserHandlesResultsThatHasMissingMeasurements_AllMeasurementsMissing()
        {
            FioMetricsParser parser = new FioMetricsParser("{ }", parseReadMetrics: true, parseWriteMetrics: true);

            IEnumerable<Metric> metrics = null;
            Assert.DoesNotThrow(() => metrics = parser.Parse());
            Assert.IsNotNull(metrics);
            Assert.IsEmpty(metrics);
        }

        [Test]
        public void FioResultsParserHandlesResultsThatHasMissingMeasurements_ReadMeasurementsMissing()
        {
            JObject results = JObject.Parse(File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json")));
            results.SelectToken($"jobs[0].read").First.Remove();

            FioMetricsParser parser = new FioMetricsParser(results.ToString(), parseReadMetrics: true, parseWriteMetrics: false);

            IEnumerable<Metric> metrics = null;
            Assert.DoesNotThrow(() => metrics = parser.Parse());
            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count() == 52);
        }

        [Test]
        public void FioResultsParserHandlesResultsThatHasMissingMeasurements_WriteMeasurementsMissing()
        {
            JObject results = JObject.Parse(File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json")));
            results.SelectToken($"jobs[0].write").First.Remove();

            FioMetricsParser parser = new FioMetricsParser(results.ToString(), parseReadMetrics: false, parseWriteMetrics: true);

            IEnumerable<Metric> metrics = null;
            Assert.DoesNotThrow(() => metrics = parser.Parse());
            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count() == 52);
        }

        [Test]
        public void FioResultsParserReadsTheExpectedMeasurementsFromTheResults_Read_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: true, parseWriteMetrics: false);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count() == 53);

            CollectionAssert.AreEquivalent(FioMetricsParserTests.ExpectedReadMetrics.Keys, metrics.Select(m => m.Name));
        }

        [Test]
        public void FioResultsParserReadsTheExpectedMeasurementsFromTheResults_Write_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: false, parseWriteMetrics: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count() == 53);

            CollectionAssert.AreEquivalent(FioMetricsParserTests.ExpectedWriteMetrics.Keys, metrics.Select(m => m.Name));
        }

        [Test]
        public void FioResultsParserReadsTheExpectedMeasurementsFromTheResults_ReadWrite_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: true, parseWriteMetrics: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.Count() == 84);

            CollectionAssert.AreEquivalent(
                FioMetricsParserTests.ExpectedReadMetrics.Keys.Union(FioMetricsParserTests.ExpectedWriteMetrics.Keys),
                metrics.Select(m => m.Name));
        }

        [Test]
        public void FioResultsParserReadsTheExpectedMeasurementValuesFromTheResults_Read_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: true, parseWriteMetrics: false, conversionUnits: MetricUnit.Nanoseconds);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            IEnumerable<Tuple<string, double?>> expectedValues = FioMetricsParserTests.ExpectedReadMetrics.Select(entry => new Tuple<string, double?>(entry.Key, entry.Value));
            IEnumerable<Tuple<string, double?>> actualValues = metrics.Select(m => new Tuple<string, double?>(m.Name, m.Value));

            CollectionAssert.AreEquivalent(expectedValues, actualValues);
        }

        [Test]
        public void FioResultsParserReadsTheExpectedMeasurementValuesFromTheResults_Write_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: false, parseWriteMetrics: true, conversionUnits: MetricUnit.Nanoseconds);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            IEnumerable<Tuple<string, double?>> expectedValues = FioMetricsParserTests.ExpectedWriteMetrics.Select(entry => new Tuple<string, double?>(entry.Key, entry.Value));
            IEnumerable<Tuple<string, double?>> actualValues = metrics.Select(m => new Tuple<string, double?>(m.Name, m.Value));

            CollectionAssert.AreEquivalent(expectedValues, actualValues);
        }

        [Test]
        public void FioResultsParserAppliesTheExpectedDefaultConversionFactorToMeasurementValues_Read_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            // The default conversion factor is from nanoseconds to milliseconds
            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: true, parseWriteMetrics: false);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            // All latency metrics are emitted in milliseconds form (vs. nanoseconds).
            double expectedConversionFactor = 0.000001;
            IEnumerable<Tuple<string, double?>> expectedValues = FioMetricsParserTests.ExpectedReadMetrics.Where(entry => entry.Key.Contains("latency"))
                .Select(entry => new Tuple<string, double?>(entry.Key, entry.Value * expectedConversionFactor));

            IEnumerable<Tuple<string, double?>> actualValues = metrics.Where(m => m.Name.Contains("latency"))
                .Select(m => new Tuple<string, double?>(m.Name, m.Value));

            Assert.IsTrue(expectedValues.Count() == actualValues.Count());
            CollectionAssert.AreEquivalent(expectedValues, actualValues);
        }

        [Test]
        public void FioResultsParserAppliesTheExpectedDefaultConversionFactorToMeasurementValues_Write_Scenario()
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, "Results_FIO.json"));

            // The default conversion factor is from nanoseconds to milliseconds
            FioMetricsParser parser = new FioMetricsParser(results, parseReadMetrics: false, parseWriteMetrics: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);

            // All latency metrics are emitted in milliseconds form (vs. nanoseconds).
            double expectedConversionFactor = 0.000001;
            IEnumerable<Tuple<string, double?>> expectedValues = FioMetricsParserTests.ExpectedWriteMetrics.Where(entry => entry.Key.Contains("latency"))
                .Select(entry => new Tuple<string, double?>(entry.Key, entry.Value * expectedConversionFactor));

            IEnumerable<Tuple<string, double?>> actualValues = metrics.Where(m => m.Name.Contains("latency"))
                .Select(m => new Tuple<string, double?>(m.Name, m.Value));

            Assert.IsTrue(expectedValues.Count() == actualValues.Count());
            CollectionAssert.AreEquivalent(expectedValues, actualValues);
        }

        [Test]
        [TestCase("Results_FIO_Verification_Error_1.txt", 1)]
        [TestCase("Results_FIO_Verification_Error_2.txt", 128)]
        public void FioResultsParserReadsDataIntegrityVerifcationErrorsFromTheResults(string exampleFileName, int expectedDataIntegrityErrors)
        {
            string results = File.ReadAllText(Path.Combine(FioMetricsParserTests.ExamplesPath, exampleFileName));

            FioMetricsParser parser = new FioMetricsParser(results, parseDataIntegrityErrors: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.All(m => m.Name == "data_integrity_errors"));
            Assert.IsTrue(metrics.Count() == 1);
            Assert.AreEqual(expectedDataIntegrityErrors, metrics.First().Value);
        }

        [Test]
        public void FioResultsParserCreatesTheExpectedMetricsWhenThereAreNoDataIntegrityErrorsFound_Scenario1()
        {
            FioMetricsParser parser = new FioMetricsParser(string.Empty, parseDataIntegrityErrors: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.All(m => m.Name == "data_integrity_errors"));
            Assert.IsTrue(metrics.Count() == 1);
            Assert.AreEqual(0, metrics.First().Value);
        }

        [Test]
        public void FioResultsParserCreatesTheExpectedMetricsWhenThereAreNoDataIntegrityErrorsFound_Scenario2()
        {
            FioMetricsParser parser = new FioMetricsParser("fio results without any errors", parseDataIntegrityErrors: true);
            IEnumerable<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsNotEmpty(metrics);
            Assert.IsTrue(metrics.All(m => m.Name == "data_integrity_errors"));
            Assert.IsTrue(metrics.Count() == 1);
            Assert.AreEqual(0, metrics.First().Value);
        }
    }
}
