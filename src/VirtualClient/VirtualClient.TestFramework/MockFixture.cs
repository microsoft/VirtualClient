﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Fixture that encapsulates the setting up and mocking
    /// of common dependencies across virtual client actions.
    /// </summary>
    public class MockFixture : Fixture
    {
        /// <summary>
        /// The test assembly.
        /// </summary>
        public static readonly Assembly TestAssembly = Assembly.GetAssembly(typeof(MockFixture));

        /// <summary>
        /// The path to the directory where the test binaries (.dlls) exist. This can be used to mimic the "runtime/working" directory
        /// of the Virtual Client for the purpose of testing dependencies expected to exist in that directory.
        /// </summary>
        public static readonly string TestAssemblyDirectory = Path.GetDirectoryName(MockFixture.TestAssembly.Location);

        /// <summary>
        /// The path to the directory where test example files can be found. Note that this requires the
        /// test project to copy the files to a directory called 'Examples'.
        /// </summary>
        public static readonly string ExamplesDirectory = Path.Combine(TestAssemblyDirectory, "Examples");

        /// <summary>
        /// The path to the directory where test test resource/example files can be found. Note that this requires the
        /// test project to copy the files to a directory called 'TestResources'.
        /// </summary>
        public static readonly string TestResourcesDirectory = Path.Combine(TestAssemblyDirectory, "TestResources");

        private string experimentId;

        static MockFixture()
        {
            VirtualClientComponent.LogToFile = true;
            VirtualClientComponent.ContentPathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MockFixture"/> class.
        /// </summary>
        public MockFixture()
        {
            this.experimentId = Guid.NewGuid().ToString();
            this.Setup(Environment.OSVersion.Platform, Architecture.X64);
        }

        /// <summary>
        /// A mock API client.
        /// </summary>
        public Mock<IApiClient> ApiClient { get; set; }

        /// <summary>
        /// A mock API client manager.
        /// </summary>
        public Mock<IApiClientManager> ApiClientManager { get; set; }

        /// <summary>
        /// Mimics the current directory of the running application (e.g. the VC root directory).
        /// </summary>
        public string CurrentDirectory
        {
            get
            {
                return this.PlatformSpecifics.CurrentDirectory;
            }
        }

        /// <summary>
        /// Mock <see cref="IBlobManager"/> instance to mimic interactions with the
        /// content blob store.
        /// </summary>
        public Mock<IBlobManager> ContentBlobManager { get; set; }

        /// <summary>
        /// The targeted CPU architecture for the fixture and test scenarios
        /// (e.g. x64, arm64).
        /// </summary>
        public Architecture CpuArchitecture
        {
            get
            {
                return this.PlatformSpecifics.CpuArchitecture;
            }
        }

        /// <summary>
        /// Collection of services used for dependency injection.
        /// </summary>
        public IServiceCollection Dependencies { get; private set; }

        /// <summary>
        /// A mock disk manager.
        /// </summary>
        public Mock<IDiskManager> DiskManager { get; set; }

        /// <summary>
        /// Mock <see cref="IDirectory"/> that is accessed via the <see cref="FileSystem"/> object.
        /// </summary>
        public Mock<IDirectory> Directory { get; set; }

        /// <summary>
        /// Mock <see cref="IDirectoryInfo"/> that is accessed via the <see cref="FileSystem"/> object.
        /// </summary>
        public Mock<IDirectoryInfo> DirectoryInfo { get; set; }

        /// <summary>
        /// Mock <see cref="IFileSystem"/>
        /// </summary>
        public Mock<IFileSystem> FileSystem { get; set; }

        /// <summary>
        /// Mock <see cref="IFile"/> that is accessed via the <see cref="FileSystem"/> object.
        /// </summary>
        public Mock<IFile> File { get; set; }

        /// <summary>
        /// Mock <see cref="IFileInfo"/> that is accessed via the <see cref="FileSystem"/> object.
        /// </summary>
        public Mock<IFileInfoFactory> FileInfo { get; set; }

        /// <summary>
        /// Mock <see cref="IFileStreamFactory"/> used to mock out file stream interactions.
        /// </summary>
        public Mock<IFileStreamFactory> FileStream { get; set; }

        /// <summary>
        /// Mock firewall manager
        /// </summary>
        public Mock<IFirewallManager> FirewallManager { get; set; }

        /// <summary>
        /// A mock environment layout.
        /// </summary>
        public EnvironmentLayout Layout { get; set; }

        /// <summary>
        /// Mock <see cref="ILogger"/>.
        /// </summary>
        public InMemoryLogger Logger { get; set; }

        /// <summary>
        /// Mock <see cref="IPackageManager"/> instance.
        /// </summary>
        public Mock<IPackageManager> PackageManager { get; set; }

        /// <summary>
        /// Mock <see cref="IBlobManager"/> instance to mimic interactions with the
        /// packages blob store.
        /// </summary>
        public Mock<IBlobManager> PackagesBlobManager { get; set; }

        /// <summary>
        /// Mock parameters.
        /// </summary>
        public IDictionary<string, IConvertible> Parameters { get; set; }

        /// <summary>
        /// The targeted OS platform for the fixture and test scenarios
        /// (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform
        {
            get
            {
                return this.PlatformSpecifics.Platform;
            }
        }

        /// <summary>
        /// The name of the platform/architecture (win-x64, win-arm64, linux-x64).
        /// </summary>
        public string PlatformArchitectureName
        {
            get
            {
                return this.PlatformSpecifics.PlatformArchitectureName;
            }
        }

        /// <summary>
        /// Test/fake platform-specifics information provider.
        /// </summary>
        public TestPlatformSpecifics PlatformSpecifics { get; private set; }

        /// <summary>
        /// Mock process manager.
        /// </summary>
        public InMemoryProcessManager ProcessManager { get; set; }

        /// <summary>
        /// Mock ssh client manager.
        /// </summary>
        public InMemorySshClientManager SshClientManager { get; set; }

        /// <summary>
        /// The mock process that will be created by the process manager.
        /// </summary>
        public InMemoryProcess Process { get; set; }

        /// <summary>
        /// The mock state manager.
        /// </summary>
        public Mock<IStateManager> StateManager { get; set; }

        /// <summary>
        /// A mock system management instance.
        /// </summary>
        public Mock<ISystemManagement> SystemManagement { get; set; }

        /// <summary>
        /// A mock profile timing/timeout definition.
        /// </summary>
        public ProfileTiming Timing { get; set; }

        /// <summary>
        /// Returns a path that is combined specific to the platform defined for this
        /// fixture.
        /// </summary>
        public string Combine(params string[] pathSegments)
        {
            return this.PlatformSpecifics.Combine(pathSegments);
        }

        /// <summary>
        /// Returns the value of the environment variable from the underlying <see cref="PlatformSpecifics"/> instance.
        /// </summary>
        public string GetEnvironmentVariable(string name, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process)
        {
            return this.PlatformSpecifics.GetEnvironmentVariable(name, target);
        }

        /// <summary>
        /// Combines the path segments into a valid log file path.
        /// </summary>
        public string GetLogsPath(params string[] pathSegments)
        {
            return this.PlatformSpecifics.GetLogsPath(pathSegments);
        }

        /// <summary>
        /// Combines the path segments into a valid default packages path.
        /// </summary>
        public string GetPackagePath(params string[] pathSegments)
        {
            return this.PlatformSpecifics.GetPackagePath(pathSegments);
        }

        /// <summary>
        /// Combines the path segments into a valid default profiles folder path.
        /// </summary>
        public string GetProfilesPath(params string[] pathSegments)
        {
            return this.PlatformSpecifics.GetProfilePath(pathSegments);
        }

        /// <summary>
        /// Sets the environment variable value in the underlying <see cref="PlatformSpecifics"/> instance.
        /// </summary>
        public void SetEnvironmentVariable(string name, string value, EnvironmentVariableTarget target = EnvironmentVariableTarget.Process, bool append = false)
        {
            this.PlatformSpecifics.SetEnvironmentVariable(name, value, target, append);
        }

        /// <summary>
        /// Sets up or resets the fixture to default mock behaviors.
        /// </summary>
        public virtual MockFixture Setup(PlatformID platform, Architecture architecture = Architecture.X64, string agentId = null)
        {
            this.SetupMocks(true);

            this.ApiClient = new Mock<IApiClient>();
            this.ApiClientManager = new Mock<IApiClientManager>();
            this.FileSystem = new Mock<IFileSystem>();
            this.File = new Mock<IFile>();
            this.FileInfo = new Mock<IFileInfoFactory>();
            this.FileStream = new Mock<IFileStreamFactory>();
            this.Directory = new Mock<IDirectory>();
            this.DirectoryInfo = new Mock<IDirectoryInfo>();
            this.Directory.Setup(dir => dir.CreateDirectory(It.IsAny<string>())).Returns(this.DirectoryInfo.Object);
            this.FileSystem.SetupGet(fs => fs.File).Returns(this.File.Object);
            this.FileSystem.SetupGet(fs => fs.FileInfo).Returns(this.FileInfo.Object);
            this.FileSystem.SetupGet(fs => fs.FileStream).Returns(this.FileStream.Object);
            this.FileSystem.SetupGet(fs => fs.Directory).Returns(this.Directory.Object);

            this.DiskManager = new Mock<IDiskManager>();
            this.Logger = new InMemoryLogger();
            this.FirewallManager = new Mock<IFirewallManager>();
            this.PlatformSpecifics = new TestPlatformSpecifics(platform, architecture);
            this.ProcessManager = new InMemoryProcessManager(platform);
            this.SshClientManager = new InMemorySshClientManager();
            this.Process = new InMemoryProcess();
            this.PackageManager = new Mock<IPackageManager>();
            this.PackageManager.SetupGet(pm => pm.PlatformSpecifics).Returns(this.PlatformSpecifics);
            this.ContentBlobManager = new Mock<IBlobManager>();
            this.PackagesBlobManager = new Mock<IBlobManager>();
            this.StateManager = new Mock<IStateManager>();
            this.Timing = new ProfileTiming(TimeSpan.FromMilliseconds(2));
            this.Parameters = new Dictionary<string, IConvertible>();
            this.Parameters[nameof(VirtualClientComponent.Scenario)] = "AnyScenario";

            this.ContentBlobManager.SetupGet(mgr => mgr.StoreDescription).Returns(new DependencyBlobStore(DependencyStore.Content, "connection token"));
            this.PackagesBlobManager.SetupGet(mgr => mgr.StoreDescription).Returns(new DependencyBlobStore(DependencyStore.Packages, "connection token"));

            this.ApiClientManager.Setup(mgr => mgr.GetApiClient(It.IsAny<string>()))
                .Returns(() => this.ApiClient.Object);

            this.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<IPAddress>(), It.IsAny<int?>()))
                .Returns(() => this.ApiClient.Object);

            this.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns(() => this.ApiClient.Object);

            // API Client is returning HTTP Status OK by default. Note that the returns for ALL DISPOSABLE OBJECTS
            // should use the factory expression to ensure it is a new object each time. Developers can override this
            // behavior in their testing scenarios if a different behavior is desired or it is necessary to use the same
            // object for testing specifics.
            this.ApiClient.SetupGet(client => client.BaseUri).Returns(new Uri("http://1.2.3.5:4500"));

            this.ApiClient.Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.DeleteStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.UpdateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(() => this.CreateHttpResponse(HttpStatusCode.OK));

            // Processes created by the workload executor should mimic starting and finishing
            // by default.
            this.Process.OnHasExited = () => true;
            this.Process.OnStart = () => true;
            this.Process.ExitCode = 0;

            string effectiveAgentId = agentId ?? Environment.MachineName;
            this.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(effectiveAgentId, "1.2.3.4", "Client"), // Always set to the local machine name.
                new ClientInstance($"{effectiveAgentId}-Server", "1.2.3.5", "Server")
            });

            this.SystemManagement = new Mock<ISystemManagement>();
            this.SystemManagement.SetupGet(sm => sm.AgentId).Returns(effectiveAgentId);
            this.SystemManagement.SetupGet(sm => sm.ExperimentId).Returns(this.experimentId);
            this.SystemManagement.SetupGet(sm => sm.Platform).Returns(platform);
            this.SystemManagement.SetupGet(sm => sm.PlatformSpecifics).Returns(this.PlatformSpecifics);
            this.SystemManagement.SetupGet(sm => sm.PlatformArchitectureName).Returns(this.PlatformSpecifics.PlatformArchitectureName);
            this.SystemManagement.SetupGet(sm => sm.CpuArchitecture).Returns(architecture);
            this.SystemManagement.SetupGet(sm => sm.DiskManager).Returns(() => this.DiskManager.Object);
            this.SystemManagement.SetupGet(sm => sm.FileSystem).Returns(() => this.FileSystem.Object);
            this.SystemManagement.SetupGet(sm => sm.FirewallManager).Returns(() => this.FirewallManager.Object);
            this.SystemManagement.SetupGet(sm => sm.PackageManager).Returns(() => this.PackageManager.Object);
            this.SystemManagement.SetupGet(sm => sm.PlatformSpecifics).Returns(() => this.PlatformSpecifics);
            this.SystemManagement.SetupGet(sm => sm.ProcessManager).Returns(() => this.ProcessManager);
            this.SystemManagement.SetupGet(sm => sm.SshClientManager).Returns(() => this.SshClientManager);
            this.SystemManagement.SetupGet(sm => sm.StateManager).Returns(() => this.StateManager.Object);
            this.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>())).Returns(true);
            this.SystemManagement.Setup(sm => sm.WaitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            this.SystemManagement.Setup(sm => sm.WaitAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };

            this.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.SystemManagement.Setup(sm => sm.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo(
                    "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz",
                    "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz Family 6 Model 106 Stepping 2, GenuineIntel",
                    4,
                    8,
                    1,
                    1,
                    true,
                    new List<CpuCacheInfo>
                    {
                        new CpuCacheInfo("L1", null, 100000),
                        new CpuCacheInfo("L1d", null, 60000),
                        new CpuCacheInfo("L1i", null, 40000),
                        new CpuCacheInfo("L2", null, 10000000),
                        new CpuCacheInfo("L3", null, 80000000)
                    }));

            this.SystemManagement.Setup(sm => sm.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(
                    346801345,
                    new List<MemoryChipInfo>
                    {
                        new MemoryChipInfo("Memory_1", "Memory", 123456789, 2166, "HK Hynix", "HM123456"),
                        new MemoryChipInfo("Memory_2", "Memory", 223344556, 2432, "Micron", "M987654")
                    }));

            this.SystemManagement.Setup(sm => sm.GetNetworkInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NetworkInfo(
                    new List<NetworkInterfaceInfo>
                    {
                        new NetworkInterfaceInfo("Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)", "Mellanox Technologies MT27800 Family [ConnectX-5 Virtual Function] (rev 80)"),
                    }));

            this.Dependencies = new ServiceCollection();
            this.Dependencies.AddSingleton<ILogger>((p) => this.Logger);
            this.Dependencies.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            this.Dependencies.AddSingleton<IExpressionEvaluator>(ProfileExpressionEvaluator.Instance);
            this.Dependencies.AddSingleton<IFileSystem>((p) => this.FileSystem.Object);
            this.Dependencies.AddSingleton<ISystemInfo>((p) => this.SystemManagement.Object);
            this.Dependencies.AddSingleton<ISystemManagement>((p) => this.SystemManagement.Object);
            this.Dependencies.AddSingleton<PlatformSpecifics>((p) => this.PlatformSpecifics);
            this.Dependencies.AddSingleton<ProcessManager>((p) => this.ProcessManager);
            this.Dependencies.AddSingleton<ISshClientManager>((p) => this.SshClientManager);
            this.Dependencies.AddSingleton<IDiskManager>((p) => this.DiskManager.Object);
            this.Dependencies.AddSingleton<IFileSystem>((p) => this.FileSystem.Object);
            this.Dependencies.AddSingleton<IPackageManager>((p) => this.PackageManager.Object);
            this.Dependencies.AddSingleton<IStateManager>((p) => this.StateManager.Object);
            this.Dependencies.AddSingleton<EnvironmentLayout>((p) => this.Layout);
            this.Dependencies.AddSingleton<IApiClientManager>((p) => this.ApiClientManager.Object);
            this.Dependencies.AddSingleton<ProfileTiming>((p) => this.Timing);
            this.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager>
            {
                this.ContentBlobManager.Object,
                this.PackagesBlobManager.Object
            });

            return this;
        }

        /// <summary>
        /// Sets up the client instances in the environment layout.
        /// </summary>
        /// <param name="clients">The set of client instances to add to the environment layout.</param>
        public MockFixture SetupLayout(params ClientInstance[] clients)
        {
            if (clients?.Any() == true)
            {
                this.Layout = new EnvironmentLayout(clients);
            }

            return this;
        }

        /// <summary>
        /// Returns the path for the dependency/package given a specific platform and CPU architecture.
        /// </summary>
        /// <param name="dependency">The dependency path.</param>
        /// <param name="platform">The OS/system platform (e.g. Windows, Unix).</param>
        /// <param name="architecture">The CPU architecture (e.g. x64, arm64).</param>
        /// <returns>
        /// The dependency/package path given the specific platform and CPU architecture
        /// (e.g. /home/any/path/virtualclient/1.2.3.4/packages/geekbench5.1.0.0/linux-x64)
        /// </returns>
        public DependencyPath ToPlatformSpecificPath(DependencyPath dependency, PlatformID platform, Architecture? architecture = null)
        {
            dependency.ThrowIfNull(nameof(dependency));
            return this.PlatformSpecifics.ToPlatformSpecificPath(dependency, platform, architecture);
        }
    }
}
