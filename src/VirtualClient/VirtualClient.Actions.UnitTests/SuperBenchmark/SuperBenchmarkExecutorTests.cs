// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public void SuperBenchmarkStateIsSerializeable(Architecture architecture)
        {
            SetupDefaultMockBehavior(architecture);

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
        public async Task SuperBenchmarkExecutorClonesTheExpectedRepoContentsOnX64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.X64);

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
        public async Task SuperBenchmarkExecutorPullsTheExpectedDockerImageContentsOnArm64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SuperBenchmarkExecutor.Version), "1.2.3" },
                { nameof(SuperBenchmarkExecutor.ContainerVersion), "testContainer" },
                { nameof(SuperBenchmarkExecutor.ConfigurationFile), "Test.yaml" },
                { nameof(SuperBenchmarkExecutor.Username), "testuser" }
            };
            string expectedCommand = $"sudo docker pull {this.mockFixture.Parameters[nameof(SuperBenchmarkExecutor.ContainerVersion)]}";

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
        public async Task SuperBenchmarkExecutorUsesTheExpectedScriptFilesOnExecutionOnX64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.X64);

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
        public async Task SuperBenchmarkExecutorDeploySuperBenchContainerOnX64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.X64);

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
        public async Task SuperBenchmarkExecutorRunsDockerContainerInDetachedModeForSetupOnArm64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedPath = this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.PlatformSpecifics.PackagesDirectory, "superbenchmark");
            string expectedCommand = $"sudo docker run -itd --name=sb-dev --privileged --net=host --ipc=host --gpus=all -w /root -v {expectedPath}:/mnt testContainer";

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
        public async Task SuperBenchmarkExecutorRunsTheExpectedWorkloadCommandOnX64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.X64);

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
        public async Task SuperBenchmarkExecutorRunsTheExpectedWorkloadCommandOnArm64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = $"sudo docker exec sb-dev sb run --no-docker -l localhost -c /mnt/Test.yaml --output-dir outputs/";

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
        public async Task SuperBenchmarkExecutorExecutesTheCorrectCommandsWithInstallationOnX64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.X64);

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
        public async Task SuperBenchmarkExecutorExecutesTheCorrectCommandsWithInstallationOnArm64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedPath = this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.PlatformSpecifics.PackagesDirectory, "superbenchmark");
            List<string> expectedCommands = new List<string>
            {
                $"sudo chmod -R 2777 \"{this.mockFixture.PlatformSpecifics.CurrentDirectory}\"",
                $"sudo docker pull testContainer",
                $"sudo docker run -itd --name=sb-dev --privileged --net=host --ipc=host --gpus=all -w /root -v {expectedPath}:/mnt testContainer",
                $"sudo docker exec sb-dev sb run --no-docker -l localhost -c /mnt/Test.yaml --output-dir outputs/"
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

            Assert.IsTrue(processCount == 4);
        }

        [Test]
        public async Task SuperBenchmarkExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRunOnX64Architecture()
        {
            this.SetupDefaultMockBehavior(Architecture.X64);

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

        [Test]
        public async Task SuperBenchmarkExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRunOnArm64Architecture()
        {
            SetupDefaultMockBehavior(Architecture.Arm64);

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

        public void SetupDefaultMockBehavior(Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix, architecture);
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
