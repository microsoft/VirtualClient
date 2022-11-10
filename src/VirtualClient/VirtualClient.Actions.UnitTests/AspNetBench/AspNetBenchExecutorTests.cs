// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class AspNetBenchExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        public void AspNetBenchExecutorThrowsIfCannotFindAspNetBenchPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            this.mockFixture.PackageManager.OnGetPackage("aspnetbenchmarks").ReturnsAsync(value: null);

            using (TestAspNetBenchExecutor executor = new TestAspNetBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void AspNetBenchExecutorThrowsIfCannotFindBombardierPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("bombardier").ReturnsAsync(value: null);

            using (TestAspNetBenchExecutor executor = new TestAspNetBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void AspNetBenchExecutorThrowsIfCannotFindDotNetSDKPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("dotnetsdk").ReturnsAsync(value: null);

            using (TestAspNetBenchExecutor executor = new TestAspNetBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task AspNetBenchExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            string packageDirectory = this.mockFixture.GetPackagePath();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                $@"sudo chmod +x ""{packageDirectory}/bombardier/linux-x64/bombardier""",
                $@"sudo {packageDirectory}/dotnet/dotnet build -c Release -p:BenchmarksTargetFramework=net123.321",
                $@"sudo {packageDirectory}/dotnet/dotnet {packageDirectory}/aspnetbenchmarks/src/Benchmarks/bin/Release/net123.321/Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:12321 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7"" --header ""Connection: keep-alive""",
                $@"sudo {packageDirectory}/bombardier/linux-x64/bombardier --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:12321/json --print r --format json"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Bombardier", "BombardierExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));
                return process;
            };

            using (TestAspNetBenchExecutor executor = new TestAspNetBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(4, commandExecuted);
        }

        [Test]
        public async Task AspNetBenchExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);

            string packageDirectory = this.mockFixture.GetPackagePath();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            List<string> expectedCommands = new List<string>()
            {
                $@"{packageDirectory}\dotnet\dotnet.exe build -c Release -p:BenchmarksTargetFramework=net123.321",
                $@"{packageDirectory}\dotnet\dotnet.exe {packageDirectory}\aspnetbenchmarks\src\Benchmarks\bin\Release\net123.321\Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:12321 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7"" --header ""Connection: keep-alive""",
                $@"{packageDirectory}\bombardier\win-x64\bombardier.exe --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:12321/json --print r --format json"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess()
                {
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
                string resultsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Bombardier", "BombardierExample.txt");
                process.StandardOutput.Append(File.ReadAllText(resultsPath));
                return process;
            };

            using (TestAspNetBenchExecutor executor = new TestAspNetBenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        private class TestAspNetBenchExecutor : AspNetBenchExecutor
        {
            public TestAspNetBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Win32NT);

                DependencyPath mockAspNetBenchPackage = new DependencyPath("aspnetbenchmarks", this.mockFixture.PlatformSpecifics.GetPackagePath("aspnetbenchmarks"));
                DependencyPath mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));
                DependencyPath mockBombardierPackage = new DependencyPath("bombardier", this.mockFixture.PlatformSpecifics.GetPackagePath("bombardier"));
                this.mockFixture.PackageManager.OnGetPackage(mockAspNetBenchPackage.Name).ReturnsAsync(mockAspNetBenchPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockDotNetPackage.Name).ReturnsAsync(mockDotNetPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockBombardierPackage.Name).ReturnsAsync(mockBombardierPackage);
            }
            else
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Unix);

                DependencyPath mockAspNetBenchPackage = new DependencyPath("aspnetbenchmarks", this.mockFixture.PlatformSpecifics.GetPackagePath("aspnetbenchmarks"));
                DependencyPath mockDotNetPackage = new DependencyPath("dotnetsdk", this.mockFixture.PlatformSpecifics.GetPackagePath("dotnet"));
                DependencyPath mockBombardierPackage = new DependencyPath("bombardier", this.mockFixture.PlatformSpecifics.GetPackagePath("bombardier"));
                this.mockFixture.PackageManager.OnGetPackage(mockAspNetBenchPackage.Name).ReturnsAsync(mockAspNetBenchPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockDotNetPackage.Name).ReturnsAsync(mockDotNetPackage);
                this.mockFixture.PackageManager.OnGetPackage(mockBombardierPackage.Name).ReturnsAsync(mockBombardierPackage);
            }

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(AspNetBenchExecutor.PackageName), "aspnetbenchmarks" },
                { nameof(AspNetBenchExecutor.DotNetSdkPackageName), "dotnetsdk" },
                { nameof(AspNetBenchExecutor.BombardierPackageName), "bombardier" },
                { nameof(AspNetBenchExecutor.TargetFramework), "net123.321" },
                { nameof(AspNetBenchExecutor.Port), "12321" }
            };
        }
    }
}
