// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Fixture provides common in-memory dependencies for use in unit and functional
    /// testing scenarios.
    /// </summary>
    public class DependencyFixture : Fixture
    {
        /// <summary>
        /// The test assembly.
        /// </summary>
        public static readonly Assembly TestAssembly = Assembly.GetAssembly(typeof(DependencyFixture));

        /// <summary>
        /// The path to the directory where the test binaries (.dlls) exist. This can be used to mimic the "runtime/working" directory
        /// of the Virtual Client for the purpose of testing dependencies expected to exist in that directory.
        /// </summary>
        public static readonly string TestAssemblyDirectory = Path.GetDirectoryName(DependencyFixture.TestAssembly.Location);

        static DependencyFixture()
        {
            VirtualClientComponent.ContentPathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyFixture"/> class.
        /// </summary>
        public DependencyFixture(PlatformID? platform = null, Architecture? architecture = null)
        {
            this.Setup(platform ?? PlatformID.Win32NT, architecture ?? Architecture.X64, useUnixStylePathsOnly: false);
        }

        /// <summary>
        /// An In-memory API Client
        /// </summary>
        public InMemoryApiClient ApiClient { get; set; }

        /// <summary>
        /// An In-memory API client manager.
        /// </summary>
        public InMemoryApiClientManager ApiClientManager { get; set; }

        /// <summary>
        /// An In-memory Api Manager.
        /// </summary>
        public InMemoryApiManager ApiManager { get; set; }

        /// <summary>
        /// A test/fake <see cref="IConfiguration"/>.
        /// </summary>
        public IConfiguration Configuration { get; set; }

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
        /// Collection of services used for dependency injection with workload
        /// executors.
        /// </summary>
        public IServiceCollection Dependencies { get; set; }

        /// <summary>
        /// An in-memory disk manager.
        /// </summary>
        public InMemoryDiskManager DiskManager { get; set; }

        /// <summary>
        /// An in-memory file system.
        /// </summary>
        public InMemoryFileSystem FileSystem { get; set; }

        /// <summary>
        /// An in-memory firewall manager.
        /// </summary>
        public InMemoryFirewallManager FirewallManager { get; set; }

        /// <summary>
        /// A test/fake environment layout.
        /// </summary>
        public EnvironmentLayout Layout { get; set; }

        /// <summary>
        /// Mock <see cref="ILogger"/>.
        /// </summary>
        public InMemoryLogger Logger { get; set; }

        /// <summary>
        /// Mock <see cref="IPackageManager"/> instance.
        /// </summary>
        public InMemoryPackageManager PackageManager { get; set; }

        /// <summary>
        /// The test/fake dependency packages directory that is relevant to the
        /// OS platform.
        /// </summary>
        public string PackagesDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.CurrentDirectory, "packages");
            }
        }

        /// <summary>
        /// The test/fake dependency scripts directory that is relevant to the
        /// OS platform.
        /// </summary>
        public string ScriptsDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.CurrentDirectory, "scripts");
            }
        }

        /// <summary>
        /// Test/fake parameters to use with workload executors.
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
        /// Test/fake process manager.
        /// </summary>
        public InMemoryProcessManager ProcessManager { get; set; }

        /// <summary>
        /// Test/fake Ssh Client manager.
        /// </summary>
        public InMemorySshClientManager SshClientManager { get; set; }

        /// <summary>
        /// Test/fake state manager.
        /// </summary>
        public InMemoryStateManager StateManager { get; set; }

        /// <summary>
        /// Test/fake system management instance.
        /// </summary>
        public Mock<ISystemManagement> SystemManagement { get; set; }

        /// <summary>
        /// Test/fake profile timing/timeout definition.
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
        /// Combines the path segments into a valid default profile downloads folder path.
        /// </summary>
        public string GetProfileDownloadsPath(params string[] pathSegments)
        {
            return this.PlatformSpecifics.GetProfileDownloadsPath(pathSegments);
        }

        /// <summary>
        /// Combines the path segments into a valid state file path.
        /// </summary>
        public string GetStatePath(params string[] pathSegments)
        {
            return this.PlatformSpecifics.GetStatePath(pathSegments);
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
        public virtual DependencyFixture Setup(PlatformID platform, Architecture architecture = Architecture.X64, string agentId = null, bool useUnixStylePathsOnly = false)
        {
            this.SetupMocks(true);

            string vcAgentId = agentId ?? Environment.MachineName;
            IPAddress ipAddress = IPAddress.Loopback;

            this.ApiClient = new InMemoryApiClient(ipAddress);
            this.ApiClientManager = new InMemoryApiClientManager();
            this.ApiManager = new InMemoryApiManager();
            this.PlatformSpecifics = new TestPlatformSpecifics(platform, architecture, useUnixStylePathsOnly);
            VirtualClient.Contracts.PlatformSpecifics.RunningInContainer = false;
            this.Configuration = new ConfigurationBuilder().Build();
            this.DiskManager = new InMemoryDiskManager();
            this.FileSystem = new InMemoryFileSystem(this.PlatformSpecifics);
            this.FirewallManager = new InMemoryFirewallManager();
            this.Logger = new InMemoryLogger();
            this.PackageManager = new InMemoryPackageManager(this.PlatformSpecifics);
            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.ProcessManager = new InMemoryProcessManager(platform);
            this.SshClientManager = new InMemorySshClientManager();
            this.StateManager = new InMemoryStateManager();
            this.Timing = new ProfileTiming(TimeSpan.FromMilliseconds(2));

            this.SystemManagement = new Mock<ISystemManagement>();
            this.SystemManagement.SetupGet(sm => sm.AgentId).Returns(vcAgentId);
            this.SystemManagement.SetupGet(sm => sm.CpuArchitecture).Returns(architecture);
            this.SystemManagement.SetupGet(sm => sm.DiskManager).Returns(this.DiskManager);
            this.SystemManagement.SetupGet(sm => sm.FileSystem).Returns(this.FileSystem);
            this.SystemManagement.SetupGet(sm => sm.FirewallManager).Returns(this.FirewallManager);
            this.SystemManagement.SetupGet(sm => sm.PackageManager).Returns(this.PackageManager);
            this.SystemManagement.SetupGet(sm => sm.SshClientManager).Returns(this.SshClientManager);
            this.SystemManagement.SetupGet(sm => sm.RunningInContainer).Returns(false);
            this.SystemManagement.SetupGet(sm => sm.Platform).Returns(platform);
            this.SystemManagement.SetupGet(sm => sm.PlatformSpecifics).Returns(this.PlatformSpecifics);
            this.SystemManagement.SetupGet(sm => sm.PlatformArchitectureName).Returns(this.PlatformSpecifics.PlatformArchitectureName);
            this.SystemManagement.SetupGet(sm => sm.ProcessManager).Returns(this.ProcessManager);
            this.SystemManagement.SetupGet(sm => sm.StateManager).Returns(this.StateManager);
            this.SystemManagement.SetupGet(sm => sm.ExperimentId).Returns(Guid.NewGuid().ToString());
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

            this.Dependencies = this.InitializeDependencies();

            return this;
        }

        /// <summary>
        /// Sets up the client instances in the environment layout.
        /// </summary>
        /// <param name="clients">The set of client instances to add to the environment layout.</param>
        public DependencyFixture SetupLayout(params ClientInstance[] clients)
        {
            if (clients?.Any() == true)
            {
                this.Layout = new EnvironmentLayout(clients);
            }

            return this;
        }

        /// <summary>
        /// Returns the path for the dependency/package given a specific platform and CPU architecture defined
        /// for the fixture itself.
        /// </summary>
        /// <param name="dependency">The dependency path.</param>
        /// <returns>
        /// The dependency/package path given the specific platform and CPU architecture
        /// (e.g. /home/any/path/virtualclient/1.2.3.4/Packages/geekbench5.1.0.0/linux-x64)
        /// </returns>
        public DependencyPath ToPlatformSpecificPath(DependencyPath dependency)
        {
            dependency.ThrowIfNull(nameof(dependency));
            return this.PlatformSpecifics.ToPlatformSpecificPath(dependency, this.Platform, this.CpuArchitecture);
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

        private IServiceCollection InitializeDependencies()
        {
            IServiceCollection dependencies = new ServiceCollection()
                .AddSingleton<IApiClient>((provider) => this.ApiClient)
                .AddSingleton<IApiClientManager>((provider) => this.ApiClientManager)
                .AddSingleton<IApiManager>(this.ApiManager)
                .AddSingleton<ILogger>((provider) => this.Logger)
                .AddSingleton<IConfiguration>((provider) => this.Configuration)
                .AddSingleton<IExpressionEvaluator>(ProfileExpressionEvaluator.Instance)
                .AddSingleton<EnvironmentLayout>((provider) => this.Layout)
                .AddSingleton<ISystemInfo>((provider) => this.SystemManagement.Object)
                .AddSingleton<ISystemManagement>((provider) => this.SystemManagement.Object)
                .AddSingleton<PlatformSpecifics>((provider) => this.PlatformSpecifics)
                .AddSingleton<ProcessManager>(this.ProcessManager)
                .AddSingleton<IDiskManager>(this.DiskManager)
                .AddSingleton<IFileSystem>(this.FileSystem)
                .AddSingleton<IFirewallManager>(this.FirewallManager)
                .AddSingleton<IPackageManager>(this.PackageManager)
                .AddSingleton<ISshClientManager>(this.SshClientManager)
                .AddSingleton<IStateManager>(this.StateManager)
                .AddSingleton<ProfileTiming>(this.Timing);

            Mock<IBlobManager> contentBlobManager = new Mock<IBlobManager>();
            contentBlobManager.SetupGet(mgr => mgr.StoreDescription).Returns(new DependencyBlobStore(DependencyStore.Content, "connection token"));
            contentBlobManager.Setup(mgr => mgr.UploadBlobAsync(It.IsAny<DependencyDescriptor>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy>()))
                .ReturnsAsync(new BlobDescriptor
                {
                    Name = "any/path/to/blob/content.txt",
                    ContainerName = Guid.NewGuid().ToString()
                });

            Mock<IBlobManager> packageBlobManager = new Mock<IBlobManager>();
            packageBlobManager.SetupGet(mgr => mgr.StoreDescription).Returns(new DependencyBlobStore(DependencyStore.Packages, "connection token"));
            packageBlobManager.Setup(mgr => mgr.DownloadBlobAsync(It.IsAny<DependencyDescriptor>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy>()))
                .ReturnsAsync(new BlobDescriptor
                {
                    Name = "anypackage.1.0.0.zip",
                    PackageName = "anypackage",
                    ContainerName = "packages"
                });

            dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager>
            {
                contentBlobManager.Object,
                packageBlobManager.Object
            });

            return dependencies;
        }
    }
}
