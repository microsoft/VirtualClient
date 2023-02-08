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

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "Transactions Per Minute", 26163);
            MetricAssert.Exists(metrics, "Number Of Operations Per Minute", 11400);
        }

        [Test]
        public void PostgreSQLParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectPostgreSQLoutputPath = Path.Combine(workingDirectory, @"Examples\PostgreSQL\PostgresqlIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectPostgreSQLoutputPath);
            this.testParser = new PostgreSQLMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("The PostgreSQL output file has incorrect format for parsing", exception.Message);
        }
    }
}
