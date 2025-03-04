// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class Compressor7zipExecutorTests : MockFixture
    {
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Win32NT);
            this.mockPackage = new DependencyPath("7zip", this.PlatformSpecifics.GetPackagePath("7zip"));

            this.SetupPackage(this.mockPackage);

            this.File.Reset();
            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.FileSystem.SetupGet(fs => fs.File).Returns(this.File.Object);

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario"}
            };

            string exampleResults = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "Compressor7zip", "Compressor7zipResultsExample.txt");
            this.defaultOutput.Clear();
            this.defaultOutput.Append(exampleResults);
        }

        [Test]
        public void Compressor7zipStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["Compressor7zipStateInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            Compression7zipExecutor.Compression7zipState result = deserializedState?.ToObject<Compression7zipExecutor.Compression7zipState>();
            Assert.AreEqual(true, result.Compressor7zipStateInitialized);
        }

        [Test]
        public async Task Compressor7zipExecutorGetsDefaultFileIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario" }
            };
            string expectedCommand = $"wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip";

            bool commandExecuted = false;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    StandardOutput = this.defaultOutput
                };
            };

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.Dependencies, this.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Compressor7zipExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"7z testOption1 testOption2 {this.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}";

            bool commandExecuted = false;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    StandardOutput = this.defaultOutput
                };
            };

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.Dependencies, this.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Compressor7zipExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario"}
            };

            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"unzip silesia.zip -d silesia",
                $"7z testOption1 testOption2 {this.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}"
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = false
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.Dependencies, this.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 3);
        }

        [Test]
        public async Task Compressor7zipExecutorExecutesTheCorrectCommandsWithInstallationIfInputFilesOrDirsAreProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"7z testOption1 testOption2 Test1.zip Test2.txt",
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = false
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.Dependencies, this.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        [Test]
        public async Task Compressor7zipExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"7z testOption1 testOption2 Test1.zip Test2.txt"
            };

            int processCount = 0;
            this.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = true
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.Dependencies, this.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        private class TestCompressor7zipExecutor : Compression7zipExecutor
        {
            public TestCompressor7zipExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
