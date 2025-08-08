// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Properties;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class TlsOpenSslClientExecutorTests
    {
        private DependencyFixture fixture;
        private DependencyPath mockPackage;

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/bin/openssl")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/bin/openssl")]
        public async Task Constructor_InitializesDependenciesAndPolicies(PlatformID platform, Architecture architecture, string binaryPath)
        {
            SetupEnvironment(platform, architecture);
            using (TestOpenSslClientExecutor executor = new TestOpenSslClientExecutor(this.fixture, this.fixture.Parameters))
            {
               
                Assert.IsNotNull(executor);
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public void Properties_ReturnExpectedValues(PlatformID platform, Architecture architecture)
        {
            SetupEnvironment(platform, architecture);
            var executor = new TlsOpenSslClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);

            Assert.IsNotNull(executor);
            Assert.AreEqual(2, executor.ClientInstances);
            Assert.AreEqual(443, executor.ServerPort);
            Assert.IsTrue(executor.WarmUp);
            Assert.IsNotNull(executor.Parameters);
        }


        // TODO: fix this. throws this error 
        [Test]
        public void CaptureMetrics_LogsMetricsOnSuccess()
        {
            SetupEnvironment(PlatformID.Unix, Architecture.X64);
            var executor = new TlsOpenSslClientExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.IsNotNull(executor);
            var process = this.fixture.CreateProcess("openssl", "s_time ...", "/tmp");
            process.ExitCode = 0;
            // Console.WriteLine(VirtualClient.Actions.Properties.TestResources.Results_FIO_Verification_Error_2);
            process.StandardOutput.Append(TestResources.Results_OpenSSL_stime);

            var method = typeof(TlsOpenSslClientExecutor).GetMethod("CaptureMetrics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Should not throw
            Assert.DoesNotThrow(() =>
            {
                method.Invoke(executor, new object[] { process, "s_time ...", EventContext.None, CancellationToken.None });
            });
        }

        private void SetupEnvironment(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            // Setup the default behaviors given all expected dependencies are in place such that the
            // workload can be executed successfully.
            this.fixture = new DependencyFixture();
            this.fixture.Setup(platform, architecture); 
            this.fixture
                .Setup(platform, architecture, "Client01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            // ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);

            this.fixture.SetupPackage(
                    "OpenSSL",
                    expectedFiles: $"{PlatformSpecifics.GetPlatformArchitectureName(platform, architecture)}/bin/openssl");

            this.mockPackage = this.fixture.PackageManager.First(pkg => pkg.Name == "OpenSSL");

            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "ClientInstances", 2 },
                { "ServerPort", 443 },
                { "WarmUp", true },
                { "CommandArguments", "s_time -connect :443 -www /test_1k.html -time 30 -ciphersuites TLS_AES_256_GCM_SHA384 -tls1_3" },
                { "Scenario", "OpenSSL_TLS_Client_AES_256_GCM_SHA384_1k" },
                { "MetricScenario", "tls_client_aes-256-gcm-sha384-1k" },
                { "PackageName", "OpenSSL" },
                { "Tags", "CPU,OpenSSL,Cryptography" },
                { "Role", "Client" }
            };
            this.fixture.ProcessManager.OnProcessCreated = (process) =>
            {
                // When we start the OpenSSL process we want to register a successful
                // result.
                if (process.IsMatch("openssl s_time"))
                {
                    process.StandardOutput.Append(TestResources.Results_OpenSSL_stime);
                }
            };
        }
        private class TestOpenSslClientExecutor : TlsOpenSslClientExecutor
        {
            public TestOpenSslClientExecutor(DependencyFixture mockFixture, IDictionary<string, IConvertible> parameters)
                : base(mockFixture.Dependencies, parameters)
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
            public IApiClient MockApiClient
            {
                get
                {
                    return base.ApiClient;
                }
            }
        }
    }
}