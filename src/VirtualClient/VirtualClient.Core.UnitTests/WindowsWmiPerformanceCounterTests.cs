// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class WindowsWmiPerformanceCounterTests
    {
        [Test]
        public void WindowsWmiPerformanceCounterForwardMappingsAreCorrect()
        {
            Assert.AreEqual("PercentProcessorTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Time"));
            Assert.AreEqual("PercentUserTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% User Time"));
            Assert.AreEqual("PercentPrivilegedTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Privileged Time"));
            Assert.AreEqual("PercentIdleTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Idle Time"));
            Assert.AreEqual("PercentInterruptTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Interrupt Time"));
            Assert.AreEqual("PercentDPCTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% DPC Time"));
            Assert.AreEqual("PercentC1Time", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% C1 Time"));
            Assert.AreEqual("PercentC2Time", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% C2 Time"));
            Assert.AreEqual("PercentC3Time", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% C3 Time"));
            Assert.AreEqual("InterruptsPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Interrupts/sec"));
            Assert.AreEqual("DPCsQueuedPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("DPCs Queued/sec"));
            Assert.AreEqual("DPCRate", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("DPC Rate"));
            Assert.AreEqual("C1TransitionsPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("C1 Transitions/sec"));
            Assert.AreEqual("C2TransitionsPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("C2 Transitions/sec"));
            Assert.AreEqual("C3TransitionsPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("C3 Transitions/sec"));
            Assert.AreEqual("PercentProcessorPerformance", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Performance"));
            Assert.AreEqual("PercentProcessorUtility", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Utility"));
            Assert.AreEqual("ProcessorFrequency", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Processor Frequency"));
            Assert.AreEqual("PercentofMaximumFrequency", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% of Maximum Frequency"));

            // Default fallback — strips spaces, replaces % and /
            Assert.AreEqual("SomeCustomCounter", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Some Custom Counter"));
        }

        [Test]
        public void WindowsWmiPerformanceCounterReverseMappingsAreCorrect()
        {
            // All known reverse mappings
            Assert.AreEqual("% Processor Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentProcessorTime"));
            Assert.AreEqual("% User Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentUserTime"));
            Assert.AreEqual("% Privileged Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentPrivilegedTime"));
            Assert.AreEqual("% Idle Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentIdleTime"));
            Assert.AreEqual("% Interrupt Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentInterruptTime"));
            Assert.AreEqual("% DPC Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentDPCTime"));
            Assert.AreEqual("% C1 Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentC1Time"));
            Assert.AreEqual("% C2 Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentC2Time"));
            Assert.AreEqual("% C3 Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentC3Time"));
            Assert.AreEqual("Interrupts/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("InterruptsPersec"));
            Assert.AreEqual("DPCs Queued/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("DPCsQueuedPersec"));
            Assert.AreEqual("DPC Rate", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("DPCRate"));
            Assert.AreEqual("C1 Transitions/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("C1TransitionsPersec"));
            Assert.AreEqual("C2 Transitions/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("C2TransitionsPersec"));
            Assert.AreEqual("C3 Transitions/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("C3TransitionsPersec"));
            Assert.AreEqual("% Processor Performance", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentProcessorPerformance"));
            Assert.AreEqual("% Processor Utility", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentProcessorUtility"));
            Assert.AreEqual("Processor Frequency", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("ProcessorFrequency"));
            Assert.AreEqual("% of Maximum Frequency", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentofMaximumFrequency"));
        }

        [Test]
        public void WindowsWmiPerformanceCounterPascalCaseFallbackMappingsAreCorrect()
        {
            // PascalCase → space-separated (non-Processor categories like Memory, System)
            Assert.AreEqual("Available Bytes", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("AvailableBytes"));
            Assert.AreEqual("Cache Bytes", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("CacheBytes"));
            Assert.AreEqual("Committed Bytes", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("CommittedBytes"));
            Assert.AreEqual("Context Switches/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("ContextSwitchesPersec"));
            Assert.AreEqual("Page Faults/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PageFaultsPersec"));
            Assert.AreEqual("Page Reads/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PageReadsPersec"));
            Assert.AreEqual("Processes", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("Processes"));
            Assert.AreEqual("Threads", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("Threads"));

            // Percent prefix handling
            Assert.AreEqual("% Guest Run Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentGuestRunTime"));
            Assert.AreEqual("% Disk Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentDiskTime"));
        }

        [Test]
        public void WindowsWmiPerformanceCounterGetWmiClassNameReturnsCorrectMappings()
        {
            // Processor
            Assert.AreEqual("Win32_PerfFormattedData_Counters_ProcessorInformation", WindowsWmiPerformanceCounter.GetWmiClassName("Processor"));
            Assert.AreEqual("Win32_PerfFormattedData_Counters_ProcessorInformation", WindowsWmiPerformanceCounter.GetWmiClassName("Processor Information"));

            // Memory, PhysicalDisk, System, IPv4
            Assert.AreEqual("Win32_PerfFormattedData_PerfOS_Memory", WindowsWmiPerformanceCounter.GetWmiClassName("Memory"));
            Assert.AreEqual("Win32_PerfFormattedData_PerfDisk_PhysicalDisk", WindowsWmiPerformanceCounter.GetWmiClassName("PhysicalDisk"));
            Assert.AreEqual("Win32_PerfFormattedData_PerfOS_System", WindowsWmiPerformanceCounter.GetWmiClassName("System"));
            Assert.AreEqual("Win32_PerfFormattedData_Tcpip_IPv4", WindowsWmiPerformanceCounter.GetWmiClassName("IPv4"));

            // Hyper-V
            Assert.AreEqual("Win32_PerfFormattedData_HvStats_HyperVHypervisorLogicalProcessor", WindowsWmiPerformanceCounter.GetWmiClassName("Hyper-V Hypervisor Logical Processor"));
            Assert.AreEqual("Win32_PerfFormattedData_HvStats_HyperVHypervisorRootVirtualProcessor", WindowsWmiPerformanceCounter.GetWmiClassName("Hyper-V Hypervisor Root Virtual Processor"));
            Assert.AreEqual("Win32_PerfFormattedData_HvStats_HyperVHypervisorVirtualProcessor", WindowsWmiPerformanceCounter.GetWmiClassName("Hyper-V Hypervisor Virtual Processor"));

            // Unsupported
            Assert.IsNull(WindowsWmiPerformanceCounter.GetWmiClassName("NonExistentCategory"));
            Assert.IsNull(WindowsWmiPerformanceCounter.GetWmiClassName(null));
        }

        [Test]
        public void WindowsWmiPerformanceCounterGetWmiClassNameIsCaseInsensitive()
        {
            Assert.AreEqual("Win32_PerfFormattedData_PerfOS_Memory", WindowsWmiPerformanceCounter.GetWmiClassName("memory"));
            Assert.AreEqual("Win32_PerfFormattedData_PerfOS_Memory", WindowsWmiPerformanceCounter.GetWmiClassName("MEMORY"));
            Assert.AreEqual("Win32_PerfFormattedData_Counters_ProcessorInformation", WindowsWmiPerformanceCounter.GetWmiClassName("processor"));
        }

        [Test]
        public void WindowsWmiPerformanceCounterConstructorSetsPropertiesCorrectly()
        {
            using (var counter = new WindowsWmiPerformanceCounter("Processor", "% Processor Time", "_Total", CaptureStrategy.Average))
            {
                Assert.AreEqual("Processor", counter.Category);
                Assert.AreEqual("% Processor Time", counter.Name);
                Assert.AreEqual("_Total", counter.InstanceName);
                Assert.AreEqual(CaptureStrategy.Average, counter.Strategy);
                Assert.AreEqual(@"\Processor(_Total)\% Processor Time", counter.MetricName);
                Assert.AreEqual(MetricRelativity.Undefined, counter.MetricRelativity);
                Assert.IsFalse(counter.IsDisabled);
            }
        }

        [Test]
        public void WindowsWmiPerformanceCounterSnapshotWithNoCapturesReturnsNone()
        {
            using (var counter = new WindowsWmiPerformanceCounter("Processor", "% Processor Time", "_Total", CaptureStrategy.Average))
            {
                Metric noData = counter.Snapshot();
                Assert.AreEqual(Metric.None, noData);
            }
        }

        [Test]
        public void WindowsWmiPerformanceCounterQueryAllInstancesReturnsEmptyForUnsupportedCategory()
        {
            Dictionary<string, Dictionary<string, float>> empty = WindowsWmiPerformanceCounter.QueryAllInstances("NonExistentCategory");
            Assert.IsEmpty(empty);
        }
    }
}
