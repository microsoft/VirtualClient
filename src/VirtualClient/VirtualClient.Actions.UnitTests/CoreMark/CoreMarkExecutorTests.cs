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
    using VirtualClient.Actions.Properties;

    [TestFixture]
    [Category("Unit")]
    public class CoreMarkExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        public async Task CoreMarkExecutorExecutesTheExpectedCommandInLinux()
        {
            this.Setup(PlatformID.Unix);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual($"XCFLAGS=\"-DMULTITHREAD=9 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread", args);
                return this.mockFixture.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkExecutorExecutesTheExpectedCommandInWindows()
        {
            this.Setup(PlatformID.Win32NT);
            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual(@$"{this.mockFixture.PlatformSpecifics.GetPackagePath("cygwin")}\bin\bash", cmd);
                Assert.AreEqual($"--login -c 'cd /cygdrive/C/users/any/tools/VirtualClient/packages/coremark; make XCFLAGS=\"-DMULTITHREAD=9 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread'", args);
                return this.mockFixture.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        [Test]
        public async Task CoreMarkExecutorExcutesAllowsThreadOverWrite()
        {
            this.Setup(PlatformID.Unix);
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CoreMarkExecutor.ThreadCount), 789 }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("make", cmd);
                Assert.AreEqual($"XCFLAGS=\"-DMULTITHREAD=789 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread", args);
                return this.mockFixture.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void Setup(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
            this.mockFixture.Parameters["PackageName"] = "coremark";

            string results = null;

            // Setup the mock logic to return valid CoreMark results.
            this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".log"))))
                .Returns(true);

            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((file, token) =>
                {
                    if (file.EndsWith("run1.log") || file.EndsWith("run2.log"))
                    {
                        results = File.ReadAllText(this.mockFixture.Combine(
                            MockFixture.TestAssemblyDirectory,
                            "Examples",
                            "CoreMark",
                            "CoreMarkExampleSingleThread.txt"));
                    }
                })
                .ReturnsAsync(() => results);

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 9, 11, 13, false));

            DependencyPath mockPackage = new DependencyPath("cygwin", this.mockFixture.PlatformSpecifics.GetPackagePath("cygwin"));
            this.mockFixture.PackageManager.OnGetPackage("cygwin").ReturnsAsync(mockPackage);
        }
    }
}
