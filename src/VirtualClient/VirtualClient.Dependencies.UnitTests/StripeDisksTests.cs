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
        private const string PackageName = "system_config";
        private MockFixture mockFixture;
        private DependencyPath systemConfigPackage;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Parameters["PackageName"] = PackageName;
            this.SetupSystemConfigPackage();
        }

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommand()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false&SizeGreaterThan:256GB";

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.sh");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo bash {expectedScriptPath} --sizeGreaterThan 256 --mountDirectory {expectedMountDir} --diskCount 0";
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

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.sh");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo bash {expectedScriptPath} --sizeGreaterThan 0 --mountDirectory {expectedMountDir} --diskCount 0";
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

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommandOnWindows()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Parameters["PackageName"] = PackageName;
            this.SetupSystemConfigPackage();
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false&SizeGreaterThan:256GB";

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.cmd");

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"cmd /c {expectedScriptPath} --sizeGreaterThan 256 --diskCount 0";
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
        public async Task StripeDisksExecutesTheExpectedCommandOnWindowsWithCustomParameters()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Parameters["PackageName"] = PackageName;
            this.SetupSystemConfigPackage();
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false&SizeGreaterThan:512GB";
            this.mockFixture.Parameters["DriveLetter"] = "D";
            this.mockFixture.Parameters["FsType"] = "ReFS";
            this.mockFixture.Parameters["PoolName"] = "MyPool";
            this.mockFixture.Parameters["VdName"] = "MyVD";
            this.mockFixture.Parameters["DiskCount"] = 4;

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.cmd");

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"cmd /c {expectedScriptPath} --sizeGreaterThan 512 --diskCount 4";
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

        private void SetupSystemConfigPackage()
        {
            this.systemConfigPackage = new DependencyPath(
                PackageName,
                this.mockFixture.GetPackagePath(PackageName));

            this.mockFixture.SetupPackage(this.systemConfigPackage);
        }

        private string GetExpectedScriptPath(string scriptFileName)
        {
            return this.mockFixture.Combine(
                this.systemConfigPackage.Path,
                this.mockFixture.PlatformSpecifics.PlatformArchitectureName,
                scriptFileName);
        }
    }
}
