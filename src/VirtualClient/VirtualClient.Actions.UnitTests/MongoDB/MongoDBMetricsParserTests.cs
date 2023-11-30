namespace VirtualClient.Actions.MongoDB
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    public class MongoDBMetricsParserTests
    {
        private string rawText;
        private MongoDBMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\MongoDB\MongoInsertData01.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new MongoDBMetricsParser(this.rawText);
        }
        [Test]
        public void MongoDBMetricsParserParsesAsExpected()
        {
            this.testParser.Parse();
            Assert.IsNotNull(this.testParser.Sections["Metrics"]);
        }
    }
}
