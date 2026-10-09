// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class IpmiUtilSensorMonitorTests : MockFixture
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.Parameters["PackageName"] = "ipmiutil";

            // Setup:
            // The monitor will get the ipmiutil toolset from a package downloaded by default.
            this.mockPackage = new DependencyPath("ipmiutil", this.mockFixture.GetPackagePath("ipmiutil"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            this.mockFixture.File.Reset();
            this.mockFixture.Directory.Reset();
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task IpmiUtilSensorMonitorChecksForTheToolsetInAPackageLocation(PlatformID expectedPlatform, Architecture expectedArchitecture)
        {
            this.SetupTest(expectedPlatform, expectedArchitecture);

            // Setup:
            // The ipmiutil toolset package exists but not for the expected platform/architecture folder.
            this.SetupToolsetExistsInPackage();

            using (TestIpmiUtilSensorMonitor monitor = new TestIpmiUtilSensorMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                string expectedToolset = expectedPlatform == PlatformID.Unix ? "ipmiutil" : "ipmiutil.exe";

                // Expectation:
                // The path should be to the location in the dependency package.
                string expectedLocation = this.mockFixture.Combine(
                    this.mockPackage.Path, 
                    this.mockFixture.PlatformArchitectureName,
                    expectedToolset);

                string actualLocation = monitor.IpmiUtilExePath;

                Assert.AreEqual(expectedLocation, actualLocation);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void IpmiUtilSensorMonitorHandlesScenariosWhereToolsetPackagesExistButNotForTheParticularPlatformArchitecture(PlatformID expectedPlatform, Architecture expectedArchitecture)
        {
            this.SetupTest(expectedPlatform, expectedArchitecture);

            // Setup:
            // The ipmiutil toolset package exists...
            this.SetupToolsetExistsInPackage();

            // ...but not for the expected platform/architecture folder.
            string platformSpecificToolsetLocation = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificToolsetLocation))
                .Returns(false);

            using (TestIpmiUtilSensorMonitor monitor = new TestIpmiUtilSensorMonitor(this.mockFixture))
            {
                // Expectation:
                // An exception should not surface nor should the path have been set to a platform/specific
                // path location (as it does not exist).
                Assert.DoesNotThrowAsync(() => monitor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.IsNull(monitor.IpmiUtilExePath);
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(234)]
        public async Task IpmiUtilSensorMonitorChecksForTheToolsetInACommonLocationDefinedByThePathEnvironmentVariable(int successExitCode)
        {
            this.SetupTest();

            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. Additionally, the
            // ipmiutil toolset does not exist in one of the alternate locations. It does however exist
            // in a common location as defined by the PATH environment variable.
            this.SetupToolsetPreInstalled(successExitCode);

            using (TestIpmiUtilSensorMonitor monitor = new TestIpmiUtilSensorMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                // Expectation:
                // The path should be to the alternate location.
                string expectedLocation = "ipmiutil.exe";
                string actualLocation = monitor.IpmiUtilExePath;

                Assert.AreEqual(expectedLocation, actualLocation);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "C:\\BladeFX_latest\\BladeFX\\Tools\\IpmiUtil\\ipmiutil.exe")]
        public async Task IpmiUtilSensorMonitorChecksForTheToolsetInAlternatePathLocations(PlatformID platform, string expectedLocation)
        {
            this.SetupTest(platform);

            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. However, the
            // ipmiutil toolset DOES exist in one of the alternate locations.
            this.SetupToolsetExistsInAlternateLocation(expectedLocation);

            using (TestIpmiUtilSensorMonitor monitor = new TestIpmiUtilSensorMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                // Expectation:
                // The path should be to the alternate location.
                string actualLocation = monitor.IpmiUtilExePath;

                Assert.AreEqual(expectedLocation, actualLocation);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void IpmiUtilSensorMonitorExitsGracefullyIfTheToolsetCannotBeLocated(PlatformID expectedPlatform, Architecture expectedArchitecture)
        {
            this.SetupTest(expectedPlatform, expectedArchitecture);

            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. Additionally, the
            // ipmiutil toolset does not exist in one of the alternate locations. And finally, it does
            // not exist in a common location as defined by the PATH environment variable.
            //
            // ...nowhere to be found!!
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Mock<IProcessProxy> process = new Mock<IProcessProxy>();

                if ($"{command} {arguments}" == "ipmiutil --help" || $"{command} {arguments}" == "ipmiutil.exe /?")
                {
                    process.Setup(command, arguments, workingDir, exitCode: 1);
                }
                else
                {
                    Assert.Fail("Code flow should not proceed if the ipmiutil toolset path cannot be located.");
                }

                return process.Object;
            };

            using (TestIpmiUtilSensorMonitor monitor = new TestIpmiUtilSensorMonitor(this.mockFixture))
            {
                // Expectation:
                // The monitor should exit gracefully and there should not be any path location
                // identified for the ipmiutil toolset.

                Assert.DoesNotThrowAsync(() => monitor.ExecuteAsync(CancellationToken.None));
                Assert.IsNull(monitor.IpmiUtilExePath);
            }
        }

        private void SetupToolsetExistsInAlternateLocation(string expectedLocation)
        {
            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. However, the
            // ipmiutil toolset DOES exist in one of the alternate locations.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                InMemoryProcess process = new InMemoryProcess();

                if (arguments.EndsWith("--help") || arguments.EndsWith("/?"))
                {
                    // Setup:
                    // The toolset is not pre-installed on the system (e.g. via a Linux package manager).
                    process.ExitCode = 1;
                }

                return process;
            };

            // Setup:
            // The toolset is installed in an alternate location.
            this.mockFixture.File.Setup(f => f.Exists(expectedLocation)).Returns(true);
        }

        private void SetupToolsetExistsInPackage()
        {
            // Setup:
            // The ipmiutil toolset package exists but not for the expected platform/architecture folder.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(this.mockPackage.Path))
                .Returns(true);

            string toolset = this.mockFixture.Platform == PlatformID.Unix ? "ipmiutil" : "ipmiutil.exe";
            string platformSpecificToolsetLocation = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificToolsetLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(this.mockFixture.Combine(platformSpecificToolsetLocation, toolset)))
                .Returns(true);
        }

        private void SetupToolsetPreInstalled(int ipmiutilHelpExitCode)
        {
            // Setup:
            // The ipmiutil toolset is pre-installed on the system.
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                Mock<IProcessProxy> process = new Mock<IProcessProxy>();

                if (command.StartsWith("ipmiutil") && (arguments == "--help" || arguments == "/?"))
                {
                    // Standard output is required to confirm the toolset is available.
                    process.Setup(command, arguments, workingDir, exitCode: ipmiutilHelpExitCode, standardOutput: "ipmiutil ver. 1.2.3");
                }
                else
                {
                    process.Setup(command, arguments, workingDir, exitCode: 0);
                }

                return process.Object;
            };
        }

        private class TestIpmiUtilSensorMonitor : IpmiUtilSensorMonitor
        {
            public TestIpmiUtilSensorMonitor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new string IpmiUtilExePath
            {
                get
                {
                    return base.IpmiUtilExePath;
                }
            }


            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
