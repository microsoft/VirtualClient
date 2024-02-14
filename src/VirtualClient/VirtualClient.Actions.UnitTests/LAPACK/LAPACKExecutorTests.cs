// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using System.Threading;
    using System.Reflection;
    using System.IO;
    using Moq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class LAPACKExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPath;
        private DependencyPath currentDirectoryPath;

        private string resultsPath;
        private string rawString;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.mockPath = this.fixture.Create<DependencyPath>();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/LapackTestScript.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/LapackTestScript.sh")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64\\LapackTestScript.sh")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64\\LapackTestScript.sh")]
        public async Task LAPACKExecutorInitializesItsDependenciesAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                {
                    return this.fixture.Process;
                };

                await executor.InitializeAsync(EventContext.None, CancellationToken.None)
                    .ConfigureAwait(false);

                string expectedScriptFilePath = this.fixture.PlatformSpecifics.Combine(
                    this.mockPath.Path, binaryPath);

                Assert.AreEqual(expectedScriptFilePath, executor.ScriptFilePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64/LapackTestScript.sh")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64/LapackTestScript.sh")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        public async Task LAPACKExecutorExecutesWorkloadAsExpected(PlatformID platform, Architecture architecture, string binaryPath)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.fixture))
            {
                string expectedFilePath = this.fixture.PlatformSpecifics.Combine(this.mockPath.Path, binaryPath);
                int executed = 0;

                if(platform == PlatformID.Unix)
                {
                    this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == "make")
                        {
                            executed++;
                        }
                        else if (arguments == "bash " + expectedFilePath)
                        {
                            executed++;
                            executor.ResultsFilePath = resultsPath;
                        }
                        return this.fixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                        .ConfigureAwait(false);

                }
                else if(platform == PlatformID.Win32NT)
                {
                    string expectedCommand = this.fixture.PlatformSpecifics.Combine("C:", "cygwin64", "bin", "bash");
                    string packageDir = Regex.Replace(expectedFilePath, @"\\", "/");
                    packageDir = Regex.Replace(packageDir, @":", string.Empty);

                    string expectedmakeCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./cmakescript.sh'";
                    string executeScriptCommandArguments = @$"--login -c 'cd /cygdrive/{packageDir}; ./LapackTestScript.sh'";

                    this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
                    {
                        if (arguments == expectedmakeCommandArguments)
                        {
                            executed++;
                        }
                        else if (arguments == executeScriptCommandArguments)
                        {
                            executed++;
                            executor.ResultsFilePath = resultsPath;
                        }
                        return this.fixture.Process;
                    };

                    await executor.ExecuteAsync(EventContext.None, CancellationToken.None)
                        .ConfigureAwait(false);

                }

                Assert.AreEqual(2, executed);
            }
        }

        [Test]
        public void LAPACKExecutorThrowsWhenTheResultsFileIsNotGenerated()
        {
            this.SetupDefaultMockBehavior();
            using (TestLAPACKExecutor executor = new TestLAPACKExecutor(this.fixture))
            {
                this.fixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    this.fixture.FileSystem.Setup(fe => fe.File.Exists(executor.ResultsFilePath)).Returns(false);
                    return this.fixture.Process;
                };

                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => executor.ExecuteAsync(EventContext.None, CancellationToken.None));

                Assert.AreEqual(ErrorReason.WorkloadFailed, exception.Reason);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.currentDirectoryPath = new DependencyPath("LAPACK", currentDirectory);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.FileSystem.Setup(fe => fe.File.Exists(null)).Returns(false);

            resultsPath = this.fixture.PlatformSpecifics.Combine(this.currentDirectoryPath.Path, @"Examples\LAPACK\LAPACKResultsExample.txt");
            this.rawString = File.ReadAllText(resultsPath);

            this.fixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.rawString);

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPath);
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, directory) => this.fixture.Process;

            this.fixture.Parameters["PackageName"] = "lapack";
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
