// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VirtualClient;
    using VirtualClient.Contracts;
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

            // Only one valid metric line exists (with parseable value)
            Assert.AreEqual(1, metrics.Count, $"Expected 1 metric but got {metrics.Count}");

            // Verify the one valid metric was parsed correctly
            // "Median throughput" -> "throughput Median" -> "index throughput Median" -> "index-throughput-Median"
            Assert.AreEqual("index-throughput-Median", metrics[0].Name);
            Assert.AreEqual(45000.5, metrics[0].Value);
            Assert.AreEqual("docs/s", metrics[0].Unit);
            Assert.AreEqual(MetricRelativity.HigherIsBetter, metrics[0].Relativity);
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

            // "Median throughput" is transformed to "throughput Median" (NOT "throughput P50")
            Metric throughputMetric = metrics.First(m => m.Name == "index-throughput-Median");
            Assert.AreEqual(45000.5, throughputMetric.Value);
            Assert.AreEqual("docs/s", throughputMetric.Unit);
            Assert.AreEqual(MetricRelativity.HigherIsBetter, throughputMetric.Relativity);
            Assert.AreEqual(1, throughputMetric.Verbosity); // Contains "throughput" -> Verbosity 1

            // "50th percentile latency" is transformed to "latency P50"
            Metric latencyMetric = metrics.First(m => m.Name == "search-latency-P50");
            Assert.AreEqual(150.25, latencyMetric.Value);
            Assert.AreEqual("ms", latencyMetric.Unit);
            Assert.AreEqual(MetricRelativity.LowerIsBetter, latencyMetric.Relativity);
            Assert.AreEqual(1, latencyMetric.Verbosity); // Contains "latency" -> Verbosity 1
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

            // "median throughput" -> "throughput Median" (NOT "throughput P50")
            Metric metric1 = metrics.First(m => m.Name == "task1-throughput-Median");
            Assert.IsNotNull(metric1);
            Assert.AreEqual(100, metric1.Value);
            Assert.AreEqual(1, metric1.Verbosity); // Contains "throughput" and ends with "Median" -> Verbosity 1

            // "mean throughput" -> "throughput Mean"
            Metric metric2 = metrics.First(m => m.Name == "task2-throughput-Mean");
            Assert.IsNotNull(metric2);
            Assert.AreEqual(200, metric2.Value);
            Assert.AreEqual(1, metric2.Verbosity); // Contains "throughput" and ends with "Mean" -> Verbosity 1

            // "100th percentile latency" -> "latency P100"
            Metric metric3 = metrics.First(m => m.Name == "task3-latency-P100");
            Assert.IsNotNull(metric3);
            Assert.AreEqual(300, metric3.Value);
            Assert.AreEqual(1, metric3.Verbosity); // Contains "latency" and ends with "P100" -> Verbosity 1
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

        #region MetricsVerbosity Tests

        [Test]
        public void MetricsVerbosityPropertyReturnsDefaultValueWhenNotSpecified()
        {
            string reportContents = "Metric,Task,Value,Unit\nMedian throughput,index,1000,docs/s";
            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            Assert.AreEqual(1, parser.MetricsVerbosity);
        }

        [Test]
        public void MetricsVerbosityPropertyReturnsSpecifiedValue()
        {
            string reportContents = "Metric,Task,Value,Unit\nMedian throughput,index,1000,docs/s";
            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 3 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            Assert.AreEqual(3, parser.MetricsVerbosity);
        }

        [Test]
        public void MetricsVerbosityPropertyReturnsDefaultValueWhenInvalidValue()
        {
            string reportContents = "Metric,Task,Value,Unit\nMedian throughput,index,1000,docs/s";
            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", "invalid" }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            Assert.AreEqual(1, parser.MetricsVerbosity);
        }

        [Test]
        public void ParserFiltersMetricsBasedOnVerbosityLevel1()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",              // Verbosity 1 (contains throughput)
                "100th percentile latency,search,100,ms",           // Verbosity 1 (P100 + contains latency)
                "50th percentile latency,query,50,ms",              // Verbosity 2 (P50) but contains latency -> Verbosity 1
                "90th percentile latency,query,90,ms",              // Verbosity 2 (P90) but contains latency -> Verbosity 1
                "mean throughput,index,500,docs/s",                 // Verbosity 1 (Mean + contains throughput)
                "some custom metric,task,123,units"                 // Verbosity 5
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 1 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Should include all metrics containing latency or throughput (Verbosity 1)
            Assert.AreEqual(5, metrics.Count);
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserFiltersMetricsBasedOnVerbosityLevel2()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",              // Verbosity 1
                "100th percentile latency,search,100,ms",           // Verbosity 1
                "50th percentile latency,query,50,ms",              // Verbosity 1 (contains latency)
                "90th percentile latency,query,90,ms",              // Verbosity 1 (contains latency)
                "99th percentile latency,query,99,ms",              // Verbosity 1 (contains latency)
                "mean throughput,index,500,docs/s",                 // Verbosity 1
                "some custom metric,task,123,units"                 // Verbosity 5
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 2 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Should include Verbosity 1 and 2 metrics (all except the custom metric)
            Assert.AreEqual(6, metrics.Count);
            Assert.IsTrue(metrics.All(m => m.Verbosity <= 2));
            Assert.IsFalse(metrics.Any(m => m.Name.Contains("custom")));
        }

        [Test]
        public void ParserFiltersMetricsBasedOnVerbosityLevel5()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",              // Verbosity 1
                "50th percentile latency,query,50,ms",              // Verbosity 1 (contains latency)
                "mean throughput,index,500,docs/s",                 // Verbosity 1
                "dataset size,,10240,MB"                            // Verbosity 5 (summary metric, no task)
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 5 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Should include Verbosity 1 and Verbosity 5 metrics when MetricsVerbosity is set to 5.
            Assert.AreEqual(4, metrics.Count);
            Assert.AreEqual(3, metrics.Count(m => m.Verbosity == 1));
            Assert.AreEqual(1, metrics.Count(m => m.Verbosity == 5));
            Assert.IsTrue(metrics.Any(m => m.Name.Contains("dataset") && m.Verbosity == 5));
        }

        [Test]
        public void ParserIgnoresVerbosityFilteringWhenCollectAllMetricsIsTrue()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",              // Verbosity 1
                "50th percentile latency,query,50,ms",              // Verbosity 1
                "mean throughput,index,500,docs/s",                 // Verbosity 1
                "some custom metric,task,123,units"                 // Verbosity 5
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 1 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            // Should include all metrics regardless of verbosity when collectAllMetrics is true
            Assert.AreEqual(4, metrics.Count);
        }

        [Test]
        public void ParserAssignsCorrectVerbosityToMedianMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",
                "median latency,search,50,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserAssignsCorrectVerbosityToP100Metrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "100th percentile latency,search,200,ms",
                "100th percentile service time,index,150,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            // Both contain "latency" or have P100 ending, so Verbosity 1
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserAssignsCorrectVerbosityToP50P90P99Metrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "50th percentile service time,search,50,ms",
                "90th percentile service time,search,90,ms",
                "99th percentile service time,search,99,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(3, metrics.Count);
            // P50, P90, P99 without latency/throughput -> Verbosity 2
            Assert.IsTrue(metrics.All(m => m.Verbosity == 2));
        }

        [Test]
        public void ParserAssignsVerbosity1ToLatencyMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "50th percentile latency,search,50,ms",
                "90th percentile latency,search,90,ms",
                "99th percentile latency,search,99,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(3, metrics.Count);
            // All contain "latency" -> Verbosity 1
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserAssignsVerbosity1ToThroughputMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",
                "mean throughput,index,500,docs/s"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            // Both contain "throughput" -> Verbosity 1
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserAssignsVerbosity1ToMeanMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "mean throughput,index,500,docs/s",
                "mean service time,search,100,ms"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            // Mean metrics with throughput/other -> Verbosity 1
            Assert.IsTrue(metrics.All(m => m.Verbosity == 1));
        }

        [Test]
        public void ParserAssignsCorrectVerbosityToOtherMetrics()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "error rate,search,0.1,%",
                "some custom metric,task,123,units"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: true);

            IList<Metric> metrics = parser.Parse();

            Assert.AreEqual(2, metrics.Count);
            // Neither contains latency/throughput, nor ends with P50/P90/P99/P100/Mean/Median -> Verbosity 5
            Assert.IsTrue(metrics.All(m => m.Verbosity == 5));
        }

        [Test]
        public void ParserCombinesVerbosityFilteringWithRelevantMetricsFiltering()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median throughput,index,1000,docs/s",              // Relevant, Verbosity 1
                "50th percentile latency,search,50,ms",             // Relevant, Verbosity 1 (contains latency)
                "mean throughput,index,500,docs/s",                 // Relevant, Verbosity 1
                "some irrelevant metric,task,100,units"             // Not relevant
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 2 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Should include only relevant metrics with verbosity <= 2
            Assert.AreEqual(3, metrics.Count);
            Assert.IsTrue(metrics.All(m => m.Verbosity <= 2));
        }

        [Test]
        public void ParserHandlesSummaryMetricsWithVerbosityFiltering()
        {
            string reportContents = string.Join(Environment.NewLine, new[]
            {
                "Metric,Task,Value,Unit",
                "median cumulative indexing time across primary shards,,1500,ms",
                "median cumulative merge time across primary shards,,500,ms",
                "total young gen gc time,,200,s",
                "dataset size,,10240,MB"
            });

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
            {
                { "MetricsVerbosity", 1 }
            };

            ElasticsearchRallyMetricsParser parser = new ElasticsearchRallyMetricsParser(
                reportContents,
                metadata,
                rallyCollectAllMetrics: false);

            IList<Metric> metrics = parser.Parse();

            // Summary metrics with "median" -> Verbosity 1
            Assert.IsTrue(metrics.Count >= 2);
            Assert.IsTrue(metrics.Any(m => m.Name.Contains("indexing") && m.Verbosity == 1));
            Assert.IsTrue(metrics.Any(m => m.Name.Contains("merge") && m.Verbosity == 1));
        }

        #endregion
    }
}