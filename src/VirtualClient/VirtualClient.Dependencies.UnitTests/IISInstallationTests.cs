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
    public class IISInstallationTests
    {
        private const string InstallIISCommand = "Install-WindowsFeature -name Web-Server,Net-Framework-45-Core,Web-Asp-Net45,NET-Framework-45-ASPNET -IncludeManagementTools";
        private const string DisableCompressioncommand = "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic";
        private const string DisableLoggingCommand = "Disable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging";
        private MockFixture fixture;
        private TestComponent component;
        private Mock<ProcessManager> mockProcessManager;
        private State mockState;

        [SetUp]
        public void SetupTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);
            this.mockProcessManager = new Mock<ProcessManager>();
            this.component = new TestComponent(this.fixture.Dependencies);
            this.mockState = new State();

            this.SetupDefaultMockBehavior();
        }

        [TearDown]
        public void TearDown()
        {
            this.component.Dispose();
        }

        [Test]
        public async Task IISInstallationDependencyStartsCorrectProcessOnExecute()
        {
            this.SetupProcessManager("powershell", InstallIISCommand);
            this.SetupProcessManager("powershell", DisableCompressioncommand);
            this.SetupProcessManager("powershell", DisableLoggingCommand);

            await this.component.ExecuteAsync(CancellationToken.None);

            this.mockProcessManager.Verify();
        }

        [Test]
        public async Task IISInstallationDependencyDoesNotInstallIISIfAlreadyInstalled()
        {
            this.fixture.StateManager.OnGetState(nameof(IISInstallation)).ReturnsAsync(JObject.FromObject(this.mockState));

            this.SetupProcessManager("powershell", InstallIISCommand);

            await this.component.ExecuteAsync(CancellationToken.None);
            Assert.Throws<MockException>(() => this.mockProcessManager.Verify());
        }

        [Test]
        public void IISInstallationDependencySurfacesExceptionWhenProcessDoesNotExitSuccessfullyOnExecute()
        {
            this.SetupProcessManager("powershell", InstallIISCommand);
            this.SetupProcessManager("powershell", DisableCompressioncommand);
            var setup = this.SetupProcessManager("powershell", DisableLoggingCommand);

            setup.Returns(IISInstallationTests.GetProcessProxy(1));

            WorkloadException exc = Assert.ThrowsAsync<WorkloadException>(() => this.component.ExecuteAsync(CancellationToken.None));
            Assert.AreEqual(ErrorReason.DependencyInstallationFailed, exc.Reason);
        }

        private void SetupDefaultMockBehavior()
        {
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
            setup.Returns(IISInstallationTests.GetProcessProxy());
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

        private class TestComponent : IISInstallation
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
