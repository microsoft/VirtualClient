namespace VirtualClient.Actions.ApacheBench
{
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.ServiceModel.Channels;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Actions.Properties;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.ApacheBenchExecutor;

    [TestFixture]
    [Category("Unit")]
    public class ApacheBenchExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockWorkloadPackage;

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApacheBenchExecutorThrowsIfTheApacheHttpWorkloadPackageDoesNotExist(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                // The package does not exist on the system.
                this.fixture.PackageManager.Reset();

                DependencyException error = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchExecutorCreatesStateWhenStateDoesNotExist(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);

            this.fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(string.Empty);

            this.fixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                    Assert.AreEqual("True", state.Properties().First().Value["ApacheBenchStateInitialized"].ToString());
                });

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                this.SetupDefaultBehaviors(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchExecutorExecutesInstallCommandWhenStateNotInitialized(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);
            bool isCommandExecuted = false;

            this.fixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = false,
                }));

            this.fixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                });

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                isCommandExecuted = true;
                IProcessProxy process = this.fixture.ProcessManager.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append('a', 5);
                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                this.SetupDefaultBehaviors(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(isCommandExecuted);
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchExecutorDoesNotExecuteInstallCommandWhenStateIsInitialized(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);
            bool isCommandExecuted = false;

            this.fixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = true,
                }));

            this.fixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                });

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                isCommandExecuted = true;
                IProcessProxy process = this.fixture.ProcessManager.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append('a', 5);
                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                this.SetupDefaultBehaviors(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsFalse(isCommandExecuted);
        }

        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchExecutorExecutesTheExpectedApacheBenchCommandWhenStateIsNotInitialized(PlatformID platform, Architecture architecture)
        {
            string expectedCommand = "ufw allow 'Apache'";
            this.SetupDefaultBehaviors(platform, architecture);

            this.fixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = false,
                }));

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string resultsPath = Path.Combine(currentDirectory, "Examples", "ApacheBench", "ApacheBenchResultsExample.txt");
                string results = File.ReadAllText(resultsPath);

                IProcessProxy process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5)
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(expectedCommand));
            }
        }

        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchExecutorDoesNotExecutesTheApacheBenchCommandWhenStateIsInitialized(PlatformID platform, Architecture architecture)
        {
            string command1 = "ufw allow 'Apache'";
            string command2 = "systemctl start apache2";
            this.SetupDefaultBehaviors(platform, architecture);

            this.fixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = true,
                }));

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string resultsPath = Path.Combine(currentDirectory, "Examples", "ApacheBench", "ApacheBenchResultsExample.txt");
                string results = File.ReadAllText(resultsPath);

                IProcessProxy process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5)
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsFalse(this.fixture.ProcessManager.CommandsExecuted(command1));
                Assert.IsFalse(this.fixture.ProcessManager.CommandsExecuted(command2));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "40000", "20")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "40000", "20")]
        [TestCase(PlatformID.Unix, Architecture.X64, "25000", "5")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "25000", "5")]
        public async Task ApacheBenchExecutorExecutesWorkloadForDifferentInputsAndGenerateMetricsForLinux(PlatformID platform, Architecture architecture, string noOfRequests, string noOfConcurrentRequests)
        {

            this.SetupDefaultBehaviors(platform, architecture);

            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
                { "NoOfRequests", noOfRequests },
                { "NoOfConcurrentRequests", noOfConcurrentRequests },
            };

            bool allowPortCommandExecuted = false;
            bool startServerCommandExecuted = false;
            bool benchmarkCommandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments.Equals("ufw allow 'Apache'"))
                {
                    allowPortCommandExecuted = true;
                }
                else if (arguments.Equals("systemctl start apache2"))
                {
                    startServerCommandExecuted = true;
                }
                else if (arguments.Equals($"/usr/bin/ab -k -n {noOfRequests} -c {noOfConcurrentRequests} http://localhost:80/"))
                {
                    benchmarkCommandExecuted = true;
                }

                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string resultsPath = Path.Combine(currentDirectory, "Examples", "ApacheBench", "ApacheBenchResultsExample.txt");
                string results = File.ReadAllText(resultsPath);

                IProcessProxy process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5)
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);
                
                Assert.IsTrue(allowPortCommandExecuted);
                Assert.IsTrue(startServerCommandExecuted);
                Assert.IsTrue(benchmarkCommandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "40000", "20")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "40000", "20")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "25000", "5")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "25000", "5")]
        public async Task ApacheBenchExecutorExecutesWorkloadForDifferentInputsAndGenerateMetricsForWindows(PlatformID platform, Architecture architecture, string noOfRequests, string noOfConcurrentRequests)
        {

            this.SetupDefaultBehaviors(platform, architecture);

            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
                { "NoOfRequests", noOfRequests },
                { "NoOfConcurrentRequests", noOfConcurrentRequests },
            };

            bool startServerCommandExecuted = false;
            bool benchmarkCommandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments.Equals("-k install"))
                {
                    startServerCommandExecuted = true;
                }
                else if (arguments.Equals($"-k -n {noOfRequests} -c {noOfConcurrentRequests} http://localhost:80/"))
                {
                    benchmarkCommandExecuted = true;
                }

                string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string resultsPath = Path.Combine(currentDirectory, "Examples", "ApacheBench", "ApacheBenchResultsExample.txt");
                string results = File.ReadAllText(resultsPath);

                IProcessProxy process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5)
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(startServerCommandExecuted);
                Assert.IsTrue(benchmarkCommandExecuted);
            }
        }

        private void SetupDefaultBehaviors(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);

            string workloadName = "apachehttpserver";

            this.fixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { nameof(ApacheBenchExecutor.PackageName), workloadName },
                { nameof(ApacheBenchExecutor.CommandLine), "Run" },
            });

            this.mockWorkloadPackage = new DependencyPath(
                workloadName,
                this.fixture.PlatformSpecifics.GetPackagePath(workloadName));

            this.fixture.PackageManager.OnGetPackage()
                    .Callback<string, CancellationToken>((packageName, token) => Assert.AreEqual(packageName, "apachehttpserver"))
                    .ReturnsAsync(this.mockWorkloadPackage);

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("text");

            this.fixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            // Profile parameters.
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
            };
        }
        private class TestApacheBenchExecutor : ApacheBenchExecutor
        {
            public TestApacheBenchExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
