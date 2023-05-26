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
    public class Pbzip2ExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("pbzip2", this.mockFixture.PlatformSpecifics.GetPackagePath("pbzip2"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Pbzip2Executor.Options), "testOption1 testOption2" },
                { nameof(Pbzip2Executor.InputFiles), "Test1.zip Test2.txt" },
                { nameof(Pbzip2Executor.PackageName), "Pbzip2" },
                { nameof(Pbzip2Executor.Scenario), "mockScenario"}
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Pbzip2", "Pbzip2ResultsExample.txt");
            string results = File.ReadAllText(resultsPath);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(results);
        }

        [Test]
        public void Pbzip2StateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["Pbzip2StateInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            Pbzip2Executor.Pbzip2State result = deserializedState?.ToObject<Pbzip2Executor.Pbzip2State>();
            Assert.AreEqual(true, result.Pbzip2StateInitialized);
        }

        [Test]
        public async Task Pbzip2ExecutorGetsDefaultFileIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Pbzip2Executor.Options), "testOption1 testOption2" },
                { nameof(Pbzip2Executor.InputFiles), "" },
                { nameof(Pbzip2Executor.PackageName), "pbzip2" },
                { nameof(Pbzip2Executor.Scenario), "mockScenario" }
            };
            string expectedCommand = $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip";

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
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            using (TestPbzip2Executor Pbzip2Executor = new TestPbzip2Executor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Pbzip2Executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Pbzip2ExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Pbzip2Executor.Options), "testOption1 testOption2" },
                { nameof(Pbzip2Executor.InputFiles), "" },
                { nameof(Pbzip2Executor.PackageName), "pbzip2" },
                { nameof(Pbzip2Executor.Scenario), "mockScenario" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"sudo bash -c \"pbzip2 testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}\"";

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
                    OnHasExited = () => true,
                    StandardError = this.defaultOutput
                };
            };

            using (TestPbzip2Executor Pbzip2Executor = new TestPbzip2Executor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Pbzip2Executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Pbzip2ExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Pbzip2Executor.Options), "testOption1 testOption2" },
                { nameof(Pbzip2Executor.InputFiles), "" },
                { nameof(Pbzip2Executor.PackageName), "pbzip2" },
                { nameof(Pbzip2Executor.Scenario), "mockScenario"}
            };

            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"sudo unzip silesia.zip -d silesia",
                $"sudo bash -c \"pbzip2 testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}\""
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Pbzip2Executor.Pbzip2State()
            {
                Pbzip2StateInitialized = false
            }));

            using (TestPbzip2Executor Pbzip2Executor = new TestPbzip2Executor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Pbzip2Executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 3);
        }

        [Test]
        public async Task Pbzip2ExecutorExecutesTheCorrectCommandsWithInstallationIfInputFilesOrDirsAreProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo bash -c \"pbzip2 testOption1 testOption2 Test1.zip Test2.txt\"",
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Pbzip2Executor.Pbzip2State()
            {
                Pbzip2StateInitialized = false
            }));

            using (TestPbzip2Executor Pbzip2Executor = new TestPbzip2Executor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Pbzip2Executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        [Test]
        public async Task Pbzip2ExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo bash -c \"pbzip2 testOption1 testOption2 Test1.zip Test2.txt\""
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Pbzip2Executor.Pbzip2State()
            {
                Pbzip2StateInitialized = true
            }));

            using (TestPbzip2Executor Pbzip2Executor = new TestPbzip2Executor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Pbzip2Executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        private class TestPbzip2Executor : Pbzip2Executor
        {
            public TestPbzip2Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
