// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BlenderBenchmarkExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockBlenderPackage;
        private string results;

        [SetUp]
        public void SetUpTests()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "benchmark-launcher-cli.exe");

                Assert.AreEqual(expectedExecutablePath, executor.ExecutablePath);
            }
        }

        //[Test]
        //[TestCase(PlatformID.Win32NT, Architecture.X64)]
        //public async Task BlenderDownloadsAsExpected(PlatformID platform, Architecture architecture)
        //{
        //    int blenderEnginerDownloaded = 0, blenderScenesDownloaded = 0;
        //    this.SetupDefaultMockBehavior(platform, architecture);

        //    using (TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture))
        //    {
        //        this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
        //        {
        //            if (arguments == expectedCommandArgument && command == expectedExecutablePath)
        //            {
        //                executed++;
        //            }

        //            return this.mockFixture.Process;
        //        };
        //    }
        //}



        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                string[] deviceTypes = this.mockFixture.Parameters["DeviceTypes"].ToString().Split(',', StringSplitOptions.TrimEntries);
                string[] scenes = this.mockFixture.Parameters["Scenes"].ToString().Split(',', StringSplitOptions.TrimEntries);

                string expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "benchmark-launcher-cli.exe");
                string workingDir = this.mockBlenderPackage.Path;
                IList<string> commands = new List<string>();

                if (platform == PlatformID.Win32NT)
                {
                    string expectedCommandArgument;
                    foreach (string deviceType in deviceTypes)
                    {
                        foreach (string scene in scenes)
                        {
                            expectedCommandArgument = $"benchmark --blender-version {this.mockFixture.Parameters["BlenderVersion"]} --device-type {deviceType} {scene} --json --verbosity 3";
                            commands.Add(@$"{expectedExecutablePath} {expectedCommandArgument}");
                        }
                    }
                }
                Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecuted(commands.ToArray<string>()));
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                {"PackageName", "blenderbenchmarkcli"},
                {"BlenderVersion", "3.6.0"},
                {"Scenes", "monster,junkshop,classroom"},
                {"DeviceTypes", "CPU,HIP"}
            };

            this.mockBlenderPackage = new DependencyPath("blenderbenchmarkcli", this.mockFixture.GetPackagePath("blenderbenchmarkcli"));
            this.mockFixture.PackageManager.OnGetPackage("blenderbenchmarkcli").ReturnsAsync(this.mockBlenderPackage);
            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "BlenderBenchmark", "MonsterCPU.json"));
            this.mockFixture.Process.StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results));
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => { ((InMemoryProcess) process).StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results)); };
        }

        private class TestBlenderExecutor : BlenderBenchmarkExecutor
        {
            public TestBlenderExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }
            public new string ExecutablePath => base.ExecutablePath;

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
