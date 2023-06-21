// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class MetricsCsvFileLifecycleHooksTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        public void LifecycleHooksAddsTheExpectedCsvColumnHeadersToTheStream()
        {
            List<string> expectedColumnHeaders = new List<string>
            {
                "Timestamp",
                "ExperimentId",
                "ClientId",
                "Profile",
                "ProfileName",
                "ToolName",
                "ScenarioName",
                "ScenarioStartTime",
                "ScenarioEndTime",
                "MetricCategorization",
                "MetricName",
                "MetricValue",
                "MetricUnit",
                "MetricDescription",
                "MetricRelativity",
                "ExecutionSystem",
                "OperatingSystemPlatform",
                "OperationId",
                "OperationParentId",
                "AppHost",
                "AppName",
                "AppVersion",
                "AppTelemetryVersion",
                "Tags"
            };

            using (MemoryStream stream = new MemoryStream())
            {
                MetricsCsvFileLifecycleHooks lifecycleHooks = new MetricsCsvFileLifecycleHooks(this.mockFixture.FileSystem.Object);
                lifecycleHooks.OnFileOpened(stream, Encoding.UTF8);

                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string actualColumnHeaders = reader.ReadToEnd();
                    Assert.AreEqual(string.Join(",", expectedColumnHeaders.Select(field => $"\"{field}\"")), actualColumnHeaders);
                }
            }
        }

        [Test]
        public void LifecycleHooksDoesNotAddDuplicateHeadersToFilesWithExistingCsvContent()
        {
            List<string> expectedColumnHeaders = new List<string>
            {
                "Timestamp",
                "ExperimentId",
                "ClientId",
                "Profile",
                "ProfileName",
                "ToolName",
                "ScenarioName",
                "ScenarioStartTime",
                "ScenarioEndTime",
                "MetricCategorization",
                "MetricName",
                "MetricValue",
                "MetricUnit",
                "MetricDescription",
                "MetricRelativity",
                "ExecutionSystem",
                "OperatingSystemPlatform",
                "OperationId",
                "OperationParentId",
                "AppHost",
                "AppName",
                "AppVersion",
                "AppTelemetryVersion",
                "Tags"
            };

            string expectedColumns = string.Join(",", expectedColumnHeaders.Select(field => $"\"{field}\""));

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(expectedColumns)))
            {
                MetricsCsvFileLifecycleHooks lifecycleHooks = new MetricsCsvFileLifecycleHooks(this.mockFixture.FileSystem.Object);
                lifecycleHooks.OnFileOpened(stream, Encoding.UTF8);

                // Should NOT add any additional headers here.
                lifecycleHooks.OnFileOpened(stream, Encoding.UTF8);
                lifecycleHooks.OnFileOpened(stream, Encoding.UTF8);

                stream.Position = 0;
                using (StreamReader reader = new StreamReader(stream))
                {
                    string actualColumnHeaders = reader.ReadToEnd();
                    Assert.AreEqual(expectedColumns, actualColumnHeaders);
                }
            }
        }
    }
}
