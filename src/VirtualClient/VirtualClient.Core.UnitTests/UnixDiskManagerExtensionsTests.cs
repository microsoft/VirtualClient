// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Contracts;
    using VirtualClient.Properties;

    [TestFixture]
    [Category("Unit")]
    public class UnixDiskManagerExtensionsTests
    {
        private MockFixture mockFixture;
        private UnixDiskManager diskManager;
        private InMemoryProcessManager processManager;
        private InMemoryProcess testProcess;
        private InMemoryStream standardInput;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();

            this.processManager = new InMemoryProcessManager(PlatformID.Unix)
            {
                // Return our test process on creation.
                OnCreateProcess = (command, args, workingDir) =>
                {
                    this.testProcess.StartInfo.FileName = command;
                    this.testProcess.StartInfo.Arguments = args;
                    return this.testProcess;
                }
            };

            this.standardInput = new InMemoryStream();
            this.diskManager = new UnixDiskManager(this.processManager)
            {
                RetryPolicy = Policy.NoOpAsync(),
                WaitTime = TimeSpan.Zero // Wait time in-between individual process calls.
            };

            this.testProcess = new InMemoryProcess(this.standardInput);
        }

        [Test]
        public async Task UnixDiskManagerCreateMountPointsForDisksWithNoAccessPaths()
        {
            // Setup the process execution to start, mimic an exit and to have the lshw command
            // results in the standard output.
            this.testProcess.OnHasExited = () => true;
            this.testProcess.OnStart = () => true;
            this.testProcess.StandardOutput.Append(Resources.lshw_disk_storage_results);

            IEnumerable<Disk> disks = await this.diskManager.GetDisksAsync(CancellationToken.None).ConfigureAwait(false);

            bool mountPointsCreated = await this.diskManager.CreateMountPointsAsync(disks, this.mockFixture.SystemManagement.Object, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(mountPointsCreated);
            
        }

        private class TestUnixDiskManager : UnixDiskManager
        {
            public TestUnixDiskManager(ProcessManager processManager)
                : base(processManager)
            {
                this.WaitTime = TimeSpan.Zero;
            }
        }
    }
}
