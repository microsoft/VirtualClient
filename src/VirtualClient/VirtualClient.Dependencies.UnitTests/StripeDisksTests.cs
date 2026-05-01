// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
        private IEnumerable<Disk> linuxDisks;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Parameters["PackageName"] = PackageName;
            this.SetupSystemConfigPackage();

            this.linuxDisks = this.mockFixture.CreateDisks(PlatformID.Unix);
            this.mockFixture.SetupDisks(this.linuxDisks.ToArray());
        }

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommand()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.sh");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";
            string expectedDiskPaths = string.Join(",", this.linuxDisks.Where(d => !d.IsOperatingSystem()).Select(d => d.DevicePath));

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo bash {expectedScriptPath} --disks \"{expectedDiskPaths}\" --mountDirectory {expectedMountDir}";
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
        public async Task StripeDisksLimitsDisksByDiskCount()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";
            this.mockFixture.Parameters["DiskCount"] = 2;

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.sh");
            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";
            IEnumerable<Disk> nonOsDisks = this.linuxDisks.Where(d => !d.IsOperatingSystem());
            string expectedDiskPaths = string.Join(",", nonOsDisks.OrderByDescending(d => d.SizeInBytes(PlatformID.Unix)).Take(2).Select(d => d.DevicePath));

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"sudo bash {expectedScriptPath} --disks \"{expectedDiskPaths}\" --mountDirectory {expectedMountDir}";
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
        public void StripeDisksThrowsWhenNoDisksMatchFilter()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";
            this.mockFixture.SetupDisks(this.linuxDisks.Where(d => d.IsOperatingSystem()).ToArray());

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.DependencyNotFound, exc.Reason);
            }
        }

        [Test]
        public void StripeDisksThrowsWhenDiskCountExceedsAvailableDisks()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";
            int nonOsDiskCount = this.linuxDisks.Count(d => !d.IsOperatingSystem());
            this.mockFixture.Parameters["DiskCount"] = nonOsDiskCount + 1;

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => component.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.DependencyNotFound, exc.Reason);
            }
        }

        [Test]
        public async Task StripeDisksExecutesTheExpectedCommandOnWindows()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Parameters["PackageName"] = PackageName;
            this.SetupSystemConfigPackage();
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            IEnumerable<Disk> windowsDisks = this.mockFixture.CreateDisks(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(windowsDisks.ToArray());
            string expectedDiskPaths = string.Join(",", windowsDisks.Where(d => !d.IsOperatingSystem()).Select(d => d.DevicePath));

            string expectedScriptPath = this.GetExpectedScriptPath("stripe_disks.cmd");

            bool confirmed = false;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                string expectedCommand = $"cmd /c {expectedScriptPath} --disks \"{expectedDiskPaths}\"";
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
        public async Task StripeDisksSkipsWhenMountDirectoryAlreadyHasContent()
        {
            this.mockFixture.Parameters["DiskFilter"] = "OSDisk:false";

            string expectedMountDir = $"/home/{Environment.UserName}/mnt_raid0";
            this.mockFixture.Directory.Setup(d => d.Exists(expectedMountDir)).Returns(true);
            this.mockFixture.Directory.Setup(d => d.EnumerateFileSystemEntries(expectedMountDir))
                .Returns(new[] { $"{expectedMountDir}/lost+found" });

            int commandsExecuted = 0;
            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                commandsExecuted++;
            };

            using (StripeDisks component = new StripeDisks(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await component.ExecuteAsync(CancellationToken.None);
            }

            Assert.AreEqual(0, commandsExecuted);
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
