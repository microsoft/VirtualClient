// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ElasticsearchRallyMetricsParserTests
    {
        private string examplePath;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.examplePath = Path.Combine(workingDirectory, "Examples", "ElasticsearchRally");
        }

        [Test]
        public void ParserThrowsArgumentNullExceptionWhenReportContentsIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ElasticsearchRallyMetricsParser(
                null,
                new Dictionary<string, IConvertible>(),
                false));
        }

        [Test]
        public void ParserThrowsArgumentNullExceptionWhenReportContentsIsEmpty()
        {
            Assert.Throws<ArgumentNullException>(() => new ElasticsearchRallyMetricsParser(
                string.Empty,
                new Dictionary<string, IConvertible>(),
                false));
        }

        [Test]
        public void ParserThrowsArgumentNullExceptionWhenMetadataIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ElasticsearchRallyMetricsParser(
                "Metric,Task,Value,Unit",
                null,
                false));
        }

        [Test]
        public void ParserParsesBasicMetricsCorrectly()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,index,45000.5,docs/s",
                "50th percentile latency,search,150.25,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "track", "geonames" },
                { "challenge", "append-no-conflicts" }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.AreEqual(2, metrics.Count);

            Metric throughputMetric = metrics.First(m => m.Name == "index-throughput-P50");
            Assert.AreEqual(45000.5, throughputMetric.Value);
            Assert.AreEqual("docs/s", throughputMetric.Unit);
            Assert.AreEqual(MetricRelativity.HigherIsBetter, throughputMetric.Relativity);

            Metric latencyMetric = metrics.First(m => m.Name == "search-latency-P50");
            Assert.AreEqual(150.25, latencyMetric.Value);
            Assert.AreEqual("ms", latencyMetric.Unit);
            Assert.AreEqual(MetricRelativity.LowerIsBetter, latencyMetric.Relativity);
        }

        [Test]
        public void ParserParsesExampleFileCorrectly()
        {
            string exampleFile = Path.Combine(this.examplePath, "ElasticsearchRallyExample.txt");
            string reportContents = File.ReadAllText(exampleFile);

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "track", "geonames" }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            Assert.IsNotNull(metrics);
            Assert.IsTrue(metrics.Count > 0);
        }

        [Test]
        public void ParserHandlesInvalidLinesGracefully()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Invalid line without enough columns",
                "Median throughput,index,45000.5,docs/s",
                "Invalid,metric,value,ms",
                "50th percentile latency,search,notanumber,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "track", "test" }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(1, metrics.Count);
            Assert.AreEqual("index-throughput-P50", metrics[0].Name);
        }

        [Test]
        public void ParserTransformsMetricNamesCorrectly()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,task1,100,docs/s",
                "mean throughput,task2,200,docs/s",
                "100th percentile latency,task3,300,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(3, metrics.Count);
            Assert.IsTrue(metrics.Any(m => m.Name == "task1-throughput-P50"));
            Assert.IsTrue(metrics.Any(m => m.Name == "task2-throughput-Mean"));
            Assert.IsTrue(metrics.Any(m => m.Name == "task3-latency-P100"));
        }

        [Test]
        public void ParserSetsCorrectRelativityForThroughputMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,index,45000.5,docs/s",
                "Mean throughput,search,30000,docs/s"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.IsTrue(metrics.All(m => m.Relativity == MetricRelativity.HigherIsBetter));
        }

        [Test]
        public void ParserSetsCorrectRelativityForLatencyMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "50th percentile latency,search,150.25,ms",
                "90th percentile latency,search,200.5,ms",
                "99th percentile latency,search,350.75,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.IsTrue(metrics.All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void ParserSetsCorrectRelativityForServiceTimeMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "50th percentile service time,search,120.5,ms",
                "100th percentile service time,index,250.75,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.IsTrue(metrics.All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void ParserSetsCorrectRelativityForRateMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "error rate,search,0.5,%"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(MetricRelativity.LowerIsBetter, metrics[0].Relativity);
        }

        [Test]
        public void ParserSetsCorrectVerbosityLevels()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,task1,100,docs/s",
                "Mean throughput,task2,200,docs/s",
                "100th percentile latency,task3,300,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            // Median and 100th percentile should have verbosity 0 (Critical)
            Assert.AreEqual(0, metrics.First(m => m.Name.Contains("P50")).Verbosity);
            Assert.AreEqual(0, metrics.First(m => m.Name.Contains("P100")).Verbosity);
            // Mean should have verbosity 1 (Standard)
            Assert.AreEqual(1, metrics.First(m => m.Name.Contains("Mean")).Verbosity);
        }

        [Test]
        public void ParserFiltersRelevantMetricsWhenCollectAllMetricsIsFalse()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,index,1000,docs/s",
                "50th percentile latency,search,100,ms",
                "Some other metric,task,50,units"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Should only collect relevant metrics
            Assert.IsTrue(metrics.Count <= 2);
            Assert.IsTrue(metrics.All(m => m.Name.Contains("throughput") || m.Name.Contains("latency")));
        }

        [Test]
        public void ParserCollectsAllMetricsWhenCollectAllMetricsIsTrue()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,index,1000,docs/s",
                "50th percentile latency,search,100,ms",
                "Some other metric,task,50,units"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(3, metrics.Count);
        }

        [Test]
        public void ParserHandlesMetricsWithoutTaskNames()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median cumulative indexing time across primary shards,,1500,ms",
                "dataset size,,10240,MB"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            Assert.IsTrue(metrics.Count > 0);
            // Metrics without task names should not have task prefix
            Assert.IsTrue(metrics.All(m => !m.Name.StartsWith("-")));
        }

        [Test]
        public void ParserHandlesCountMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "segment count,,100,"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(1, metrics.Count);
            Assert.AreEqual("count", metrics[0].Unit);
        }

        [Test]
        public void ParserIncludesMetadataInParsedMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "Median throughput,index,45000.5,docs/s"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "track", "geonames" },
                { "challenge", "append-no-conflicts" },
                { "version", "8.0.0" }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(1, metrics.Count);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("track"));
            Assert.AreEqual("geonames", metrics[0].Metadata["track"]);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("challenge"));
            Assert.AreEqual("append-no-conflicts", metrics[0].Metadata["challenge"]);
            Assert.IsTrue(metrics[0].Metadata.ContainsKey("version"));
            Assert.AreEqual("8.0.0", metrics[0].Metadata["version"]);
        }

        [Test]
        public void ParserReplacesSpacesWithHyphensInMetricNames()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "50th percentile latency,search query,150.25,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(1, metrics.Count);
            Assert.IsFalse(metrics[0].Name.Contains(" "));
            Assert.IsTrue(metrics[0].Name.Contains("-"));
            Assert.AreEqual("search-query-latency-P50", metrics[0].Name);
        }

        [Test]
        public void ParserHandlesTimeUnitsCorrectly()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "some metric,task1,100,s",
                "another metric,task2,200,ms",
                "third metric,task3,300,min"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            // Time units should have LowerIsBetter relativity if not otherwise specified
            Assert.AreEqual(3, metrics.Count);
            Assert.IsTrue(metrics.All(m => m.Relativity == MetricRelativity.LowerIsBetter));
        }

        [Test]
        public void ParserHandlesCaseInsensitiveMetricNames()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "MEDIAN THROUGHPUT,index,1000,docs/s",
                "50TH PERCENTILE LATENCY,search,100,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            Assert.IsTrue(metrics.Any(m => m.Name.Contains("throughput")));
            Assert.IsTrue(metrics.Any(m => m.Name.Contains("latency")));
        }
    }
}