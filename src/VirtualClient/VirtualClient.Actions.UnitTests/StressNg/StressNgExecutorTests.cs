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
        public async Task StressNgExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            string expectedCommand = @$"stress-ng --cpu 71 --timeout 321 --metrics --yaml {this.mockFixture.GetPackagePath()}/stressNg/vcStressNg.yaml";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
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
                { nameof(StressNgExecutor.DurationInSecond), 321 },
            };
        }
    }
}
