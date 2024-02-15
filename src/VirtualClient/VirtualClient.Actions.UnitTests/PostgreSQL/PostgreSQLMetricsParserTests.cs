// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
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
    using VirtualClient.Actions;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLMetricsParserTests
    {
        private string rawText;
        private PostgreSQLMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\PostgreSQL\PostgresqlresultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new PostgreSQLMetricsParser(this.rawText);
        }

        [Test]
        public void PostgreSQLParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "Transactions/min", 26163);
            MetricAssert.Exists(metrics, "Transactions/sec", 436.05);
            MetricAssert.Exists(metrics, "Operations/min", 11400);
            MetricAssert.Exists(metrics, "Operations/sec", 190);
        }

        [Test]
        public void PostgreSQLParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectPostgreSQLoutputPath = Path.Combine(workingDirectory, @"Examples\PostgreSQL\PostgresqlIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectPostgreSQLoutputPath);
            this.testParser = new PostgreSQLMetricsParser(this.rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }
    }
}
