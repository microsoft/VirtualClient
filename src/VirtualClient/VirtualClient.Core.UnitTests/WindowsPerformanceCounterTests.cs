// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class WindowsPerformanceCounterTests
    {
        private TestWindowsPerformanceCounter performanceCounter;

        [SetUp]
        public void SetupTest()
        {
            this.performanceCounter = new TestWindowsPerformanceCounter("AnyCategory", "AnyName", "AnyInstance", CaptureStrategy.Raw);
        }

        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("   ")]
        public void WindowsPerformanceCounterConstructorsValidatesRequiredParameters(string invalidArgument)
        {
            Assert.Throws<ArgumentException>(() => new WindowsPerformanceCounter(invalidArgument, "AnyName", CaptureStrategy.Raw));
            Assert.Throws<ArgumentException>(() => new WindowsPerformanceCounter("AnyCategory", invalidArgument, CaptureStrategy.Raw));
            Assert.Throws<ArgumentException>(() => new WindowsPerformanceCounter(invalidArgument, "AnyName", "AnyInstance", CaptureStrategy.Raw));
            Assert.Throws<ArgumentException>(() => new WindowsPerformanceCounter("AnyCategory", invalidArgument, "AnyInstance", CaptureStrategy.Raw));
        }

        [Test]
        public void WindowsPerformanceCounterConstructorsSetPropertiesToExpectedValues()
        {
            string expectedCategory = "AnyCategory";
            string expectedName = "AnyName";
            string expectedInstance = "AnyInstance";
            CaptureStrategy expectedStrategy = CaptureStrategy.Average;

            WindowsPerformanceCounter performanceCounter = new WindowsPerformanceCounter(expectedCategory, expectedName, expectedStrategy);
            Assert.AreEqual(expectedCategory, performanceCounter.Category);
            Assert.AreEqual(expectedName, performanceCounter.Name);
            Assert.AreEqual(expectedStrategy, performanceCounter.Strategy);

            performanceCounter = new WindowsPerformanceCounter(expectedCategory, expectedName, expectedInstance, expectedStrategy);
            Assert.AreEqual(expectedCategory, performanceCounter.Category);
            Assert.AreEqual(expectedName, performanceCounter.Name);
            Assert.AreEqual(expectedInstance, performanceCounter.InstanceName);
            Assert.AreEqual(expectedStrategy, performanceCounter.Strategy);
        }

        [Test]
        public void WindowsPerformanceCounterCapturesPerformanceCounterValuesAsExpected_1()
        {
            float expectedValue = 23456;
            this.performanceCounter.OnGetCounterValue = () => expectedValue;
            this.performanceCounter.Capture();

            Assert.IsNotEmpty(this.performanceCounter.CounterValues);
            Assert.AreEqual(expectedValue, this.performanceCounter.CounterValues.First());
        }

        [Test]
        public void WindowsPerformanceCounterCapturesPerformanceCounterValuesAsExpected_2()
        {
            float[] expectedCaptures = new float[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };
            int captureIndex = 0;
            this.performanceCounter.OnGetCounterValue = () =>
            {
                float value = expectedCaptures[captureIndex];
                captureIndex++;
                return value;
            };

            expectedCaptures.ToList().ForEach(v => this.performanceCounter.Capture());

            Assert.IsNotEmpty(this.performanceCounter.CounterValues);
            Assert.AreEqual(expectedCaptures.Count(), this.performanceCounter.CounterValues.Count());
        }

        [Test]
        public void WindowsPerformanceCounterReturnsTheExpectedSnapshotWhenAnAveragingStrategyIsUsed()
        {
            float[] expectedCaptures = new float[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

            int captureIndex = 0;
            this.performanceCounter = new TestWindowsPerformanceCounter("AnyCategory", "AnyName", "AnyInstance", CaptureStrategy.Average);
            this.performanceCounter.OnGetCounterValue = () =>
            {
                float value = expectedCaptures[captureIndex];
                captureIndex++;
                return value;
            };

            expectedCaptures.ToList().ForEach(v => this.performanceCounter.Capture());
            Metric snapshot = this.performanceCounter.Snapshot();

            Assert.AreEqual(expectedCaptures.Sum() / expectedCaptures.Length, snapshot.Value);
        }

        [Test]
        public void WindowsPerformanceCounterReturnsTheExpectedSnapshotWhenARawStrategyIsUsed()
        {
            float[] captures = new float[] { 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000 };

            int captureIndex = 0;
            this.performanceCounter = new TestWindowsPerformanceCounter("AnyCategory", "AnyName", "AnyInstance", CaptureStrategy.Raw);
            this.performanceCounter.OnGetCounterValue = () =>
            {
                float value = captures[captureIndex];
                captureIndex++;
                return value;
            };

            captures.ToList().ForEach(v => this.performanceCounter.Capture());
            Metric snapshot = this.performanceCounter.Snapshot();

            Assert.AreEqual((double)captures.Last(), snapshot.Value);
        }

        private class TestWindowsPerformanceCounter : WindowsPerformanceCounter
        {
            public TestWindowsPerformanceCounter(string category, string name, string instance, CaptureStrategy captureStrategy)
                : base(category, name, instance, captureStrategy)
            {
            }

            public new IEnumerable<float> CounterValues
            {
                get
                {
                    return base.CounterValues;
                }
            }

            public Func<float?> OnGetCounterValue { get; set; }

            protected override bool TryGetCounterValue(out float? value)
            {
                value = null;
                if (this.OnGetCounterValue != null)
                {
                    value = this.OnGetCounterValue.Invoke();
                    return true;
                }
                else
                {
                    value = 100;
                }

                return value != null;
            }
        }
    }
}
