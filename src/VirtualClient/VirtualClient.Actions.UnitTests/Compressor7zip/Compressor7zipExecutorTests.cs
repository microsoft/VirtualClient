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
    public class Compressor7zipExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockPackage = new DependencyPath("7zip", this.mockFixture.PlatformSpecifics.GetPackagePath("7zip"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario"}
            };

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "Compressor7zip", "Compressor7zipResultsExample.txt");
            string results = File.ReadAllText(resultsPath);
            this.defaultOutput.Clear();
            this.defaultOutput.Append(results);
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
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario" }
            };
            string expectedCommand = $"wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip";

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
                    StandardOutput = this.defaultOutput
                };
            };

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Compressor7zipExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(Compression7zipExecutor.Options), "testOption1 testOption2" },
                { nameof(Compression7zipExecutor.InputFilesOrDirs), "" },
                { nameof(Compression7zipExecutor.PackageName), "7zip" },
                { nameof(Compression7zipExecutor.Scenario), "mockScenario" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"7z testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}";

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
                    StandardOutput = this.defaultOutput
                };
            };

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await Compressor7zipExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task Compressor7zipExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
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
                $"7z testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia/*")}"
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = false
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = false
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
                    StandardOutput = this.defaultOutput
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new Compression7zipExecutor.Compression7zipState()
            {
                Compressor7zipStateInitialized = true
            }));

            using (TestCompressor7zipExecutor Compressor7zipExecutor = new TestCompressor7zipExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
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
