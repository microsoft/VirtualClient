
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
    public class FurmarkMeticsParser2Tests
    {
        private string rawText;
        private FurmarkXmlMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\Furmark\FurmarkExample2.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new FurmarkXmlMetricsParser (this.rawText);
        }

        [Test]
        public void FurmarkParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(15, metrics.Count);
            MetricAssert.Exists(metrics, "GPU1_AvgTemperatur ", 54.8125);   
            MetricAssert.Exists(metrics, "GPU2_AvgTemperatur ", 50.78125);  
            MetricAssert.Exists(metrics, "GPU1_AvgFPS ", 71.15625);         
            MetricAssert.Exists(metrics, "GPU2_AvgFPS ", 71.15625);         
            MetricAssert.Exists(metrics, "GPU1_MaxFPS ", 75);               
            MetricAssert.Exists(metrics, "GPU2_MaxFPS ", 75);               
            MetricAssert.Exists(metrics, "GPU1_MaxTemperatur ", 60);        
            MetricAssert.Exists(metrics, "GPU2_MaxTemperatur ", 56);        
            MetricAssert.Exists(metrics, "GPU1_Vddc ", 0);        
            MetricAssert.Exists(metrics, "GPU2_Vddc ", 0.95281250000000028); 
            MetricAssert.Exists(metrics, "GPU1_AvgCoreLoad ", 94.34375);
            MetricAssert.Exists(metrics, "GPU2_AvgCoreLoad ", 2.84375);
            MetricAssert.Exists(metrics, "GPU1_MaxCoreLoad ", 98);
            MetricAssert.Exists(metrics, "GPU2_MaxCoreLoad ", 4);
            MetricAssert.Exists(metrics, "check", 100);

        }

        [Test]
        public void FurmarkParserThrowIfInvalidOutputFormat()
        {
             string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string IncorrectFurmarkoutputPath = Path.Combine(workingDirectory, @"Examples\Furmark\FurmarkIncorrectResultsExample.txt");
            this.rawText = File.ReadAllText(IncorrectFurmarkoutputPath);
            this.testParser = new FurmarkXmlMetricsParser (this.rawText);
            SchemaException exception = Assert.Throws<SchemaException>(() => this.testParser.Parse());
            StringAssert.Contains("furmark workload didn't generate results files.", exception.Message);

        }
    }
}
