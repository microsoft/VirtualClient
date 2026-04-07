// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LAPACKExecutorTests
    {
        private static readonly string ExamplesDirectory = MockFixture.GetDirectory(typeof(LAPACKExecutorTests), "Examples", "LAPACK");

        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private DependencyPath mockCygwinPackage;
        private string mockResultsPath;
        private string exampleResults;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockPackage = new DependencyPath("lapack", this.mockFixture.GetPackagePath("lapack"));
            this.mockCygwinPackage = new DependencyPath("cygwin", this.mockFixture.GetPackagePath("cygwin"));

            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupPackage(this.mockPackage);
            this.mockFixture.SetupPackage(this.mockCygwinPackage);

            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            this.mockResultsPath = this.mockFixture.Combine(LAPACKExecutorTests.ExamplesDirectory, "LAPACKResultsExample.txt");
            this.exampleResults = File.ReadAllText(this.mockResultsPath);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.exampleResults);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.mockFixture.Process;
            this.mockFixture.Parameters["PackageName"] = "lapack";
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/LapackTestScript.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/LapackTestScript.sh")]
        public async Task LAPACKExecutorInitializesItsDependenciesAsExpected_Linux(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                string expectedScriptFilePath = this.mockFixture.Combine(this.mockPackage.Path, binaryPath);
                Assert.AreEqual(expectedScriptFilePath, executor.ScriptFilePath);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64\\LapackTestScript.sh")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64\\LapackTestScript.sh")]
        public async Task LAPACKExecutorInitializesItsDependenciesAsExpected_Windows(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.mockFixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                string expectedScriptFilePath = this.mockFixture.Combine(this.mockPackage.Path, binaryPath);
                Assert.AreEqual(expectedScriptFilePath, executor.ScriptFilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/LapackTestScript.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/LapackTestScript.sh")]
        public async Task LAPACKExecutorExecutesWorkloadAsExpected_Linux(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.mockFixture))
            {
                string expectedFilePath = this.mockFixture.Combine(this.mockPackage.Path, binaryPath);
                int executed = 0;

                if(platform == PlatformID.Unix)
                {
                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == "make")
                        {
                            executed++;
                        }
                        else if (arguments == "bash " + expectedFilePath)
                        {
                            executed++;
                            executor.ResultsFilePath = mockResultsPath;
                        }

                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                }
                else if(platform == PlatformID.Win32NT)
                {
                    string expectedCommand = this.mockFixture.Combine("C:", "cygwin64", "bin", "bash");
                    string packageDir = Regex.Replace(expectedFilePath, @"\\", "/");
                    packageDir = Regex.Replace(packageDir, @":", string.Empty);

                    string expectedmakeCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./cmakescript.sh'";
                    string executeScriptCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./LapackTestScript.sh'";

                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedmakeCommandArguments)
                        {
                            executed++;
                        }
                        else if (arguments == executeScriptCommandArguments)
                        {
                            executed++;
                            executor.ResultsFilePath = mockResultsPath;
                        }

                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                }

                Assert.AreEqual(2, executed);
            }
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task LAPACKExecutorExecutesWorkloadAsExpected_Windows(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupTest(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.mockFixture))
            {
                string expectedFilePath = this.mockFixture.Combine(this.mockPackage.Path, binaryPath);
                int executed = 0;

                if(platform == PlatformID.Unix)
                {
                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == "make")
                        {
                            executed++;
                        }
                        else if (arguments == "bash " + expectedFilePath)
                        {
                            executed++;
                            executor.ResultsFilePath = mockResultsPath;
                        }
                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                }
                else if(platform == PlatformID.Win32NT)
                {
                    string expectedCommand = this.mockFixture.Combine("C:", "cygwin64", "bin", "bash");
                    string packageDir = Regex.Replace(expectedFilePath, @"\\", "/");
                    packageDir = Regex.Replace(packageDir, @":", string.Empty);

                    string expectedmakeCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./cmakescript.sh'";
                    string executeScriptCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./LapackTestScript.sh'";

                    this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedmakeCommandArguments)
                        {
                            executed++;
                        }
                        else if (arguments == executeScriptCommandArguments)
                        {
                            executed++;
                            executor.ResultsFilePath = mockResultsPath;
                        }

                        return this.mockFixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None);

                }

                Assert.AreEqual(2, executed);
            }
        }

        [Test]
        public void LAPACKExecutorThrowsWhenTheResultsFileIsNotGenerated()
        {
            this.SetupTest();
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.mockFixture))
            {
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.mockFixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsFilePath)).Returns(false);
                    return this.mockFixture.Process;
                };

                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);
            }
        }

        private class TestLAPACKExecutor : LAPACKExecutor
        {
            public TestLAPACKExecutor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public TestLAPACKExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                this.InitializeAsync(context, cancellationToken).GetAwaiter().GetResult();
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
