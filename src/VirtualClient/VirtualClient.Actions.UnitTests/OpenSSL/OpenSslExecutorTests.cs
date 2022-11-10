// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.CpuPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Properties;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class OpenSslExecutorTests
    {
        private DependencyFixture fixture;
        private DependencyPath mockPackage;

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/bin/openssl")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/bin/openssl")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64\\bin\\openssl.exe")]
        public async Task OpenSslExecutorInitializesDependenciesAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupDefaultBehaviors(platform, architecture);

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                Assert.IsNull(executor.ExecutablePath);

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                DependencyPath expectedWorkloadPackage = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platform, architecture);
                Assert.IsTrue(expectedWorkloadPackage.Equals(executor.Package));

                string expectedWorkloadExecutablePath = Path.Combine(this.mockPackage.Path, binaryPath);
                Assert.IsTrue(PlatformAgnosticPathComparer.Instance.Equals(expectedWorkloadExecutablePath, executor.ExecutablePath));
            }
        }

        [Test]
        public void OpenSslExecutorThrowsIfTheOpenSslWorkloadPackageDoesNotExist()
        {
            this.SetupDefaultBehaviors();

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                // The package does not exist on the system.
                this.fixture.PackageManager.Clear();

                DependencyException error = Assert.ThrowsAsync<DependencyException>(
                    () => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
            }
        }

        [Test]
        public void OpenSslExecutorThrowsIfTheOpenSslWorkloadExecutableBinaryDoesNotExist()
        {
            this.SetupDefaultBehaviors();

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                // The package exists on the system, but the workload binary/.exe does not.
                this.fixture.FileSystem.Clear();

                Assert.ThrowsAsync<FileNotFoundException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task OpenSslExecutorMakesTheWorkloadBinaryExecutableOnUnixSystems()
        {
            this.SetupDefaultBehaviors(PlatformID.Unix);

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                bool executablePrepared = false;
                this.fixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    executablePrepared = process.IsMatch($"sudo chmod \\+x \"{executor.ExecutablePath}\"");
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(executablePrepared);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, "openssl speed -multi [0-9]+ -elapsed -seconds 100 aes-256-cbc")]
        [TestCase(PlatformID.Win32NT, "openssl.exe speed -elapsed -seconds 100 aes-256-cbc")]

        public async Task OpenSslExecutorExecutesTheExpectedOpenSslCommand(PlatformID platform, string expectedCommand)
        {
            this.SetupDefaultBehaviors(platform);

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(expectedCommand));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task OpenSslExecutorSetsExpectedEnvironmentVariablesRequiredToExecuteTheWorkloadBinary(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultBehaviors(platform, architecture);

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                DependencyPath expectedPackagePath = this.fixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, platform, architecture);
                IProcessProxy workloadProcess = this.fixture.ProcessManager.Processes.Last();

                bool environmentVariablesSet = false;
                if (platform == PlatformID.Unix)
                {
                    // On Linux systems, the OpenSSL toolset needs to know where to find the engine
                    // libraries. The LD_LIBRARY_PATH environment variable provides the location.
                    Assert.IsTrue(workloadProcess.EnvironmentVariables.ContainsKey("LD_LIBRARY_PATH"));
                    Assert.AreEqual(this.fixture.PlatformSpecifics.Combine(expectedPackagePath.Path, "lib64"), workloadProcess.EnvironmentVariables["LD_LIBRARY_PATH"]);

                    environmentVariablesSet = true;
                }
                else if (platform == PlatformID.Win32NT)
                {
                    // The OpenSSL toolset used was compiled using the Visual Studio C++ compiler. The binary
                    // produced has a dependency on the Visual Studio C++ runtime. The binaries required are
                    // in the OpenSSL package. We reference them by adding an entry to the Windows 'Path'
                    // environment variable.
                    Assert.IsTrue(workloadProcess.EnvironmentVariables.ContainsKey("Path"));
                    Assert.IsTrue(workloadProcess.EnvironmentVariables["Path"].EndsWith(this.fixture.PlatformSpecifics.Combine(expectedPackagePath.Path, "vcruntime")));

                    environmentVariablesSet = true;
                }

                Assert.IsTrue(environmentVariablesSet);
            }
        }

        [Test]
        public async Task OpenSslExecutorCapturesTheRawWorkloadResults()
        {
            this.SetupDefaultBehaviors();

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotEmpty(this.fixture.Logger.MessagesLogged($"{nameof(OpenSslExecutor)}.ProcessDetails"));
            }
        }

        [Test]
        public async Task OpenSslExecutorCapturesTheExpectedMetricsFromTheWorkloadResults()
        {
            this.SetupDefaultBehaviors();

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                var messages = this.fixture.Logger.MessagesLogged("OpenSSL.ScenarioResult");
                Assert.IsNotEmpty(messages);
                Assert.True(messages.Count() == 6);
                Assert.IsTrue(messages.All(msg => msg.Item3 as EventContext != null));
                Assert.IsTrue(messages.All(msg => (msg.Item3 as EventContext).Properties["ScenarioName"].ToString() == "OpenSSL Speed"));
            }
        }

        [Test]
        public async Task OpenSslExecutorCapturesTelemetryOnFailedAttemptsToParseTheWorkloadOutput()
        {
            this.SetupDefaultBehaviors();

            using (TestOpenSslExecutor executor = new TestOpenSslExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    // Make the results invalid...They don't contain any standard output.
                    process.StandardOutput.Clear();
                };

                await executor.ExecuteAsync(CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotEmpty(this.fixture.Logger.MessagesLogged($"{nameof(OpenSslExecutor)}.WorkloadOutputParsingFailed"));
            }
        }

        private void SetupDefaultBehaviors(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            // Setup the default behaviors given all expected dependencies are in place such that the
            // workload can be executed successfully.
            this.fixture = new DependencyFixture();
            this.fixture.Setup(platform, architecture);

            // The package that contains the OpenSSL toolsets.
            if (platform == PlatformID.Unix)
            {
                this.fixture.SetupWorkloadPackage(
                    "OpenSSL",
                    expectedFiles: $"{PlatformSpecifics.GetPlatformArchitectureName(platform, architecture)}/bin/openssl");
            }
            else
            {
                this.fixture.SetupWorkloadPackage(
                    "OpenSSL",
                    expectedFiles: $@"{PlatformSpecifics.GetPlatformArchitectureName(platform, architecture)}\bin\openssl.exe");
            }

            this.mockPackage = this.fixture.PackageManager.First(pkg => pkg.Name == "OpenSSL");

            // Profile parameters.
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "CommandArguments", "speed -elapsed -seconds 100 aes-256-cbc" },
                { "PackageName", "OpenSSL" },
                { "Tags", "CPU,OpenSSL,Cryptography" }
            };

            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                // When we start the OpenSSL process we want to register a successful
                // result.
                if (process.IsMatch("openssl(.exe)* speed"))
                {
                    process.StandardOutput.Append(TestResources.Results_OpenSSL_speed);
                }
            };
        }

        private class TestOpenSslExecutor : OpenSslExecutor
        {
            public TestOpenSslExecutor(DependencyFixture mockFixture)
                : base(mockFixture.Dependencies, mockFixture.Parameters)
            {
            }

            public new DependencyPath Package
            {
                get
                {
                    return base.Package;
                }
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
