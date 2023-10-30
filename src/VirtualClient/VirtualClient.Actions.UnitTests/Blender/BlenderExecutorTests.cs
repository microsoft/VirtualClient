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
    public class BlenderExecutorTests
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
                int executed = 0, index = 0;

                // Blender will execute twice, each time benchmarking on one device type.
                string[] deviceTypes = this.mockFixture.Parameters["DeviceTypes"].ToString().Split(',', StringSplitOptions.TrimEntries);

                if (platform == PlatformID.Win32NT)
                {
                    string[] commandArgumentsArray = new string[deviceTypes.Length];
                    string commandArgument;
                    for (int i = 0; i < deviceTypes.Length; i++)
                    {
                        commandArgument = $"benchmark --blender-version {this.mockFixture.Parameters["BlenderVersion"]} --device-type {deviceTypes[i]} {this.mockFixture.Parameters["Scenes"]} --json --verbosity 3";
                        commandArgumentsArray[i] = commandArgument;
                    }

                    string expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(
                        this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path, "benchmark-launcher-cli.exe");
                    string workingDir = this.mockBlenderPackage.Path;

                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == commandArgumentsArray[index] && command == expectedExecutablePath)
                        {
                            executed++;
                            index++; // If Blender needs to benchmark on another device, the increase index result in the next set of command and arguments being used.
                        }

                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                }

                Assert.AreEqual(deviceTypes.Length, executed);
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
                {"Scenes", "monster junkshop classroom"},
                {"DeviceTypes", "CPU, HIP"}
            };

            this.mockBlenderPackage = new DependencyPath("blenderbenchmarkcli", this.mockFixture.GetPackagePath("blenderbenchmarkcli"));
            this.mockFixture.PackageManager.OnGetPackage("blenderbenchmarkcli").ReturnsAsync(this.mockBlenderPackage);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "Blender", "results_example.txt"));
            this.mockFixture.Process.StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results));
        }

        private class TestBlenderExecutor : BlenderExecutor
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
