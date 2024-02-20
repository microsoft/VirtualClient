// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions;
    using Microsoft.VisualStudio.TestPlatform.Utilities;
    using Moq;
    using Moq.Language.Flow;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Disposed in tear down.")]
    public class AMDGPUDriverInstallationTests
    {
        private MockFixture fixture;
        private TestComponent component;
        private Mock<ProcessManager> mockProcessManager;
        private State mockState;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
        }

        [TearDown]
        public void TearDown()
        {
            this.component.Dispose();
        }

        [Test]
        public void AMDGPUDriverInstallationDependencyThrowsIfLinuxInstallationFileIsEmpty()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, string.Empty, string.Empty);

            DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyNotFound, exc.Reason);
        }

        [Test]
        public async Task AMDGPUDriverInstallationDependencyStartsCorrectProcessesOnExecuteForLinux()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            List<string> commands = new List<string>
            {
                "apt-get -yq update",
                "sudo apt-get install -yq libpci3 libpci-dev doxygen unzip cmake git",
                "sudo apt-get install -yq libnuma-dev libncurses5",
                "sudo apt-get install -yq libyaml-cpp-dev",
                "sudo apt-get -yq update",
                "wget https://repo.radeon.com/amdgpu-install/5.5/ubuntu/focal/amdgpu-install_5.5.50500-1_all.deb",
                "apt-get install -yq ./amdgpu-install_5.5.50500-1_all.deb",
                "sudo amdgpu-install -y --usecase=hiplibsdk,rocm,dkms",
                $"sudo bash -c \"echo 'export PATH=/opt/rocm/bin${{PATH:+:${{PATH}}}}' | " +
                $"sudo tee -a /home/testuser/.bashrc\"",
                "sudo apt-get install -yq rocblas rocm-smi-lib",
                "sudo apt-get install -yq rocm-validation-suite"
            };

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted(commands.ToArray()));
        }

        [Test]
        public async Task AMDGPUDriverInstallationDependencyStartsCorrectProcessOnExecuteForMi25()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, "mi25");
            string installScriptPath = this.fixture.Combine(this.mockPackage.Path, "mi25", "AMD-mi25.exe");

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"{installScriptPath} /S /v/qn"));
        }

        [Test]
        public async Task AMDGPUDriverInstallationDependencyStartsCorrectProcessOnExecuteForV620()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT);
            string installScriptPath = this.fixture.Combine(this.mockPackage.Path, "v620", "Setup.exe");

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.IsTrue(this.fixture.ProcessManager.CommandsExecuted($"{installScriptPath} -INSTALL -OUTPUT screen"));
        }

        [Test]
        public async Task AMDGPUDriverInstallationDependencyDoesNotInstallAMDGPUDriverIfAlreadyInstalled()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);
            this.fixture.StateManager.OnGetState(nameof(AMDGPUDriverInstallation)).ReturnsAsync(JObject.FromObject(this.mockState));
            string installScriptPath = this.fixture.Combine(this.mockPackage.Path, "v620", "Setup.exe");

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.IsFalse(this.fixture.ProcessManager.CommandsExecuted($"{installScriptPath} -INSTALL -OUTPUT screen"));
        }

        private void SetupDefaultMockBehavior(PlatformID platformID, string gpuModel = "v620", string linuxInstallationFile = "https://repo.radeon.com/amdgpu-install/5.5/ubuntu/focal/amdgpu-install_5.5.50500-1_all.deb")
        {
            this.fixture.Setup(platformID);
            this.mockPackage = new DependencyPath("amddriverpackage", this.fixture.GetPackagePath("amddriverpackage"));
            this.fixture.PackageManager.OnGetPackage("amddriverpackage").ReturnsAsync(this.mockPackage);
            this.mockProcessManager = new Mock<ProcessManager>();

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "PackageName", "amddriverpackage" },
                { "GpuModel", gpuModel },
                { "RebootRequired", false },
                { "Username", "testuser" },
                { "LinuxInstallationFile", linuxInstallationFile }
            };

            this.component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters);
            this.mockState = new State();

            ISetup<ProcessManager, IProcessProxy> setup = this.mockProcessManager.Setup(mgr => mgr.CreateProcess(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()));
            setup.Callback<string, string, string>((cmd, args, wd) =>
            {
                throw new Exception($"A setup is not registered for cmd: '{cmd}' args: '{args}' wd: '{wd}'");
            });

            this.fixture.File.Setup(file => file.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.SystemManagement.SetupGet(mgr => mgr.ProcessManager).Returns(this.mockProcessManager.Object);

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        private class TestComponent : AMDGPUDriverInstallation
        {
            public TestComponent(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnExecuteAsync => this.ExecuteAsync;

            public Func<EventContext, CancellationToken, Task> OnInitializeAsync => this.InitializeAsync;
        }
    }
}
