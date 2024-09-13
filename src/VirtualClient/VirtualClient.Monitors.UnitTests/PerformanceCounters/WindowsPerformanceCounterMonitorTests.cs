// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    internal class WindowsPerformanceCounterMonitorTests
    {
        private static readonly List<string> Counters = new List<string>
        {
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% c1 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% c2 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% c3 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% guest run time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% hypervisor run time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% idle time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% of max frequency",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\% total run time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\hardware interrupts/sec",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\inter-processor interrupts sent/sec",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\inter-processor interrupts/sec",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\posted interrupt notifications/sec",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(_total)\total interrupts/sec",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 0)\% c1 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 0)\% c2 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 1)\% c1 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 1)\% c2 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 2)\% c1 time",
            @"hyper-v hypervisor logical processor,\hyper-v hypervisor logical processor(hv lp 2)\% c2 time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% guest idle time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% guest run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% hypervisor run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% remote run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% total core run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\% total run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\cpu wait time per dispatch",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\hardware interrupts/sec",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\hypercalls Cost",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\hypercalls/sec",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\total intercepts cost",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\total intercepts/sec",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(_total)\long spin wait hypercalls/sec",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 0)\% guest idle time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 0)\% guest run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 1)\% guest idle time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 1)\% guest run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 1)\% total core run time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 2)\% guest idle time",
            @"hyper-v hypervisor root virtual processor,\hyper-v hypervisor root virtual processor(root vp 2)\% guest run time",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\hypercalls/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\hypercalls cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\page invalidations/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\page invalidations cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\control register accesses/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\control register accesses cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\io instructions/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\io instructions cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\logical processor migrations/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\hardware interrupts/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\cpu wait time per dispatch",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\posted interrupt notifications/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\posted interrupt scans/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\% total run time",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\% hypervisor run time",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\% guest run time",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\total intercepts/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\total intercepts cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\long spin wait hypercalls/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(_total)\nested page fault intercepts/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\hypercalls/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\hypercalls cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\page invalidations/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\page invalidations cost",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\control register accesses/sec",
            @"hyper-v hypervisor virtual processor,\hyper-v hypervisor virtual processor(ubuntu20.04:hv vp 0)\control register accesses cost",
            @"ipv4,\ipv4\datagrams forwarded/sec",
            @"ipv4,\ipv4\datagrams outbound discarded",
            @"ipv4,\ipv4\datagrams outbound no route",
            @"ipv4,\ipv4\datagrams received address errors",
            @"ipv4,\ipv4\datagrams received delivered/sec",
            @"ipv4,\ipv4\datagrams received discarded",
            @"ipv4,\ipv4\datagrams received header errors",
            @"ipv4,\ipv4\datagrams received unknown protocol",
            @"ipv4,\ipv4\datagrams received/sec",
            @"ipv4,\ipv4\datagrams sent/sec",
            @"ipv4,\ipv4\datagrams/sec",
            @"ipv4,\ipv4\fragment re-assembly failures",
            @"ipv4,\ipv4\fragmentation failures",
            @"ipv4,\ipv4\fragmented datagrams/sec",
            @"ipv4,\ipv4\fragments created/sec",
            @"ipv4,\ipv4\fragments re-assembled/sec",
            @"ipv4,\ipv4\fragments received/sec",
            @"memory,\memory\% committed bytes in use",
            @"memory,\memory\available bytes",
            @"memory,\memory\available kbytes",
            @"memory,\memory\available mbytes",
            @"memory,\memory\cache bytes",
            @"memory,\memory\cache bytes peak",
            @"memory,\memory\cache faults/sec",
            @"memory,\memory\commit limit",
            @"memory,\memory\committed bytes",
            @"memory,\memory\demand zero faults/sec",
            @"memory,\memory\free & zero page list bytes",
            @"memory,\memory\free system page table entries",
            @"memory,\memory\long-term average standby cache lifetime (s)",
            @"memory,\memory\modified page list bytes",
            @"memory,\memory\page faults/sec",
            @"memory,\memory\page reads/sec",
            @"memory,\memory\page writes/sec",
            @"memory,\memory\pages input/sec",
            @"memory,\memory\pages output/sec",
            @"memory,\memory\pages/sec",
            @"memory,\memory\pool nonpaged allocs",
            @"memory,\memory\pool nonpaged bytes",
            @"memory,\memory\pool paged allocs",
            @"memory,\memory\pool paged bytes",
            @"memory,\memory\pool paged resident bytes",
            @"memory,\memory\standby cache core bytes",
            @"memory,\memory\standby cache normal priority bytes",
            @"memory,\memory\standby cache reserve bytes",
            @"memory,\memory\system cache resident bytes",
            @"memory,\memory\system code resident bytes",
            @"memory,\memory\system code total bytes",
            @"memory,\memory\system driver resident bytes",
            @"memory,\memory\system driver total bytes",
            @"memory,\memory\transition faults/sec",
            @"memory,\memory\transition pages repurposed/sec",
            @"memory,\memory\write copies/sec",
            @"physicaldisk,\physicaldisk(_total)\% disk read time",
            @"physicaldisk,\physicaldisk(_total)\% disk time",
            @"physicaldisk,\physicaldisk(_total)\% disk write time",
            @"physicaldisk,\physicaldisk(_total)\% idle time",
            @"physicaldisk,\physicaldisk(_total)\avg. disk bytes/read",
            @"physicaldisk,\physicaldisk(_total)\avg. disk bytes/transfer",
            @"physicaldisk,\physicaldisk(_total)\avg. disk bytes/write",
            @"physicaldisk,\physicaldisk(_total)\avg. disk queue length",
            @"physicaldisk,\physicaldisk(_total)\avg. disk read queue length",
            @"physicaldisk,\physicaldisk(_total)\avg. disk sec/read",
            @"physicaldisk,\physicaldisk(_total)\avg. disk sec/transfer",
            @"physicaldisk,\physicaldisk(_total)\avg. disk sec/write",
            @"physicaldisk,\physicaldisk(_total)\avg. disk write queue length",
            @"physicaldisk,\physicaldisk(_total)\current disk queue length",
            @"physicaldisk,\physicaldisk(_total)\disk bytes/sec",
            @"physicaldisk,\physicaldisk(_total)\disk read bytes/sec",
            @"physicaldisk,\physicaldisk(_total)\disk reads/sec",
            @"physicaldisk,\physicaldisk(_total)\disk transfers/sec",
            @"physicaldisk,\physicaldisk(_total)\disk write bytes/sec",
            @"physicaldisk,\physicaldisk(_total)\disk writes/sec",
            @"physicaldisk,\physicaldisk(_total)\split io/sec",
            @"physicaldisk,\physicaldisk(0 c: s:)\% disk read time",
            @"physicaldisk,\physicaldisk(0 c: s:)\% disk time",
            @"physicaldisk,\physicaldisk(0 c: s:)\% disk write time",
            @"physicaldisk,\physicaldisk(0 c: s:)\% idle time",
            @"processor,\processor(_total)\% c1 time",
            @"processor,\processor(_total)\% c2 time",
            @"processor,\processor(_total)\% c3 time",
            @"processor,\processor(_total)\% dpc time",
            @"processor,\processor(_total)\% idle time",
            @"processor,\processor(_total)\% interrupt time",
            @"processor,\processor(_total)\% privileged time",
            @"processor,\processor(_total)\% processor time",
            @"processor,\processor(_total)\% user time",
            @"processor,\processor(0)\% c1 time",
            @"processor,\processor(0)\% c2 time",
            @"processor,\processor(0)\% c3 time",
            @"processor,\processor(0)\% idle time",
            @"processor,\processor(0)\% interrupt time",
            @"processor,\processor(0)\% privileged time",
            @"processor,\processor(0)\% processor time",
            @"processor,\processor(0)\% user time",
            @"processor,\processor(1)\% c1 time",
            @"processor,\processor(1)\% c2 time",
            @"processor,\processor(1)\% c3 time",
            @"processor,\processor(1)\% idle time",
            @"processor,\processor(1)\% interrupt time",
            @"processor,\processor(1)\% privileged time",
            @"processor,\processor(1)\% processor time",
            @"processor,\processor(1)\% user time",
            @"processor,\processor(10)\% c1 time",
            @"processor,\processor(10)\% c2 time",
            @"processor,\processor(10)\% c3 time",
            @"processor,\processor(10)\% idle time",
            @"processor,\processor(10)\% interrupt time",
            @"processor,\processor(10)\% privileged time",
            @"processor,\processor(10)\% processor time",
            @"processor,\processor(10)\% user time",
            @"system,\system\% registry quota in use",
            @"system,\system\alignment fixups/sec",
            @"system,\system\context switches/sec",
            @"system,\system\exception dispatches/sec",
            @"system,\system\file control bytes/sec",
            @"system,\system\file control operations/sec",
            @"system,\system\file data operations/sec",
            @"system,\system\file read bytes/sec",
            @"system,\system\file read operations/sec",
            @"system,\system\file write bytes/sec",
            @"system,\system\file write operations/sec",
            @"system,\system\floating emulations/sec",
            @"system,\system\processes",
            @"system,\system\processor queue length",
            @"system,\system\system calls/sec",
            @"system,\system\system up time",
            @"system,\system\threads"
        };

        private MockFixture mockFixture;

        [SetUp]
        public void InitializeTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);

            // Normally, there would be some amount of time intervals applied. To ensure the tests run
            // efficiently below, we are removing the time intervals.
            TimeSpan fastInterval = TimeSpan.FromMilliseconds(1);
            this.mockFixture.Parameters[nameof(WindowsPerformanceCounterMonitor.CounterCaptureInterval)] = fastInterval.ToString();
            this.mockFixture.Parameters[nameof(WindowsPerformanceCounterMonitor.CounterDiscoveryInterval)] = fastInterval.ToString();
            this.mockFixture.Parameters[nameof(WindowsPerformanceCounterMonitor.MonitorFrequency)] = fastInterval.ToString();
        }

        [Test]
        public void WindowsPerformanceCounterMonitorThrowsWhenThereAreNoCounterDescriptorsDefined()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
                {
                    MonitorException error = Assert.ThrowsAsync<MonitorException>(() => monitor.InitializeAsync(EventContext.None, CancellationToken.None));
                    Assert.AreEqual(ErrorReason.InvalidProfileDefinition, error.Reason);
                }
            }
        }

        [Test]
        [TestCase("Processor=.")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenDescriptor_1(string counterFilter)
        {
            this.mockFixture.Parameters["Counters"] = counterFilter;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\processor(_total)\% c1 time",
                    @"\processor(_total)\% c2 time",
                    @"\processor(_total)\% c3 time",
                    @"\processor(_total)\% dpc time",
                    @"\processor(_total)\% idle time",
                    @"\processor(_total)\% interrupt time",
                    @"\processor(_total)\% privileged time",
                    @"\processor(_total)\% processor time",
                    @"\processor(_total)\% user time",
                    @"\processor(0)\% c1 time",
                    @"\processor(0)\% c2 time",
                    @"\processor(0)\% c3 time",
                    @"\processor(0)\% idle time",
                    @"\processor(0)\% interrupt time",
                    @"\processor(0)\% privileged time",
                    @"\processor(0)\% processor time",
                    @"\processor(0)\% user time",
                    @"\processor(1)\% c1 time",
                    @"\processor(1)\% c2 time",
                    @"\processor(1)\% c3 time",
                    @"\processor(1)\% idle time",
                    @"\processor(1)\% interrupt time",
                    @"\processor(1)\% privileged time",
                    @"\processor(1)\% processor time",
                    @"\processor(1)\% user time",
                    @"\processor(10)\% c1 time",
                    @"\processor(10)\% c2 time",
                    @"\processor(10)\% c3 time",
                    @"\processor(10)\% idle time",
                    @"\processor(10)\% interrupt time",
                    @"\processor(10)\% privileged time",
                    @"\processor(10)\% processor time",
                    @"\processor(10)\% user time"
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        [TestCase("Processor=\\(_Total\\)")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenDescriptor_2(string counterFilter)
        {
            this.mockFixture.Parameters["Counters1"] = counterFilter;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\processor(_total)\% c1 time",
                    @"\processor(_total)\% c2 time",
                    @"\processor(_total)\% c3 time",
                    @"\processor(_total)\% dpc time",
                    @"\processor(_total)\% idle time",
                    @"\processor(_total)\% interrupt time",
                    @"\processor(_total)\% privileged time",
                    @"\processor(_total)\% processor time",
                    @"\processor(_total)\% user time",
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }


        [Test]
        [TestCase("Processor=\\([0-9]+\\)\\\\% (C[0-9]+|Idle|Interrupt|Privileged|Processor|User) Time")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenDescriptor_3(string counterFilter)
        {
            this.mockFixture.Parameters["Counters1"] = counterFilter;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\processor(0)\% c1 time",
                    @"\processor(0)\% c2 time",
                    @"\processor(0)\% c3 time",
                    @"\processor(0)\% idle time",
                    @"\processor(0)\% interrupt time",
                    @"\processor(0)\% privileged time",
                    @"\processor(0)\% processor time",
                    @"\processor(0)\% user time",
                    @"\processor(1)\% c1 time",
                    @"\processor(1)\% c2 time",
                    @"\processor(1)\% c3 time",
                    @"\processor(1)\% idle time",
                    @"\processor(1)\% interrupt time",
                    @"\processor(1)\% privileged time",
                    @"\processor(1)\% processor time",
                    @"\processor(1)\% user time",
                    @"\processor(10)\% c1 time",
                    @"\processor(10)\% c2 time",
                    @"\processor(10)\% c3 time",
                    @"\processor(10)\% idle time",
                    @"\processor(10)\% interrupt time",
                    @"\processor(10)\% privileged time",
                    @"\processor(10)\% processor time",
                    @"\processor(10)\% user time"
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        [TestCase("IPv4=datagrams")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenDescriptor_4(string counterFilter)
        {
            this.mockFixture.Parameters["Counters1"] = counterFilter;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\ipv4\datagrams forwarded/sec",
                    @"\ipv4\datagrams outbound discarded",
                    @"\ipv4\datagrams outbound no route",
                    @"\ipv4\datagrams received address errors",
                    @"\ipv4\datagrams received delivered/sec",
                    @"\ipv4\datagrams received discarded",
                    @"\ipv4\datagrams received header errors",
                    @"\ipv4\datagrams received unknown protocol",
                    @"\ipv4\datagrams received/sec",
                    @"\ipv4\datagrams sent/sec",
                    @"\ipv4\datagrams/sec",
                    @"\ipv4\fragmented datagrams/sec"
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        [TestCase("Hyper-V Hypervisor Logical Processor=\\(_Total\\)")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenDescriptor_5(string counterFilter)
        {
            this.mockFixture.Parameters["Counters1"] = counterFilter;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\hyper-v hypervisor logical processor(_total)\% c1 time",
                    @"\hyper-v hypervisor logical processor(_total)\% c2 time",
                    @"\hyper-v hypervisor logical processor(_total)\% c3 time",
                    @"\hyper-v hypervisor logical processor(_total)\% guest run time",
                    @"\hyper-v hypervisor logical processor(_total)\% hypervisor run time",
                    @"\hyper-v hypervisor logical processor(_total)\% idle time",
                    @"\hyper-v hypervisor logical processor(_total)\% of max frequency",
                    @"\hyper-v hypervisor logical processor(_total)\% total run time",
                    @"\hyper-v hypervisor logical processor(_total)\hardware interrupts/sec",
                    @"\hyper-v hypervisor logical processor(_total)\inter-processor interrupts sent/sec",
                    @"\hyper-v hypervisor logical processor(_total)\inter-processor interrupts/sec",
                    @"\hyper-v hypervisor logical processor(_total)\posted interrupt notifications/sec",
                    @"\hyper-v hypervisor logical processor(_total)\total interrupts/sec",
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        [TestCase("Memory=\\\\(page|pages)", "PhysicalDisk=_Total")]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForAGivenSetOfDescriptors_1(string counterFilter1, string counterFilter2)
        {
            this.mockFixture.Parameters["Counters1"] = counterFilter1;
            this.mockFixture.Parameters["Counters2"] = counterFilter2;

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\memory\page faults/sec",
                    @"\memory\page reads/sec",
                    @"\memory\page writes/sec",
                    @"\memory\pages input/sec",
                    @"\memory\pages output/sec",
                    @"\memory\pages/sec",
                    @"\physicaldisk(_total)\% disk read time",
                    @"\physicaldisk(_total)\% disk time",
                    @"\physicaldisk(_total)\% disk write time",
                    @"\physicaldisk(_total)\% idle time",
                    @"\physicaldisk(_total)\avg. disk bytes/read",
                    @"\physicaldisk(_total)\avg. disk bytes/transfer",
                    @"\physicaldisk(_total)\avg. disk bytes/write",
                    @"\physicaldisk(_total)\avg. disk queue length",
                    @"\physicaldisk(_total)\avg. disk read queue length",
                    @"\physicaldisk(_total)\avg. disk sec/read",
                    @"\physicaldisk(_total)\avg. disk sec/transfer",
                    @"\physicaldisk(_total)\avg. disk sec/write",
                    @"\physicaldisk(_total)\avg. disk write queue length",
                    @"\physicaldisk(_total)\current disk queue length",
                    @"\physicaldisk(_total)\disk bytes/sec",
                    @"\physicaldisk(_total)\disk read bytes/sec",
                    @"\physicaldisk(_total)\disk reads/sec",
                    @"\physicaldisk(_total)\disk transfers/sec",
                    @"\physicaldisk(_total)\disk write bytes/sec",
                    @"\physicaldisk(_total)\disk writes/sec",
                    @"\physicaldisk(_total)\split io/sec",
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForTheDefaultMonitorProfile_1()
        {
            this.mockFixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { "Counters01", "IPv4=." },
                { "Counters02", "Memory=(Available|Cache|Committed) Bytes" },
                { "Counters03", "Memory=Faults/sec" },
                { "Counters04", "Memory=(Page Reads/sec|Page Writes/sec|Pages/sec|Pages Input/sec|Pages Output/sec)" },
                { "Counters05", "PhysicalDisk=\\(_Total\\)" },
                { "Counters06", "Processor=\\(_Total\\)" },
                { "Counters07", "Processor=\\([0-9]+\\)\\\\% (C[0-9]+|Idle|Interrupt|Privileged|Processor|User) Time" },
                { "Counters08", "System=." }
            });

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\ipv4\datagrams forwarded/sec",
                    @"\ipv4\datagrams outbound discarded",
                    @"\ipv4\datagrams outbound no route",
                    @"\ipv4\datagrams received address errors",
                    @"\ipv4\datagrams received delivered/sec",
                    @"\ipv4\datagrams received discarded",
                    @"\ipv4\datagrams received header errors",
                    @"\ipv4\datagrams received unknown protocol",
                    @"\ipv4\datagrams received/sec",
                    @"\ipv4\datagrams sent/sec",
                    @"\ipv4\datagrams/sec",
                    @"\ipv4\fragment re-assembly failures",
                    @"\ipv4\fragmentation failures",
                    @"\ipv4\fragmented datagrams/sec",
                    @"\ipv4\fragments created/sec",
                    @"\ipv4\fragments re-assembled/sec",
                    @"\ipv4\fragments received/sec",
                    @"\memory\% committed bytes in use",
                    @"\memory\available bytes",
                    @"\memory\cache bytes",
                    @"\memory\cache bytes peak",
                    @"\memory\cache faults/sec",
                    @"\memory\committed bytes",
                    @"\memory\demand zero faults/sec",
                    @"\memory\page faults/sec",
                    @"\memory\page reads/sec",
                    @"\memory\page writes/sec",
                    @"\memory\pages input/sec",
                    @"\memory\pages output/sec",
                    @"\memory\pages/sec",
                    @"\memory\transition faults/sec",
                    @"\physicaldisk(_total)\% disk read time",
                    @"\physicaldisk(_total)\% disk time",
                    @"\physicaldisk(_total)\% disk write time",
                    @"\physicaldisk(_total)\% idle time",
                    @"\physicaldisk(_total)\avg. disk bytes/read",
                    @"\physicaldisk(_total)\avg. disk bytes/transfer",
                    @"\physicaldisk(_total)\avg. disk bytes/write",
                    @"\physicaldisk(_total)\avg. disk queue length",
                    @"\physicaldisk(_total)\avg. disk read queue length",
                    @"\physicaldisk(_total)\avg. disk sec/read",
                    @"\physicaldisk(_total)\avg. disk sec/transfer",
                    @"\physicaldisk(_total)\avg. disk sec/write",
                    @"\physicaldisk(_total)\avg. disk write queue length",
                    @"\physicaldisk(_total)\current disk queue length",
                    @"\physicaldisk(_total)\disk bytes/sec",
                    @"\physicaldisk(_total)\disk read bytes/sec",
                    @"\physicaldisk(_total)\disk reads/sec",
                    @"\physicaldisk(_total)\disk transfers/sec",
                    @"\physicaldisk(_total)\disk write bytes/sec",
                    @"\physicaldisk(_total)\disk writes/sec",
                    @"\physicaldisk(_total)\split io/sec",
                    @"\processor(_total)\% c1 time",
                    @"\processor(_total)\% c2 time",
                    @"\processor(_total)\% c3 time",
                    @"\processor(_total)\% dpc time",
                    @"\processor(_total)\% idle time",
                    @"\processor(_total)\% interrupt time",
                    @"\processor(_total)\% privileged time",
                    @"\processor(_total)\% processor time",
                    @"\processor(_total)\% user time",
                    @"\processor(0)\% c1 time",
                    @"\processor(0)\% c2 time",
                    @"\processor(0)\% c3 time",
                    @"\processor(0)\% idle time",
                    @"\processor(0)\% interrupt time",
                    @"\processor(0)\% privileged time",
                    @"\processor(0)\% processor time",
                    @"\processor(0)\% user time",
                    @"\processor(1)\% c1 time",
                    @"\processor(1)\% c2 time",
                    @"\processor(1)\% c3 time",
                    @"\processor(1)\% idle time",
                    @"\processor(1)\% interrupt time",
                    @"\processor(1)\% privileged time",
                    @"\processor(1)\% processor time",
                    @"\processor(1)\% user time",
                    @"\processor(10)\% c1 time",
                    @"\processor(10)\% c2 time",
                    @"\processor(10)\% c3 time",
                    @"\processor(10)\% idle time",
                    @"\processor(10)\% interrupt time",
                    @"\processor(10)\% privileged time",
                    @"\processor(10)\% processor time",
                    @"\processor(10)\% user time",
                    @"\system\% registry quota in use",
                    @"\system\alignment fixups/sec",
                    @"\system\context switches/sec",
                    @"\system\exception dispatches/sec",
                    @"\system\file control bytes/sec",
                    @"\system\file control operations/sec",
                    @"\system\file data operations/sec",
                    @"\system\file read bytes/sec",
                    @"\system\file read operations/sec",
                    @"\system\file write bytes/sec",
                    @"\system\file write operations/sec",
                    @"\system\floating emulations/sec",
                    @"\system\processes",
                    @"\system\processor queue length",
                    @"\system\system calls/sec",
                    @"\system\system up time",
                    @"\system\threads"
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorIdentifiesTheExpectedCountersForTheDefaultMonitorProfile_2_HyperV_Counters()
        {
            this.mockFixture.Parameters.AddRange(new Dictionary<string, IConvertible>
            {
                { "Counters01", "Hyper-V Hypervisor Logical Processor=\\(_Total\\)" },
                { "Counters02", "Hyper-V Hypervisor Root Virtual Processor=\\(_Total\\)\\\\% (Guest|Hypervisor|Total) Run Time" },
                { "Counters03", "Hyper-V Hypervisor Root Virtual Processor=\\(_Total\\)\\\\(CPU Wait Time Per Dispatch|Hardware Interrupts/sec|Hypercalls Cost|Hypercalls/sec|Long Spin Wait Hypercalls/sec)" },
                { "Counters04", "Hyper-V Hypervisor Root Virtual Processor=\\(_Total\\)\\\\(Total Intercepts Cost|Total Intercepts/sec)" },
                { "Counters05", "Hyper-V Hypervisor Virtual Processor=\\(_Total\\)\\\\(% (Guest|Hypervisor|Total) Run Time)" },
                { "Counters06", "Hyper-V Hypervisor Virtual Processor=\\(_Total\\)\\\\(CPU Wait Time Per Dispatch|Hardware Interrupts/sec|Hypercalls Cost|Hypercalls/sec|Logical Processor Migrations/sec|Long Spin Wait Hypercalls/sec)" },
                { "Counters07", "Hyper-V Hypervisor Virtual Processor=\\(_Total\\)\\\\(Nested Page Fault Intercepts/sec|Posted Interrupt Notifications/sec|Posted Interrupt Scans/sec|Total Intercepts Cost|Total Intercepts/sec)" },
            });

            using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
            {
                await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                List<string> supportedCounters = new List<string>();
                foreach (string item in WindowsPerformanceCounterMonitorTests.Counters)
                {
                    string[] parts = item.Split(',');
                    string categoryName = parts[0];
                    string counterName = parts[1];

                    if (monitor.IsSupportedCounter(categoryName, counterName))
                    {
                        supportedCounters.Add(counterName);
                    }
                }

                List<string> expectedCounters = new List<string>
                {
                    @"\hyper-v hypervisor logical processor(_total)\% c1 time",
                    @"\hyper-v hypervisor logical processor(_total)\% c2 time",
                    @"\hyper-v hypervisor logical processor(_total)\% c3 time",
                    @"\hyper-v hypervisor logical processor(_total)\% guest run time",
                    @"\hyper-v hypervisor logical processor(_total)\% hypervisor run time",
                    @"\hyper-v hypervisor logical processor(_total)\% idle time",
                    @"\hyper-v hypervisor logical processor(_total)\% of max frequency",
                    @"\hyper-v hypervisor logical processor(_total)\% total run time",
                    @"\hyper-v hypervisor logical processor(_total)\hardware interrupts/sec",
                    @"\hyper-v hypervisor logical processor(_total)\inter-processor interrupts sent/sec",
                    @"\hyper-v hypervisor logical processor(_total)\inter-processor interrupts/sec",
                    @"\hyper-v hypervisor logical processor(_total)\posted interrupt notifications/sec",
                    @"\hyper-v hypervisor logical processor(_total)\total interrupts/sec",
                    @"\hyper-v hypervisor root virtual processor(_total)\% guest run time",
                    @"\hyper-v hypervisor root virtual processor(_total)\% hypervisor run time",
                    @"\hyper-v hypervisor root virtual processor(_total)\% total run time",
                    @"\hyper-v hypervisor root virtual processor(_total)\cpu wait time per dispatch",
                    @"\hyper-v hypervisor root virtual processor(_total)\hardware interrupts/sec",
                    @"\hyper-v hypervisor root virtual processor(_total)\hypercalls Cost",
                    @"\hyper-v hypervisor root virtual processor(_total)\hypercalls/sec",
                    @"\hyper-v hypervisor root virtual processor(_total)\total intercepts cost",
                    @"\hyper-v hypervisor root virtual processor(_total)\total intercepts/sec",
                    @"\hyper-v hypervisor root virtual processor(_total)\long spin wait hypercalls/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\hypercalls/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\hypercalls cost",
                    @"\hyper-v hypervisor virtual processor(_total)\logical processor migrations/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\hardware interrupts/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\cpu wait time per dispatch",
                    @"\hyper-v hypervisor virtual processor(_total)\posted interrupt notifications/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\posted interrupt scans/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\% total run time",
                    @"\hyper-v hypervisor virtual processor(_total)\% hypervisor run time",
                    @"\hyper-v hypervisor virtual processor(_total)\% guest run time",
                    @"\hyper-v hypervisor virtual processor(_total)\total intercepts/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\total intercepts cost",
                    @"\hyper-v hypervisor virtual processor(_total)\long spin wait hypercalls/sec",
                    @"\hyper-v hypervisor virtual processor(_total)\nested page fault intercepts/sec"
                };

                CollectionAssert.AreEquivalent(expectedCounters, supportedCounters);
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorPerformsCounterCaptureOnIntervals()
        {
            this.mockFixture.Parameters["Counters1"] = "Processor=.";

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
                {
                    var counter = new TestWindowsPerformanceCounter("AnyCategory", "AnyCounter", CaptureStrategy.Average);

                    int captureAttempts = 0;
                    counter.OnTryGetCounterValue = () =>
                    {
                        captureAttempts++;
                        if (captureAttempts >= 10)
                        {
                            cancellationSource.Cancel();
                        }

                        return true;
                    };

                    monitor.Counters.Add(@"\AnyCategory\AnyCounter", counter);

                    await monitor.InitializeAsync(EventContext.None, CancellationToken.None);
                    Task discoveryTask = monitor.CaptureCountersAsync(EventContext.None, cancellationSource.Token);

                    // Allow up to 1 min for the task to complete before forcing a timeout.
                    await Task.WhenAny(discoveryTask, Task.Delay(60000));
                    Assert.AreEqual(10, captureAttempts);
                }
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorPerformsCounterDiscoveryOnIntervals()
        {
            this.mockFixture.Parameters["Counters1"] = "Processor=.";

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
                {
                    int discoveryAttempts = 0;
                    monitor.OnLoadCounters = () =>
                    {
                        discoveryAttempts++;
                        if (discoveryAttempts >= 100)
                        {
                            cancellationSource.Cancel();
                        }
                    };

                    await monitor.InitializeAsync(EventContext.None, CancellationToken.None);
                    Task discoveryTask = monitor.DiscoverCountersAsync(EventContext.None, cancellationSource.Token);

                    // Allow up to 1 min for the task to complete before forcing a timeout.
                    await Task.WhenAny(discoveryTask, Task.Delay(60000));
                    Assert.AreEqual(100, discoveryAttempts);
                }
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorPerformsCounterSnapshotsOnIntervals()
        {
            this.mockFixture.Parameters["Counters1"] = "Processor=.";

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
                {
                    var counter = new TestWindowsPerformanceCounter("AnyCategory", "AnyCounter", CaptureStrategy.Average);
                    monitor.Counters.Add(@"\AnyCategory\AnyCounter", counter);

                    int snapshotAttempts = 0;
                    this.mockFixture.Logger.OnLog = (level, eventId, state, exc) =>
                    {
                        if (eventId.Name == "PerformanceCounter")
                        {
                            snapshotAttempts++;
                        }

                        if (snapshotAttempts >= 100)
                        {
                            cancellationSource.Cancel();
                        }
                    };

                    await monitor.InitializeAsync(EventContext.None, CancellationToken.None);

                    Task captureTask = monitor.CaptureCountersAsync(EventContext.None, cancellationSource.Token);
                    Task discoveryTask = monitor.SnapshotCountersAsync(EventContext.None, cancellationSource.Token);

                    // Allow up to 1 min for the task to complete before forcing a timeout.
                    await Task.WhenAny(discoveryTask, Task.Delay(60000));
                    Assert.AreEqual(100, snapshotAttempts);
                }
            }
        }

        [Test]
        public async Task WindowsPerformanceCounterMonitorHandlesNewCountersBeingAddedMidstream()
        {
            this.mockFixture.Parameters["Counters1"] = "Processor=.";

            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                using (var monitor = new TestWindowsPerformanceCounterMonitor(this.mockFixture))
                {
                    var counter = new TestWindowsPerformanceCounter("AnyCategory", "AnyCounter", CaptureStrategy.Average);
                    monitor.Counters.Add(@"\AnyCategory\AnyCounter", counter);

                    bool errorsOccurred = false;
                    int snapshotAttempts = 0;
                    this.mockFixture.Logger.OnLog = (level, eventId, state, exc) =>
                    {
                        if (eventId.Name == "PerformanceCounter")
                        {
                            snapshotAttempts++;
                        }

                        if (level >= LogLevel.Warning)
                        {
                            errorsOccurred = true;
                        }

                        // We want to run the logic hard to try to flesh out any errors.
                        if (snapshotAttempts >= 1000)
                        {
                            cancellationSource.Cancel();
                        }
                    };

                    // Force the counters to be added midstream while counter capture is happening. If any errors
                    // occur they will be logged.
                    monitor.OnLoadCounters = () =>
                    {
                        string counterName = Guid.NewGuid().ToString();
                        var counter = new TestWindowsPerformanceCounter("AnyCategory", counterName, CaptureStrategy.Average);
                        monitor.Counters.Add($@"\AnyCategory\{counterName}", counter);
                    };

                    Task executeTask = monitor.ExecuteAsync(cancellationSource.Token);

                    // Allow up to 10 seconds for the task to complete before forcing a timeout.
                    await Task.WhenAny(executeTask, Task.Delay(10000));
                    Assert.IsFalse(errorsOccurred);
                }
            }
        }

        private class TestWindowsPerformanceCounterMonitor : WindowsPerformanceCounterMonitor
        {
            public TestWindowsPerformanceCounterMonitor(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new IDictionary<string, WindowsPerformanceCounter> Counters
            {
                get
                {
                    return base.Counters;
                }
            }

            public Action OnLoadCounters { get; set; }

            public new Task CaptureCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.CaptureCountersAsync(telemetryContext, cancellationToken);
            }

            public new Task DiscoverCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.DiscoverCountersAsync(telemetryContext, cancellationToken);
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new bool IsSupportedCounter(string categoryName, string counterName)
            {
                return base.IsSupportedCounter(categoryName, counterName);
            }

            public new Task SnapshotCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.SnapshotCountersAsync(telemetryContext, cancellationToken);
            }

            protected override void LoadCounters(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.OnLoadCounters?.Invoke();
            }
        }

        private class TestWindowsPerformanceCounter : WindowsPerformanceCounter
        {
            public TestWindowsPerformanceCounter(string counterCategory, string counterName, CaptureStrategy captureStrategy)
                : base(counterCategory, counterName, captureStrategy)
            {
            }

            public Func<bool> OnTryGetCounterValue { get; set; }

            protected override bool TryGetCounterValue(out float? counterValue)
            {
                counterValue = 1;
                if (this.OnTryGetCounterValue != null)
                {
                    return this.OnTryGetCounterValue.Invoke();
                }
               
                return true;
            }
        }
    }
}
