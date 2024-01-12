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

        [Test]
        public void ApacheBenchExecutorThrowsIfTheApacheHttpWorkloadPackageDoesNotExist()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT, Architecture.X64);

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                // The package does not exist on the system.
                this.fixture.PackageManager.Reset();

                DependencyException error = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        public async Task ApacheBenchExecutorCreatesStateWhenStateDoesNotExist()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT, Architecture.X64);
            this.fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(string.Empty);
            this.fixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.IsEmpty(content);
                    });

            using (var executor = new TestApacheBenchExecutor(this.fixture))
            {
                this.SetupDefaultBehaviors(PlatformID.Win32NT);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        public async Task ApacheBenchExecutorExecutesInstallCommandWhenStateNotInitialized()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT, Architecture.X64);
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
                this.SetupDefaultBehaviors(PlatformID.Win32NT);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(isCommandExecuted);
        }

        [Test]
        public async Task ApacheBenchExecutorDoesNotExecuteInstallCommandWhenStateIsInitialized()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT, Architecture.X64);
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
                this.SetupDefaultBehaviors(PlatformID.Win32NT);
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsFalse(isCommandExecuted);
        }

        [Test]
        [TestCase(PlatformID.Unix, "ufw allow 'Apache'")]
        public async Task ApacheBenchExecutorExecutesTheExpectedApacheBenchCommand(PlatformID platform, string expectedCommand)
        {

            this.SetupDefaultBehaviors(platform);
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
                    }
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
        private void SetupDefaultBehaviors(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(platform, architecture);

            string workloadName = "apachehttpserver";
            this.fixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { nameof(Example2WorkloadExecutor.PackageName), workloadName },
                { nameof(Example2WorkloadExecutor.CommandLine), "Run" },
                { nameof(Example2WorkloadExecutor.TestName), "ExampleTest" }
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
                    .Callback<string, string, CancellationToken>((path, content, token) =>
                    {
                        Assert.AreEqual(content, "text");
                    });

            // Profile parameters.
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "CommandArguments", "-k -n 100 -c 100 http://localhost:80/" },
                { "PackageName", "apachehttpserver" },
                { "CommandLine", "" },
                { "Scenario", "ApacheBench_N50000_C10" },
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
