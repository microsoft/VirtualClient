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
    public class NvidiaContainerToolkitInstallationTests
    {
        private const string SetupCommand = "curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey "
            + "| sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg \\\n  "
            + "&& curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \\\n "
            + "   sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \\\n  "
            + "  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list";

        private const string UpdateCommand = "apt-get update";
        private const string NvidiaDockerInstallationCommand = "apt-get install -y nvidia-container-toolkit";
        private const string ConfigureDockerRuntimeCommand = "nvidia-ctk runtime configure --runtime=docker";
        private const string RestartDockerCommand = "systemctl restart docker";

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
        public void NvidiaContainerToolkitInstallationDependencyThrowsForPlatformsOtherThanUnix()
        {
            this.SetupDefaultMockBehavior(PlatformID.Win32NT);

            WorkloadException exc = Assert.ThrowsAsync<WorkloadException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.PlatformNotSupported, exc.Reason);
        }

        [Test]
        public void NvidiaContainerToolkitInstallationDependencyThrowsForUnsupportedDistros()
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
        public async Task NvidiaContainerToolkitInstallationDependencyStartsCorrectProcessOnExecute()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            this.SetupProcessManager("sudo", $"bash -c \"{SetupCommand}\"", Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", UpdateCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", NvidiaDockerInstallationCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", ConfigureDockerRuntimeCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", RestartDockerCommand, Environment.CurrentDirectory);

            await this.component.ExecuteAsync(CancellationToken.None);

            this.mockProcessManager.Verify();
        }

        [Test]
        public async Task NvidiaContainerToolkitInstallationDependencyDoesNotInstallNvidiaContainerToolkitIfAlreadyInstalled()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            this.fixture.StateManager.OnGetState(nameof(NvidiaContainerToolkitInstallation)).ReturnsAsync(JObject.FromObject(this.mockState));

            this.SetupProcessManager("sudo", SetupCommand);

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.Throws<MockException>(() => this.mockProcessManager.Verify());
        }

        [Test]
        public void NvidiaContainerToolkitInstallationDependencySurfacesExceptionWhenProcessDoesNotExitSuccessfullyOnExecute()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            this.SetupProcessManager("sudo", $"bash -c \"{SetupCommand}\"", Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", UpdateCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", NvidiaDockerInstallationCommand, Environment.CurrentDirectory);
            this.SetupProcessManager("sudo", ConfigureDockerRuntimeCommand, Environment.CurrentDirectory);

            var setup = this.SetupProcessManager("sudo", RestartDockerCommand, Environment.CurrentDirectory);
            setup.Returns(NvidiaContainerToolkitInstallationTests.GetProcessProxy(1));

            this.component.RetryPolicy = Policy.NoOpAsync();
            DependencyException exc = Assert.ThrowsAsync<DependencyException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exc.Reason);
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.fixture.Setup(platformID);

            this.mockProcessManager = new Mock<ProcessManager>();
            this.component = new TestComponent(this.fixture.Dependencies);
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
            setup.Returns(NvidiaContainerToolkitInstallationTests.GetProcessProxy());
            return setup;
        }

        private static IProcessProxy GetProcessProxy(int exitCode = 0)
        {
            Mock<IProcessProxy> process = new Mock<IProcessProxy>();
            process.SetupGet(p => p.ExitCode).Returns(exitCode);
            process.SetupGet(p => p.HasExited).Returns(true);
            process.SetupGet(p => p.StartInfo).Returns(new ProcessStartInfo());
            process.SetupGet(p => p.ProcessDetails).Returns(new ProcessDetails());
            return process.Object;
        }

        private class TestComponent : NvidiaContainerToolkitInstallation
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
