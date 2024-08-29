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
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class GzipExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("gzip", this.fixture.PlatformSpecifics.GetPackagePath("gzip"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GzipExecutor.Options), "testOption1 testOption2" },
                { nameof(GzipExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(GzipExecutor.PackageName), "Gzip" },
                { nameof(GzipExecutor.Scenario), "mockScenario"}
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Gzip", "GzipResultsExample.txt");
            string results = File.ReadAllText(resultsPath);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(results);
        }

        [Test]
        public void GzipStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["GzipStateInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            GzipExecutor.GzipState result = deserializedState?.ToObject<GzipExecutor.GzipState>();
            Assert.AreEqual(true, result.GzipStateInitialized);
        }

        [Test]
        public async Task GzipExecutorGetsDefaultFileIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GzipExecutor.Options), "testOption1 testOption2" },
                { nameof(GzipExecutor.InputFilesOrDirs), "" },
                { nameof(GzipExecutor.PackageName), "gzip" },
                { nameof(GzipExecutor.Scenario), "mockScenario" }
            };
            string expectedCommand = $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip";

            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            using (TestGzipExecutor GzipExecutor = new TestGzipExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await GzipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task GzipExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GzipExecutor.Options), "testOption1 testOption2" },
                { nameof(GzipExecutor.InputFilesOrDirs), "" },
                { nameof(GzipExecutor.PackageName), "gzip" },
                { nameof(GzipExecutor.Scenario), "mockScenario" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"sudo bash -c \"gzip testOption1 testOption2 {this.fixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\"";

            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            using (TestGzipExecutor GzipExecutor = new TestGzipExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await GzipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task GzipExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(GzipExecutor.Options), "testOption1 testOption2" },
                { nameof(GzipExecutor.InputFilesOrDirs), "" },
                { nameof(GzipExecutor.PackageName), "gzip" },
                { nameof(GzipExecutor.Scenario), "mockScenario"}
            };

            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"sudo unzip silesia.zip -d silesia",
                $"sudo bash -c \"gzip testOption1 testOption2 {this.fixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\""
            };

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new GzipExecutor.GzipState()
            {
                GzipStateInitialized = false
            }));

            using (TestGzipExecutor GzipExecutor = new TestGzipExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await GzipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 3);
        }

        [Test]
        public async Task GzipExecutorExecutesTheCorrectCommandsWithInstallationIfInputFilesOrDirsAreProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo bash -c \"gzip testOption1 testOption2 Test1.zip Test2.txt\"",
            };

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new GzipExecutor.GzipState()
            {
                GzipStateInitialized = false
            }));

            using (TestGzipExecutor GzipExecutor = new TestGzipExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await GzipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        [Test]
        public async Task GzipExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo bash -c \"gzip testOption1 testOption2 Test1.zip Test2.txt\""
            };

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new GzipExecutor.GzipState()
            {
                GzipStateInitialized = true
            }));

            using (TestGzipExecutor GzipExecutor = new TestGzipExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await GzipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        private class TestGzipExecutor : GzipExecutor
        {
            public TestGzipExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
