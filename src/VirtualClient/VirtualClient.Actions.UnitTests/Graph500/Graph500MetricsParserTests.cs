using VirtualClient.Common.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class Graph500MetricsParserTests
    {
        private string rawText;
        private Graph500MetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Graph500\Graph500ResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new Graph500MetricsParser(this.rawText);
        }

        [Test]
        public void Graph500ParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(55, metrics.Count);
            MetricAssert.Exists(metrics, "SCALE", 5);
            MetricAssert.Exists(metrics, "edgefactor", 4);
            MetricAssert.Exists(metrics, "NBFS", 64);
            MetricAssert.Exists(metrics, "graph_generation", 0.00336051);
            MetricAssert.Exists(metrics, "num_mpi_processes", 1);

            MetricAssert.Exists(metrics, "bfs  min_time", 1.09673e-05, "seconds");
            MetricAssert.Exists(metrics, "bfs  firstquartile_time", 2.19345e-05, "seconds");
            MetricAssert.Exists(metrics, "bfs  median_time", 2.52724e-05, "seconds");
            MetricAssert.Exists(metrics, "bfs  thirdquartile_time", 3.02792e-05, "seconds");
            MetricAssert.Exists(metrics, "bfs  max_time", 0.000289679, "seconds");
            MetricAssert.Exists(metrics, "bfs  mean_time", 4.88423e-05, "seconds");
            MetricAssert.Exists(metrics, "bfs  stddev_time", 7.07797e-05, "seconds");

            MetricAssert.Exists(metrics, "sssp min_time", 2.16961e-05, "seconds");
            MetricAssert.Exists(metrics, "sssp firstquartile_time", 0.000314713, "seconds");
            MetricAssert.Exists(metrics, "sssp median_time", 0.000433803, "seconds");
            MetricAssert.Exists(metrics, "sssp thirdquartile_time", 0.000584006, "seconds");
            MetricAssert.Exists(metrics, "sssp max_time", 0.00132704, "seconds");
            MetricAssert.Exists(metrics, "sssp mean_time", 0.000461359, "seconds");
            MetricAssert.Exists(metrics, "sssp stddev_time", 0.000264222, "seconds");

            
            MetricAssert.Exists(metrics, "min_nedge", 0);
            MetricAssert.Exists(metrics, "firstquartile_nedge", 119);
            MetricAssert.Exists(metrics, "median_nedge", 119);
            MetricAssert.Exists(metrics, "thirdquartile_nedge", 119);
            MetricAssert.Exists(metrics, "max_nedge", 119);
            MetricAssert.Exists(metrics, "mean_nedge", 104.125);
            MetricAssert.Exists(metrics, "stddev_nedge", 39.666666667);

            MetricAssert.Exists(metrics, "bfs  min_TEPS", 0, "TEPS");
            MetricAssert.Exists(metrics, "bfs  firstquartile_TEPS", 2.3215e+06, "TEPS");
            MetricAssert.Exists(metrics, "bfs  median_TEPS", 4.53747e+06, "TEPS");
            MetricAssert.Exists(metrics, "bfs  thirdquartile_TEPS", 5.28172e+06, "TEPS");
            MetricAssert.Exists(metrics, "bfs  max_TEPS", 5.5458e+06, "TEPS");
            MetricAssert.Exists(metrics, "bfs  harmonic_mean_TEPS", 0, "TEPS");
            MetricAssert.Exists(metrics, "bfs  harmonic_stddev_TEPS", 0, "TEPS");

            MetricAssert.Exists(metrics, "sssp min_TEPS", 0, "TEPS");
            MetricAssert.Exists(metrics, "sssp firstquartile_TEPS", 154503, "TEPS");
            MetricAssert.Exists(metrics, "sssp median_TEPS", 250878, "TEPS");
            MetricAssert.Exists(metrics, "sssp thirdquartile_TEPS", 282629, "TEPS");
            MetricAssert.Exists(metrics, "sssp max_TEPS", 616200, "TEPS");
            MetricAssert.Exists(metrics, "sssp harmonic_mean_TEPS", 0, "TEPS");
            MetricAssert.Exists(metrics, "sssp harmonic_stddev_TEPS", 0, "TEPS");

            MetricAssert.Exists(metrics, "bfs  min_validate", 1.93119e-05);
            MetricAssert.Exists(metrics, "bfs  firstquartile_validate", 2.8491e-05);
            MetricAssert.Exists(metrics, "bfs  median_validate", 3.19481e-05);
            MetricAssert.Exists(metrics, "bfs  thirdquartile_validate", 3.57628e-05);
            MetricAssert.Exists(metrics, "bfs  max_validate", 0.000328064);
            MetricAssert.Exists(metrics, "bfs  mean_validate", 4.82313e-05);
            MetricAssert.Exists(metrics, "bfs  stddev_validate", 5.77144e-05);

            MetricAssert.Exists(metrics, "sssp min_validate", 1.26362e-05);
            MetricAssert.Exists(metrics, "sssp firstquartile_validate", 1.51396e-05);
            MetricAssert.Exists(metrics, "sssp median_validate", 1.63317e-05);
            MetricAssert.Exists(metrics, "sssp thirdquartile_validate", 1.83582e-05);
            MetricAssert.Exists(metrics, "sssp max_validate", 0.000291109);
            MetricAssert.Exists(metrics, "sssp mean_validate", 3.37996e-05);
            MetricAssert.Exists(metrics, "sssp stddev_validate", 5.92378e-05);
        }

        [Test]
        public void Graph500ParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectGraph500outputPath = Path.Combine(workingDirectory, @"Examples\Graph500\Graph500IncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectGraph500outputPath);
            this.testParser = new Graph500MetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("Graph500 workload didn't generate results because of insufficient memory for running the workload", exception.Message);
        }
    }
}
