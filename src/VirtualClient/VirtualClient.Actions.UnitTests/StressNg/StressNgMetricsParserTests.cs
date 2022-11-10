using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class StressNgParserUnitTests
    {
        private string rawText;
        private StressNgMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "StressNg");
            }
        }

        [Test]
        public void StressNgParserVerifyMetricsCpu()
        {
            string outputPath = Path.Combine(this.ExamplePath, "StressNgCpuExample.yaml");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StressNgMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(6, metrics.Count);
            MetricAssert.Exists(metrics, "cpu-bogo-ops", 31532, "BogoOps");
            MetricAssert.Exists(metrics, "cpu-bogo-ops-per-second-usr-sys-time", 196.448819, "BogoOps/s");
            MetricAssert.Exists(metrics, "cpu-bogo-ops-per-second-real-time", 3239.115687, "BogoOps/s");
            MetricAssert.Exists(metrics, "cpu-wall-clock-time", 10.045186, "second");
            MetricAssert.Exists(metrics, "cpu-user-time", 160.510000, "second");
            MetricAssert.Exists(metrics, "cpu-system-time", 0, "second");
        }

        [Test]
        public void StressNgParserVerifyMetricsCpuVm()
        {
            string outputPath = Path.Combine(this.ExamplePath, "StressNgCpuVmExample.yaml");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new StressNgMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(12, metrics.Count);
            MetricAssert.Exists(metrics, "cpu-bogo-ops", 30539, "BogoOps");
            MetricAssert.Exists(metrics, "cpu-bogo-ops-per-second-usr-sys-time", 216.266553, "BogoOps/s");
            MetricAssert.Exists(metrics, "cpu-bogo-ops-per-second-real-time", 3049.623634, "BogoOps/s");
            MetricAssert.Exists(metrics, "cpu-wall-clock-time", 10.014023, "second");
            MetricAssert.Exists(metrics, "cpu-user-time", 141.210000, "second");
            MetricAssert.Exists(metrics, "cpu-system-time", 0, "second");

            MetricAssert.Exists(metrics, "vm-bogo-ops", 1067912, "BogoOps");
            MetricAssert.Exists(metrics, "vm-bogo-ops-per-second-usr-sys-time", 56803.829787, "BogoOps/s");
            MetricAssert.Exists(metrics, "vm-bogo-ops-per-second-real-time", 106785.171174, "BogoOps/s");
            MetricAssert.Exists(metrics, "vm-wall-clock-time", 10.000565, "second");
            MetricAssert.Exists(metrics, "vm-user-time", 18.030000, "second");
            MetricAssert.Exists(metrics, "vm-system-time", 0.770000, "second");
        }
    }
}