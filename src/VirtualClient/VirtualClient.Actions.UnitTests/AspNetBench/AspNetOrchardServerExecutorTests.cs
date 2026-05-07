// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class AspNetOrchardServerExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockOrchardCorePackage;
        private DependencyPath mockDotNetPackage;

        [Test]
        public void AspNetOrchardServerOrchardExecutorThrowsIfCannotFindAspNetOrchardPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            this.mockFixture.PackageManager.OnGetPackage("orchardcore").ReturnsAsync(value: null);

            using (TestAspNetOrchardServerExecutor executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void AspNetOrchardServerOrchardExecutorThrowsIfCannotFindDotNetSDKPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("dotnetsdk").ReturnsAsync(value: null);

            using (TestAspNetOrchardServerExecutor executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task AspNetOrchardServerExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            this.mockFixture.TrackProcesses();

            using (var executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill OrchardCore",
                    "fuser -n tcp -k 5014",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}/dotnet publish -c Release --sc -f net9\\.0 {Regex.Escape(this.mockOrchardCorePackage.Path)}/src/OrchardCore\\.Cms\\.Web/OrchardCore\\.Cms\\.Web\\.csproj",
                    $"nohup {Regex.Escape(this.mockOrchardCorePackage.Path)}/src/OrchardCore\\.Cms\\.Web/bin/Release/net9\\.0/linux-x64/publish/OrchardCore\\.Cms\\.Web --urls http://\\*:5014"
                );

                this.mockFixture.Tracking.AssertCommandExecutedTimes("pkill", 1);
                this.mockFixture.Tracking.AssertCommandExecutedTimes("fuser", 1);
            }
        }

        [Test]
        public async Task AspNetOrchardServerExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);

            this.mockFixture.TrackProcesses();

            using (var executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill OrchardCore",
                    "fuser -n tcp -k 5014",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}\\\\dotnet\\.exe publish -c Release --sc -f net9\\.0 {Regex.Escape(this.mockOrchardCorePackage.Path)}\\\\src\\\\OrchardCore\\.Cms\\.Web\\\\OrchardCore\\.Cms\\.Web\\.csproj",
                    $"nohup {Regex.Escape(this.mockOrchardCorePackage.Path)}\\\\src\\\\OrchardCore\\.Cms\\.Web\\\\bin\\\\Release\\\\net9\\.0\\\\win-x64\\\\publish\\\\OrchardCore\\.Cms\\.Web --urls http://\\*:5014"
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

            using (TestAspNetOrchardServerExecutor executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                // Verify correct paths are set
                string expectedAspNetDir = this.mockFixture.Combine(
                    this.mockOrchardCorePackage.Path,
                    "src",
                    "OrchardCore.Cms.Web");

                Assert.AreEqual(expectedAspNetDir, executor.AspnetOrchardDirectory);

                string expectedDotNetPath = this.mockFixture.Combine(
                    this.mockDotNetPackage.Path,
                    "dotnet");

                Assert.AreEqual(expectedDotNetPath, executor.DotNetExePath);

                // Verify API client is initialized
                Assert.IsNotNull(executor.ServerApi);
            }
        }

        [Test]
        public void AspNetOrchardServerExecutorThrowsWhenBindToCoresIsTrueButCoreAffinityIsNotProvided()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(AspNetOrchardServerExecutor.BindToCores)] = true;
            this.mockFixture.Parameters.Remove(nameof(AspNetOrchardServerExecutor.CoreAffinity));

            using (TestAspNetOrchardServerExecutor executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.Throws<DependencyException>(() => executor.Validate());
            }
        }

        [Test]
        public async Task AspNetOrchardServerExecutorExecutesWithCoreAffinityOnLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(AspNetOrchardServerExecutor.BindToCores)] = true;
            this.mockFixture.Parameters[nameof(AspNetOrchardServerExecutor.CoreAffinity)] = "0-3";

            this.mockFixture.TrackProcesses();

            using (var executor = new TestAspNetOrchardServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None);

                // Verify numactl was used with correct core affinity
                // Actual format: /bin/bash -c "numactl -C 0-3 nohup ..."
                this.mockFixture.Tracking.AssertCommandsExecuted(true,
                    "pkill OrchardCore",
                    "fuser -n tcp -k 5014",
                    $"{Regex.Escape(this.mockDotNetPackage.Path)}/dotnet publish -c Release --sc -f net9\\.0 {Regex.Escape(this.mockOrchardCorePackage.Path)}/src/OrchardCore\\.Cms\\.Web/OrchardCore\\.Cms\\.Web\\.csproj",
                    "/bin/bash -c \\\"numactl -C 0-3 .*"
                );
            }
        }

        private class TestAspNetOrchardServerExecutor : AspNetOrchardServerExecutor
        {
            public TestAspNetOrchardServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
                this.ServerRetryPolicy = Policy.NoOpAsync();
            }

            public string AspnetOrchardDirectory
            {
                get
                {
                    var field = typeof(AspNetOrchardServerExecutor).GetField("aspnetOrchardDirectory",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this) as string;
                }
            }

            public string DotNetExePath
            {
                get
                {
                    var field = typeof(AspNetOrchardServerExecutor).GetField("dotnetExePath",
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
                this.mockOrchardCorePackage = new DependencyPath("orchardcore", this.mockFixture.PlatformSpecifics.GetPackagePath("orchardcore"));
                this.mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));
                this.mockFixture.PackageManager.OnGetPackage(mockOrchardCorePackage.Name).ReturnsAsync(mockOrchardCorePackage);
                this.mockFixture.PackageManager.OnGetPackage(mockDotNetPackage.Name).ReturnsAsync(mockDotNetPackage);
            }
            else
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Unix);
                this.mockOrchardCorePackage = new DependencyPath("orchardcore", this.mockFixture.PlatformSpecifics.GetPackagePath("orchardcore"));
                this.mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));
                this.mockFixture.PackageManager.OnGetPackage(mockOrchardCorePackage.Name).ReturnsAsync(mockOrchardCorePackage);
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
                { nameof(AspNetOrchardServerExecutor.PackageName), "orchardcore" },
                { nameof(AspNetOrchardServerExecutor.DotNetSdkPackageName), "dotnetsdk" },
                { nameof(AspNetOrchardServerExecutor.TargetFramework), "net9.0" },
                { nameof(AspNetOrchardServerExecutor.ServerPort), "5014" }
            };

            this.mockFixture.ApiClient.OnUpdateState<State>(nameof(State))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));
        }
    }
}
