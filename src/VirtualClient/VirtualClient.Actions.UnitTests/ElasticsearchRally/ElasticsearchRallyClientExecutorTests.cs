// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using CRC.VirtualClient.Actions;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ElasticsearchRallyClientExecutorTests : MockFixture
    {
        private IEnumerable<Disk> disks;
        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);

            this.File.Reset();
            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.FileSystem.SetupGet(fs => fs.File).Returns(this.File.Object);

            string agentId = $"{Environment.MachineName}";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.disks = this.CreateDisks(PlatformID.Unix, true);

            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);

            this.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(ElasticsearchRallyClientExecutor.DiskFilter), "osdisk:false&biggestsize" },
                { nameof(ElasticsearchRallyClientExecutor.DistributionVersion), "8.0.0" },
                { nameof(ElasticsearchRallyClientExecutor.Port), "9200" },
                { nameof(ElasticsearchRallyClientExecutor.RallyTestMode), true },
                { nameof(ElasticsearchRallyClientExecutor.Scenario), "ExecuteGeoNamesBenchmark" },
                { nameof(ElasticsearchRallyClientExecutor.TrackName), "geonames" },
            };
        }

        [Test]
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void TestElasticsearchRallyClientExecutorWhenReportNotGenerated(bool rallyConfigured, bool serverAvailable)
        {
            SetupTest();

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new ElasticsearchRallyState()
            {
                RallyConfigured = rallyConfigured,
            }));

            using (TestElasticsearchRallyClientExecutor executor = new TestElasticsearchRallyClientExecutor(this.Dependencies, this.Parameters))
            {
                executor.ServerAvailable = serverAvailable;
                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public void TestElasticsearchRallyClientExecutorWhenRallyConfiguredAndReportFailure()
        {
            SetupTest();

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new ElasticsearchRallyState()
            {
                RallyConfigured = true,
            }));

            bool commandExecuted = false;

            using (TestElasticsearchRallyClientExecutor executor = new TestElasticsearchRallyClientExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (command == "/usr/bin/sudo" && arguments.Contains("python3 -m pipx run esrally race"))
                    {
                        commandExecuted = true;
                    }
                };

                executor.ReportCsvExists = true;

                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(EventContext.None, CancellationToken.None));
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public void TestElasticsearchRallyClientExecutorWhenRallyConfiguredAndReportWithInsufficientData()
        {
            SetupTest();

            bool logMessageCaptured = false;

            // Use VirtualClient's LogMessage extension method
            this.Logger.OnLog = (level, eventId, state, exception) =>
            {
                if (eventId.Name.Contains("RallyReportCsvInsufficientData"))
                {
                    logMessageCaptured = true;
                }
            };

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new ElasticsearchRallyState()
            {
                RallyConfigured = true,
            }));

            bool commandExecuted = false;

            using (TestElasticsearchRallyClientExecutor executor = new TestElasticsearchRallyClientExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (command == "/usr/bin/sudo" && arguments.Contains("python3 -m pipx run esrally race"))
                    {
                        commandExecuted = true;
                    }
                };

                executor.ReportCsvExists = true;
                executor.ReportLines = new string[]
                {
                    "Metric,Task,Value,Unit"
                };

                executor.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(logMessageCaptured);
            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public void TestElasticsearchRallyClientExecutorWhenRallyMetricReaderFailure()
        {
            SetupTest();

            this.Parameters.Remove(nameof(ElasticsearchRallyClientExecutor.Scenario));

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new ElasticsearchRallyState()
            {
                RallyConfigured = true,
            }));

            using (TestElasticsearchRallyClientExecutor executor = new TestElasticsearchRallyClientExecutor(this.Dependencies, this.Parameters))
            {                
                executor.ReportCsvExists = true;
                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                executor.ReportLines = System.IO.File.ReadAllLines(Path.Combine(currentDirectory, "Examples", "ElasticsearchRally", "ElasticsearchRallyExample.txt"));

                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

            }
        }
        [Test]
        public async Task TestElasticsearchRallyClientExecutorWhenRallyConfiguredAndReportGenerated()
        {
            SetupTest();

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new ElasticsearchRallyState()
            {
                RallyConfigured = true,
            }));

            bool commandExecuted = false;

            using (TestElasticsearchRallyClientExecutor executor = new TestElasticsearchRallyClientExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (command == "/usr/bin/sudo" && arguments.Contains("python3 -m pipx run esrally race"))
                    {
                        commandExecuted = true;
                    }
                };

                executor.ReportCsvExists = true;
                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                executor.ReportLines = System.IO.File.ReadAllLines(Path.Combine(currentDirectory, "Examples", "ElasticsearchRally", "ElasticsearchRallyExample.txt"));

                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(commandExecuted);
        }

        private class TestElasticsearchRallyClientExecutor : ElasticsearchRallyClientExecutor
        {
            public Action<string, string> OnRunCommand { get; set; }
            public bool ServerAvailable { get; set; }
            public bool ReportCsvExists { get; set; }
            public string[] ReportLines { get; set; }

            public TestElasticsearchRallyClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            protected override bool RunCommand(string command, string arguments, out string output, out string error)
            {
                output = string.Empty;
                error = string.Empty;

                OnRunCommand?.Invoke(command, arguments);

                return true;
            }

            protected override bool CheckServerAvailable(EventContext telemetryContext, string targetHost, int port, int timeout)
            {
                return this.ServerAvailable;
            }

            protected override bool CheckFileExists(string path)
            {
                return this.ReportCsvExists;
            }

            override protected string[] ReadReportLines(string reportPath)
            {
                return this.ReportLines;
            }
        }

    }
}
