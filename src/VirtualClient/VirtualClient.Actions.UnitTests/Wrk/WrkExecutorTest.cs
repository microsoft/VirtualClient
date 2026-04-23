// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Contracts;
    using Microsoft.CodeAnalysis;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using Architecture = System.Runtime.InteropServices.Architecture;

    [TestFixture]
    [Category("Unit")]
    public class WrkExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyFixture dependencyFixture;
        private InMemoryProcess memoryProcess;
        private Dictionary<string, IConvertible> defaultProperties;
        private string wrkPackageName = "wrk";
        private string scriptPackageName = "wrkconfiguration";

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
            this.memoryProcess = new InMemoryProcess
            {
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true
            };
            this.defaultProperties = new Dictionary<string, IConvertible>()
            {
                { "PackageName", this.wrkPackageName },
                { "Scenario", "{ThreadCount}th_{Connection}c_{FileSizeInKB}kb" },
                { "CommandArguments", "--latency --threads{ThreadCount} --connections{Connection} --duration{Duration.TotalSeconds}s" },
                { "Connection", 100 },
                { "ThreadCount", 10 },
                { "MaxCoreCount", 10 },
                { "TestDuration", "00:02:30"},
                { "Timeout", "00:20:00"},
                { "FileSizeInKB", 10},
                { "Role", "Client"},
                { "Tags", "Networking,NGINX,WRK"},
            };

            dependencyFixture = new DependencyFixture();
        }

        public void SetSingleServerInstance()
        {
            // Setup:
            // One server instance running on port 9876 with affinity to 4 logical processors
            this.mockFixture.ApiClient.OnGetState(nameof(ServerState))
               .ReturnsAsync(this.mockFixture.CreateHttpResponse(
                   HttpStatusCode.OK,
                   new Item<ServerState>(nameof(ServerState), new ServerState(new List<PortDescription>
                   {
                        new PortDescription
                        {
                            CpuAffinity = "0,1,2,3",
                            Port = 9876
                        }
                   }))));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns<string, ClientInstance>((id, instance) => this.mockFixture.ApiClient.Object);

            this.mockFixture.ApiClient.OnGetHeartbeat()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        [TestCase("-L -t 10 -c 50 -d 100s --timeout 10s https://{serverip}/api_new/1kb")]
        [TestCase("{serverip}_{clientip}_{reverseproxyip}")]
        public void WrkClientExecutorReturnsCorrectArguments(string commandArg)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);
            ClientInstance reverseProxyInstance = new ClientInstance(name: nameof(ClientRole.ReverseProxy), ipAddress: "9.0.1.2", role: ClientRole.ReverseProxy);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance, reverseProxyInstance });

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "CommandArguments", commandArg }
            };

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            string results = commandArg
                .Replace("{serverip}", "1.2.3.4")
                .Replace("{clientip}", "5.6.7.8")
                .Replace("{reverseproxyip}", "9.0.1.2");

            Assert.AreEqual(executor.GetCommandLineArguments(), results);
        }

        [Test]
        public async Task WrkClientExecutorRunsWorkloadWithCorrectArguments()
        {
            string commandArgumentInput = @"--latency --threads 5 --connections 100 --duration 60s --timeout 10s https://{serverip}/api_new/5kb";
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);
            ClientInstance reverseProxyInstance = new ClientInstance(name: nameof(ClientRole.ReverseProxy), ipAddress: "9.0.1.2", role: ClientRole.ReverseProxy);

            string directory = @"some/random\dir/name/";
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance, reverseProxyInstance });
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                {"CommandArguments", commandArgumentInput },
                { "Scenario", "bar" },
                { "ToolName", "wrk" },
                { "PackageName", "wrk" },
                { "FileSizeInKB", 5},
                { "TestDuration", TimeSpan.FromSeconds(60).ToString() },
                { "TargetService", "server"}
            };

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            executor.PackageDirectory = directory;
            this.SetUpWorkloadOutput();
            string result = executor.GetCommandLineArguments();

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string file) =>
                {
                    string result = executor.Combine(directory, "runwrk.sh");
                    Assert.AreEqual(file, result);
                });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string results = commandArgumentInput.Replace("{serverip}", "1.2.3.4");
                Assert.AreEqual(command, "sudo");
                if (arguments.Contains("--version"))
                {
                    Assert.AreEqual(arguments, $"bash {executor.Combine(directory, "runwrk.sh")} --version");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                }
                else
                {
                    Assert.AreEqual(arguments, $"bash {executor.Combine(directory, "runwrk.sh")} {results}");
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
                    string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
                }
                Assert.AreEqual(workingDir, directory);
                return this.memoryProcess;
            };

            await executor.ExecuteWorkloadAsync(result, workingDir: directory).ConfigureAwait(false);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task WrkClientExecutorSetsServerWarmedUpFlagAfterWarmupExecution(PlatformID platform, Architecture architecture)
        {
            // Arrange
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);

            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance });

            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            dependencyFixture.SetupPackage(this.scriptPackageName);

            // Setup parameters with WarmUp=true
            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.Parameters.Add("WarmUp", true);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string packageName, CancellationToken cancellationToken) =>
                    dependencyFixture.PackageManager.FirstOrDefault(pkg => pkg.Name == packageName));

            // Setup API client for server heartbeat and state
            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() =>
                {
                    Item<State> result = new Item<State>(nameof(State), new State());
                    result.Definition.Online(true);
                    return this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, result);
                });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments.Contains("--version"))
                {
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                }
                else
                {
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
                    string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
                }

                return this.memoryProcess;
            };

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);

            // Act
            await executor.InitializeAsync().ConfigureAwait(false);
            await executor.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.IsTrue(executor.GetIsServerWarmedUp(), "IsServerWarmedUp flag should be set to true after warm-up execution");
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void WrkClientExecutorThrowsErrorIfPlatformIsWrong(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Parameters = this.defaultProperties;
            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            Assert.IsFalse(VirtualClientComponent.IsSupported(executor));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void WrkExecutorOnlySupportsWrkandWrk2(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                 { "PackageName", "wrk2" },
            };

            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage("wrk2");
            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.FirstOrDefault());

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            DependencyException exc = Assert.ThrowsAsync<DependencyException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });

            Assert.AreEqual(exc.Message, "TestWrkExecutor did not find correct package in the directory. Supported Package: wrk. Package Provided: wrk2");
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void WrkClientExecutorThrowsErrorIfPackageIsMissing(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.SetSingleServerInstance();
            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            Assert.ThrowsAsync<DependencyException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });

            this.mockFixture.Parameters = this.defaultProperties;
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.FirstOrDefault());

            TestWrkExecutor executor2 = new TestWrkExecutor(this.mockFixture);
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await executor2.InitializeAsync().ConfigureAwait(false);
            });

        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task WrkClientExecutorSkipsExecutionWhenWarmupAndServerIsWarmedUp(PlatformID platform, Architecture architecture)
        {
            // Arrange
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);

            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance });

            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            dependencyFixture.SetupPackage(this.scriptPackageName);

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.Parameters.Add("WarmUp", true);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string packageName, CancellationToken cancellationToken) =>
                    dependencyFixture.PackageManager.FirstOrDefault(pkg => pkg.Name == packageName));

            // Used to track if ExecuteWorkloadAsync is called
            bool workloadExecuted = false;

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            executor.SetIsServerWarmedUp(true); // Simulate server already warmed up

            // Override the ExecuteWorkloadAsync to track if it's called
            executor.ExecuteWorkloadAsyncCallback = () => workloadExecuted = true;

            // Act
            await executor.InitializeAsync().ConfigureAwait(false);
            await executor.ExecuteAsync().ConfigureAwait(false);

            // Assert
            Assert.IsFalse(workloadExecuted, "WorkloadAsync should not be executed when WarmUp=true and server is already warmed up");
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task WrkClientExecutorSetsUpWrkClientForFirstTime(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            dependencyFixture.SetupPackage(this.scriptPackageName);
            TestWrkExecutor dummyExecutor = new TestWrkExecutor(this.mockFixture);

            string wrkPackagePath = dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault().Path;
            string scriptPackagePath = dummyExecutor.ToPlatformSpecificPath(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault(), platform, architecture).Path;

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.Is<string>(x => x == "reset.sh")))
                .Returns(false);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.Is<string>(x => x != "reset.sh")))
                .Returns(true);

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.scriptPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault());

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.wrkPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault());

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            await executor.InitializeAsync().ConfigureAwait(false);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task WrkClientExecutorInitializesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, nameof(State));
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            dependencyFixture.SetupPackage(this.scriptPackageName);
            TestWrkExecutor dummyExecutor = new TestWrkExecutor(this.mockFixture);

            string wrkPackagePath = dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault().Path;
            string scriptPackagePath = dummyExecutor.ToPlatformSpecificPath(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault(), platform, architecture).Path;

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string file) =>
                {
                    if (file.EndsWith("wrk"))
                    {
                        string result = dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "wrk");
                        Assert.AreEqual(file, result);
                    }
                    else
                    {
                        string result = dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath, Path.GetFileName(file));
                        Assert.AreEqual(file, result);
                    }
                });

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.scriptPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault());

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.wrkPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault());

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            await executor.InitializeAsync().ConfigureAwait(false);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "server")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "server")]
        [TestCase(PlatformID.Unix, Architecture.X64, "rp")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "rp")]
        public async Task WrkClientExecutorExecutesAsyncAsExpected(PlatformID platform, Architecture architecture, string targetService)
        {
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);
            ClientInstance reverseProxyInstance = new ClientInstance(name: nameof(ClientRole.ReverseProxy), ipAddress: "9.0.1.2", role: ClientRole.ReverseProxy);

            this.mockFixture.Setup(platform, architecture, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance, reverseProxyInstance });
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.wrkPackageName);
            dependencyFixture.SetupPackage(this.scriptPackageName);
            TestWrkExecutor dummyExecutor = new TestWrkExecutor(this.mockFixture);
            string wrkPackagePath = dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault().Path;
            string scriptPackagePath = dummyExecutor.ToPlatformSpecificPath(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault(), platform, architecture).Path;

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.Parameters.Add("TargetService", targetService);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string file) =>
                {
                    if (file.EndsWith("wrk"))
                    {
                        string result = dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "wrk");
                        Assert.AreEqual(file, result);
                    }
                    else if (file.EndsWith("runwrk.sh"))
                    {
                        string result = dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh");
                        Assert.AreEqual(file, result);
                    }
                    else
                    {
                        string result = dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath, Path.GetFileName(file));
                        Assert.AreEqual(file, result);
                    }
                });

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.scriptPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptPackageName).FirstOrDefault());

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.wrkPackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.wrkPackageName).FirstOrDefault());

            // create local state
            this.mockFixture.ApiClient
                .Setup(x => x.CreateStateAsync<State>(It.Is<string>(y => y == nameof(State)), It.IsAny<State>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK))
                .Callback((string id, State state, CancellationToken _, IAsyncPolicy<HttpResponseMessage> __) =>
                {
                    Assert.IsTrue(state.Online());
                });

            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() =>
                {
                    Item<State> result = new Item<State>(nameof(State), new State());
                    result.Definition.Online(true);
                    return this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, result);
                });

            // delete local state before exiting
            this.mockFixture.ApiClient.Setup(x => x.DeleteStateAsync(It.Is<string>(y => y == nameof(State)), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.AreEqual(command, "sudo");

                if (arguments.Contains("chmod"))
                {
                    Assert.AreEqual(arguments, $"chmod +x \"{dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "wrk")}\"");
                }
                else if (arguments.Contains("setup-config"))
                {
                    Assert.AreEqual(arguments, $"bash setup-config.sh");
                }
                else if (arguments.Contains("--version"))
                {
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")} --version"));
                }
                else
                {
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")}"));
                }

                return this.memoryProcess;
            };
            //this.SetUpWorkloadOutput();
            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            await executor.InitializeAsync().ConfigureAwait(false);
            Assert.ThrowsAsync<WorkloadException>(async () =>
            {
                await executor.ExecuteAsync().ConfigureAwait(false);
            });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.AreEqual(command, "sudo");

                if (arguments.Contains("chmod"))
                {
                    Assert.AreEqual(arguments, $"chmod +x \"{dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "wrk")}\"");
                }
                else if (arguments.Contains("setup-config"))
                {
                    Assert.AreEqual(arguments, $"bash setup-config.sh");
                }
                else if (arguments.Contains("--version"))
                {
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")} --version"));
                }
                else
                {
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")}"));
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
                    string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
                }

                return this.memoryProcess;
            };
            TestWrkExecutor executor2 = new TestWrkExecutor(this.mockFixture);
            await executor2.InitializeAsync().ConfigureAwait(false);
            await executor2.ExecuteAsync().ConfigureAwait(false);

            this.mockFixture.ApiClient.Verify(x => x.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
        }

        [Test]
        public async Task GetWrkVersionReturnsCorrectVersion()
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            string expectedVersion = "4.2.0";
            string wrkOutput = $"wrk {expectedVersion} [epoll] Copyright (C) 2012 Will Glozer";

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput("--version", wrkOutput);

            string actualVersion = await executor.GetWrkVersionAsync();

            Assert.AreEqual(expectedVersion, actualVersion);
            this.mockFixture.Tracking.AssertCommandsExecuted(true,
                $"sudo bash {Regex.Escape(executor.Combine(executor.PackageDirectory, WrkExecutor.WrkRunShell))} --version"
            );
        }

        [Test]
        public async Task GetWrkVersion_ReturnsNull_WhenVersionCannotBeParsed()
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput("--version", "Invalid output without version");

            string version = await executor.GetWrkVersionAsync();
            Assert.IsNull(version);

            this.mockFixture.Tracking.AssertCommandsExecuted(true, "sudo bash .* --version");
        }

        [Test]
        public async Task WrkClientExecutorRunsWorkloadWithAffinityUsingCorrectQuoting()
        {
            string commandArgumentInput = @"--latency --threads 8 --connections 256 --duration 30s --timeout 10s http://1.2.3.4:9876/json --header ""Accept: application/json""";
            ClientInstance serverInstance = new ClientInstance(name: nameof(ClientRole.Server), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(ClientRole.Client), ipAddress: "5.6.7.8", role: ClientRole.Client);

            string directory = @"/some/random/dir/name/";
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance });
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "CommandArguments", commandArgumentInput },
                { "Scenario", "affinity_test" },
                { "ToolName", "wrk" },
                { "PackageName", "wrk" },
                { "BindToCores", true },
                { "CoreAffinity", "8-15" },
                { "TargetService", "server" }
            };

            TestWrkExecutor executor = new TestWrkExecutor(this.mockFixture);
            executor.PackageDirectory = directory;

            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true);

            string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
            string wrkOutput = File.ReadAllText(Path.Combine(examplesDirectory, @"wrkStandardExample1.txt"));

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput("--version", "wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer")
                .SetupProcessOutput("numactl", wrkOutput);

            string result = executor.GetCommandLineArguments();
            await executor.ExecuteWorkloadAsync(result, workingDir: directory).ConfigureAwait(false);

            // The affinity path uses GetAffinityProcessInfo which sets numactl as the
            // executable, then CreateElevatedProcess wraps it with sudo to ensure
            // ulimit and process elevation work correctly (matching non-affinity path).
            string scriptPath = Regex.Escape(executor.Combine(directory, WrkExecutor.WrkRunShell));
            this.mockFixture.Tracking.AssertCommandsExecuted(true,
                $@"sudo numactl -C 8-15 bash {scriptPath} {Regex.Escape(commandArgumentInput)}");
        }

        public void SetUpWorkloadOutput()
        {
            string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
            string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
            this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(""));
        }

        private class TestWrkExecutor : WrkExecutor
        {
            public Action ExecuteWorkloadAsyncCallback { get; set; }

            public TestWrkExecutor(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
                this.ServerApi = mockFixture.ApiClient.Object;
                this.ReverseProxyApi = mockFixture.ApiClient.Object;
                this.ClientFlowRetryPolicy = Policy.NoOpAsync();
                this.ClientRetryPolicy = Policy.NoOpAsync();
            }

            public new void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }

            public string GetCommandLineArguments()
            {
                return base.GetCommandLineArguments(CancellationToken.None);
            }

            public bool GetIsServerWarmedUp()
            {
                return base.IsServerWarmedUp;
            }

            public async Task<string> GetWrkVersionAsync()
            {
                return await base.GetWrkVersionAsync(EventContext.None, CancellationToken.None);
            }

            public void SetIsServerWarmedUp(bool value)
            {
                base.IsServerWarmedUp = value;
            }

            public async Task InitializeAsync()
            {
                await base.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task SetupWrkClient()
            {
                await base.SetupWrkClient(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task ExecuteWorkloadAsync(string commandArguments, string workingDir)
            {
                await base.ExecuteWorkloadAsync(commandArguments, workingDir, EventContext.None, CancellationToken.None).ConfigureAwait(false);
                ExecuteWorkloadAsyncCallback?.Invoke();
            }

            public async Task ExecuteAsync()
            {
                await base.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
