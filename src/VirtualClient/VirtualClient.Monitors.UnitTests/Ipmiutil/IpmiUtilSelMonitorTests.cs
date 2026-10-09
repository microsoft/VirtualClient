// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class IpmiUtilSelMonitorTests : MockFixture
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private string examplesDirectory;
        private string validSelResults;

        public void SetupTest(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.Parameters["ClearSel"] = false;
            this.mockFixture.Parameters["EventsFilter"] = "IERR,Correctable Error";
            this.mockFixture.Parameters["PackageName"] = "ipmiutil";

            // Setup:
            // The monitor will get the ipmiutil toolset from a package downloaded by default.
            this.mockPackage = new DependencyPath("ipmiutil", this.mockFixture.GetPackagePath("ipmiutil"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(null as DependencyPath);

            this.mockFixture.File.Reset();
            this.mockFixture.Directory.Reset();

            // Setup:
            // The ipmiutil sel command produces valid SEL results.
            this.examplesDirectory = MockFixture.GetDirectory(typeof(IpmiUtilSelMonitorTests), "test_examples", "ipmiutil");
            this.validSelResults = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "ipmiutil_sel_example_1.txt"));
        }

        [Test]
        [TestCase(PlatformID.Win32NT, true, "IERR")]
        [TestCase(PlatformID.Win32NT, false, "Correctable Error")]
        public void IpmiUtilSelMonitorSetsParametersToExpectedValues(PlatformID platform, bool clearSelLog, string eventsFilter)
        {
            this.SetupTest(platform);

            this.mockFixture.Parameters["ClearSel"] = clearSelLog;
            this.mockFixture.Parameters["EventsFilter"] = eventsFilter;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.ClearSel = clearSelLog;
                monitor.EventsFilter = eventsFilter;
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IpmiUtilSelMonitorSetsCaptureSuspectedEventsParameterToExpectedValue(bool captureSuspectedEvents)
        {
            this.SetupTest();
            this.mockFixture.Parameters["CaptureSuspectedEvents"] = captureSuspectedEvents;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual(captureSuspectedEvents, monitor.CaptureSuspectedEvents);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IpmiUtilSelMonitorSetsIncludeWarningsInSuspectedEventsParameterToExpectedValue(bool includeWarnings)
        {
            this.SetupTest();
            this.mockFixture.Parameters["IncludeWarningsInSuspectedEvents"] = includeWarnings;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual(includeWarnings, monitor.IncludeWarningsInSuspectedEvents);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorCaptureSuspectedEventsDefaultsToFalse()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsFalse(monitor.CaptureSuspectedEvents);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorIncludeWarningsInSuspectedEventsDefaultsToTrue()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsTrue(monitor.IncludeWarningsInSuspectedEvents);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorParseSelRecordReturnsNullForInvalidInput()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsNull(monitor.TestParseSelRecord(null));
                Assert.IsNull(monitor.TestParseSelRecord(string.Empty));
                Assert.IsNull(monitor.TestParseSelRecord("invalid line format"));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorParseSelRecordParsesValidSelRecord()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string validRecord = "07cc | 01/24/25 21:35:33 | MAJ | BMC | Temperature | GPU0_DRAM0_TMP0 | Hi Crit thresh actual=127.00C";
                var record = monitor.TestParseSelRecord(validRecord);

                Assert.IsNotNull(record);
                Assert.AreEqual("07cc", record.RecordId);
                Assert.AreEqual("01/24/25 21:35:33", record.Timestamp);
                Assert.AreEqual("MAJ", record.Severity);
                Assert.AreEqual("BMC", record.Source);
                Assert.AreEqual("Temperature", record.EventType);
                Assert.AreEqual("GPU0_DRAM0_TMP0", record.Sensor);
                Assert.AreEqual("Hi Crit thresh actual=127.00C", record.EventDetail);
            }
        }

        [Test]
        [TestCase("CRT", "failure detected", true)]
        [TestCase("MAJ", "throttled", true)]
        [TestCase("CRT", "power supply", true)]
        [TestCase("INF", "normal operation", false)]
        [TestCase("WRN", "warning message", false)]
        public void IpmiUtilSelMonitorIsSelEventSuspectedDetectsHardwareErrors(string severity, string eventDetail, bool expectedResult)
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                var record = new TestSelRecord
                {
                    Severity = severity,
                    EventDetail = eventDetail,
                    EventType = "Hardware",
                    IsHardwareError = severity == "CRT" || severity == "MAJ"
                };

                bool result = monitor.TestIsSelEventSuspected(record);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        [TestCase("correctable ecc", true)]
        [TestCase("uncorrectable ecc", true)]
        [TestCase("ierr", true)]
        [TestCase("frb2", true)]
        [TestCase("frb3", true)]
        [TestCase("predictive failure", true)]
        [TestCase("bus error", true)]
        [TestCase("fatal nmi", true)]
        [TestCase("normal operation", false)]
        public void IpmiUtilSelMonitorIsSelEventSuspectedDetectsSuspectedKeywords(string eventDetail, bool expectedResult)
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                var record = new TestSelRecord
                {
                    Severity = "INF",
                    EventDetail = eventDetail,
                    EventType = "Hardware",
                    IsHardwareError = false
                };

                bool result = monitor.TestIsSelEventSuspected(record);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        [TestCase("asserted", true, true)]
        [TestCase("deasserted", true, true)]
        [TestCase("exceeded", true, true)]
        [TestCase("within", true, true)]
        [TestCase("above", true, true)]
        [TestCase("below", true, true)]
        [TestCase("normal message", true, false)]
        [TestCase("asserted", false, false)]
        public void IpmiUtilSelMonitorIsSelEventWarningDetectsWarningEvents(string eventDetail, bool includeWarnings, bool expectedResult)
        {
            this.SetupTest();
            this.mockFixture.Parameters["IncludeWarningsInSuspectedEvents"] = includeWarnings;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                var record = new TestSelRecord
                {
                    Severity = "WRN",
                    EventDetail = eventDetail,
                    EventType = "Temperature",
                    IsHardwareError = false
                };

                bool result = monitor.TestIsSelEventWarning(record);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorAnalyzeSuspectedSelEventsWithNoEvents()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                var (suspectedEvents, warningEvents) = monitor.TestAnalyzeSuspectedSelEvents(string.Empty);

                Assert.IsNotNull(suspectedEvents);
                Assert.IsNotNull(warningEvents);
                Assert.AreEqual(0, suspectedEvents.Count);
                Assert.AreEqual(0, warningEvents.Count);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorAnalyzeSuspectedSelEventsWithSuspectedEvents()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected\n" +
                               "07cd | 01/24/25 21:35:34 | INF | BMC | System | SEL | normal operation";

                var (suspectedEvents, warningEvents) = monitor.TestAnalyzeSuspectedSelEvents(selData);

                Assert.AreEqual(1, suspectedEvents.Count);
                Assert.IsTrue(suspectedEvents[0].Contains("failure detected"));
                Assert.AreEqual(0, warningEvents.Count);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorAnalyzeSuspectedSelEventsWithWarningEvents()
        {
            this.SetupTest();
            this.mockFixture.Parameters["IncludeWarningsInSuspectedEvents"] = true;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | WRN | BMC | Temperature | GPU0_DRAM0_TMP0 | threshold exceeded\n" +
                               "07cd | 01/24/25 21:35:34 | INF | BMC | System | SEL | normal operation";

                var (suspectedEvents, warningEvents) = monitor.TestAnalyzeSuspectedSelEvents(selData);

                Assert.AreEqual(0, suspectedEvents.Count);
                Assert.AreEqual(1, warningEvents.Count);
                Assert.IsTrue(warningEvents[0].Contains("threshold exceeded"));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorCountSelRecordsReturnsCorrectCount()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected\n" +
                               "07cd | 01/24/25 21:35:34 | INF | BMC | System | SEL | normal operation\n" +
                               "Header line that should be ignored\n" +
                               "07ce | 01/24/25 21:35:35 | MAJ | BMC | Power | PSU1 | power failure";

                int count = monitor.TestCountSelRecords(selData);
                Assert.AreEqual(3, count);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorCountSelRecordsReturnsZeroForEmptyInput()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual(0, monitor.TestCountSelRecords(null));
                Assert.AreEqual(0, monitor.TestCountSelRecords(string.Empty));
                Assert.AreEqual(0, monitor.TestCountSelRecords("No valid SEL records here"));
            }
        }

        [Test]
        [TestCase("CRT", "power failure", true)]
        [TestCase("MAJ", "temperature error", true)]
        [TestCase("INF", "normal message", false)]
        public void IpmiUtilSelMonitorDetermineIfHardwareErrorDetectsHardwareErrors(string severity, string eventDetail, bool expectedResult)
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                bool result = monitor.TestDetermineIfHardwareError(severity, eventDetail);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        [TestCase("CRT", null, true)]
        [TestCase("CRT", "", true)]
        [TestCase("CRT", "   ", true)]
        [TestCase("MAJ", null, true)]
        [TestCase("MAJ", "", true)]
        [TestCase("MAJ", "   ", true)]
        [TestCase("INF", null, false)]
        [TestCase("INF", "", false)]
        [TestCase("INF", "   ", false)]
        [TestCase("WRN", null, false)]
        [TestCase("WRN", "", false)]
        [TestCase("WRN", "   ", false)]
        [TestCase(null, "failure detected", false)]
        [TestCase("", "failure detected", false)]
        [TestCase("   ", "failure detected", false)]
        public void IpmiUtilSelMonitorDetermineIfHardwareErrorHandlesNullOrEmptyInputs(string severity, string eventDetail, bool expectedResult)
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                bool result = monitor.TestDetermineIfHardwareError(severity, eventDetail);
                Assert.AreEqual(expectedResult, result);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorWritesSuspectedEventsFileWhenEnabled()
        {
            this.SetupTest();
            this.mockFixture.Parameters["CaptureSuspectedEvents"] = true;

            List<string> filesWritten = new List<string>();
            List<string> fileContents = new List<string>();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    filesWritten.Add(path);
                    fileContents.Add(content);
                })
                .Returns(Task.CompletedTask);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";
                monitor.TestCaptureSuspectedEventsAsync(selData, EventContext.None, CancellationToken.None).Wait();

                Assert.IsTrue(filesWritten.Any(f => f.Contains("SuspectSelEntries")));
                Assert.IsTrue(fileContents.Any(c => c.Contains("failure detected")));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorWritesEmptySuspectedEventsFileWhenNoSuspectedEvents()
        {
            this.SetupTest();
            this.mockFixture.Parameters["CaptureSuspectedEvents"] = true;

            List<string> filesWritten = new List<string>();
            List<string> fileContents = new List<string>();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    filesWritten.Add(path);
                    fileContents.Add(content);
                })
                .Returns(Task.CompletedTask);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | INF | BMC | System | SEL | normal operation";
                monitor.TestCaptureSuspectedEventsAsync(selData, EventContext.None, CancellationToken.None).Wait();

                Assert.IsTrue(filesWritten.Any(f => f.Contains("SuspectSelEntries")));
                Assert.IsTrue(fileContents.Any(c => c.Contains("No suspected SEL events found")));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorWritesEmptySuspectedEventsFileWhenNoSelData()
        {
            this.SetupTest();
            this.mockFixture.Parameters["CaptureSuspectedEvents"] = true;

            List<string> filesWritten = new List<string>();
            List<string> fileContents = new List<string>();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    filesWritten.Add(path);
                    fileContents.Add(content);
                })
                .Returns(Task.CompletedTask);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.TestCaptureSuspectedEventsAsync(string.Empty, EventContext.None, CancellationToken.None).Wait();

                Assert.IsTrue(filesWritten.Any(f => f.Contains("SuspectSelEntries")));
                Assert.IsTrue(fileContents.Any(c => c.Contains("No SEL events found during monitoring period")));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorWritesWarningEventsFileWhenWarningEventsExist()
        {
            this.SetupTest();
            this.mockFixture.Parameters["CaptureSuspectedEvents"] = true;
            this.mockFixture.Parameters["IncludeWarningsInSuspectedEvents"] = true;

            List<string> filesWritten = new List<string>();
            List<string> fileContents = new List<string>();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    filesWritten.Add(path);
                    fileContents.Add(content);
                })
                .Returns(Task.CompletedTask);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                string selData = "07cc | 01/24/25 21:35:33 | WRN | BMC | Temperature | GPU0_DRAM0_TMP0 | threshold exceeded";
                monitor.TestCaptureSuspectedEventsAsync(selData, EventContext.None, CancellationToken.None).Wait();

                Assert.IsTrue(filesWritten.Any(f => f.Contains("SuspectSelEntries")));
                Assert.IsTrue(filesWritten.Any(f => f.Contains("WarningSelEntries")));
                Assert.IsTrue(fileContents.Any(c => c.Contains("threshold exceeded")));
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void IpmiUtilSelMonitorHandlesScenariosWhereToolsetPackagesExistButNotForTheParticularPlatformArchitecture(PlatformID expectedPlatform, Architecture expectedArchitecture)
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

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                // Expectation:
                // An exception should not surface nor should the path have been set to a platform/specific
                // path location (as it does not exist).
                Assert.DoesNotThrowAsync(() => monitor.InitializeAsync(EventContext.None, CancellationToken.None));
                Assert.IsNull(monitor.IpmiUtilExePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public async Task IpmiUtilSelMonitorChecksForTheToolsetInAPackageLocation(PlatformID expectedPlatform, Architecture expectedArchitecture)
        {
            this.SetupTest(expectedPlatform, expectedArchitecture);

            // Setup:
            // The ipmiutil toolset package exists but not for the expected platform/architecture folder.
            this.SetupToolsetExistsInPackage();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
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
        [TestCase(0)]
        [TestCase(234)]
        public async Task IpmiUtilSelMonitorChecksForTheToolsetInACommonLocationDefinedByThePathEnvironmentVariable(int successExitCode)
        {
            this.SetupTest();

            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. Additionally, the
            // ipmiutil toolset does not exist in one of the alternate locations. It does however exist
            // in a common location as defined by the PATH environment variable.
            this.SetupToolsetPreInstalled(successExitCode);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
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
        public async Task IpmiUtilSelMonitorChecksForTheToolsetInAlternatePathLocations(PlatformID platform, string expectedLocation)
        {
            this.SetupTest(platform);

            // Setup:
            // A toolset package containing ipmiutil does not exist on the system. However, the
            // ipmiutil toolset DOES exist in one of the alternate locations.
            this.SetupToolsetExistsInAlternateLocation(expectedLocation);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
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
        public void IpmiUtilSelMonitorExitsGracefullyIfTheToolsetCannotBeLocated(PlatformID expectedPlatform, Architecture expectedArchitecture)
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

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                // Expectation:
                // The monitor should exit gracefully and there should not be any path location
                // identified for the ipmiutil toolset.

                Assert.DoesNotThrowAsync(() => monitor.ExecuteAsync(CancellationToken.None));
                Assert.IsNull(monitor.IpmiUtilExePath);
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task IpmiUtilSelMonitorExecutesTheExpectedToolsetCommands_Toolset_Package_Downloaded(PlatformID platform, Architecture architecture)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> commandsExecuted = new List<string>();
                this.SetupTest(platform, architecture);

                // Setup:
                // A package containing ipmiutil is downloaded to the system.
                this.SetupToolsetExistsInPackage();

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    commandsExecuted.Add($"{command} {arguments}");
                    InMemoryProcess process = new InMemoryProcess();

                    if (arguments.EndsWith("sel -uc"))
                    {
                        process.StandardOutput.Append("0002 | 12/13/24 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared");
                    }
                    else if (arguments.EndsWith("sel -r"))
                    {
                        process.StandardOutput.Append("02 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff");

                        // Cause the 'while' loop in the monitor to exit.
                        tokenSource.Cancel();
                    }

                    return process;
                };

                using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
                {
                    await Task.WhenAny(monitor.ExecuteAsync(tokenSource.Token), Task.Delay(200000));
                }

                string expectedPackagePath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);

                if (platform == PlatformID.Win32NT)
                {
                    Assert.AreEqual(2, commandsExecuted.Count);
                    Assert.AreEqual($"{expectedPackagePath}\\ipmiutil.exe sel -uc", commandsExecuted[0]);
                    Assert.AreEqual($"{expectedPackagePath}\\ipmiutil.exe sel -r", commandsExecuted[1]);
                }
                else if (platform == PlatformID.Unix)
                {
                    Assert.AreEqual(3, commandsExecuted.Count);
                    Assert.AreEqual($"sudo chmod +x \"{expectedPackagePath}/ipmiutil\"", commandsExecuted[0]);
                    Assert.AreEqual($"sudo {expectedPackagePath}/ipmiutil sel -uc", commandsExecuted[1]);
                    Assert.AreEqual($"sudo {expectedPackagePath}/ipmiutil sel -r", commandsExecuted[2]);
                }
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        public async Task IpmiUtilSelMonitorExecutesTheExpectedToolsetCommands_Toolset_PreInstalled(PlatformID platform, Architecture architecture)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> commandsExecuted = new List<string>();
                this.SetupTest(platform, architecture);

                // Setup:
                // A package containing ipmiutil is NOT installed. This will force the logic to attempt to
                // identify the binary installation location in other locations.
                this.SetupToolsetPreInstalled(0);

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    commandsExecuted.Add($"{command} {arguments}");
                    InMemoryProcess process = new InMemoryProcess();

                    // Setup:
                    // The toolset is preinstalled on the system (e.g. via Linux package manager).
                    if (arguments.EndsWith("--help") || arguments.EndsWith("/?"))
                    {
                        process.ExitCode = 0;
                        process.StandardOutput.Append("ipmiutil ver. 1.2.3");
                    }
                    else if (arguments.EndsWith("sel -uc"))
                    {
                        process.StandardOutput.Append("0002 | 12/13/24 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared");
                    }
                    else if (arguments.EndsWith("sel -r"))
                    {
                        process.StandardOutput.Append("02 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff");

                        // Cause the 'while' loop in the monitor to exit.
                        tokenSource.Cancel();
                    }

                    return process;
                };

                using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
                {
                    await Task.WhenAny(monitor.ExecuteAsync(tokenSource.Token), Task.Delay(200000));
                }

                string expectedPackagePath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);

                if (platform == PlatformID.Win32NT)
                {
                    Assert.AreEqual(3, commandsExecuted.Count);
                    Assert.AreEqual($"ipmiutil.exe /?", commandsExecuted[0]);
                    Assert.AreEqual($"ipmiutil.exe sel -uc", commandsExecuted[1]);
                    Assert.AreEqual($"ipmiutil.exe sel -r", commandsExecuted[2]);
                }
                else if (platform == PlatformID.Unix)
                {
                    Assert.AreEqual(3, commandsExecuted.Count);
                    Assert.AreEqual($"ipmiutil --help", commandsExecuted[0]);
                    Assert.AreEqual($"sudo ipmiutil sel -uc", commandsExecuted[1]);
                    Assert.AreEqual($"sudo ipmiutil sel -r", commandsExecuted[2]);
                }
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, "C:\\BladeFX_latest\\BladeFX\\Tools\\IpmiUtil\\ipmiutil.exe")]
        public async Task IpmiUtilSelMonitorExecutesTheExpectedToolsetCommands_Toolset_Installed_In_Alternate_Location(PlatformID platform, string expectedLocation)
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                List<string> commandsExecuted = new List<string>();
                this.SetupTest(platform);

                // Setup:
                // A package containing ipmiutil is NOT installed. This will force the logic to attempt to
                // identify the binary installation location in other locations. The toolset is installed in
                // an alternate location on the system.
                this.SetupToolsetExistsInAlternateLocation(expectedLocation);

                this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
                {
                    commandsExecuted.Add($"{command} {arguments}");
                    InMemoryProcess process = new InMemoryProcess();

                    // Setup:
                    // The toolset is not preinstalled on the system (e.g. via Linux package manager).
                    if (arguments.EndsWith("--help") || arguments.EndsWith("/?"))
                    {
                        process.ExitCode = 1;
                    }
                    else if (arguments.EndsWith("sel -uc"))
                    {
                        process.StandardOutput.Append("0002 | 12/13/24 14:39:03 | INF | BMC| Event Log | SEL  | Log Cleared");
                    }
                    else if (arguments.EndsWith("sel -r"))
                    {
                        process.StandardOutput.Append("02 00 02 87 32 7a 65 20 00 04 10 8a 6f 02 ff ff");

                        // Cause the 'while' loop in the monitor to exit.
                        tokenSource.Cancel();
                    }

                    return process;
                };

                using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
                {
                    await Task.WhenAny(monitor.ExecuteAsync(tokenSource.Token), Task.Delay(200000));
                }

                string expectedPackagePath = this.mockFixture.Combine(this.mockPackage.Path, this.mockFixture.PlatformArchitectureName);

                Assert.AreEqual(3, commandsExecuted.Count);
                Assert.AreEqual($"{expectedLocation} sel -uc", commandsExecuted[1]);
                Assert.AreEqual($"{expectedLocation} sel -r", commandsExecuted[2]);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorEnableSelDecodingDefaultsToFalse()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsFalse(monitor.EnableSelDecoding);
            }
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void IpmiUtilSelMonitorSetsEnableSelDecodingParameterToExpectedValue(bool enableSelDecoding)
        {
            this.SetupTest();
            this.mockFixture.Parameters["EnableSelDecoding"] = enableSelDecoding;

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual(enableSelDecoding, monitor.EnableSelDecoding);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderArgumentsReturnsNullWhenNotProvided()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsNull(monitor.DecoderArguments);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderArgumentsReturnsValueWhenProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderArguments"] = "-nc -t Pacific -v --no-animation";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual("-nc -t Pacific -v --no-animation", monitor.DecoderArguments);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderPackageNameReturnsNullWhenNotProvided()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsNull(monitor.DecoderPackageName);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderPackageNameReturnsValueWhenProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderPackageName"] = "sherlockSelDecoder";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual("sherlockSelDecoder", monitor.DecoderPackageName);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorInitializesSherlockToolsetWhenPackageIsAvailable()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Sherlock";
            this.mockFixture.Parameters["DecoderPackageName"] = "sherlockSelDecoder";
            this.SetupSherlockPackage();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNotNull(monitor.DecoderExePath);
                StringAssert.Contains("sel_decode.exe", monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorSetsDecoderExePathToNullWhenPackageNameIsNotProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Sherlock";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNull(monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorSetsDecoderExePathToNullWhenExecutableDoesNotExistInPackage()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Sherlock";
            this.mockFixture.Parameters["DecoderPackageName"] = "sherlockSelDecoder";

            var sherlockPackage = new DependencyPath("sherlockSelDecoder", this.mockFixture.GetPackagePath("sherlockSelDecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(sherlockPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(sherlockPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(sherlockPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            // sel_decode.exe does not exist at the expected path
            this.mockFixture.File
                .Setup(file => file.Exists(It.Is<string>(s => s.Contains("sel_decode.exe"))))
                .Returns(false);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNull(monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorSherlockDecodingSkipsWhenSelRecordsIsEmpty()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                // Should not throw and should skip decoding
                await monitor.TestExecuteSherlockDecodingAsync(string.Empty, EventContext.None, CancellationToken.None);
                await monitor.TestExecuteSherlockDecodingAsync(null, EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorSherlockDecodingWritesSelRecordsToFileAndExecutesDecoder()
        {
            this.SetupTest();

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";
            string writtenContent = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.Contains("selRecords"))
                    {
                        writtenContent = content;
                    }
                })
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.AreEqual(selRecords, writtenContent);
        }

        [Test]
        public async Task IpmiUtilSelMonitorSherlockDecodingPassesDecoderArgumentsToDecoder()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderArguments"] = "-nc -t Pacific -v --no-animation";

            string capturedArguments = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                capturedArguments = arguments;
                return new InMemoryProcess();
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(capturedArguments);
            StringAssert.Contains("-nc -t Pacific -v --no-animation", capturedArguments);
            StringAssert.Contains("-f", capturedArguments);
        }

        [Test]
        public void IpmiUtilSelMonitorSherlockDecodingDoesNotThrowOnNonZeroExitCode()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.ExitCode = 1;
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                // Should not throw - non-zero exit code is logged as warning, not thrown
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorSherlockDecodingOmitsExtraArgsWhenDecoderArgumentsIsNull()
        {
            this.SetupTest();

            string capturedArguments = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                capturedArguments = arguments;
                return new InMemoryProcess();
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(capturedArguments);
            StringAssert.StartsWith(" -f", capturedArguments);
        }

        [Test]
        public void IpmiUtilSelMonitorSherlockInitializationDoesNotThrowOnException()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Sherlock";
            this.mockFixture.Parameters["DecoderPackageName"] = "sherlockSelDecoder";

            // Setup:
            // Force GetPlatformSpecificPackageAsync to throw an exception by returning a package
            // but having the file system throw when checking file existence.
            var sherlockPackage = new DependencyPath("sherlockSelDecoder", this.mockFixture.GetPackagePath("sherlockSelDecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(sherlockPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(sherlockPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(sherlockPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(It.Is<string>(s => s.Contains("sel_decode.exe"))))
                .Throws(new InvalidOperationException("Simulated initialization error"));

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                // Expectation:
                // The outer catch block in InitializeDecoderAsync should catch the exception
                // and log it as a warning without throwing.
                Assert.DoesNotThrowAsync(() => monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None));
                Assert.IsNull(monitor.DecoderExePath);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorSherlockDecodingDoesNotThrowOnFileWriteException()
        {
            this.SetupTest();

            // Setup:
            // Force an exception during file write operations.
            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Simulated file write error"));

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                // Expectation:
                // The catch block in ExecuteSherlockDecodingAsync should catch the exception
                // and log it as a warning without throwing.
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorSherlockDecodingDoesNotThrowOnProcessException()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Setup:
            // Force an exception when creating the process.
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                throw new InvalidOperationException("Simulated process execution error");
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                // Expectation:
                // The catch block in ExecuteSherlockDecodingAsync should catch the exception
                // and log it as a warning without throwing.
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesAndLogsDecodedSelEntries_WithValidOutput()
        {
            this.SetupTest();

            string decodedSelOutput = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "decodedIpmiUtilSelOutput.txt"));

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.Greater(monitor.DecodedSelCount, 0, "Expected at least one SEL entry to be decoded.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesAndLogsDecodedSelEntries_WithMultiLineMessages()
        {
            this.SetupTest();

            string decodedSelOutput = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "decodedIpmiUtilSelOutput.txt"));

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.Greater(monitor.DecodedSelCount, 2, "Expected at least 3 SEL entries to be decoded.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesAndLogsDecodedSelEntries_ExtractsCorrectFields()
        {
            this.SetupTest();

            string decodedSelOutput = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "decodedIpmiUtilSelOutput.txt"));

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.Greater(monitor.DecodedSelCount, 0, "Expected SEL entries to be decoded.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesAndLogsDecodedSelEntries_SkipsWhenNoTableFound()
        {
            this.SetupTest();

            string decodedSelOutput = "Some random output without a table";

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(0, monitor.DecodedSelCount, "No SEL entries should be decoded when table is not found.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesAndLogsDecodedSelEntries_HandlesParsingErrors()
        {
            this.SetupTest();

            string decodedSelOutput = @"
ID    Timestamp (UTC)      Sensor/OEM(Owner)                              Sensor Type                       Event Type                Message                                             Direction   Record Type      Raw Hex                                         
----  -------------------  ---------------------------------------------  --------------------------------  ------------------------  --------------------------------------------------  ----------  ---------------  ------------------------------------------------
0x1   2026-03-19 19:39:40  SEL(BMC)=0x03
0x2   2026-03-19 19:41:28  SoC_Status(BMC)=0x98                           BMC OEM                           OEM Discrete              POST Complete Timeout                               ASSERTED    System Event     02 00 02 68 51 bc 69 20 00 04 df 98 70 aa e3 ff 
";

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(1, monitor.DecodedSelCount, "Only valid SEL entry should be decoded.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorLogsDecodedSelCountMetric()
        {
            this.SetupTest();

            string decodedSelOutput = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "decodedIpmiUtilSelOutput.txt"));

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.Greater(monitor.DecodedSelCount, 0, "DecodedSelCount should be greater than 0.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesMultipleDecodedSelEntries()
        {
            this.SetupTest();

            string decodedSelOutput = System.IO.File.ReadAllText(Path.Combine(this.examplesDirectory, "decodedIpmiUtilSelOutput.txt"));

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.StandardOutput.Append(decodedSelOutput);
                return process;
            };

            string selRecords = "07cc | 01/24/25 21:35:33 | CRT | BMC | Temperature | GPU0_DRAM0_TMP0 | failure detected";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "sel_decode.exe";

                await monitor.TestExecuteSherlockDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.Greater(monitor.DecodedSelCount, 5, "Expected multiple SEL entries to be decoded.");
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderTypeReturnsValueWhenProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Generic";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual("Generic", monitor.DecoderType);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderExecutableNameReturnsNullWhenNotProvided()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.IsNull(monitor.DecoderExecutableName);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorDecoderExecutableNameReturnsValueWhenProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderExecutableName"] = "custom_decoder.exe";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.AreEqual("custom_decoder.exe", monitor.DecoderExecutableName);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorInitializesGenericDecoderToolsetWhenPackageIsAvailable()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Generic";
            this.mockFixture.Parameters["DecoderPackageName"] = "crcsdk_seldecoder";
            this.SetupGenericPackage();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNotNull(monitor.DecoderExePath);
                StringAssert.Contains("crcsdkseldecoder.exe", monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorInitializesGenericDecoderToolsetWhenPackageIsAvailableOnLinux()
        {
            this.SetupTest(PlatformID.Unix, Architecture.X64);
            this.mockFixture.Parameters["DecoderType"] = "Generic";
            this.mockFixture.Parameters["DecoderPackageName"] = "crcsdk_seldecoder";

            var genericPackage = new DependencyPath("crcsdk_seldecoder", this.mockFixture.GetPackagePath("crcsdk_seldecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(genericPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(genericPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(genericPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(this.mockFixture.Combine(platformSpecificLocation, "crcsdkseldecoder")))
                .Returns(true);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNotNull(monitor.DecoderExePath);
                StringAssert.Contains("crcsdkseldecoder", monitor.DecoderExePath);
                StringAssert.DoesNotContain(".exe", monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericInitializationSetsDecoderExePathToNullWhenPackageNameIsNotProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Generic";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNull(monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericInitializationSetsDecoderExePathToNullWhenExecutableDoesNotExistInPackage()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Generic";
            this.mockFixture.Parameters["DecoderPackageName"] = "crcsdk_seldecoder";

            var genericPackage = new DependencyPath("crcsdk_seldecoder", this.mockFixture.GetPackagePath("crcsdk_seldecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(genericPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(genericPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(genericPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            // crcsdkseldecoder.exe does not exist at the expected path
            this.mockFixture.File
                .Setup(file => file.Exists(It.Is<string>(s => s.Contains("crcsdkseldecoder.exe"))))
                .Returns(false);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNull(monitor.DecoderExePath);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorInitializeDecoderThrowsArgumentExceptionForUnsupportedDecoderType()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "InvalidDecoder";
            this.mockFixture.Parameters["DecoderPackageName"] = "crcsdk_seldecoder";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                Assert.ThrowsAsync<ArgumentException>(
                    () => monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorInitializeDecoderUsesCustomExecutableNameWhenProvided()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderType"] = "Generic";
            this.mockFixture.Parameters["DecoderPackageName"] = "crcsdk_seldecoder";
            this.mockFixture.Parameters["DecoderExecutableName"] = "custom_decoder.exe";

            var genericPackage = new DependencyPath("crcsdk_seldecoder", this.mockFixture.GetPackagePath("crcsdk_seldecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(genericPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(genericPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(genericPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(this.mockFixture.Combine(platformSpecificLocation, "custom_decoder.exe")))
                .Returns(true);

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                await monitor.TestInitializeDecoderAsync(EventContext.None, CancellationToken.None);

                Assert.IsNotNull(monitor.DecoderExePath);
                StringAssert.Contains("custom_decoder.exe", monitor.DecoderExePath);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingSkipsWhenSelRecordsIsEmpty()
        {
            this.SetupTest();

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                // Should not throw and should skip decoding
                await monitor.TestExecuteGenericDecodingAsync(string.Empty, EventContext.None, CancellationToken.None);
                await monitor.TestExecuteGenericDecodingAsync(null, EventContext.None, CancellationToken.None);

                Assert.AreEqual(0, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingWritesSelRecordsToFileAndExecutesDecoder()
        {
            this.SetupTest();

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";
            string writtenContent = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.Contains("selRecords"))
                    {
                        writtenContent = content;
                    }
                })
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.AreEqual(selRecords, writtenContent);
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingPassesDecoderArgumentsToDecoder()
        {
            this.SetupTest();
            this.mockFixture.Parameters["DecoderArguments"] = "--sku c41a8";

            string capturedArguments = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                capturedArguments = arguments;
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(capturedArguments);
            StringAssert.Contains("--sku c41a8", capturedArguments);
            StringAssert.Contains("-o", capturedArguments);
            // Generic format: "{inputFile}" -o "{outputDir}" {DecoderArguments} — file comes first
            StringAssert.StartsWith("\"", capturedArguments);
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingOmitsExtraArgsWhenDecoderArgumentsIsNull()
        {
            this.SetupTest();

            string capturedArguments = null;

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                capturedArguments = arguments;
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(capturedArguments);
            StringAssert.Contains("-o", capturedArguments);
            StringAssert.StartsWith("\"", capturedArguments);
        }

        [Test]
        public void IpmiUtilSelMonitorGenericDecodingDoesNotThrowOnNonZeroExitCode()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                InMemoryProcess process = new InMemoryProcess();
                process.ExitCode = 1;
                return process;
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                // Should not throw - non-zero exit code is logged as warning, not thrown
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
                Assert.AreEqual(0, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingHandlesEmptyDecodedOutputFile()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(0, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingHandlesMissingDecodedOutputFile()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(false);

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(0, monitor.DecodedSelCount);
            }
        }

        [Test]
        public void IpmiUtilSelMonitorGenericDecodingDoesNotThrowOnFileWriteException()
        {
            this.SetupTest();

            // Setup:
            // Force an exception during file write operations.
            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Simulated file write error"));

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                // Expectation:
                // The catch block in ExecuteGenericDecodingAsync should catch the exception
                // and log it as a warning without throwing.
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public void IpmiUtilSelMonitorGenericDecodingDoesNotThrowOnProcessException()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Setup:
            // Force an exception when creating the process.
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                throw new InvalidOperationException("Simulated process execution error");
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                // Expectation:
                // The catch block in ExecuteGenericDecodingAsync should catch the exception
                // and log it as a warning without throwing.
                Assert.DoesNotThrowAsync(() => monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingUsesMillisecondGranularityInFileNames()
        {
            this.SetupTest();

            string capturedInputPath = null;
            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.Contains("selRecords"))
                    {
                        capturedInputPath = path;
                    }
                })
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);
            }

            Assert.IsNotNull(capturedInputPath);
            // Millisecond format: yyyy-MM-dd-HH-mm-ss-fff — the timestamp portion has 23 chars
            // Example: 2026-05-17-10-27-15-830_input_selRecords.txt
            string fileName = Path.GetFileName(capturedInputPath);
            string timestampPart = fileName.Substring(0, fileName.IndexOf("_input_selRecords"));
            Assert.AreEqual(23, timestampPart.Length, "Timestamp should be in yyyy-MM-dd-HH-mm-ss-fff format (23 chars).");
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_WithSerRecords()
        {
            this.SetupTest();

            // SER record format (7 fields): RecordID | Timestamp | Source | SensorType | SensorID | Message | Direction
            string decodedOutput = string.Join(
                Environment.NewLine,
                "0001 | [11/05/2025 18:56:57] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Log Area Reset/cleared | Asserted",
                "0009 | [11/11/2025 23:42:10] | BMC, LUN 0 | Physical Security (Chassis Intrusion) | ID:0xF2 | General Chassis Intrusion. detected at power off state | Asserted");

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(2, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_WithOemTimestampedRecords()
        {
            this.SetupTest();

            // OEM Timestamped format (5 fields): RecordID | Timestamp | Manufacturer | RecordName | Message
            string decodedOutput = string.Join(
                Environment.NewLine,
                "0007 | [11/11/2025 23:41:59] | BMC        | Generic OEM SEL | Component ID: Manticore. Status: Keep Alive normal",
                "0015 | [11/11/2025 23:42:24] | Microsoft  | RecType: 0xCA | Unknown OEM record type");

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(2, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_WithOemNonTimestampedRecords()
        {
            this.SetupTest();

            // OEM Non-timestamped format (4 fields): RecordID | Manufacturer | RecordName | Message
            string decodedOutput = "00F0 | Microsoft | OEM_NonTS | Raw OEM data";

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(1, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_WithMixedRecordTypes()
        {
            this.SetupTest();

            // Mix of SER (7 fields), OEM Timestamped (5 fields), and OEM Non-timestamped (4 fields)
            string decodedOutput = string.Join(
                Environment.NewLine,
                "0001 | [11/05/2025 18:56:57] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Log Area Reset/cleared | Asserted",
                "0007 | [11/11/2025 23:41:59] | BMC        | Generic OEM SEL | Component ID: Manticore. Status: Keep Alive normal",
                "00F0 | Microsoft | OEM_NonTS | Raw OEM data");

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(3, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_ReturnsZeroForEmptyOutput()
        {
            this.SetupTest();

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync("   \n   \n");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(0, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_CountsLinesWithFewerThanFourFieldsAsUnknown()
        {
            this.SetupTest();

            // Lines with fewer than 4 pipe-delimited fields fall into the Unknown fallback path
            string decodedOutput = string.Join(
                Environment.NewLine,
                "some random text",
                "0001 | partial | data",
                "0002 | [11/05/2025 18:56:57] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Log Area Reset/cleared | Asserted");

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(3, monitor.DecodedSelCount, "All lines should be counted: 2 with fewer than 4 fields logged as Unknown, plus 1 valid SER record.");
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_PreservesPipeCharactersInMessageField()
        {
            this.SetupTest();

            // SER record where the message itself contains pipe characters — string.Join("|", ...) should
            // recombine the middle parts into the message, preserving the original content.
            string decodedOutput = "0001 | [11/05/2025 18:56:57] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Message with | embedded | pipes | Asserted";

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(1, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorParsesGenericDecodedSelEntries_PreservesUnicodeCharacters()
        {
            this.SetupTest();

            // OEM records containing CJK/Unicode characters in the message field.
            // \u6800 = 栀, \u5400 = 吀, \u6500 = 攀, \u7900 = 礀, \u7300 = 猀
            string decodedOutput = string.Join(
                Environment.NewLine,
                "0001 | [05/12/2026 20:05:11] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Log Area Reset/cleared | Asserted",
                "0004 | [05/12/2026 20:05:11] | Microsoft  | OS Shutdown | Sequence Number: 1. Shutdown Comment: \u6800\u5400",
                "0005 | [05/12/2026 20:05:11] | Microsoft  | OS Shutdown | Sequence Number: 1. Shutdown Comment: \u6500",
                "0006 | [05/12/2026 20:05:11] | Microsoft  | OS Shutdown | Sequence Number: 1. Shutdown Comment: \u7900\u7300");

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                // 1 SER record (7 fields) + 3 OEM timestamped records (5 fields) = 4 total
                Assert.AreEqual(4, monitor.DecodedSelCount);
            }
        }

        [Test]
        public async Task IpmiUtilSelMonitorGenericDecodingCopiesDecodedFileToTimestampedPath()
        {
            this.SetupTest();

            string decodedOutput = "0001 | [11/05/2025 18:56:57] | BMC, LUN 0 | Event Logging Disabled | ID:0xF1 | Log Area Reset/cleared | Asserted";

            this.mockFixture.FileSystem.Setup(fs => fs.File.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            this.mockFixture.FileSystem.Setup(fs => fs.File.Exists(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log"))))
                .Returns(true);
            this.mockFixture.FileSystem.Setup(fs => fs.File.ReadAllTextAsync(It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(decodedOutput);

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                return new InMemoryProcess();
            };

            string selRecords = "cc 07 02 a5 07 94 67 20 00 04 01 5a 01 59 7f 6b";

            using (TestIpmiUtilSelMonitor monitor = new TestIpmiUtilSelMonitor(this.mockFixture))
            {
                monitor.DecoderExePath = "crcsdkseldecoder.exe";

                await monitor.TestExecuteGenericDecodingAsync(selRecords, EventContext.None, CancellationToken.None);

                Assert.AreEqual(1, monitor.DecodedSelCount);
            }

            // Verify the decoded file was copied to a timestamped path
            this.mockFixture.FileSystem.Verify(
                fe => fe.File.Copy(
                    It.Is<string>(s => s.Contains("All-BMC-SEL-Decoded.log")),
                    It.Is<string>(s => s.Contains("_decoded_selRecords.log")),
                    true),
                Times.Once);
        }

        private void SetupGenericPackage()
        {
            var genericPackage = new DependencyPath("crcsdk_seldecoder", this.mockFixture.GetPackagePath("crcsdk_seldecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(genericPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(genericPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(genericPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(this.mockFixture.Combine(platformSpecificLocation, "crcsdkseldecoder.exe")))
                .Returns(true);
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

        private void SetupSherlockPackage()
        {
            var sherlockPackage = new DependencyPath("sherlockSelDecoder", this.mockFixture.GetPackagePath("sherlockSelDecoder"));
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(sherlockPackage);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(sherlockPackage.Path))
                .Returns(true);

            string platformSpecificLocation = this.mockFixture.Combine(sherlockPackage.Path, this.mockFixture.PlatformArchitectureName);

            this.mockFixture.Directory
                .Setup(dir => dir.Exists(platformSpecificLocation))
                .Returns(true);

            this.mockFixture.File
                .Setup(file => file.Exists(this.mockFixture.Combine(platformSpecificLocation, "sel_decode.exe")))
                .Returns(true);
        }

        public class TestSelRecord
        {
            public string RecordId { get; set; }
            public string Timestamp { get; set; }
            public string Severity { get; set; }
            public string Source { get; set; }
            public string EventType { get; set; }
            public string Sensor { get; set; }
            public string EventDetail { get; set; }
            public bool IsHardwareError { get; set; }
        }

        private class TestIpmiUtilSelMonitor : IpmiUtilSelMonitor
        {
            public TestIpmiUtilSelMonitor(MockFixture fixture)
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

            public new void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
            {
                base.CaptureMetrics(results, commandArguments, startTime, endTime, telemetryContext);
            }

            public TestSelRecord TestParseSelRecord(string line)
            {
                var record = base.ParseSelRecord(line);
                if (record == null) return null;

                return new TestSelRecord
                {
                    RecordId = record.RecordId,
                    Timestamp = record.Timestamp,
                    Severity = record.Severity,
                    Source = record.Source,
                    EventType = record.EventType,
                    Sensor = record.Sensor,
                    EventDetail = record.EventDetail,
                    IsHardwareError = record.IsHardwareError
                };
            }

            public bool TestIsSelEventSuspected(TestSelRecord selRecord)
            {
                if (selRecord == null) return false;

                var record = new SelRecord
                {
                    RecordId = selRecord.RecordId,
                    Timestamp = selRecord.Timestamp,
                    Severity = selRecord.Severity,
                    Source = selRecord.Source,
                    EventType = selRecord.EventType,
                    Sensor = selRecord.Sensor,
                    EventDetail = selRecord.EventDetail,
                    IsHardwareError = selRecord.IsHardwareError
                };

                return base.IsSelEventSuspected(record);
            }

            public bool TestIsSelEventWarning(TestSelRecord selRecord)
            {
                if (selRecord == null) return false;

                var record = new SelRecord
                {
                    RecordId = selRecord.RecordId,
                    Timestamp = selRecord.Timestamp,
                    Severity = selRecord.Severity,
                    Source = selRecord.Source,
                    EventType = selRecord.EventType,
                    Sensor = selRecord.Sensor,
                    EventDetail = selRecord.EventDetail,
                    IsHardwareError = selRecord.IsHardwareError
                };

                return base.IsSelEventWarning(record);
            }

            public (IList<string> suspectedEvents, IList<string> warningEvents) TestAnalyzeSuspectedSelEvents(string results)
            {
                return base.AnalyzeSuspectedSelEvents(results);
            }

            public int TestCountSelRecords(string results)
            {
                return base.CountSelRecords(results);
            }

            public bool TestDetermineIfHardwareError(string severity, string eventDetail)
            {
                return base.DetermineIfHardwareError(severity, eventDetail);
            }

            public Task TestCaptureSuspectedEventsAsync(string results, EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.CaptureSuspectedEventsAsync(results, telemetryContext, cancellationToken);
            }

            public new string DecoderExePath
            {
                get
                {
                    return base.DecoderExePath;
                }

                set
                {
                    base.DecoderExePath = value;
                }
            }

            public Task TestInitializeDecoderAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeDecoderAsync(telemetryContext, cancellationToken);
            }

            public Task TestExecuteSherlockDecodingAsync(string selRecords, EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteSherlockDecodingAsync(selRecords, telemetryContext, cancellationToken);
            }

            public Task TestExecuteGenericDecodingAsync(string selRecords, EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteGenericDecodingAsync(selRecords, telemetryContext, cancellationToken);
            }
        }

        private class TestIpmiUtilSelMonitor2 : IpmiUtilSelMonitor
        {
            public TestIpmiUtilSelMonitor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
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

            public new void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
            {
                base.CaptureMetrics(results, commandArguments, startTime, endTime, telemetryContext);
            }
        }
    }
}
