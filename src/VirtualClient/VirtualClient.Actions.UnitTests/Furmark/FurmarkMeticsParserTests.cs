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
using static System.Net.Mime.MediaTypeNames;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class FurmarkMeticsParserTests
    {
        private string rawText;
        private FurmarkMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Furmark\FurmarkExampleResults.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new FurmarkMetricsParser(this.rawText);
        }

        [Test]
        public void FurmarkParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(2, metrics.Count);
            MetricAssert.Exists(metrics, "Score", 4700);
            MetricAssert.Exists(metrics, "DurationInMs", 60000, "ms");

        }

        [Test]
        public void FurmarkParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectFurmarkoutputPath = Path.Combine(workingDirectory, @"Examples\Furmark\FurmarkIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectFurmarkoutputPath);
            this.testParser = new FurmarkMetricsParser(this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("Furmark workload didn't generate results files.", exception.Message);

        }
    }
 }
