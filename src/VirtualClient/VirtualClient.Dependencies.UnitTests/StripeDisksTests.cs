// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class StripeDisksTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommand()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false&SizeGreaterThan:256GB";

            string expectedScriptPath = this.mockFixture.GetScriptPath("stripedisks", "striping.py");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo python3 {expectedScriptPath} --sizeGreaterThan 256 --mountDirectory {expectedMountDir}";
                if (process.FullCommand() == expectedCommand)
                {
                    confirmed = true;
                }
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommandWhenNoSizeGreaterThanFilterIsProvided()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            string expectedScriptPath = this.mockFixture.GetScriptPath("stripedisks", "striping.py");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo python3 {expectedScriptPath} --sizeGreaterThan 0 --mountDirectory {expectedMountDir}";
                if (process.FullCommand() == expectedCommand)
                {
                    confirmed = true;
                }
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task StripeDisksResolvesTheMountDirectoryForNonRootUser()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                if (process.FullCommand().Contains($"--mountDirectory {expectedMountDir}"))
                {
                    confirmed = true;
                }
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task StripeDisksResolvesTheMountDirectoryForRootUser()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.PlatformSpecifics.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, "root");

                bool confirmed = false;
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    if (process.FullCommand().Contains("--mountDirectory /mnt_raid0"))
                    {
                        confirmed = true;
                    }
                };

                await component.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task StripeDisksResolvesTheMountDirectoryWhenSudoUserIsSet()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                component.PlatformSpecifics.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, "user01");

                bool confirmed = false;
                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    if (process.FullCommand().Contains("--mountDirectory /home/user01/mnt_raid0"))
                    {
                        confirmed = true;
                    }
                };

                await component.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        [TestCase("mount_points")]
        [TestCase("/mount_points")]
        [TestCase("/mount_points/")]
        [TestCase("  /mount_points/  ")]
        public async Task StripeDisksResolvesTheMountDirectoryWhenMountLocationIsProvided(string mountLocation)
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";
            this.mockFixture.Parameters["MountLocation"] = mountLocation;

            string expectedMountDir = $"/{mountLocation.Trim().Trim('/')}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                if (process.FullCommand().Contains($"--mountDirectory {expectedMountDir}"))
                {
                    confirmed = true;
                }
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task StripeDisksResolvesTheMountDirectoryWithCustomPrefix()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";
            this.mockFixture.Parameters["MountPointPrefix"] = "mnt_test";

            string expectedMountDir = $"/home/{Environment.UserName}/mnt_test_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                if (process.FullCommand().Contains($"--mountDirectory {expectedMountDir}"))
                {
                    confirmed = true;
                }
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        public void StripeDisksThrowsWhenScriptIsNotFound()
        {
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.DependencyNotFound, exc.Reason);
            }
        }

        [Test]
        [TestCase("OSDisk:false&SizeGreaterThan:256GB", 256)]
        [TestCase("OSDisk:false&SizeGreaterThan:512GB", 512)]
        [TestCase("OSDisk:false&SizeGreaterThan:1024GB", 1024)]
        [TestCase("OSDisk:false", 0)]
        [TestCase("SizeGreaterThan:128GB&OSDisk:false", 128)]
        [TestCase("BiggestSize", 0)]
        public void ParseSizeGreaterThanReturnsTheExpectedValue(string diskFilter, int expectedSizeGreaterThan)
        {
            int result = StripeDisks.ParseSizeGreaterThan(diskFilter);
            Assert.AreEqual(expectedSizeGreaterThan, result);
        }
    }
}
