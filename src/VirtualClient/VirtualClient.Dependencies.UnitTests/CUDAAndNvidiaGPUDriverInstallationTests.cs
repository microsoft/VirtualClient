// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
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
    public class CUDAAndNvidiaGPUDriverInstallationTests
    {
        private const string UpdateCommand = "apt update";
        private const string BuildEssentialInstallationCommand = "apt install build-essential -yq";
        private const string GetRunFileCommand = "wget https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run";
        private const string RunRunFileCommand = "sh cuda_11.6.0_510.39.01_linux.run --silent";

        private const string ExportPathCommand = $"bash -c \"echo 'export PATH=/usr/local/cuda-11.6/bin${{PATH:+:${{PATH}}}}' | " +
            $"sudo tee -a /home/anyuser/.bashrc\"";

        private const string ExportLibraryPathCommand = $"bash -c \"echo 'export LD_LIBRARY_PATH=/usr/local/cuda-11.6/lib64${{LD_LIBRARY_PATH:+:${{LD_LIBRARY_PATH}}}}' | " +
            $"sudo tee -a /home/anyuser/.bashrc\"";

        private const string UpgradeCommand = "apt upgrade -y";
        private const string InstallDriverCommand = "apt install nvidia-driver-510 nvidia-dkms-510 -y";
        private const string InstallFabricManagerCommand = "apt install cuda-drivers-fabricmanager-510 -y";

        private MockFixture fixture;
        private TestComponent component;
        private Mock<ProcessManager> mockProcessManager;
        private State mockState;

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
        public void CUDAAndNvidiaGPUDriverInstallationDependencyThrowsForPlatformsOtherThanUnix()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT, "11.6");

            WorkloadException exc = Assert.ThrowsAsync<WorkloadException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.PlatformNotSupported, exc.Reason);
        }

        [Test]
        public void CUDAAndNvidiaGPUDriverInstallationDependencyThrowsForUnsupportedDistros()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestCentOS7",
                LinuxDistribution = LinuxDistribution.Flatcar
            };

            this.SetupDefaultMockBehavior(PlatformID.Unix);
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);          

            WorkloadException exc = Assert.ThrowsAsync<WorkloadException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exc.Reason);
        }

        [Test]
        [TestCase("11.6")]
        public async Task CUDAAndNvidiaGPUDriverInstallationDependencyStartsCorrectProcessesOnExecute(string version)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, version);

            this.SetupProcessManager("sudo", UpdateCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", BuildEssentialInstallationCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", GetRunFileCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", RunRunFileCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", ExportPathCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", ExportLibraryPathCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", UpgradeCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", InstallDriverCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", InstallFabricManagerCommand, Environment.CurrentDirectory);

            await this.component.ExecuteAsync(CancellationToken.None);

            this.mockProcessManager.Verify();
        }

        [Test]
        [TestCase("11.6")]
        public async Task CUDAAndNvidiaGPUDriverInstallationDependencyDoesNotInstallCUDAAndNvidiaGPUDriverIfAlreadyInstalled(string version)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, version);

            this.fixture.StateManager.OnGetState(nameof(CudaAndNvidiaGPUDriverInstallation)).ReturnsAsync(JObject.FromObject(this.mockState));

            this.SetupProcessManager("sudo", UpdateCommand);

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.Throws<MockException>(() => this.mockProcessManager.Verify());
        }

        [Test]
        [TestCase("11.6")]
        public void CUDAAndNvidiaGPUDriverInstallationDependencySurfacesExceptionWhenProcessDoesNotExitSuccessfullyOnExecute(string version)
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix, version);

            this.SetupProcessManager("sudo", UpdateCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", BuildEssentialInstallationCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", GetRunFileCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", RunRunFileCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", ExportPathCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", UpgradeCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", InstallDriverCommand, Environment.CurrentDirectory);
            var setup = this.SetupProcessManager("sudo", InstallFabricManagerCommand, Environment.CurrentDirectory);

            setup.Returns(CUDAAndNvidiaGPUDriverInstallationTests.GetProcessProxy(1));

            DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exc.Reason);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID, string version = "")
        {
            this.fixture.Setup(platformID);

            this.mockProcessManager = new Mock<ProcessManager>();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "CudaVersion", "11.6" },
                { "DriverVersion", "510" },
                { "Username", "anyuser" },
                { "LocalRunFile", "https://developer.download.nvidia.com/compute/cuda/11.6.0/local_installers/cuda_11.6.0_510.39.01_linux.run" }
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

            this.fixture.SystemManagement.SetupGet(mgr => mgr.ProcessManager).Returns(this.mockProcessManager.Object);

            this.fixture.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            this.fixture.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.fixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        private ISetup<ProcessManager, IProcessProxy> SetupProcessManager(string expectedCmd, string expectedArgs = null, string expectedWorkingDirectory = null)
        {
            ISetup<ProcessManager, IProcessProxy> setup = this.mockProcessManager.Setup(mgr => mgr.CreateProcess(
                It.Is<string>(cmd => cmd.Equals(expectedCmd)),
                It.Is<string>(args => args == null || args.Equals(expectedArgs)),
                It.Is<string>(wd => wd == null || wd.Equals(expectedWorkingDirectory))));

            setup.Verifiable();
            setup.Returns(CUDAAndNvidiaGPUDriverInstallationTests.GetProcessProxy());
            return setup;
        }

        private static IProcessProxy GetProcessProxy(int exitCode = 0)
        {
            Mock<IProcessProxy> process = new Mock<IProcessProxy>();
            process.SetupGet(p => p.ExitCode).Returns(exitCode);
            process.SetupGet(p => p.HasExited).Returns(true);
            process.SetupGet(p => p.StartInfo).Returns(new ProcessStartInfo());
            return process.Object;
        }

        private class TestComponent : CudaAndNvidiaGPUDriverInstallation
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
