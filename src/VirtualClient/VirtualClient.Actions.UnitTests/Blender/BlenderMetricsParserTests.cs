// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp.Framing;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BlenderMetricsParserTests
    {
        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Blender");
            }
        }


        [Test]
        public void BlenderMetricsParserTestsCorrectly_MultipleScenes()
        {
            string outputPath = Path.Combine(ExamplePath, "cpu_results_example.txt");
            string rawText = File.ReadAllText(outputPath);

            BlenderMetricsParser testParser = new BlenderMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, "monster", 1166.1962830821662, "samples_per_minute");
            MetricAssert.Exists(metrics, "junkshop", 642.9505285066615, "samples_per_minute");
            MetricAssert.Exists(metrics, "classroom", 561.2825000856167, "samples_per_minute");
        }

        //[Test]
        //public void BlenderMetricsParserTestsCorrectly_OneViewSet()
        //{
        //    string outputPath = Path.Combine(ExamplePath, "resultCSV_oneViewSet.csv");
        //    string rawText = File.ReadAllText(outputPath);

        //    BlenderMetricsParser testParser = new BlenderMetricsParser(rawText);
        //    IList<Metric> metrics = testParser.Parse();
        //    IList<Metric> expected = new List<Metric>();

        //    expected.Add(new Metric("3dsmax-07", 29.33, "fps"));

        //    MetricAssert.Equals(metrics, expected);
        //}

    }
}