namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ApacheBenchExecutorTests : MockFixture
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockPackage = new DependencyPath("apachehttpserver", this.mockFixture.GetPackagePath("apachehttpserver"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { nameof(ApacheBenchExecutor.PackageName), "apachehttpserver" },
                { nameof(ApacheBenchExecutor.CommandLine), "Run" },
            });

            this.mockFixture.File.Setup(file => file.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("text");

            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);

            // Profile parameters.
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
            };
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApacheBenchExecutorThrowsIfTheApacheHttpWorkloadPackageDoesNotExist(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                // The package does not exist on the system.
                this.mockFixture.PackageManager.Reset();

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
            this.SetupTest(platform, architecture);

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(string.Empty);

            this.mockFixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                    Assert.AreEqual("True", state.Properties().First().Value["ApacheBenchStateInitialized"].ToString());
                });

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                this.SetupTest(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchExecutorExecutesInstallCommandWhenStateNotInitialized(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            bool isCommandExecuted = false;

            this.mockFixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = false,
                }));

            this.mockFixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                isCommandExecuted = true;
                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    OnHasExited = () => true
                };
                process.StandardOutput.Append('a', 5);
                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                this.SetupTest(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(isCommandExecuted);
        }

        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchExecutorSkipsInstallAndStartsServerWhenStateIsInitialized(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);
            bool installCommandExecuted = false;
            bool startCommandExecuted = false;

            this.mockFixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = true,
                }));

            this.mockFixture.StateManager.OnSaveState()
                .Callback<string, JObject, CancellationToken, IAsyncPolicy>((stateId, state, token, retryPolicy) =>
                {
                    Assert.IsNotNull(state);
                });

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                installCommandExecuted = arguments.Equals("-k install");
                startCommandExecuted = arguments.Equals("-k start");
                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    OnHasExited = () => true
                };
                process.StandardOutput.Append('a', 5);
                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                this.SetupTest(platform);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsFalse(installCommandExecuted);
            Assert.IsTrue(startCommandExecuted);
        }

        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchExecutorExecutesTheExpectedApacheBenchCommandWhenStateIsNotInitialized(PlatformID platform, Architecture architecture)
        {
            string expectedCommand = "ufw allow 80/tcp";
            this.SetupTest(platform, architecture);

            this.mockFixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = false,
                }));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string results = MockFixture.ReadFile(MockFixture.TestExamplesDirectory, "ApacheBench", "ApacheBenchResultsExample.txt");

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted(expectedCommand));
            }
        }

        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task ApacheBenchExecutorDoesNotExecutesTheApacheBenchCommandWhenStateIsInitialized(PlatformID platform, Architecture architecture)
        {
            string command1 = "ufw allow 80/tcp";
            string command2 = "systemctl start apache2";
            this.SetupTest(platform, architecture);

            this.mockFixture.StateManager.OnGetState()
                .ReturnsAsync(JObject.FromObject(new ApacheBenchExecutor.ApacheBenchState()
                {
                    ApacheBenchStateInitialized = true,
                }));

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string results = MockFixture.ReadFile(MockFixture.TestExamplesDirectory, "ApacheBench", "ApacheBenchResultsExample.txt");

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsFalse(this.mockFixture.ProcessManager.CommandsExecuted(command1));
                Assert.IsFalse(this.mockFixture.ProcessManager.CommandsExecuted(command2));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "40000", "20")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "40000", "20")]
        [TestCase(PlatformID.Unix, Architecture.X64, "25000", "5")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "25000", "5")]
        public async Task ApacheBenchExecutorExecutesWorkloadForDifferentInputsAndGenerateMetricsForLinux(PlatformID platform, Architecture architecture, string noOfRequests, string noOfConcurrentRequests)
        {

            this.SetupTest(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
                { "NoOfRequests", noOfRequests },
                { "NoOfConcurrentRequests", noOfConcurrentRequests },
            };

            bool allowPortCommandExecuted = false;
            bool startServerCommandExecuted = false;
            bool benchmarkCommandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (arguments.Equals("ufw allow 80/tcp"))
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

                string results = MockFixture.ReadFile(MockFixture.TestExamplesDirectory, "ApacheBench", "ApacheBenchResultsExample.txt");

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
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

            this.SetupTest(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
                { "NoOfRequests", noOfRequests },
                { "NoOfConcurrentRequests", noOfConcurrentRequests },
            };

            bool vcRedistCommandExecuted = false;
            bool installServerCommandExecuted = false;
            bool startServerCommandExecuted = false;
            bool benchmarkCommandExecuted = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                if (command.Contains("vc_redist.x64.exe") && arguments.Equals("/install /quiet /norestart"))
                {
                    vcRedistCommandExecuted = true;
                }
                else if (arguments.Equals("-k install"))
                {
                    installServerCommandExecuted = true;
                }
                else if (arguments.Equals("-k start"))
                {
                    startServerCommandExecuted = true;
                }
                else if (arguments.Equals($"-k -n {noOfRequests} -c {noOfConcurrentRequests} http://localhost:80/"))
                {
                    benchmarkCommandExecuted = true;
                }

                string results = MockFixture.ReadFile(MockFixture.TestExamplesDirectory, "ApacheBench", "ApacheBenchResultsExample.txt");

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                process.StandardOutput.Append(results);
                return process;
            };
            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(vcRedistCommandExecuted);
                Assert.IsTrue(installServerCommandExecuted);
                Assert.IsTrue(startServerCommandExecuted);
                Assert.IsTrue(benchmarkCommandExecuted);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task ApacheBenchExecutorGeneratesMetricsWhenStandardErrorIsPresentButTheExitCodeIsSuccess(PlatformID platform, Architecture architecture)
        {
            // ApacheBench (ab) writes progress information to standard error even on a successful
            // run. The executor must not treat non-empty standard error as a failure.
            this.SetupTest(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                string results = MockFixture.ReadFile(MockFixture.TestExamplesDirectory, "ApacheBench", "ApacheBenchResultsExample.txt");

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = 0,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                process.StandardOutput.Append(results);
                process.StandardError.Append("Completed 5000 requests\nFinished 50000 requests\n");
                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                Assert.DoesNotThrowAsync(() => executor.ExecuteAsync(CancellationToken.None));
            }

            await Task.CompletedTask;
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApacheBenchExecutorThrowsWhenTheWorkloadProcessExitsWithAnError(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "PackageName", "apachehttpserver" },
                { "Scenario", "ExecuteApacheBenchBenchmark" },
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                // The executor runs setup commands before the actual ApacheBench workload. Fail only the
                // ApacheBench workload command so the test exercises the intended workload failure path
                // rather than passing because a setup command failed.
                bool isApacheBenchWorkloadCommand = arguments?.Contains("http://localhost:80/", StringComparison.OrdinalIgnoreCase) == true;
                int exitCode = isApacheBenchWorkloadCommand ? 1 : 0;

                IProcessProxy process = new InMemoryProcess
                {
                    ExitCode = exitCode,
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir
                    },
                    ExitTime = DateTime.Now.AddSeconds(5),
                    OnHasExited = () => true
                };

                if (isApacheBenchWorkloadCommand)
                {
                    process.StandardError.Append("apr_socket_connect(): Connection refused\n");
                }

                return process;
            };

            using (var executor = new TestApacheBenchExecutor(this.mockFixture))
            {
                Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
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
