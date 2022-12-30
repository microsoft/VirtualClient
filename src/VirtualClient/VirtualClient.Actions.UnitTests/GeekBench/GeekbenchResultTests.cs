// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class GeekbenchResultTests
    {
        public static string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Examples", "Geekbench", "GeekbenchResults.txt");
        private MockFixture mockFixture = new MockFixture();

        [Test]
        public void GeekbenchResultSupportsResultsInJsonFormat()
        {
            string json = File.ReadAllText(filePath);
            bool success = GeekbenchResult.TryParseGeekbenchResult(json, out GeekbenchResult geekbenchResult);

            Assert.IsTrue(success);
            Assert.IsNotNull(geekbenchResult);
        }

        [Test]
        [TestCase("")]
        [TestCase(null)]
        public void GeekbenchResultValidatesTheResults(string json)
        {
            Assert.Throws<ArgumentNullException>(() => GeekbenchResult.TryParseGeekbenchResult(json, out GeekbenchResult geekbenchResult));
        }

        [Test]
        [TestCase(@"{ ""key"" : ""value"", ""some key""}")]
        [TestCase("not a json string")]
        public void GeekbenchResultValidatesTheResults_Scenario2(string json)
        {
            bool success = GeekbenchResult.TryParseGeekbenchResult(json, out GeekbenchResult geekbenchResult);
            Assert.IsFalse(success);
            Assert.IsNull(geekbenchResult);
        }

        [Test]
        public void GeekbenchResultParsesJsonResultsCorrectly()
        {
            string json = File.ReadAllText(filePath);
            GeekbenchResult geekbenchResult = JsonConvert.DeserializeObject<GeekbenchResult>(json);
            Dictionary<string, Metric> results = new Dictionary<string, Metric>(geekbenchResult.GetResults());

            // expected: 92 results
            // 46 results/section, 2 sections (Single-Core and Multi-Core)
            // each section has a score, three subsection scores, and 21 workloads that each produce a score and numerical result: 46

            Dictionary<string, Metric> expectedSubsectionResults = new Dictionary<string, Metric>()
            {
                { "Cryptography Score (Single-Core)", new Metric("Test Score", 814, "score") },
                { "Integer Score (Single-Core)", new Metric("Test Score", 416, "score") },
                { "FloatingPoint Score (Single-Core)", new Metric("Test Score", 512, "score") },
                { "Cryptography Score (Multi-Core)", new Metric("Test Score", 1202, "score") },
                { "Integer Score (Multi-Core)", new Metric("Test Score", 878, "score") },
                { "FloatingPoint Score (Multi-Core)", new Metric("Test Score", 1209, "score") }
            };

            Assert.IsInstanceOf(typeof(Dictionary<string, Metric>), results);
            Assert.IsTrue(results.Count == 92);

            expectedSubsectionResults.ToList().ForEach(x =>
            {
                Assert.IsTrue(results.TryGetValue(x.Key, out Metric metric));
                Assert.AreEqual(x.Value.Name, metric.Name);
                Assert.AreEqual(x.Value.Value, metric.Value);
                Assert.AreEqual(x.Value.Unit, metric.Unit);
            });
        }
    }
}