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
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("lzbench", this.mockFixture.PlatformSpecifics.GetPackagePath("lzbench"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MockFixture.ReadFile(MockFixture.ExamplesDirectory, "Lzbench", "LzbenchResultsExample.csv"));
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
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
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "Test1.zip Test2.txt" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };
            string expectedCommand = $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git";

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

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorGetsDefaultFileIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
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
                    OnHasExited = () => true
                };
            };

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };
            string mockPackagePath = this.mockPackage.Path;

            string expectedCommand = $"bash \"{this.mockFixture.PlatformSpecifics.GetScriptPath("lzbench", "lzbenchexecutor.sh")}\" \"testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\" \"{this.mockFixture.PlatformSpecifics.GetTempPath("lzbench", "results-summary.csv")}\"";

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

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task LzbenchExecutorExecutesTheCorrectCommandsWithInstallationIfInputFileOrDirNotProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(LzbenchExecutor.Version), "1.8.1" },
                { nameof(LzbenchExecutor.Options), "testOption1 testOption2" },
                { nameof(LzbenchExecutor.InputFilesOrDirs), "" },
                { nameof(LzbenchExecutor.PackageName), "lzbench" }
            };

            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo rm -rf \"{mockPackagePath}\"",
                $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git",
                $"sudo make",
                $"sudo wget https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                $"sudo unzip silesia.zip -d silesia",
                $"bash \"{this.mockFixture.PlatformSpecifics.GetScriptPath("lzbench", "lzbenchexecutor.sh")}\" \"testOption1 testOption2 {this.mockFixture.PlatformSpecifics.Combine(mockPackagePath, "silesia")}\" \"{this.mockFixture.PlatformSpecifics.GetTempPath("lzbench", "results-summary.csv")}\""
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
                    OnHasExited = () => true
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = false
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 6);
        }

        [Test]
        public async Task LzbenchExecutorExecutesTheCorrectCommandsWithInstallationIfInputFilesOrDirsAreProvided()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"sudo rm -rf \"{mockPackagePath}\"",
                $"sudo git clone -b v1.8.1 https://github.com/inikep/lzbench.git",
                $"sudo make",
                $"bash \"{this.mockFixture.PlatformSpecifics.GetScriptPath("lzbench", "lzbenchexecutor.sh")}\" \"testOption1 testOption2 Test1.zip Test2.txt\" \"{this.mockFixture.PlatformSpecifics.GetTempPath("lzbench", "results-summary.csv")}\"",
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
                    OnHasExited = () => true
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = false
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 4);
        }

        [Test]
        public async Task LzbenchExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string mockPackagePath = this.mockPackage.Path;
            List<string> expectedCommands = new List<string>
            {
                $"bash \"{this.mockFixture.PlatformSpecifics.GetScriptPath("lzbench", "lzbenchexecutor.sh")}\" \"testOption1 testOption2 Test1.zip Test2.txt\" \"{this.mockFixture.PlatformSpecifics.GetTempPath("lzbench", "results-summary.csv")}\""
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
                    OnHasExited = () => true
                };
            };

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = true
            }));

            using (TestLzbenchExecutor LzbenchExecutor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await LzbenchExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        [Test]
        public async Task LzbenchExecutorCreatesAndCleansUpResultsUsingTheCurrentUser()
        {
            string resultsDirectory = this.mockFixture.PlatformSpecifics.GetTempPath("lzbench");
            string resultsFile = this.mockFixture.PlatformSpecifics.Combine(resultsDirectory, "results-summary.csv");
            this.mockFixture.Directory.Setup(directory => directory.Exists(resultsDirectory)).Returns(false);
            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
            {
                LzbenchInitialized = true
            }));

            using (TestLzbenchExecutor executor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            this.mockFixture.Directory.Verify(directory => directory.CreateDirectory(resultsDirectory), Times.Once);
            this.mockFixture.File.Verify(file => file.Delete(resultsFile), Times.Once);
        }

        [Test]
        public async Task LzbenchExecutorReportsCancellationAsAWorkloadFailure()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new LzbenchExecutor.LzbenchState()
                {
                    LzbenchInitialized = true
                }));
                this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
                {
                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        ExitCode = 0,
                        OnStart = () =>
                        {
                            cancellationSource.Cancel();
                            return true;
                        },
                        OnHasExited = () => true
                    };
                };

                using (TestLzbenchExecutor executor = new TestLzbenchExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
                {
                    await executor.ExecuteAsync(cancellationSource.Token).ConfigureAwait(false);
                }

                IEnumerable<string> statusMetrics = this.mockFixture.Logger
                    .Where(entry => entry.Item3 is EventContext context
                        && context.Properties.ContainsKey("metricName"))
                    .Select(entry => ((EventContext)entry.Item3).Properties["metricName"].ToString());

                CollectionAssert.Contains(statusMetrics, "Failed");
                CollectionAssert.DoesNotContain(statusMetrics, "Succeeded");
            }
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
