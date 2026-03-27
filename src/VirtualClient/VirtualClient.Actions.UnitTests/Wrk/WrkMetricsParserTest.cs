using System.Collections.Generic;
using System.IO;
using System.Reflection;
using VirtualClient.Contracts;
using NUnit.Framework;
using VirtualClient;
using VirtualClient.Actions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class WrkMetricsParserTest
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        [TestCase(null)]
        public void WRKParsesResultsCorrectly01(bool emitSpectrum)
        {
            string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
            string rawText = File.ReadAllText(outputPath);
            WrkMetricParser testParser = new WrkMetricParser(rawText);

            WrkMetricParser parser = new WrkMetricParser(rawText);
            IList<Metric> actualMetrics = parser.Parse(emitSpectrum);

            Assert.AreEqual(parser.GetTestConfig(), "Running 1m test @ https://10.1.0.26/api_new/1kb with 2 threads and 20 connections");
            MetricAssert.Exists(actualMetrics, "latency_p50", 29.56 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p75", 39.75 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p90", 45.97 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99", 49.74 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_9", 50.17 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_99", 50.23 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_999", 50.23 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p100", 50.23 * 1000, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "requests/sec", 16305.17);
            MetricAssert.Exists(actualMetrics, "transfers/sec", 20.01, MetricUnit.Megabytes);

            if (emitSpectrum == true)
            {

                MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_000000", 8372.223);
                MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_887500", 45449.215);
                MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_997266", 50069.503);
                MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_999805", 50200.575);
                MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_999902", 50233.343);
                MetricAssert.Exists(actualMetrics, "latency_spectrum_p1_000000", 50233.343);
            }

        }

        [Test]
        public void WRKParsesResultsCorrectly02()
        {
            string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample2.txt");
            string rawText = File.ReadAllText(outputPath);
            WrkMetricParser testParser = new WrkMetricParser(rawText);

            WrkMetricParser parser = new WrkMetricParser(rawText);
            IList<Metric> actualMetrics = parser.Parse(true);

            // Error
            MetricAssert.Exists(actualMetrics, "Non-2xx or 3xx responses", 58902);

            // Raw data
            MetricAssert.Exists(actualMetrics, "requests/sec", 1963.39);
            MetricAssert.Exists(actualMetrics, "transfers/sec", 0.61033203125, MetricUnit.Megabytes);

            //Latency Distribution
            MetricAssert.Exists(actualMetrics, "latency_p50", 1.43 , MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p75", 1.98 , MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p90", 2.68 , MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99", 3.96, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_9", 6.93, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_99", 8.99, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99_999", 9.77, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p100", 9.77, MetricUnit.Milliseconds);

            //Uncorrected Latency Distribution
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p50", 483 * 0.001, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p75", 1.12, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p90", 1.71, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p99", 2.87, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p99_9", 5.76, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p99_99", 8.02, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p99_999", 8.41, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_p100", 8.41, MetricUnit.Milliseconds);

            // latency spectrum
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_000000", 0.175);
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_775000", 2.067);
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_937500", 2.981);
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_998437", 6.331);
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_999219", 7.511);
            MetricAssert.Exists(actualMetrics, "latency_spectrum_p0_999969", 9.503);

            // Uncorrected latency spectrum
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_000000", 0.135);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_100000", 0.242);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_200000", 0.287);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_981250", 2.531);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_996094", 3.793);
            MetricAssert.Exists(actualMetrics, "uncorrected_latency_spectrum_p0_999939", 8.223);

            Assert.AreEqual(parser.GetTestConfig(), "Running 30s test @ http://10.1.0.13/index.html with 2 threads and 100 connections");
        }

        [Test]
        public void WRKParsesResultsCorrectly03()
        {
            string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample3.txt");
            string rawText = File.ReadAllText(outputPath);
            WrkMetricParser testParser = new WrkMetricParser(rawText);

            WrkMetricParser parser = new WrkMetricParser(rawText);
            IList<Metric> actualMetrics = parser.Parse(true);

            MetricAssert.Exists(actualMetrics, "Non-2xx or 3xx responses", 184596465);
            Assert.AreEqual(parser.GetTestConfig(), "Running 2m test @ http://10.9.0.7/api_new/10kb with 32 threads and 5000 connections");
            // Raw data
            MetricAssert.Exists(actualMetrics, "requests/sec", 1229838.61);
            MetricAssert.Exists(actualMetrics, "transfers/sec", 382.35, MetricUnit.Megabytes);

            //Latency Distribution
            MetricAssert.Exists(actualMetrics, "latency_p50", 3.85, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p75", 5.05, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p90", 7.27, MetricUnit.Milliseconds);
            MetricAssert.Exists(actualMetrics, "latency_p99", 11.16, MetricUnit.Milliseconds);
        }

        [Test]
        public void WRKParsesErrorCorrectly01()
        {
            string outputPath = Path.Combine(examplesDirectory, @"wrkErrorExample1.txt");
            string rawText = File.ReadAllText(outputPath);
            WrkMetricParser testParser = new WrkMetricParser(rawText);

            WrkMetricParser parser = new WrkMetricParser(rawText);
            Assert.AreEqual(parser.GetTestConfig(), "Running 2m test @ http://10.1.0.15/api_new/10kb with 1 threads and 10000 connections");

            WorkloadException exc = Assert.Throws<WorkloadException>(() =>
            {
                parser.Parse();
            });

            Assert.AreEqual(exc.Message, "Socket errors: connect 0, read 1645610, write 16, timeout 0");
        }

        [Test]
        public void WRKParsesErrorCorrectly02()
        {
            string outputPath = Path.Combine(examplesDirectory, @"wrkErrorExample2.txt");
            string rawText = File.ReadAllText(outputPath);
            WrkMetricParser testParser = new WrkMetricParser(rawText);

            WrkMetricParser parser = new WrkMetricParser(rawText);
            Assert.AreEqual(parser.GetTestConfig(), "Running 30s test @ http://10.1.0.13/index.html with 5 threads and 100 connections");
            
            Assert.DoesNotThrow(() => { parser.Parse(); });

            IList<Metric> actualMetrics = parser.Parse();
            MetricAssert.Exists(actualMetrics, "Non-2xx or 3xx responses", 59956);

            MetricAssert.Exists(actualMetrics, "requests/sec", 1998.42);
            MetricAssert.Exists(actualMetrics, "transfers/sec", 636.13/1024.0, MetricUnit.Megabytes);
        }
    }
}
