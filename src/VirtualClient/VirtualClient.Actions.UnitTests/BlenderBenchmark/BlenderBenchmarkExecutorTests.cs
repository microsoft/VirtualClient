// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
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
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };
                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "benchmark-launcher-cli.exe");

                Assert.AreEqual(expectedExecutablePath, executor.ExecutablePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture))
            {
                int executed = 0;

                string[] deviceTypes = this.mockFixture.Parameters["DeviceTypes"].ToString().Split(',', StringSplitOptions.TrimEntries);
                string[] scenes = this.mockFixture.Parameters["Scenes"].ToString().Split(',', StringSplitOptions.TrimEntries);

                string expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(
                    this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "benchmark-launcher-cli.exe");
                string workingDir = this.mockBlenderPackage.Path;

                if (platform == PlatformID.Win32NT)
                {
                    string expectedCommandArgument;
                    foreach (string deviceType in deviceTypes)
                    {
                        foreach (string scene in scenes)
                        {
                            expectedCommandArgument = $"benchmark --blender-version {this.mockFixture.Parameters["BlenderVersion"]} --device-type {deviceType} {scene} --json --verbosity 3";

                            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                            {
                                if (arguments == expectedCommandArgument && command == expectedExecutablePath)
                                {
                                    executed++;
                                }

                                return this.mockFixture.Process;
                            };

                            await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                        }
                    }
                }

                Assert.AreEqual(deviceTypes.Length*scenes.Length, executed);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                {"Scenario", "BlenderbenchmarkUnitTest"},
                {"PackageName", "blenderbenchmarkcli"},
                {"BlenderVersion", "3.6.0"},
                {"Scenes", "monster,junkshop,classroom"},
                {"DeviceTypes", "CPU,HIP"}
            };

            this.mockBlenderPackage = new DependencyPath("blenderbenchmarkcli", this.mockFixture.GetPackagePath("blenderbenchmarkcli"));
            this.mockFixture.PackageManager.OnGetPackage("blenderbenchmarkcli").ReturnsAsync(this.mockBlenderPackage);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "BlenderBenchmark", "monster_cpu.json"));
            this.mockFixture.Process.StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results));
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
