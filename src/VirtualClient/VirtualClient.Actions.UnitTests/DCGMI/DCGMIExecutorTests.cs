// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using System.Runtime.InteropServices;
    using System.Reflection;

    [TestFixture]
    [Category("Unit")]
    public class DCGMIExecutorTests
    {
        private MockFixture mockFixture;
        private State mockState;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        [TestCase("Diagnostics")]
        [TestCase("Discovery")]
        [TestCase("FieldGroup")]
        [TestCase("Group")]
        [TestCase("Health")]
        [TestCase("Modules")]
        [TestCase("CUDATestGenerator")]
        public async Task TestDCGMIExecutesExpectedCommandsOnUbuntu(string subsystem)
        {
            int diagnosticsCommandsexecuted;
            int discoveryCommandsexecuted;
            int fieldGroupCommandsexecuted;
            int groupCommandsexecuted;
            int healthCommandsexecuted;
            int modulesCommandsexecuted;
            int cudaTestGeneratorCommandsexecuted;

            this.SetupDefaultMockBehavior(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters.Add("Subsystem", subsystem);
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationtoken = cancellationTokenSource.Token;
            if (subsystem == "Diagnostics")
            {
                diagnosticsCommandsexecuted = await this.ExecuteDiagnosticsSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(3, diagnosticsCommandsexecuted);
            }
            else if (subsystem == "Discovery")
            {
                discoveryCommandsexecuted = await this.ExecuteDiscoverySubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(1, discoveryCommandsexecuted);
            }
            else if (subsystem == "FieldGroup")
            {
                fieldGroupCommandsexecuted = await this.ExecuteFieldGroupSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(1, fieldGroupCommandsexecuted);
            }
            else if (subsystem == "Group")
            {
                groupCommandsexecuted = await this.ExecuteGroupSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(1, groupCommandsexecuted);
            }
            else if (subsystem == "Health")
            {
                healthCommandsexecuted = await this.ExecuteHealthSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(2, healthCommandsexecuted);
            }
            else if (subsystem == "Modules")
            {
                modulesCommandsexecuted = await this.ExecuteModulesSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(1, modulesCommandsexecuted);
            }
            else if (subsystem == "CUDATestGenerator")
            {
                cudaTestGeneratorCommandsexecuted = await this.ExecuteCUDATestGeneratorSubsystemCommandsAsync(cancellationtoken, cancellationTokenSource);
                Assert.AreEqual(2, cudaTestGeneratorCommandsexecuted);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Level", "1" },
                { "Username", "anyuser" },
                { "FieldIDProftester", "1004" },
                {"ListOfFieldIDsDmon", "1004,1005,1001"}
            };

            this.mockState = new State();
            this.mockFixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            this.mockFixture.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        private async Task<int> ExecuteDiagnosticsSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedDiagnosticsCommands = new List<string>()
                {
                    "sudo nvidia-smi -pm 1",
                    "sudo nvidia-smi -e 1",
                    $"sudo dcgmi diag -r {this.mockFixture.Parameters["Level"]} -j"
                };

            int diagnosticscommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedDiagnosticsCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    diagnosticscommandExecuted++;
                    if (arguments == $"dcgmi diag -r {this.mockFixture.Parameters["Level"]} -j")
                    {
                        cancellationTokenSource.Cancel();
                        return process;
                    }
                    else if (arguments == $"nvidia-smi -e 1")
                    {
                        this.mockFixture.StateManager.OnGetState(nameof(DCGMIExecutor)).ReturnsAsync(JObject.FromObject(this.mockState));
                    }
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return diagnosticscommandExecuted;
        }

        private async Task<int> ExecuteDiscoverySubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedDiscoveryCommands = new List<string>()
                {
                    "sudo dcgmi discovery -l"
                };

            int DiscoverycommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedDiscoveryCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    DiscoverycommandExecuted++;
                }
                if (arguments == $"dcgmi discovery -l")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return DiscoverycommandExecuted;
        }

        private async Task<int> ExecuteFieldGroupSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedFieldGroupCommands = new List<string>()
                {
                    "sudo dcgmi fieldgroup -l"
                };

            int FieldGroupcommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedFieldGroupCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    FieldGroupcommandExecuted++;   
                }
                if (arguments == $"dcgmi fieldgroup -l")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return FieldGroupcommandExecuted;
        }

        private async Task<int> ExecuteGroupSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedGroupCommands = new List<string>()
                {
                    "sudo dcgmi group -l"
                };

            int GroupcommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedGroupCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    GroupcommandExecuted++;
                }
                if (arguments == $"dcgmi group -l")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return GroupcommandExecuted;
        }

        private async Task<int> ExecuteHealthSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedHealthCommands = new List<string>()
                {
                "sudo dcgmi health -s mpi",
                "sudo dcgmi health -c -j"
                };

            int HealthcommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedHealthCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    HealthcommandExecuted++;                  
                }
                if (arguments == $"dcgmi health -c -j")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return HealthcommandExecuted;
        }
        private async Task<int> ExecuteModulesSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedModulesCommands = new List<string>()
                {
                    "sudo dcgmi modules -l"
                };

            int ModulescommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedModulesCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    ModulescommandExecuted++;                  
                }
                if (arguments == $"dcgmi modules -l")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return ModulescommandExecuted;
        }
        private async Task<int> ExecuteCUDATestGeneratorSubsystemCommandsAsync(CancellationToken cancellationToken, CancellationTokenSource cancellationTokenSource)
        {
            List<string> expectedCUDATestGeneratorCommands = new List<string>()
                {
                    $"sudo /usr/bin/dcgmproftester11 --no-dcgm-validation -t {this.mockFixture.Parameters["FieldIDProftester"]} -d 10",
                    $"sudo dcgmi dmon -e {this.mockFixture.Parameters["ListOfFieldIDsDmon"]} -c 15"
                };

            int CUDATestGeneratorcommandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                IProcessProxy process = new InMemoryProcess
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

                if (expectedCUDATestGeneratorCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    CUDATestGeneratorcommandExecuted++;
                }
                if (arguments == $"dcgmi dmon -e {this.mockFixture.Parameters["ListOfFieldIDsDmon"]} -c 15")
                {
                    cancellationTokenSource.Cancel();
                    return process;
                }
                return process;
            };

            using (TestDCGMIExecutor testDCGMIExecutor = new TestDCGMIExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testDCGMIExecutor.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            return CUDATestGeneratorcommandExecuted;
        }

        private class TestDCGMIExecutor : DCGMIExecutor
        {
            public TestDCGMIExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context,CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
