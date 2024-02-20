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
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SuperBenchmarkExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("SuperBenchmark", this.mockFixture.PlatformSpecifics.GetPackagePath("superbenchmark"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsRegex("superbenchmark")))
                .Returns(false);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SuperBenchmarkExecutor.Version), "0.0.1" },
                { nameof(SuperBenchmarkExecutor.ContainerVersion), "testContainer" },
                { nameof(SuperBenchmarkExecutor.ConfigurationFile), "Test.yaml" },
                { nameof(SuperBenchmarkExecutor.Username), "testuser" }
            };
        }

        [Test]
        public void SuperBenchmarkStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["SuperBenchmarkInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            SuperBenchmarkExecutor.SuperBenchmarkState result = deserializedState?.ToObject<SuperBenchmarkExecutor.SuperBenchmarkState>();
            Assert.AreEqual(true, result.SuperBenchmarkInitialized);
        }

        [Test]
        public async Task SuperBenchmarkExecutorClonesTheExpectedRepoContents()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SuperBenchmarkExecutor.Version), "1.2.3" },
                { nameof(SuperBenchmarkExecutor.ContainerVersion), "testContainer" },
                { nameof(SuperBenchmarkExecutor.ConfigurationFile), "Test.yaml" },
                { nameof(SuperBenchmarkExecutor.Username), "testuser" }
            };
            string expectedCommand = $"sudo git clone -b v1.2.3 https://github.com/microsoft/superbenchmark";

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

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SuperBenchmarkExecutorUsesTheExpectedScriptFilesOnExecution()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = $"sudo bash initialize.sh testuser";

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

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SuperBenchmarkExecutorDeploySuperBenchContainer()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = $"sb deploy --host-list localhost -i testContainer";

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

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SuperBenchmarkExecutorRunsTheExpectedWorkloadCommand()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = $"sb run --host-list localhost -c Test.yaml";

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

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SuperBenchmarkExecutorExecutesTheCorrectCommandsWithInstallation()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>
            {
                $"sudo chmod -R 2777 \"{this.mockFixture.PlatformSpecifics.CurrentDirectory}\"",
                $"sudo git clone -b v0.0.1 https://github.com/microsoft/superbenchmark",
                $"sudo bash initialize.sh testuser",
                $"sb deploy --host-list localhost -i testContainer",
                $"sb run --host-list localhost -c Test.yaml"
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SuperBenchmarkExecutor.SuperBenchmarkState()
            {
                SuperBenchmarkInitialized = false
            }));

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 5);
        }

        [Test]
        public async Task SuperBenchmarkExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>
            {
                $"sb run --host-list localhost -c Test.yaml"
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

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SuperBenchmarkExecutor.SuperBenchmarkState()
            {
                SuperBenchmarkInitialized = true
            }));

            using (TestSuperBenchmarkExecutor superBenchmarkExecutor = new TestSuperBenchmarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await superBenchmarkExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(processCount == 1);
        }

        private class TestSuperBenchmarkExecutor : SuperBenchmarkExecutor
        {
            public TestSuperBenchmarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
