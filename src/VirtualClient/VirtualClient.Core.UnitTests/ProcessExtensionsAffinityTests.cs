// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Core
{
    using System;
    using System.Diagnostics;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.ProcessAffinity;

    [TestFixture]
    [Category("Unit")]
    public class ProcessExtensionsAffinityTests
    {
        private MockFixture mockFixture;

        public void SetupDefaults(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);
        }

        [Test]
        public void CreateProcessWithAffinityThrowsOnWindowsPlatform()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Win32NT, new[] { 0, 1 });

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
                this.mockFixture.ProcessManager.CreateProcessWithAffinity(
                    "command.exe",
                    "--args",
                    "C:\\workdir",
                    config));

            StringAssert.Contains("CreateProcessWithAffinity is only supported on Linux. For Windows, use: CreateProcess() + process.Start() + process.ApplyAffinity(windowsConfig).", ex.Message);
        }

        [Test]
        public void CreateProcessWithAffinityCreatesExpectedBashCommandOnLinux()
        {
            this.SetupDefaults(PlatformID.Unix);

            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, new[] { 0, 1, 2 });

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateProcessWithAffinity(
                "bash -c",
                "myworkload --option1=value",
                "/home/user/workdir",
                config))
            {
                Assert.IsNotNull(process);
                Assert.AreEqual("bash -c \"numactl -C 0-2 myworkload --option1=value\"", process.StartInfo.FileName);
                Assert.AreEqual("/home/user/workdir", process.StartInfo.WorkingDirectory);
            }
        }

        [Test]
        public void CreateProcessWithAffinityThrowsOnNullConfiguration()
        {
            this.SetupDefaults(PlatformID.Unix);

            Assert.Throws<ArgumentException>(() =>
                this.mockFixture.ProcessManager.CreateProcessWithAffinity(
                    "myworkload",
                    "--args",
                    "/home/user/workdir",
                    null));
        }

        [Test]
        public void CreateElevatedProcessWithAffinityThrowsOnWindowsPlatform()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Win32NT, new[] { 0, 1 });

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
                this.mockFixture.ProcessManager.CreateElevatedProcessWithAffinity(
                    PlatformID.Win32NT,
                    "command.exe",
                    "--args",
                    "C:\\workdir",
                    config));

            StringAssert.Contains("CreateElevatedProcessWithAffinity is only supported on Linux. For Windows, use: CreateElevatedProcess() + process.Start() + process.ApplyAffinity(windowsConfig).", ex.Message);
        }

        [Test]
        public void CreateElevatedProcessWithAffinityCreatesExpectedCommandOnLinux()
        {
            this.SetupDefaults(PlatformID.Unix);

            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, new[] { 0, 1, 2 });

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcessWithAffinity(
                PlatformID.Unix,
                "bash -c",
                "myworkload --option1=value",
                "/home/user/workdir",
                config))
            {
                Assert.IsNotNull(process);
                Assert.AreEqual("sudo", process.StartInfo.FileName);
                Assert.AreEqual("bash -c \"numactl -C 0-2 myworkload --option1=value\"", process.StartInfo.Arguments);
                Assert.AreEqual("/home/user/workdir", process.StartInfo.WorkingDirectory);
            }
        }

        [Test]
        public void CreateElevatedProcessWithAffinityHandlesComplexArguments()
        {
            this.SetupDefaults(PlatformID.Unix);

            ProcessAffinityConfiguration config = ProcessAffinityConfiguration.Create(PlatformID.Unix, new[] { 0, 2, 4 });

            using (IProcessProxy process = this.mockFixture.ProcessManager.CreateElevatedProcessWithAffinity(
                PlatformID.Unix,
                "bash -c",
                "myworkload --file=\\\"path with spaces\\\" --number=123",
                "/home/user/workdir",
                config))
            {
                Assert.IsNotNull(process);
                Assert.AreEqual("sudo", process.StartInfo.FileName);
                Assert.AreEqual("bash -c \"numactl -C 0,2,4 myworkload --file=\\\"path with spaces\\\" --number=123\"", process.StartInfo.Arguments);
            }
        }

        [Test]
        public void CreateElevatedProcessWithAffinityThrowsOnNullConfiguration()
        {
            this.SetupDefaults(PlatformID.Unix);

            Assert.Throws<ArgumentException>(() =>
                this.mockFixture.ProcessManager.CreateElevatedProcessWithAffinity(
                    PlatformID.Unix,
                    "myworkload",
                    "--args",
                    "/home/user/workdir",
                    null));
        }

        [Test]
        public void ApplyAffinityHelperThrowsOnNullConfiguration()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            IProcessProxy process = this.mockFixture.ProcessManager.CreateProcess("command.exe");

            Assert.Throws<ArgumentException>(() =>
                process.ApplyAffinity(null));
        }
    }
}
