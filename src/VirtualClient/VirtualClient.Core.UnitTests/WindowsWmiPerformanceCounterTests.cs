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
        public void WindowsWmiPerformanceCounterStaticMappingsAreCorrect()
        {
            // Forward mapping — known counters
            Assert.AreEqual("PercentProcessorTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Time"));
            Assert.AreEqual("PercentUserTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% User Time"));
            Assert.AreEqual("PercentPrivilegedTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Privileged Time"));
            Assert.AreEqual("PercentIdleTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Idle Time"));
            Assert.AreEqual("PercentInterruptTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Interrupt Time"));
            Assert.AreEqual("PercentDPCTime", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% DPC Time"));
            Assert.AreEqual("InterruptsPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Interrupts/sec"));
            Assert.AreEqual("DPCsQueuedPersec", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("DPCs Queued/sec"));
            Assert.AreEqual("DPCRate", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("DPC Rate"));
            Assert.AreEqual("PercentProcessorPerformance", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Performance"));
            Assert.AreEqual("PercentProcessorUtility", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% Processor Utility"));
            Assert.AreEqual("ProcessorFrequency", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Processor Frequency"));
            Assert.AreEqual("PercentofMaximumFrequency", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("% of Maximum Frequency"));

            // Forward mapping — default fallback
            Assert.AreEqual("SomeCustomCounter", WindowsWmiPerformanceCounter.MapCounterNameToWmiProperty("Some Custom Counter"));

            // Reverse mapping — known properties
            Assert.AreEqual("% Processor Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentProcessorTime"));
            Assert.AreEqual("% User Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentUserTime"));
            Assert.AreEqual("% Idle Time", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("PercentIdleTime"));
            Assert.AreEqual("Interrupts/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("InterruptsPersec"));
            Assert.AreEqual("DPCs Queued/sec", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("DPCsQueuedPersec"));

            // Reverse mapping — default passthrough
            Assert.AreEqual("UnknownProp", WindowsWmiPerformanceCounter.MapWmiPropertyToCounterName("UnknownProp"));

            // GetWmiClassName — supported categories
            Assert.AreEqual("Win32_PerfFormattedData_Counters_ProcessorInformation", WindowsWmiPerformanceCounter.GetWmiClassName("Processor"));
            Assert.AreEqual("Win32_PerfFormattedData_Counters_ProcessorInformation", WindowsWmiPerformanceCounter.GetWmiClassName("Processor Information"));

            // GetWmiClassName — unsupported categories return null
            Assert.IsNull(WindowsWmiPerformanceCounter.GetWmiClassName("Memory"));
            Assert.IsNull(WindowsWmiPerformanceCounter.GetWmiClassName("PhysicalDisk"));
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

                // Snapshot with no captures returns Metric.None
                Metric noData = counter.Snapshot();
                Assert.AreEqual(Metric.None, noData);

                // QueryAllInstances with unsupported category returns empty
                Dictionary<string, Dictionary<string, float>> empty = WindowsWmiPerformanceCounter.QueryAllInstances("Memory");
                Assert.IsEmpty(empty);
            }
        }
    }
}
