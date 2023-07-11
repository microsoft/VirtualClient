// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading;
    using System.Reflection;
    using System.IO;
    using Moq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Telemetry;
    using Microsoft.VisualStudio.TestPlatform.TestHost;

    [TestFixture]
    [Category("Unit")]
    public class FurmarkExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;

        private string resultsPath;
        private string rawString;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.mockPath = this.fixture.Create<DependencyPath>();
        }
        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task FurmarkExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.fixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedScriptFilePath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path,"win-x64", "Geeks3D", "Benchmarks", "FurMark", "Furmark");

                Assert.AreEqual(expectedScriptFilePath, executor.ExecutableLocation);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task FurmarkExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.fixture.Parameters["Time"] = "20";
            this.fixture.Parameters["Width"] = "100";
            this.fixture.Parameters["Height"] = "200";

            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.fixture))
            {
                string expectedFilePath = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path);
                int executed = 0;
                if (platform == PlatformID.Win32NT)
                {
                    string expectedCommand = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path ,"win-x64","Geeks3D" ,"Benchmarks" ,"FurMark" ,"Furmark");
                    string packageDir = Regex.Replace(expectedFilePath, @"\\", "/");
                    packageDir = Regex.Replace(packageDir, @":", string.Empty);

                    // string expectedmakeCommandArguments = @$"C:\Program Files (x86)\Geeks3D\Benchmarks\FurMark\Furmark";
                    string executeScriptCommandArguments = $"/width={this.fixture.Parameters["Width"]} /height={this.fixture.Parameters["Height"]} /msaa=4 /max_time={this.fixture.Parameters["Time"]} /nogui /nomenubar /noscore /run_mode=1 /log_score /disable_catalyst_warning /log_temperature /max_frames";

                    this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == executeScriptCommandArguments && command == expectedCommand)
                        {
                            executed++;
                        }
                        
                        return this.fixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                        .ConfigureAwait(false);

                }

                Assert.AreEqual(1, executed);
            }
        }

        [Test]
        public void FurmarkExecutorThrowsWhenTheResultsFileIsNotGenerated()
        {
            this.SetupDefaultMockBehavior();

            using (TestFurmarkxecutor executor = new TestFurmarkxecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.fixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsFilePath)).Returns(false);
                    return this.fixture.Process;
                };

                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("Furmark", currentDirectory);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\Furmark\FurmarkExample.txt");
            this.rawString = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.fixture.Parameters["PackageName"] = "Furmark";
        }

        private class TestFurmarkxecutor : FurmarkExecutor
        {
            public TestFurmarkxecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestFurmarkxecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }


            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
