// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CoreMarkProExecutorTests : MockFixture
    {
        private ConcurrentBuffer coremarkProOutput = new ConcurrentBuffer();

        public void SetupTest(PlatformID platform)
        {
            this.Setup(platform);
            this.Parameters["PackageName"] = "coremarkpro";

            string results = MockFixture.ReadFile(MockFixture.ExamplesDirectory, "CoreMark", "CoreMarkProExample1.txt");
            this.coremarkProOutput.Clear();
            this.coremarkProOutput.Append(results);

            this.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, false));

            DependencyPath mockPackage = new DependencyPath("cygwin", this.PlatformSpecifics.GetPackagePath("cygwin"));
            this.SetupPackage(mockPackage);
        }

        [Test]
        public async Task CoreMarkProExecutorExecutesAsExpectedInLinux()
        {
            this.SetupTest(PlatformID.Unix);
            this.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual(args, $"TARGET=linux64 XCMD='-c9' certify-all");
                this.Process.StandardOutput = this.coremarkProOutput;
                return this.Process;
            };

            using (CoreMarkProExecutor executor = new CoreMarkProExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkProExecutorExecutesAsExpectedInWindows()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(@$"{this.PlatformSpecifics.GetPackagePath("cygwin")}\bin\bash", cmd);
                Assert.AreEqual(args, $"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremarkpro; make TARGET=linux64 XCMD='-c9' certify-all'");
                this.Process.StandardOutput = this.coremarkProOutput;
                return this.Process;
            };

            using (CoreMarkProExecutor executor = new CoreMarkProExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
