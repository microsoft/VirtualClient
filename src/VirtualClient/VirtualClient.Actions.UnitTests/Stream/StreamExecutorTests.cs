// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class StreamExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(StreamExecutorTests), "Examples", "Stream");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private ConcurrentBuffer defaultOutput = new ConcurrentBuffer();
        private IProcessProxy defaultMemoryProcess;

        public void SetupTest(PlatformID platformID, Architecture cpuArchiture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID, cpuArchiture);
            this.mockPackage = new DependencyPath("stream", this.mockFixture.PlatformSpecifics.GetPackagePath("stream"));
            this.mockFixture.SetupPackage(this.mockPackage);

            this.mockFixture.Parameters["PackageName"] = this.mockPackage.Name;
            this.mockFixture.Parameters["Toolset"] = "STREAM";
            this.mockFixture.Parameters["CompilerVersion"] = 10;
            this.mockFixture.Parameters["CompilerParameters"] = "-fopenmp -mcmodel=large -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000";
            this.mockFixture.Parameters["ThreadCount"] = 1;

            string exampleResults = MockFixture.ReadFile(StreamExecutorTests.ExamplesDirectory, "StreamExample.txt");

            this.defaultOutput.Clear();
            this.defaultOutput.Append(exampleResults);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, hyperThreadingEnabled: true));

            this.defaultMemoryProcess = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "exe",
                    Arguments = "args"
                },
                ExitCode = 0,
                OnStart = () => true,
                OnHasExited = () => true,
                StandardOutput = this.defaultOutput
            };
        }

        [Test]
        public async Task StreamExecutorDefaultScenarioRunsExpectedCommandsOnWindowsx64()
        {
            this.SetupTest(PlatformID.Win32NT, Architecture.X64);

            this.mockFixture.Parameters[nameof(StreamExecutor.ThreadCount)] = "2";

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            string packagePath = this.mockFixture.PlatformSpecifics.ToPlatformSpecificPath(this.mockPackage, PlatformID.Win32NT, Architecture.X64).Path;

            string executablePath = this.mockFixture.PlatformSpecifics.Combine(packagePath, "stream.exe");

            List<string> commandsExpected = new List<string>
            {
                $"{executablePath} -n 50 -s 320000000"
            };

            string windowsResults = MockFixture.ReadFile(StreamExecutorTests.ExamplesDirectory, "StreamExampleWindows.txt");

            StringBuilder builder = new StringBuilder();
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                builder.AppendLine(fullCommand);
                commandsExpected.Remove(fullCommand);

                if (fullCommand.Contains("stream.exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(windowsResults);
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            string here = builder.ToString();
            Assert.IsEmpty(commandsExpected, $"Remaining commands not matched. Count: {commandsExpected.Count}\nObserved:\n{here}");
        }

        [Test]
        public void StreamExecutorThrowsIfANonSupportedToolsetIsProvidedOnWindows()
        {
            this.SetupTest(PlatformID.Win32NT, Architecture.X64);
            this.mockFixture.Parameters["Toolset"] = "STREAMTriad";

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exp = Assert.ThrowsAsync<WorkloadException>(() => streamExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exp.Reason);
            }
        }

        [Test]
        public void StreamExecutorThrowsIfANonSupportedToolsetIsProvided()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["Toolset"] = "NotSupported";

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var exp = Assert.ThrowsAsync<WorkloadException>(() => streamExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exp.Reason);
            }
        }

        [Test]
        public void StreamExecutorTriadScenarioThrowsOnNonSupportedArchitecture()
        {
            this.SetupTest(PlatformID.Unix, Architecture.Arm64);

            this.mockFixture.Parameters["PackageName"] = "Stream";
            this.mockFixture.Parameters["Toolset"] = "STREAMTriad";
            this.mockFixture.Parameters[nameof(StreamExecutor.ThreadCount)] = "2";
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => streamExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.PlatformNotSupported);
            }
        }

        [Test]
        public void StreamExecutorMsftScenarioThrowsOnNonSupportedArchitecture()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            this.mockFixture.Parameters["PackageName"] = "Stream";
            this.mockFixture.Parameters["Toolset"] = "StreamMsft";
            this.mockFixture.Parameters[nameof(StreamExecutor.ThreadCount)] = "2";
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => streamExecutor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.PlatformNotSupported);
            }
        }

        [Test]
        public async Task StreamExecutorDefaultScenarioRunsExpectedCommandsOnx64()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string packagePath = this.mockPackage.Path;
            
            List<string> commandsExpected = new List<string>
            {
                $"sudo bash -c \"gcc {packagePath}/linux-x64/stream.c -o {packagePath}/linux-x64/streamworkload -fopenmp -mcmodel=large -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000\"",
                $"sudo bash -c \"export OMP_NUM_THREADS=1 && chmod +x {packagePath}/linux-x64/streamworkload && {packagePath}/linux-x64/streamworkload\""
            };

            StringBuilder builder = new StringBuilder();
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                builder.AppendLine(fullCommand);
                commandsExpected.Remove(fullCommand);

                if (fullCommand.Contains("streamworkload") && !fullCommand.Contains("gcc"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(this.defaultOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            string here = builder.ToString();
            Assert.IsEmpty(commandsExpected, $"Remaining commands not matched. Count: {commandsExpected.Count}\nObserved:\n{here}");
        }

        [Test]
        public async Task StreamExecutorDefaultScenarioRunsExpectedCommandsOnarm64()
        {
            this.SetupTest(PlatformID.Unix, Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string packagePath = this.mockPackage.Path;

            List<string> commandsExpected = new List<string>
            {
                $"sudo bash -c \"gcc {packagePath}/linux-arm64/stream.c -o {packagePath}/linux-arm64/streamworkload -fopenmp  -D_OPENMP -DNTIMES=5000 -DSTREAM_ARRAY_SIZE=100000000\"",
                $"sudo bash -c \"export OMP_NUM_THREADS=1 && chmod +x {packagePath}/linux-arm64/streamworkload && {packagePath}/linux-arm64/streamworkload\""
            };

            StringBuilder builder = new StringBuilder();
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                builder.AppendLine(fullCommand);
                commandsExpected.Remove(fullCommand);
                if (fullCommand.Contains("streamworkload") && !fullCommand.Contains("gcc"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(this.defaultOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            string here = builder.ToString();
            Assert.IsEmpty(commandsExpected);
        }

        [Test]
        public async Task StreamExecutorMsftScenarioRunsExpectedCommandsOnarm64()
        {
            this.SetupTest(PlatformID.Unix, Architecture.Arm64);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters["Toolset"] = "StreamMsft";
            string packagePath = this.mockPackage.Path;

            List<string> commandsExpected = new List<string>
            {
                $"sudo bash -c \"make\"",
                $"sudo bash -c \"{packagePath}/linux-arm64/perfrunner --threads 1 \""
            };

            StringBuilder builder = new StringBuilder();
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                builder.AppendLine(fullCommand);
                commandsExpected.Remove(fullCommand);
                if (fullCommand.Contains("perfrunner"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(this.defaultOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            string here = builder.ToString();
            Assert.IsEmpty(commandsExpected);
        }

        [Test]
        public async Task StreamExecutorTriadScenarioRunsExpectedCommandsWhenHyperThreadingIsEnabled()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.Parameters["Toolset"] = "STREAMTriad";
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string packagePath = this.mockPackage.Path;

            List<string> commandsExpected = new List<string>
            {
                $"sudo bash -c \"lscpu | grep 'Flags'\"",
                $"sudo bash -c \"export KMP_AFFINITY=granularity=fine,compact,1,0 && export OMP_NUM_THREADS=1 && export LD_LIBRARY_PATH={packagePath}/linux-x64/icclib && chmod +x {packagePath}/linux-x64/StreamTriad && {packagePath}/linux-x64/StreamTriad\""
            };

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                commandsExpected.Remove(fullCommand);
                if (fullCommand.Contains("StreamTriad") && !fullCommand.Contains("lscpu"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(this.defaultOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsEmpty(commandsExpected);
        }

        [Test]
        public async Task StreamExecutorTriadScenarioRunsExpectedCommandsWhenHyperThreadingIsNotEnabled()
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("Name", "Description", 1, 2, 1, 1, hyperThreadingEnabled: false));

            this.mockFixture.Parameters["Toolset"] = "STREAMTriad";
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            string packagePath = this.mockPackage.Path;
            List<string> commandsExpected = new List<string>
            {
                $"sudo bash -c \"lscpu | grep 'Flags'\"",
                $"sudo bash -c \"export KMP_AFFINITY=compact && export OMP_NUM_THREADS=1 && export LD_LIBRARY_PATH={packagePath}/linux-x64/icclib && chmod +x {packagePath}/linux-x64/StreamTriad && {packagePath}/linux-x64/StreamTriad\""
            };

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();
                commandsExpected.Remove(fullCommand);
                if (fullCommand.Contains("StreamTriad") && !fullCommand.Contains("lscpu"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(this.defaultOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsEmpty(commandsExpected);
        }

        [Test]
        [TestCase("StreamTriadAVX512", "Flags: fpu vme de pse avx avx2 avx512f")]
        [TestCase("StreamTriadAVX2", "Flags: fpu vme de pse avx avx2")]
        [TestCase("StreamTriadAVX", "Flags: fpu vme de pse avx")]
        [TestCase("StreamTriad ", "Flags: fpu vme de")]
        public async Task StreamExecutorTriadScenarioSelectsCorrectBinaryToExecuteDependingOnAVX(string expectedBinary, string flags)
        {
            this.SetupTest(PlatformID.Unix);

            this.mockFixture.Parameters["Toolset"] = "STREAMTriad";
            string flagsCmd = $"sudo bash -c \"lscpu | grep 'Flags'\"";
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            ConcurrentBuffer flagsOutput = new ConcurrentBuffer();
            flagsOutput.Append(flags);
            bool expectedBinaryExecuted = false;

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string fullCommand = process.FullCommand();

                if (fullCommand.Contains(expectedBinary, StringComparison.Ordinal))
                {
                    expectedBinaryExecuted = true;

                    var triadOutput =
                        "Function    Best Rate MB/s   Avg Rate MB/s   Min Rate MB/s" + System.Environment.NewLine +
                        "Triad       12345.67          12000.00        11000.00" + System.Environment.NewLine;

                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(triadOutput);
                    }
                }

                if (string.Equals(fullCommand, flagsCmd, StringComparison.Ordinal))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append(flagsOutput.ToString());
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await streamExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(expectedBinaryExecuted);
        }

        [Test]
        public async Task ExecuteCommandAsyncReturnsProcessOutput()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string cmd = process.FullCommand();
                if (cmd.Contains("echo hello"))
                {
                    if (process.StandardOutput != null)
                    {
                        process.StandardOutput.Clear();
                        process.StandardOutput.Append("hello\n");
                    }
                }
            };

            using (StreamExecutor streamExecutor = new StreamExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var method = typeof(StreamExecutor).GetMethod("ExecuteCommandAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(method, "Expected private method ExecuteCommandAsync to exist.");

                var telemetryContext = global::VirtualClient.Common.Telemetry.EventContext.Persisted();
                var task = (Task<string>)method.Invoke(streamExecutor, new object[] { "bash", "-c \"echo hello\"", telemetryContext, CancellationToken.None, null, null });
                string output = await task.ConfigureAwait(false);

                StringAssert.Contains("hello", output);
            }
        }
    }
}