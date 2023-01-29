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
    public class CoreMarkExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.Parameters["PackageName"] = "CoreMark";
        }

        [Test]
        public void CoreMarkExecutorThrowsOnNonSupportedPlatform()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.Parameters["PackageName"] = "CoreMark";

            using (var executor = new CoreMarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                var workloadException = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.IsTrue(workloadException.Reason == ErrorReason.PlatformNotSupported);
            }
        }

        [Test]
        public async Task CoreMarkExecutorExcutesAsExpected()
        {
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);

            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("sudo", cmd);
                Assert.AreEqual(args, $"make XCFLAGS=\"-DMULTITHREAD=71 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread");
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
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CoreMarkExecutor.ThreadCount), 789 }
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (cmd, args, wd) =>
            {
                Assert.AreEqual("sudo", cmd);
                Assert.AreEqual(args, $"make XCFLAGS=\"-DMULTITHREAD=789 -DUSE_PTHREAD\" REBUILD=1 LFLAGS_END=-pthread");
                return this.mockFixture.Process;
            };

            using (CoreMarkExecutor executor = new CoreMarkExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
