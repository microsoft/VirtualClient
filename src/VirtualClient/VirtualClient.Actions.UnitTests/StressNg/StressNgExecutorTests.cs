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
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class StressNgExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        [TestCase("--timeout 90 --cpu 16", "--metrics", "--timeout 90 --cpu 16")]
        [TestCase("", "--cpu coreCount --timeout 60 --metrics", "")]
        public async Task StressNgExecutorRunsTheExpectedWorkloadCommandInLinux(string inputCommandLineArgs, string expectedArgsInCommandPrefix, string expectedArgsInCommandSuffix)
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.Parameters[nameof(StressNgExecutor.CommandLine)] = inputCommandLineArgs;

            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            string expectedArgsPrefix = expectedArgsInCommandPrefix.Replace("coreCount", Environment.ProcessorCount.ToString());
            string expectedCommand = @$"sudo stress-ng {expectedArgsPrefix} --yaml {this.mockFixture.GetPackagePath()}/stressNg/vcStressNg.yaml {expectedArgsInCommandSuffix}";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand.Trim() == $"{exe} {arguments}")
                {
                    commandExecuted = true;
                }

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestStressNgExecutor StressNgExecutor = new TestStressNgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await StressNgExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        [TestCase(PlatformID.Unix)]
        public void StressNgExecutorThrowsOnInvalidProfileDefinition(PlatformID platform)
        {
            this.SetupDefaultMockBehaviors(platform);

            this.mockFixture.Parameters[nameof(StressNgExecutor.CommandLine)] = "--cpu 16 --yaml output.yaml";
            using (TestStressNgExecutor executor = new TestStressNgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.Throws<WorkloadException>(() => executor.Validate());
            }
        }

        private class TestStressNgExecutor : StressNgExecutor
        {
            public TestStressNgExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new void Validate()
            {
                base.Validate();
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory,"Examples", "StressNg", "StressNgCpuExample.yaml");
            string results = File.ReadAllText(resultsPath);

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(results);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(StressNgExecutor.CommandLine), "--cpu 16 --timeout 60" },
                { nameof(StressNgExecutor.Scenario), "CaptureSystemThroughput" }
            };
        }
    }
}
