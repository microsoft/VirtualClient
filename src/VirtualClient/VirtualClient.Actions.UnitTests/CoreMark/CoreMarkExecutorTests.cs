// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CoreMarkExecutorTests : MockFixture
    {
        public void SetupTest(PlatformID platform)
        {
            this.Setup(platform);
            this.Parameters["PackageName"] = "coremark";

            string results = null;

            // Setup the mock logic to return valid CoreMark results.
            this.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".log"))))
                .Returns(true);

            this.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((file, token) =>
                {
                    if (file.EndsWith("run1.log") || file.EndsWith("run2.log"))
                    {
                        results = MockFixture.ReadFile(
                            MockFixture.ExamplesDirectory,
                            "CoreMark",
                            "CoreMarkExampleSingleThread.txt");
                    }
                })
                .ReturnsAsync(() => results);

            this.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, false));

            DependencyPath mockPackage = new DependencyPath("cygwin", this.PlatformSpecifics.GetPackagePath("cygwin"));
            this.PackageManager.OnGetPackage("cygwin").ReturnsAsync(mockPackage);
        }

        [Test]
        public async Task CoreMarkExecutorExecutesTheExpectedCommandInLinux()
        {
            this.SetupTest(PlatformID.Unix);
            this.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual($"XCFLAGS=\"-DMULTITHREAD=9 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread", args);
                return this.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkExecutorExecutesTheExpectedCommandInWindows()
        {
            this.SetupTest(PlatformID.Win32NT);
            this.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(@$"{this.PlatformSpecifics.GetPackagePath("cygwin")}\bin\bash", cmd);
                Assert.AreEqual($"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremark; make XCFLAGS=\"-DMULTITHREAD=9 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread'", args);
                return this.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkExecutorExcutesAllowsThreadOverWrite()
        {
            this.SetupTest(PlatformID.Unix);
            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CoreMarkExecutor.ThreadCount), 789 }
            };

            this.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual($"XCFLAGS=\"-DMULTITHREAD=789 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread", args);
                return this.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
