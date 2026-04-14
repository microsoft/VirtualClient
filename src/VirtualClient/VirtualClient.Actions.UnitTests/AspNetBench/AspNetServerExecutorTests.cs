// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class AspNetServerExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockAspNetBenchPackage;
        private DependencyPath mockDotNetPackage;

        [Test]
        public void AspNetServerExecutorThrowsIfCannotFindAspNetBenchPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("aspnetbenchmarks").ReturnsAsync(value: null);

            using (TestAspNetServerExecutor executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void AspNetServerExecutorThrowsIfCannotFindDotNetSDKPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("dotnetsdk").ReturnsAsync(value: null);

            using (TestAspNetServerExecutor executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task AspNetServerExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    ".*",
                    File.ReadAllText(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Examples", "Bombardier", "BombardierExample.txt")));

            using (var executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill dotnet",
                    "fuser -n tcp -k 9876",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}/dotnet build -c Release -p:BenchmarksTargetFramework=net8\\.0",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}/dotnet {Regex.Escape(this.mockAspNetBenchPackage.Path)}/src/Benchmarks/bin/Release/net8\\.0/Benchmarks\\.dll --nonInteractive true --scenarios json --urls http://\\*:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header \\\"Accept: application/json,text/html;q=0\\.9,application/xhtml\\+xml;q=0\\.9,application/xml;q=0\\.8,\\*/\\*;q=0\\.7\\\" --header \\\"Connection: keep-alive\\\""
                );

                this.mockFixture.Tracking.AssertCommandExecutedTimes("pkill", 1);
                this.mockFixture.Tracking.AssertCommandExecutedTimes("fuser", 1);
            }
        }

        [Test]
        public async Task AspNetServerExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    ".*",
                    File.ReadAllText(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Examples", "Bombardier", "BombardierExample.txt")));

            using (var executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill dotnet",
                    "fuser -n tcp -k 9876",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}\\\\dotnet\\.exe build -c Release -p:BenchmarksTargetFramework=net8\\.0",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}\\\\dotnet\\.exe {Regex.Escape(this.mockAspNetBenchPackage.Path)}\\\\src\\\\Benchmarks\\\\bin\\\\Release\\\\net8\\.0\\\\Benchmarks\\.dll --nonInteractive true --scenarios json --urls http://\\*:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header \\\"Accept: application/json,text/html;q=0\\.9,application/xhtml\\+xml;q=0\\.9,application/xml;q=0\\.8,\\*/\\*;q=0\\.7\\\" --header \\\"Connection: keep-alive\\\""
                );

                this.mockFixture.Tracking.AssertCommandExecutedTimes("pkill", 1);
                this.mockFixture.Tracking.AssertCommandExecutedTimes("fuser", 1);
            }
        }

        [Test]
        public async Task AspNetServerExecutorInitializeAsyncSetsCorrectPaths()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("Server", "1.2.3.4", ClientRole.Server),
                new ClientInstance("Client", "5.6.7.8", ClientRole.Client)
            });

            using (TestAspNetServerExecutor executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                // Verify correct paths are set
                string expectedAspNetDir = this.mockFixture.Combine(
                    this.mockAspNetBenchPackage.Path,
                    "src",
                    "Benchmarks");

                Assert.AreEqual(expectedAspNetDir, executor.AspNetBenchDirectory);

                string expectedDotNetPath = this.mockFixture.Combine(
                    this.mockDotNetPackage.Path,
                    "dotnet");

                Assert.AreEqual(expectedDotNetPath, executor.DotNetExePath);

                // Verify API client is initialized
                Assert.IsNotNull(executor.ServerApi);
            }
        }

        [Test]
        public void AspNetServerExecutorThrowsWhenBindToCoresIsTrueButCoreAffinityIsNotProvided()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(AspNetServerExecutor.BindToCores)] = true;
            this.mockFixture.Parameters.Remove(nameof(AspNetServerExecutor.CoreAffinity));

            using (TestAspNetServerExecutor executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.Throws<DependencyException>(() => executor.Validate());
            }
        }

        [Test]
        public async Task AspNetServerExecutorExecutesWithCoreAffinityOnLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(AspNetServerExecutor.BindToCores)] = true;
            this.mockFixture.Parameters[nameof(AspNetServerExecutor.CoreAffinity)] = "0-7";

            this.mockFixture
                .TrackProcesses()
                .SetupProcessOutput(
                    ".*",
                    File.ReadAllText(Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Examples", "Bombardier", "BombardierExample.txt")));

            using (var executor = new TestAspNetServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                // Verify numactl was used with correct core affinity
                // The actual command format is: /bin/bash -c "numactl -C 0-7 /path/to/dotnet /path/to/dll ..."
                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill dotnet",
                    "fuser -n tcp -k 9876",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}/dotnet build -c Release -p:BenchmarksTargetFramework=net8\\.0",
                    "/bin/bash -c \\\"numactl -C 0-7 .*"
                );
            }
        }

        private class TestAspNetServerExecutor : AspNetServerExecutor
        {
            public TestAspNetServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
                this.ServerRetryPolicy = Policy.NoOpAsync();
            }

            public string AspNetBenchDirectory
            {
                get
                {
                    var field = typeof(AspNetServerExecutor).GetField("aspnetBenchDirectory",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this) as string;
                }
            }

            public string AspNetBenchDllPath
            {
                get
                {
                    var field = typeof(AspNetServerExecutor).GetField("aspnetBenchDllPath",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this) as string;
                }
            }

            public string DotNetExePath
            {
                get
                {
                    var field = typeof(AspNetServerExecutor).GetField("dotnetExePath",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this) as string;
                }
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }

            protected override Task WaitForPortReadyAsync(EventContext context, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Win32NT);
                this.mockAspNetBenchPackage = new DependencyPath("aspnetbenchmarks", this.mockFixture.PlatformSpecifics.GetPackagePath("aspnetbenchmarks"));
                this.mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));

                this.mockFixture.PackageManager.OnGetPackage(mockAspNetBenchPackage.Name).ReturnsAsync(mockAspNetBenchPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockDotNetPackage.Name).ReturnsAsync(mockDotNetPackage);

                this.mockFixture.ApiClient.OnUpdateState<ServerState>(nameof(ServerState))
                   .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));
            }
            else
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Unix);

                this.mockAspNetBenchPackage = new DependencyPath("aspnetbenchmarks", this.mockFixture.PlatformSpecifics.GetPackagePath("aspnetbenchmarks"));
                this.mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));
                this.mockFixture.PackageManager.OnGetPackage(mockAspNetBenchPackage.Name).ReturnsAsync(mockAspNetBenchPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockDotNetPackage.Name).ReturnsAsync(mockDotNetPackage);
            }

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(AspNetServerExecutor.PackageName), "aspnetbenchmarks" },
                { nameof(AspNetServerExecutor.DotNetSdkPackageName), "dotnetsdk" },
                { nameof(AspNetServerExecutor.TargetFramework), "net8.0" },
                { nameof(AspNetServerExecutor.ServerPort), "9876" },
                { nameof(AspNetServerExecutor.AspNetCoreThreadCount), "1" },
                { nameof(AspNetServerExecutor.DotNetSystemNetSocketsThreadCount), "1" }
            };

            this.mockFixture.ApiClient.OnUpdateState<State>(nameof(State))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));
        }
    }
}
