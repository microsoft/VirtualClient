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
    class LAPACKMetricsParserTests
    {
        private string rawText;
        private LAPACKMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LAPACK", "LAPACKResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LAPACKMetricsParser(this.rawText);
        }

        [Test]
        public void LAPACKParserVerifyResults()
        {
            this.testParser.Parse();
            Assert.AreEqual(4, this.testParser.LINSingleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.LINDoubleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.LINComplexResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.LINComplexDoubleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.EIGSingleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.EIGDoubleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.EIGDoubleResult.Columns.Count);
            Assert.AreEqual(4, this.testParser.EIGComplexDoubleResult.Columns.Count);
        }

        [Test]
        public void LAPACKParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();
            MetricAssert.Exists(metrics, "compute_time_LIN_Single_Precision", 4.02, "seconds");
            MetricAssert.Exists(metrics, "compute_time_LIN_Double_Precision", 4.11, "seconds");
            MetricAssert.Exists(metrics, "compute_time_LIN_Complex", 10.5, "seconds");
            MetricAssert.Exists(metrics, "compute_time_LIN_Complex_Double", 11.63, "seconds");
            MetricAssert.Exists(metrics, "compute_time_EIG_Single_Precision", 6.53, "seconds");
            MetricAssert.Exists(metrics, "compute_time_EIG_Double_Precision", 8.19, "seconds");
            MetricAssert.Exists(metrics, "compute_time_EIG_Complex", 11.059999999999995, "seconds");
            MetricAssert.Exists(metrics, "compute_time_EIG_Complex_Double", 13.359999999999996, "seconds");
        }

        [Test]
        public void LAPACKParserThrowIfInvalidOutput()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "LAPACK", "LAPACKIncorrectFormatExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LAPACKMetricsParser(this.rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());

            workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            outputPath = Path.Combine(workingDirectory, "Examples", "LAPACK", "LAPACKIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LAPACKMetricsParser(this.rawText);

            exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
        }
    }
}
