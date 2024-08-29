// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LzbenchExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("lzbench", this.fixture.PlatformSpecifics.GetPackagePath("lzbench"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };
        }

        [Test]
        public void LzbenchStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["LzbenchInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            LzbenchExecutor.LzbenchState result = deserializedState?.ToObject<LzbenchExecutor.LzbenchState>();
            Assert.AreEqual(true, result.LzbenchInitialized);
        }

        [Test]
        public async Task LzbenchExecutorClonesTheExpectedRepoContents()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };
            string expectedCommand = $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git";

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
                    OnHasExited = () => true
                };
            };

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorGetsDefaultFileIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
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
                    OnHasExited = () => true
                };
            };

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"sudo bash lzbenchexecutor.sh \"testOption1 testOption2 {this.fixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\"";

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
                    OnHasExited = () => true
                };
            };

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };

            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git",
                $"sudo make",
                $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"sudo unzip silesia.zip -d silesia",
                $"sudo bash lzbenchexecutor.sh \"testOption1 testOption2 {this.fixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\""
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
                    OnHasExited = () => true
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = false
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 5);
        }

        [Test]
        public async Task LzbenchExecutorExecutesTheCorrectCommandsWithInstallationIfInputFilesOrDirsAreProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git",
                $"sudo make",
                $"sudo bash lzbenchexecutor.sh \"testOption1 testOption2 Test1.zip Test2.txt\"",
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
                    OnHasExited = () => true
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = false
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 3);
        }

        [Test]
        public async Task LzbenchExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo bash lzbenchexecutor.sh \"testOption1 testOption2 Test1.zip Test2.txt\""
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
                    OnHasExited = () => true
                };
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = true
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        private class TestLzbenchExecutor : LzbenchExecutor
        {
            public TestLzbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
