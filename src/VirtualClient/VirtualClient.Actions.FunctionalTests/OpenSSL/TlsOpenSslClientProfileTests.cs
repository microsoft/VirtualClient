// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class TlsOpenSslClientProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            this.mockFixture
                .Setup(PlatformID.Unix, Architecture.X64, "Client01")
                .SetupLayout(
                    new ClientInstance("Client01", "1.2.3.4", "Client"),
                    new ClientInstance("Server01", "1.2.3.5", "Server"));

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("Compete-OPENSSL-TLS.json")]
        public async Task TlsOpenSslClientCreatesExpectedStateAndExecutesWorkload(string profile)
        {
            List<string> expectedCommands = new List<string>();

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - Expected processes are executed.
            IPAddress.TryParse("1.2.3.4", out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient("1.2.3.4", ipAddress);

            State state = new State();

            await apiClient.CreateStateAsync(nameof(State), state, CancellationToken.None);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments?.Contains("s_time") == true)
                {
                    return process;
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "openssl");
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }
    }
}