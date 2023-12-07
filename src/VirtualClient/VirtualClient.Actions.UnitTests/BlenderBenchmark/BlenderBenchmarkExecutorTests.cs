// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
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
        private State mockState;
        private string results;
        private string expectedExecutableDir;
        private string expectedExecutablePath;

        private string[] deviceTypes
        {
            get
            {
                return this.mockFixture.Parameters["DeviceTypes"].ToString().Split(',', StringSplitOptions.TrimEntries);
            }
        }

        private string[] scenes
        {
            get
            {
                return this.mockFixture.Parameters["Scenes"].ToString().Split(',', StringSplitOptions.TrimEntries);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            
            TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture);
            await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(expectedExecutablePath, executor.ExecutablePath);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture);
            await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

            IList<string> commands = new List<string>();

            // engine download cmd
            commands.Add($"{expectedExecutablePath} blender download {this.mockFixture.Parameters["BlenderVersion"]}");

            // scenes download cmd
            commands.Add($"{expectedExecutablePath} scenes download --blender-version {this.mockFixture.Parameters["BlenderVersion"]} {string.Join(" ", this.scenes)}");

            string expectedCommandArgument;
            foreach (string deviceType in this.deviceTypes)
            {
                foreach (string scene in this.scenes)
                {
                    expectedCommandArgument = $"benchmark --blender-version {this.mockFixture.Parameters["BlenderVersion"]} --device-type {deviceType} {scene} --json --verbosity 3";
                    commands.Add($"{expectedExecutablePath} {expectedCommandArgument}");
                }
            }

            Assert.IsTrue(this.mockFixture.ProcessManager.CommandsExecutedInWorkingDirectory(expectedExecutableDir, commands.ToArray<string>()));
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        public async Task BlenderExecutorDoesNotDownloadIfAlreadyDownloaded(PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            this.mockFixture.StateManager.OnGetState(nameof(BlenderBenchmarkExecutor)).ReturnsAsync(JObject.FromObject(this.mockState));

            // engine download cmd
            string engineDownloadCmd = $"{expectedExecutablePath} blender download {this.mockFixture.Parameters["BlenderVersion"]}";

            // scene download cmd
            string scenesDownloadCmd = $"{expectedExecutablePath} scenes download --blender-version {this.mockFixture.Parameters["BlenderVersion"]} {string.Join(" ", this.scenes)}";

            TestBlenderExecutor executor = new TestBlenderExecutor(this.mockFixture);
            await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

            try
            {
                Assert.IsFalse(this.mockFixture.ProcessManager.CommandsExecuted(engineDownloadCmd, scenesDownloadCmd));
            }
            catch (RegexParseException)
            {
                // Ignore Regex Exceptions since we are comparing raw strings
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
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
            this.expectedExecutableDir = this.mockFixture.ToPlatformSpecificPath(this.mockBlenderPackage, this.mockFixture.Platform, this.mockFixture.CpuArchitecture).Path;
            this.expectedExecutablePath = this.mockFixture.PlatformSpecifics.Combine(expectedExecutableDir, "benchmark-launcher-cli.exe");

            this.results = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, "BlenderBenchmark", "MonsterCPU.json"));
            this.mockFixture.Process.StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results));

            // Set up the process's standard output to be the mock blender metrics result as the parser reads the results from the std out.
            this.mockFixture.ProcessManager.OnProcessCreated = (process) => { ((InMemoryProcess)process).StandardOutput = new Common.ConcurrentBuffer(new StringBuilder(this.results)); };

            this.mockState = new State();
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
