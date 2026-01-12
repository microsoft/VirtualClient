// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using CRC.VirtualClient.Actions;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ElasticsearchRallyServerExecutorTests : MockFixture
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
                { nameof(ElasticsearchRallyServerExecutor.DiskFilter), "osdisk:false&biggestsize" },
                { nameof(ElasticsearchRallyServerExecutor.Port), "9200" },
                { nameof(ElasticsearchRallyServerExecutor.PackageName), "elasticsearchrally" },
            };
        }

        [Test]
        public void TestElasticsearchRallyServerExecutorInitializeYmlNotFound()
        {
            SetupTest();

            bool commandExecuted = false;

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    commandExecuted = true;
                };
                
                Assert.ThrowsAsync<WorkloadException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task TestElasticsearchRallyServerExecutorInitialize(bool useWget)
        {
            SetupTest();

            bool commandExecuted = false;

            this.Parameters.Add("UseWgetForElasticsearhDownloadOnLinux", useWget);

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    commandExecuted = true;
                };

                executor.FileExists = true;
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task TestElasticsearchRallyServerExecutorExpectedRun()
        {
            SetupTest();

            bool commandExecuted = false;

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                commandExecuted = true;
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public void StartElasticsearchWin_ThrowsException_WhenDataDirectoryDoesNotExist()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            this.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(false);

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DataDirectory = this.Combine("C:", "nonexistent");

                var ex = Assert.ThrowsAsync<WorkloadException>(async () =>
                    await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None));

                Assert.IsTrue(ex.Message.Contains("data directory could not be found"));
                Assert.AreEqual(ErrorReason.WorkloadUnexpectedAnomaly, ex.Reason);
            }
        }

        [Test]
        [TestCase("win-x64", "x86_64")]
        [TestCase("win-arm64", "arm_64")]
        public async Task StartElasticsearchWin_DownloadsCorrectArchitecture(string platformArchitecture, string expectedArch)
        {
            this.Setup(PlatformID.Win32NT, platformArchitecture.EndsWith("x64") ? System.Runtime.InteropServices.Architecture.X64 : System.Runtime.InteropServices.Architecture.Arm64);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            string actualDownloadUrl = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.TestPlatformArchitectureName = platformArchitecture;
                executor.OnDownloadFile = (url, path, ct) =>
                {
                    actualDownloadUrl = url;
                    return Task.CompletedTask;
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(actualDownloadUrl);
            Assert.IsTrue(actualDownloadUrl.Contains($"windows-{expectedArch}"));
            Assert.IsTrue(actualDownloadUrl.Contains("elasticsearch-8.15.0"));
        }

        [Test]
        public async Task StartElasticsearchWin_ExtractsZipFile()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var extractCommandExecuted = false;
            string extractCommand = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (arguments.Contains("Expand-Archive"))
                    {
                        extractCommandExecuted = true;
                        extractCommand = arguments;
                    }
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(extractCommandExecuted);
            Assert.IsNotNull(extractCommand);
            Assert.IsTrue(extractCommand.Contains("Expand-Archive"));
            Assert.IsTrue(extractCommand.Contains("elasticsearch-8.15.0.zip"));
        }

        [Test]
        public async Task StartElasticsearchWin_CopiesElasticsearchYmlToConfigDirectory()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var ymlCopied = false;
            string sourcePath = null;
            string destinationPath = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnFileCopy = (source, dest, overwrite) =>
                {
                    sourcePath = source;
                    destinationPath = dest;
                    ymlCopied = true;
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(ymlCopied);
            Assert.IsNotNull(sourcePath);
            Assert.IsNotNull(destinationPath);
            Assert.IsTrue(sourcePath.Contains("elasticsearch.yml"));
            Assert.IsTrue(destinationPath.Contains("config"));
            Assert.IsTrue(destinationPath.Contains("elasticsearch.yml"));
        }

        [Test]
        public async Task StartElasticsearchWin_DisablesFirewall()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var firewallCommandExecuted = false;
            string firewallCommand = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (arguments.Contains("Set-NetFirewallProfile"))
                    {
                        firewallCommandExecuted = true;
                        firewallCommand = arguments;
                    }
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(firewallCommandExecuted);
            Assert.IsNotNull(firewallCommand);
            Assert.IsTrue(firewallCommand.Contains("Set-NetFirewallProfile"));
            Assert.IsTrue(firewallCommand.Contains("Enabled False"));
        }

        [Test]
        [TestCase(9200)]
        [TestCase(9300)]
        [TestCase(8080)]
        public async Task StartElasticsearchWin_StartsElasticsearchWithCorrectPort(int port)
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters[nameof(ElasticsearchRallyServerExecutor.Port)] = port.ToString();
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var startCommandExecuted = false;
            string startCommand = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (arguments.Contains("elasticsearch.bat"))
                    {
                        startCommandExecuted = true;
                        startCommand = arguments;
                    }
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(startCommandExecuted);
            Assert.IsNotNull(startCommand);
            Assert.IsTrue(startCommand.Contains("elasticsearch.bat"));
            Assert.IsTrue(startCommand.Contains($"-E http.port={port}"));
        }

        [Test]
        public async Task StartElasticsearchWin_VerifiesElasticsearchIsRunning()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var pingCommandExecuted = false;
            string pingCommand = null;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (arguments.Contains("Invoke-WebRequest") && arguments.Contains("localhost:9200"))
                    {
                        pingCommandExecuted = true;
                        pingCommand = arguments;
                    }
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(pingCommandExecuted);
            Assert.IsNotNull(pingCommand);
            Assert.IsTrue(pingCommand.Contains("Invoke-WebRequest"));
            Assert.IsTrue(pingCommand.Contains("localhost:9200"));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task StartElasticsearchWin_UsesCorrectDownloadMethod(bool useWebRequest)
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");
            this.Parameters.Add("UseWebRequestForElasticsearchDownloadOnWindows", useWebRequest);

            var webRequestUsed = false;
            var parallelDownloadUsed = false;

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");
                executor.OnRunCommand = (command, arguments) =>
                {
                    if (arguments.Contains("Invoke-WebRequest") && arguments.Contains("artifacts.elastic.co"))
                    {
                        webRequestUsed = true;
                    }
                };
                executor.OnDownloadFile = (url, path, ct) =>
                {
                    parallelDownloadUsed = true;
                    return Task.CompletedTask;
                };

                await executor.TestStartElasticsearchWin(EventContext.None, CancellationToken.None);
            }

            if (useWebRequest)
            {
                Assert.IsTrue(webRequestUsed);
                Assert.IsFalse(parallelDownloadUsed);
            }
            else
            {
                Assert.IsFalse(webRequestUsed);
                Assert.IsTrue(parallelDownloadUsed);
            }
        }

        [Test]
        public async Task StartElasticsearchWin_AddsCorrectTelemetryContext()
        {
            this.Setup(PlatformID.Win32NT);
            this.Parameters.Add("ElasticsearchVersion", "8.15.0");

            var telemetryContext = new EventContext(Guid.NewGuid());

            using (var executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.FileExists = true;
                executor.DirectoryExists = true;
                executor.DataDirectory = this.Combine("C:", "data");

                await executor.TestStartElasticsearchWin(telemetryContext, CancellationToken.None);
            }

            Assert.IsTrue(telemetryContext.Properties.ContainsKey("elasticsearchVersion"));
            Assert.IsTrue(telemetryContext.Properties.ContainsKey("port"));
            Assert.IsTrue(telemetryContext.Properties.ContainsKey("platformArchitecture"));
            Assert.IsTrue(telemetryContext.Properties.ContainsKey("distroVersion"));
        }

        private class TestElasticsearchRallyServerExecutor : ElasticsearchRallyServerExecutor
        {
            public Action<string, string> OnRunCommand { get; set; }
            public Action<string, string, bool> OnFileCopy { get; set; }
            public Func<string, string, CancellationToken, Task> OnDownloadFile { get; set; }
            public bool FileExists { get; set; }
            public bool DirectoryExists { get; set; }
            public string DataDirectory { get; set; }
            public string TestPlatformArchitectureName { get; set; }

            public TestElasticsearchRallyServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
                this.DataDirectory = "/data";
                this.PackageName = "elasticsearchrally";
                this.WaitForElasticsearchAvailabilityTimeout = 0;
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public async Task TestStartElasticsearchWin(EventContext context, CancellationToken cancellationToken)
            {
                var method = typeof(ElasticsearchRallyServerExecutor).GetMethod(
                    "StartElasticsearchWin",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                await (Task)method.Invoke(this, new object[] { context, cancellationToken });
            }

            protected override Task<string> GetDataDirectoryAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(this.DataDirectory);
            }

            protected override bool RunCommand(string command, string arguments, out string output, out string error)
            {
                output = string.Empty;
                error = string.Empty;

                OnRunCommand?.Invoke(command, arguments);

                return true;
            }
            protected override void RunCommandWindowsScriptDetached(EventContext telemetryContext, string key, string script)
            {
                OnRunCommand?.Invoke(key, script);
            }

            protected override bool CheckFileExists(string path)
            {
                return this.FileExists;
            }

            protected override bool CheckDirectoryExists(string path)
            {
                return this.DirectoryExists;
            }

            protected override void WriteAllText(string path, string content)
            {
                
            }

            protected override string ReadAllText(string path)
            {
                return "sample text";
            }

            protected override void FileCopy(string sourcePath, string destinationPath, bool overwrite)
            {
                OnFileCopy?.Invoke(sourcePath, destinationPath, overwrite);
            }

            protected override Task ParallelDownloadFile(string url, string destinationPath, CancellationToken cancellationToken)
            {
                OnDownloadFile?.Invoke(url, destinationPath, cancellationToken);

                return Task.CompletedTask;
            }
        }
    }
}
