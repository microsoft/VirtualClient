// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Core.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class PerformanceTrackerTests
    {
        private static Random randomGen = new Random();
        private PerformanceTracker tracker;
        private Mock<IPerformanceMetric> mockMetric1;
        private Mock<IPerformanceMetric> mockMetric2;
        private Mock<ILogger> mockLogger;

        [SetUp]
        public void SetupTest()
        {
            this.mockMetric1 = new Mock<IPerformanceMetric>();
            this.mockMetric2 = new Mock<IPerformanceMetric>();
            this.mockLogger = new Mock<ILogger>();
            this.tracker = new PerformanceTracker(this.mockLogger.Object);
        }

        [Test]
        public void PerformanceTrackerConstructorsSetPropertiesToExpectedValues()
        {
            this.tracker = new PerformanceTracker(null);
            Assert.IsTrue(object.ReferenceEquals(this.tracker.Logger, NullLogger.Instance));

            this.tracker = new PerformanceTracker(this.mockLogger.Object);
            Assert.IsTrue(object.ReferenceEquals(this.tracker.Logger, this.mockLogger.Object));
        }

        [Test]
        public void PerformanceTrackerCapturesMetricsAsExpectedOnAnInterval()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                int captureCount = 0;
                this.mockMetric1.Setup(metric => metric.Capture())
                    .Callback(() =>
                    {
                        captureCount++;
                        if (captureCount > 1)
                        {
                            tokenSource.Cancel();
                        }
                    });

                this.tracker.Add(this.mockMetric1.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10), cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult();
                Assert.IsTrue(captureCount == 2);
            }
        }

        [Test]
        public void PerformanceTrackerCapturesMetricsFromAllPerformanceMetricProviders()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                this.mockMetric2.Setup(metric => metric.Capture())
                    .Callback(() => tokenSource.Cancel());

                this.tracker.Add(this.mockMetric1.Object);
                this.tracker.Add(this.mockMetric2.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10), cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult();

                this.mockMetric1.Verify(m => m.Capture());
                this.mockMetric2.Verify(m => m.Capture());
            }
        }

        [Test]
        public void PerformanceTrackerHandlesExceptionsThrownByIndividualMetricsProvidersOnCapture()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                this.mockMetric1.Setup(m => m.Capture())
                    .Throws(new Exception());

                this.mockMetric2.Setup(metric => metric.Capture())
                    .Callback(() => tokenSource.Cancel());

                this.tracker.Add(this.mockMetric1.Object);
                this.tracker.Add(this.mockMetric2.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10), cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Assert.DoesNotThrow(() => Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult());
            }
        }

        [Test]
        public void PerformanceTrackerLogsExceptionsThrownByIndividualMetricsProvidersOnCapture()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                this.mockMetric1.Setup(m => m.Capture())
                    .Callback(() => tokenSource.Cancel())
                    .Throws(new Exception());

                this.tracker.Add(this.mockMetric1.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10), cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult();

                this.mockLogger.Verify(logger => logger.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), null));
            }
        }

        [Test]
        public void PerformanceTrackerGetsSnapshotsAsExpectedOnAnInterval()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                int resultSnapshotCount = 0;
                this.mockMetric1.Setup(metric => metric.Snapshot())
                    .Callback(() =>
                    {
                        resultSnapshotCount++;
                        if (resultSnapshotCount > 1)
                        {
                            tokenSource.Cancel();
                        }
                    })
                    .Returns(new Metric("Any", 123));

                this.tracker.Add(this.mockMetric1.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.FromSeconds(10), TimeSpan.Zero, cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult();
                Assert.IsTrue(resultSnapshotCount == 2);
            }
        }

        [Test]
        public void PerformanceTrackerGetsSnapshotsFromAllPerformanceMetricProviders()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                this.mockMetric2.Setup(metric => metric.Snapshot())
                    .Callback(() => tokenSource.Cancel());

                this.tracker.Add(this.mockMetric1.Object);
                this.tracker.Add(this.mockMetric2.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.FromSeconds(10), TimeSpan.Zero, cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult();

                this.mockMetric1.Verify(m => m.Snapshot());
                this.mockMetric2.Verify(m => m.Snapshot());
            }
        }

        [Test]
        public void PerformanceTrackerHandlesExceptionsThrownByIndividualMetricsProvidersOnSnapshot()
        {
            using (CancellationTokenSource tokenSource = new CancellationTokenSource())
            {
                CancellationToken cancellationToken = tokenSource.Token;

                this.mockMetric1.Setup(m => m.Snapshot())
                    .Throws(new Exception());

                this.mockMetric2.Setup(metric => metric.Snapshot())
                    .Callback(() => tokenSource.Cancel())
                    .Returns(new Metric("Any", 123));

                this.tracker.Add(this.mockMetric1.Object);
                this.tracker.Add(this.mockMetric2.Object);
                Task trackingTask = this.tracker.BeginTrackingAsync(TimeSpan.Zero, TimeSpan.FromSeconds(10), cancellationToken);
                Task timeout = Task.Run(() =>
                {
                    Task.Delay(1000).GetAwaiter().GetResult();
                    tokenSource.Cancel();
                });

                Assert.DoesNotThrow(() => Task.WhenAny(trackingTask, timeout).GetAwaiter().GetResult());
            }
        }
    }
}