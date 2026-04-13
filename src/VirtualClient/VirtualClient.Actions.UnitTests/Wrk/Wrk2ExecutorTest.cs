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
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
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
    public class Wrk2ExecutorTests
    {
        private string ClientStateId = nameof(ClientStateId);
        private string ServerStateId = nameof(ServerStateId);

        private MockFixture mockFixture;
        private DependencyFixture dependencyFixture;
        private InMemoryProcess memoryProcess;
        private Dictionary<string, IConvertible> defaultProperties;
        private string packageName = "wrk2";
        private string scriptpackageName = "wrkconfiguration";

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
                { "PackageName", this.packageName },
                { "Scenario", "1000r_{ThreadCount}th_{Connection}c_{FileSizeInKB}kb" },
                { "CommandArguments", "--latency --threads{ThreadCount} --connections{Connection} --duration{Duration.TotalSeconds}s" },
                { "Connection", 100 },
                { "ThreadCount", 10 },
                { "MaxCoreCount", 10 },
                { "TestDuration", "00:02:30"},
                { "Timeout", "00:20:00"},
                { "FileSizeInKB", 10},
                { "Role", "Client"},
                { "Tags", "Networking,NGINX,WRK2"},
            };

            dependencyFixture = new DependencyFixture();
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void Wrk2ExecutorThrowsErrorIfPlatformIsWrong(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, this.ClientStateId);
            this.mockFixture.Parameters = this.defaultProperties;
            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            Assert.IsFalse(VirtualClientComponent.IsSupported(executor));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void Wrk2ExecutorThrowsErrorIfPackageIsMissing(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, this.ClientStateId);
            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            Assert.ThrowsAsync<DependencyException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });

            this.mockFixture.Parameters = this.defaultProperties;
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.packageName);
            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.FirstOrDefault());

            TestWrk2Executor executor2 = new TestWrk2Executor(this.mockFixture);
            Assert.ThrowsAsync<FileNotFoundException>(async () =>
            {
                await executor2.InitializeAsync().ConfigureAwait(false);
            });
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void Wrk2ExecutorOnlySupportsWrk2(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, this.ClientStateId);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                 { "PackageName", "wrk" },
            };

            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage("wrk");
            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.FirstOrDefault());

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            DependencyException exc = Assert.ThrowsAsync<DependencyException>(async () =>
            {
                await executor.InitializeAsync().ConfigureAwait(false);
            });

            Assert.AreEqual(exc.Message, "TestWrk2Executor did not find correct package in the directory. Supported Package: wrk2. Package Provided: wrk");
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task Wrk2ExecutorInitializesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, this.ServerStateId);
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.packageName);
            dependencyFixture.SetupPackage(this.scriptpackageName);
            TestWrk2Executor dummyExecutor = new TestWrk2Executor(this.mockFixture);

            string wrkPackagePath = dependencyFixture.PackageManager.Where(x => x.Name == this.packageName).FirstOrDefault().Path;
            string scriptPackagePath = dummyExecutor.ToPlatformSpecificPath(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptpackageName).FirstOrDefault(), platform, architecture).Path;
            string[] expectedFiles = new string[]
            {
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath, "setup-reset.sh"),
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath,"setup-config.sh"),
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath,"reset.sh"),
                dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath,"wrk"),
                dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath,"runwrk.sh")
            };

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string fileName) =>
                {
                    if (!expectedFiles.Any(y => y == fileName))
                    {
                        Assert.Fail($"Unexpected File Name: {fileName}. \n{string.Join("\n", expectedFiles)}");
                    }
                });

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.scriptpackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptpackageName).FirstOrDefault());

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.packageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.packageName).FirstOrDefault());

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            await executor.InitializeAsync().ConfigureAwait(false);
            this.mockFixture.FileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Exactly(expectedFiles.Count()));
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [Ignore("Unit test is way too complex and needs to be refactored.")]
        public async Task Wrk2ExecutorExecutesAsyncAsExpected(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture, this.ServerStateId);
            dependencyFixture.Setup(platform, architecture);
            dependencyFixture.SetupPackage(this.packageName);
            dependencyFixture.SetupPackage(this.scriptpackageName);
            TestWrk2Executor dummyExecutor = new TestWrk2Executor(this.mockFixture);

            string wrkPackagePath = dependencyFixture.PackageManager.Where(x => x.Name == this.packageName).FirstOrDefault().Path;
            string scriptPackagePath = dummyExecutor.ToPlatformSpecificPath(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptpackageName).FirstOrDefault(), platform, architecture).Path;
            string[] expectedFiles = new string[]
            {
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath, "setup-reset.sh"),
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath,"setup-config.sh"),
                dummyExecutor.PlatformSpecifics.Combine(scriptPackagePath,"reset.sh"),
                dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath,"wrk"),
                dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath,"runwrk.sh")
            };

            this.mockFixture.Parameters = this.defaultProperties;
            this.mockFixture.FileSystem
                .Setup(x => x.File.Exists(It.IsAny<string>()))
                .Returns(true)
                .Callback((string fileName) =>
                {
                    if (!expectedFiles.Any(y => y == fileName))
                    {
                        Assert.Fail($"Unexpected File Name: {fileName}. \n{string.Join("\n", expectedFiles)}");
                    }
                });

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.scriptpackageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.scriptpackageName).FirstOrDefault());

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.Is<string>(x => x == this.packageName), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dependencyFixture.PackageManager.Where(x => x.Name == this.packageName).FirstOrDefault());

            Item<State> serverState = new Item<State>(this.ServerStateId, new State());
            serverState.Definition.Online(true);
            Item<State> clientState = new Item<State>(this.ClientStateId, new State());

            // create local state
            this.mockFixture.ApiClient
                .Setup(x => x.CreateStateAsync(It.Is<string>(y => y == this.ClientStateId), It.IsAny<State>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK))
                .Callback((string id, State state, CancellationToken _, IAsyncPolicy<HttpResponseMessage> __) =>
                {
                    Assert.IsTrue(state.Online());
                });

            this.mockFixture.ApiClient.Setup(s => s.GetStateAsync(It.Is<string>(x => x == this.ServerStateId), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() =>
                {
                    Item<State> result = new Item<State>(this.ServerStateId, new State());
                    result.Definition.Online(true);
                    return this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, result);
                });

            this.mockFixture.ApiClient.Setup(s => s.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() =>
                {
                    // both ServerStateId and serverVersion will be covered with this set up.
                    Item<State> result = new Item<State>(this.ServerStateId, new State());
                    result.Definition.Online(true);
                    return this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, result);
                });

            // update local state before running working
            this.mockFixture.ApiClient
                .Setup(x => x.UpdateStateAsync(It.Is<string>(y => y == this.ClientStateId), It.IsAny<Item<State>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback((string id, Item<State> state, CancellationToken _, IAsyncPolicy<HttpResponseMessage> __) =>
                {
                    Assert.IsTrue(state.Definition.Online());
                });

            // delete local state before exiting
            this.mockFixture.ApiClient.Setup(x => x.DeleteStateAsync(It.Is<string>(y => y == this.ClientStateId), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo" || command.EndsWith("wrk"));

                if (arguments.Contains("--version"))
                {
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")} --version"));
                }

                return this.memoryProcess;
            };
            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            await executor.InitializeAsync().ConfigureAwait(false);
            Assert.ThrowsAsync<WorkloadException>(async () =>
            {
                await executor.ExecuteAsync().ConfigureAwait(false);
            }, "wrk2 did not write metrics to console.");

            this.mockFixture.FileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Exactly(8));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Assert.IsTrue(command == "sudo" || command.EndsWith("wrk"));

                if (arguments.Contains("--version"))
                {
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer"));
                    Assert.IsTrue(arguments.StartsWith($"bash {dummyExecutor.PlatformSpecifics.Combine(wrkPackagePath, "runwrk.sh")} --version"));
                }
                else
                {
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
                    string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
                }

                return this.memoryProcess;
            };
            TestWrk2Executor executor2 = new TestWrk2Executor(this.mockFixture);
            await executor2.InitializeAsync().ConfigureAwait(false);
            await executor2.ExecuteAsync().ConfigureAwait(false);

            this.mockFixture.FileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Exactly(15));
            this.mockFixture.ApiClient.Verify(x => x.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()));
        }

        [Test]
        [TestCase("-L -R 1000 -t 10 -c 50 -d 100s --timeout 10s https://{serverip}/api_new/1kb")]
        [TestCase("{serverip}_{clientip}_{reverseproxyip}_{serverip}")]
        public void WrkClientExecutorReturnsCorrectArguments(string commandArg)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            ClientInstance serverInstance = new ClientInstance(name: nameof(State), ipAddress: "1.2.3.4", role: ClientRole.Server);
            ClientInstance clientInstance = new ClientInstance(name: nameof(State), ipAddress: "5.6.7.8", role: ClientRole.Client);
            ClientInstance reverseProxyInstance = new ClientInstance(name: nameof(State), ipAddress: "9.0.1.2", role: ClientRole.ReverseProxy);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance, clientInstance, reverseProxyInstance });

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "CommandArguments", commandArg }
            };

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            string expected = executor.GetCommandLineArguments();

            string result = commandArg
                .Replace("{serverip}", "1.2.3.4")
                .Replace("{clientip}", "5.6.7.8")
                .Replace("{reverseproxyip}", "9.0.1.2");

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Wrk2ExecutorThrowsWhenBindToCoresIsTrueButCoreAffinityIsNotProvided()
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
            ClientInstance serverInstance = new ClientInstance(name: nameof(State), ipAddress: "1.2.3.4", role: ClientRole.Server);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance });
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "wrk2" },
                { "CommandArguments", "-t 10 -c 100 -d 15s http://{ServerIp}:9876/json" },
                { "BindToCores", true }
            };

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            Assert.Throws<DependencyException>(() => executor.Validate());
        }

        [Test]
        [Ignore("Unit test requires additional state management setup and needs to be simplified.")]
        public async Task Wrk2ExecutorExecutesWithCoreAffinityOnLinux()
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, nameof(State));
            ClientInstance serverInstance = new ClientInstance(name: nameof(State), ipAddress: "1.2.3.4", role: ClientRole.Server);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance });
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "wrk2" },
                { "CommandArguments", "-t 10 -c 100 -d 15s http://{ServerIp}:9876/json" },
                { "BindToCores", true },
                { "CoreAffinity", "0-7" },
                { "TargetService", "server" }
            };

            DependencyFixture dependencyFixture = new DependencyFixture();
            dependencyFixture.Setup(PlatformID.Unix, Architecture.X64);
            dependencyFixture.SetupPackage("wrk2");
            dependencyFixture.SetupPackage("wrkconfiguration");

            this.mockFixture.PackageManager
                .Setup(x => x.GetPackageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string name, CancellationToken token) => dependencyFixture.PackageManager.FirstOrDefault(p => p.Name == name));

            this.mockFixture.FileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(true);

            // Setup API client for state management
            this.mockFixture.ApiClient
                .Setup(x => x.CreateStateAsync<State>(It.IsAny<string>(), It.IsAny<State>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

            this.mockFixture.ApiClient
                .Setup(x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<Item<State>>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

            this.mockFixture.ApiClient
                .Setup(x => x.DeleteStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.NoContent));

            this.mockFixture.ApiClient.Setup(s => s.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() =>
                {
                    Item<State> result = new Item<State>(nameof(State), new State());
                    result.Definition.Online(true);
                    return this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, result);
                });

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    "--version",
                    "wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer")
                .SetupProcessOutput(
                    ".*wrk.*-t 10 -c 100.*",
                    File.ReadAllText(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Examples", "Wrk", "wrkStandardExample1.txt")));

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            await executor.InitializeAsync();
            await executor.ExecuteAsync();

            // Verify numactl was used with correct core affinity
            this.mockFixture.Tracking.AssertCommandsExecuted(true,
                "sudo bash -c \\\"numactl -C 0-7 .*wrk.*"
            );

            this.mockFixture.Tracking.AssertCommandExecutedTimes("numactl", 1);
        }

        [Test]
        public async Task WrkClientExecutorReturnsCorrectArguments()
        {
            string commandArgumentInput = @"--rate 1000 --latency --threads 10 --connections 100 --duration 100s --timeout 10s https://{serverip}/api_new/5kb";
            ClientInstance serverInstance = new ClientInstance(name: nameof(State), ipAddress: "1.2.3.4", role: ClientRole.Server);

            string directory = @"some/random\dir/name/";
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64, nameof(State));
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>() { serverInstance });
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                {"CommandArguments", commandArgumentInput },
                { "Scenario", "bar" },
                { "ToolName", "wrk" },
                { "PackageName", "wrk" },
                { "FileSizeInKB", 5},
                { "TestDuration", TimeSpan.FromSeconds(60).ToString() }
            };

            TestWrk2Executor executor = new TestWrk2Executor(this.mockFixture);
            executor.PackageDirectory = directory;
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
                    Assert.AreEqual(arguments, $"bash {executor.Combine(directory, "runwrk.sh")} \"{results}\"");
                    string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
                    string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample1.txt");
                    this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
                }
                Assert.AreEqual(workingDir, directory);
                return this.memoryProcess;
            };

            await executor.ExecuteWorkloadAsync(result, workingDir: directory).ConfigureAwait(false);
        }

        public void SetUpWorkloadOutput()
        {
            string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Wrk");
            string outputPath = Path.Combine(examplesDirectory, @"wrkStandardExample2.txt");
            this.memoryProcess.StandardOutput = new ConcurrentBuffer(new StringBuilder(File.ReadAllText(outputPath)));
        }

        private class TestWrk2Executor : Wrk2Executor
        {
            public TestWrk2Executor(MockFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
                this.ServerApi = mockFixture.ApiClient.Object;
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

            public async Task InitializeAsync()
            {
                await base.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task ExecuteAsync()
            {
                await base.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public async Task ExecuteWorkloadAsync(string commandArguments, string workingDir)
            {
                await base.ExecuteWorkloadAsync(commandArguments, workingDir, EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }
    }
}
