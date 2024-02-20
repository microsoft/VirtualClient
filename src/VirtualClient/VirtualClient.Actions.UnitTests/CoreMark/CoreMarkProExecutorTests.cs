// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using VirtualClient.Common;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CoreMarkProExecutorTests
    {
        private MockFixture mockFixture;
        private ConcurrentBuffer coremarkProOutput = new ConcurrentBuffer();

        [Test]
        public async Task CoreMarkProExecutorExcutesAsExpectedInLinux()
        {
            this.SetupDefaults(PlatformID.Unix);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual(args, $"TARGET=linux64 XCMD='-c9' certify-all");
                this.mockFixture.Process.StandardOutput = this.coremarkProOutput;
                return this.mockFixture.Process;
            };

            using (CoreMarkProExecutor executor = new CoreMarkProExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkProExecutorExcutesAsExpectedInWindows()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(@$"{this.mockFixture.PlatformSpecifics.GetPackagePath("cygwin")}\bin\bash", cmd);
                Assert.AreEqual(args, $"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremarkpro; make TARGET=linux64 XCMD='-c9' certify-all'");
                this.mockFixture.Process.StandardOutput = this.coremarkProOutput;
                return this.mockFixture.Process;
            };

            using (CoreMarkProExecutor executor = new CoreMarkProExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters["PackageName"] = "coremarkpro";

            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string resultsPath = Path.Combine(currentDirectory, "Examples", "CoreMark", "CoreMarkProExample1.txt");
            string results = File.ReadAllText(resultsPath);
            this.coremarkProOutput.Clear();
            this.coremarkProOutput.Append(results);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, false));

            DependencyPath mockPackage = new DependencyPath("cygwin", this.mockFixture.PlatformSpecifics.GetPackagePath("cygwin"));
            this.mockFixture.PackageManager.OnGetPackage("cygwin").ReturnsAsync(mockPackage);
        }
    }
}
