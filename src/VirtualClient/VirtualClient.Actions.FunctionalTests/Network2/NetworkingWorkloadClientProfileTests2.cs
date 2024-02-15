// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.CPSExecutor2;
    using static VirtualClient.Actions.LatteExecutor2;
    using static VirtualClient.Actions.NTttcpExecutor2;
    using static VirtualClient.Actions.SockPerfExecutor2;

    [TestFixture]
    [Ignore("There are some intermittent issue preventing the FunctionTests to return on GitHub Actions.")]
    [Category("Functional")]
    public class NetworkingWorkloadClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();

            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-NETWORK-2.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task NetworkingWorkloadProfileExecutesTheExpectedWorkloadsOnClient(string profile, PlatformID platformID, Architecture arch)
        {
            this.SetupFixtureBasedOnPlatformAndArchitecture(platformID, arch);
            IEnumerable<string> expectedCommands = this.GetCommands(platformID, arch);          

            string serverIPAddress = "1.2.3.5";
            this.SetupApiClient(serverIPAddress: serverIPAddress);
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);

            await this.mockFixture.StateManager.SaveStateAsync<State>(nameof(Dependencies.NetworkConfigurationSetup), new State(), CancellationToken.None)
             .ConfigureAwait(false);

            this.SetupCreateProcess();
            int count = 0;

            InMemoryApiClient apiClient = (InMemoryApiClient)this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);

            apiClient.OnGetState = (stateId) =>
            {
                count++;
                if ((count % 3) == 2)
                {
                    HttpResponseMessage response;
                    CPSWorkloadState expectedCPSState = new CPSWorkloadState(ClientServerStatus.ExecutionStarted);
                    NTttcpWorkloadState expectedNTttcpState = new NTttcpWorkloadState(ClientServerStatus.ExecutionStarted);
                    SockPerfWorkloadState expectedSockPerfState = new SockPerfWorkloadState(ClientServerStatus.ExecutionStarted);
                    LatteWorkloadState expectedLatteState = new LatteWorkloadState(ClientServerStatus.ExecutionStarted);

                    Item<JObject> stateItem = null;

                    switch (stateId)
                    {
                        case nameof(CPSWorkloadState):
                            stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedCPSState));
                            break;

                        case nameof(NTttcpWorkloadState):
                            stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedNTttcpState));
                            break;

                        case nameof(SockPerfWorkloadState):
                            stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedSockPerfState));
                            break;

                        case nameof(LatteWorkloadState):
                            stateItem = new Item<JObject>(stateId, JObject.FromObject(expectedLatteState));
                            break;
                    }

                    response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    response.Content = new StringContent(stateItem.ToJson());
                    return response;
                }
                else
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private void SetupCreateProcess()
        {
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (command.Contains("NTttcp.exe", StringComparison.OrdinalIgnoreCase) || command.Contains("NTttcp", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("ntttcp-results.xml"));
                }

                if (command.Contains("cps.exe", StringComparison.OrdinalIgnoreCase) || command.Contains("cps", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("cps-results.txt"));
                }

                if (command.Contains("latte.exe", StringComparison.OrdinalIgnoreCase) || command.Contains("latte", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("latte-results.txt"));
                }

                if (command.Contains("sockperf.exe", StringComparison.OrdinalIgnoreCase) || command.Contains("sockperf", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("sockperf-results.txt"));
                }

                return process;
            };
        }

        private void SetupFixtureBasedOnPlatformAndArchitecture(PlatformID platformID, Architecture architecture)
        {
            this.mockFixture.Setup(platformID, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));

            this.mockFixture.SetupDisks(withRemoteDisks: false);
            string platformArch = PlatformSpecifics.GetPlatformArchitectureName(platformID, architecture);

            this.SetupWorkloadPackages(platformID, platformArch);

            if (platformID == PlatformID.Unix)
            {
                string expectedFile = "/etc/rc.local";
                this.mockFixture.SetupFile(expectedFile);
            }

            this.SetupResultsFiles(platformArch);
        }

        private void SetupResultsFiles(string platformArch)
        {
            string packagePath = this.mockFixture.PlatformSpecifics.Combine(this.mockFixture.PackagesDirectory, "networking");
            byte[] ntttcpContent = Encoding.ASCII.GetBytes(TestDependencies.GetResourceFileContents("ntttcp-results.xml"));
            byte[] cpsContent = Encoding.ASCII.GetBytes(TestDependencies.GetResourceFileContents("cps-results.txt"));
            byte[] latteContent = Encoding.ASCII.GetBytes(TestDependencies.GetResourceFileContents("latte-results.txt"));
            byte[] sockPerfContent = Encoding.ASCII.GetBytes(TestDependencies.GetResourceFileContents("sockperf-results.txt"));

            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_4K_Buffer_T1/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_64K_Buffer_T1/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_256K_Buffer_T1/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_4K_Buffer_T32/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_64K_Buffer_T32/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_256K_Buffer_T32/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_4K_Buffer_T256/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_64K_Buffer_T256/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_TCP_256K_Buffer_T256/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_UDP_1400B_Buffer_T1/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/NTttcp_UDP_1400B_Buffer_T32/ntttcp-results.xml"), ntttcpContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/CPS_T16/cps-results.txt"), cpsContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/Latte_UDP/latte-results.txt"), latteContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/Latte_TCP/latte-results.txt"), latteContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/SockPerf_TCP_Ping_Pong/sockperf-results.txt"), sockPerfContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/SockPerf_UDP_Ping_Pong/sockperf-results.txt"), sockPerfContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/SockPerf_TCP_Under_Load/sockperf-results.txt"), sockPerfContent);
            this.mockFixture.SetupFile(this.mockFixture.PlatformSpecifics.Combine(packagePath, $"{platformArch}/SockPerf_UDP_Under_Load/sockperf-results.txt"), sockPerfContent);
        }

        private void SetupWorkloadPackages(PlatformID platformID, string platformArch)
        {
            this.mockFixture.SetupWorkloadPackage("visualstudiocruntime");

            string ntttcpExe = platformID == PlatformID.Win32NT ? "NTttcp.exe" : "ntttcp";
            string cpsExe = platformID == PlatformID.Win32NT ? "cps.exe" : "cps";
            string sockperfExe = platformID == PlatformID.Win32NT ? "sockperf.exe" : "sockperf";
            string latteExe = platformID == PlatformID.Win32NT ? "latte.exe" : "latte";

            List<string> expectedFiles = new List<string>
            {
                $"{platformArch}/{ntttcpExe}",
                $"{platformArch}/{cpsExe}",
                $"{platformArch}/{sockperfExe}",
                $"{platformArch}/{latteExe}",
            };

            this.mockFixture.SetupWorkloadPackage("networking", expectedFiles: expectedFiles.ToArray());
        }

        private IEnumerable<string> GetCommands(PlatformID platformID, Architecture arch)
        {
            if (platformID == PlatformID.Unix && arch == Architecture.X64)
            {
                return new List<string>
                {
                    @"sudo sh /etc/rc.local",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_4K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_64K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_256K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_4K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_64K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_256K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_4K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_64K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_TCP_256K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 1400 -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_UDP_1400B_Buffer_T1/ntttcp-results.xml -p 5500 -u",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 1400 -x /home/user/tools/VirtualClient/packages/networking/linux-x64/NTttcp_UDP_1400B_Buffer_T32/ntttcp-results.xml -p 5500 -u",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/cps -c -r 16 1.2.3.4,0,1.2.3.5,7201,100,100,0,1 -i 10 -wt 60 -t 300",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/sockperf ping-pong -i 1.2.3.5 -p 8201 --tcp -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-x64/SockPerf_TCP_Ping_Pong/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/sockperf ping-pong -i 1.2.3.5 -p 8201  -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-x64/SockPerf_UDP_Ping_Pong/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/sockperf under-load -i 1.2.3.5 -p 8201 --tcp -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-x64/SockPerf_TCP_Under_Load/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-x64/sockperf under-load -i 1.2.3.5 -p 8201  -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-x64/SockPerf_UDP_Under_Load/sockperf-results.txt"
                };
            }
            else if (platformID == PlatformID.Unix && arch == Architecture.Arm64)
            {
                return new List<string>
                {
                    @"sudo sh /etc/rc.local",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_4K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_64K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_256K_Buffer_T1/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_4K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_64K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_256K_Buffer_T32/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 4K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_4K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 64K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_64K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 256,*,1.2.3.5 -W 10 -C 10 -t 60 -b 256K -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_TCP_256K_Buffer_T256/ntttcp-results.xml -p 5500",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 1,*,1.2.3.5 -W 10 -C 10 -t 60 -b 1400 -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_UDP_1400B_Buffer_T1/ntttcp-results.xml -p 5500 -u",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/ntttcp -s -V -m 32,*,1.2.3.5 -W 10 -C 10 -t 60 -b 1400 -x /home/user/tools/VirtualClient/packages/networking/linux-arm64/NTttcp_UDP_1400B_Buffer_T32/ntttcp-results.xml -p 5500 -u",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/cps -c -r 16 1.2.3.4,0,1.2.3.5,7201,100,100,0,1 -i 10 -wt 60 -t 300",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/sockperf ping-pong -i 1.2.3.5 -p 8201 --tcp -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-arm64/SockPerf_TCP_Ping_Pong/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/sockperf ping-pong -i 1.2.3.5 -p 8201  -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-arm64/SockPerf_UDP_Ping_Pong/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/sockperf under-load -i 1.2.3.5 -p 8201 --tcp -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-arm64/SockPerf_TCP_Under_Load/sockperf-results.txt",
                    @"/home/user/tools/VirtualClient/packages/networking/linux-arm64/sockperf under-load -i 1.2.3.5 -p 8201  -t 60 --mps=max --full-rtt --msg-size 64 --client_ip 1.2.3.4 --full-log /home/user/tools/VirtualClient/packages/networking/linux-arm64/SockPerf_UDP_Under_Load/sockperf-results.txt"
                };
            }
            else if (platformID == PlatformID.Win32NT && arch == Architecture.X64)
            {
                return new List<string>
                {
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_4K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_64K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_256K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_4K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_64K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_256K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_4K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_64K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_TCP_256K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 1400 -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_UDP_1400B_Buffer_T1\ntttcp-results.xml -u -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 1400 -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-x64\NTttcp_UDP_1400B_Buffer_T32\ntttcp-results.xml -u -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\latte.exe -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -tcp -hist -hl 1 -hc 9998 -bl 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\latte.exe -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -udp -hist -hl 1 -hc 9998 -bl 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-x64\cps.exe -c -r 16 1.2.3.4,0,1.2.3.5,7201,100,100,0,1 -i 10 -wt 60 -t 300"
                };
            }
            else
            {
                return new List<string>
                {
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_4K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_64K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_256K_Buffer_T1\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_4K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_64K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_256K_Buffer_T32\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 4K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_4K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 64K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_64K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 256,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 256K -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_TCP_256K_Buffer_T256\ntttcp-results.xml  -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 1,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 1400 -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_UDP_1400B_Buffer_T1\ntttcp-results.xml -u -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp.exe -s -m 32,*,1.2.3.5 -wu 10 -cd 10 -t 60 -l 1400 -p 5500 -xml C:\users\any\tools\VirtualClient\packages\networking\win-arm64\NTttcp_UDP_1400B_Buffer_T32\ntttcp-results.xml -u -nic 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\latte.exe -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -tcp -hist -hl 1 -hc 9998 -bl 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\latte.exe -so -c -a 1.2.3.5:6100 -rio -i 100100 -riopoll 100000 -udp -hist -hl 1 -hc 9998 -bl 1.2.3.4",
                    @"C:\users\any\tools\VirtualClient\packages\networking\win-arm64\cps.exe -c -r 16 1.2.3.4,0,1.2.3.5,7201,100,100,0,1 -i 10 -wt 60 -t 300"
                };
            }
        }

        private void SetupApiClient(string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverIPAddress, ipAddress);

            CPSWorkloadState expectedCPSState = new CPSWorkloadState(ClientServerStatus.Ready);
            NTttcpWorkloadState expectedNTttcpState = new NTttcpWorkloadState(ClientServerStatus.Ready);
            SockPerfWorkloadState expectedSockPerfState = new SockPerfWorkloadState(ClientServerStatus.Ready);
            LatteWorkloadState expectedLatteState = new LatteWorkloadState(ClientServerStatus.Ready);

            apiClient.CreateStateAsync(nameof(CPSWorkloadState), expectedCPSState, CancellationToken.None)
                .GetAwaiter().GetResult();

            apiClient.CreateStateAsync(nameof(NTttcpWorkloadState), expectedNTttcpState, CancellationToken.None)
                .GetAwaiter().GetResult();

            apiClient.CreateStateAsync(nameof(SockPerfWorkloadState), expectedSockPerfState, CancellationToken.None)
                .GetAwaiter().GetResult();

            apiClient.CreateStateAsync(nameof(LatteWorkloadState), expectedLatteState, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }
}
