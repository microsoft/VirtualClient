// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common.Extensions;

    [TestFixture]
    [Category("Unit")]
    public class EventContextTests
    {
        private Guid exampleActivityId;
        private Guid exampleParentActivityId;
        private string exampleUserIdentity;

        [SetUp]
        public void InitializeTests()
        {
            this.exampleActivityId = Guid.NewGuid();
            this.exampleParentActivityId = Guid.NewGuid();
            this.exampleUserIdentity = "AnyUser";
        }

        // Note on Ordered Tests:
        // Some of the tests in the class have an [Order] attribute placed upon them. These
        // tests are validating the behaviors of persisted correlation IDs. Correlation IDs
        // by default are persisted to thread-local storage. Were the tests to be run in
        // parallel, this would cause the tests to affect the fidelity of one another. To ensure
        // that we can validate the behaviors successfully, the tests are forced to run in
        // a specific order.

        [Test]
        public void EventContextConstructor1SetsPropertiesToExpectedValues()
        {
            EventContext context = new EventContext(this.exampleActivityId);
            Assert.AreEqual(this.exampleActivityId, context.ActivityId, $"{nameof(context.ActivityId)} property does not match.");
            Assert.AreEqual(Guid.Empty, context.ParentActivityId, $"{nameof(context.ParentActivityId)} property does not match.");
            Assert.AreEqual(null, context.UserIdentity, $"{nameof(context.UserIdentity)} property does not match.");
        }

        [Test]
        public void EventContextConstructor2SetsPropertiesToExpectedValues()
        {
            EventContext context = new EventContext(this.exampleActivityId, this.exampleParentActivityId);
            Assert.AreEqual(this.exampleActivityId, context.ActivityId, $"{nameof(context.ActivityId)} property does not match.");
            Assert.AreEqual(this.exampleParentActivityId, context.ParentActivityId, $"{nameof(context.ParentActivityId)} property does not match.");
            Assert.AreEqual(null, context.UserIdentity, $"{nameof(context.UserIdentity)} property does not match.");
        }

        [Test]
        public void EventContextConstructor3SetsPropertiesToExpectedValues()
        {
            EventContext context = new EventContext(this.exampleActivityId, this.exampleUserIdentity);
            Assert.AreEqual(this.exampleActivityId, context.ActivityId, $"{nameof(context.ActivityId)} property does not match.");
            Assert.AreEqual(Guid.Empty, context.ParentActivityId, $"{nameof(context.ParentActivityId)} property does not match.");
            Assert.AreEqual(this.exampleUserIdentity, context.UserIdentity, $"{nameof(context.UserIdentity)} property does not match.");
        }

        [Test]
        public void EventContextConstructor4SetsPropertiesToExpectedValues()
        {
            EventContext context = new EventContext(this.exampleActivityId, this.exampleParentActivityId, this.exampleUserIdentity);
            Assert.AreEqual(this.exampleActivityId, context.ActivityId, $"{nameof(context.ActivityId)} property does not match.");
            Assert.AreEqual(this.exampleParentActivityId, context.ParentActivityId, $"{nameof(context.ParentActivityId)} property does not match.");
            Assert.AreEqual(this.exampleUserIdentity, context.UserIdentity, $"{nameof(context.UserIdentity)} property does not match.");
        }

        [Test]
        public void EventContextTimestampsAreConsecutive()
        {
            DateTime contextTimestamp = DateTime.UtcNow;
            DateTime lastTimestamp = DateTime.UtcNow;
            for (int i = 0; i < 5; i++)
            {
                // The thread sleeps are to ensure we remove any timing race condition that would
                // otherwise cause the date/time comparisons to fail.
                Thread.Sleep(1);
                contextTimestamp = EventContext.Timestamp;
                Thread.Sleep(1);
                Assert.IsTrue(contextTimestamp > lastTimestamp);
                lastTimestamp = contextTimestamp;
            }
        }

        [Test]
        public void EventContextTimestampsAreConsecutiveWhenSynchronized()
        {
            DateTime contextTimestamp = DateTime.UtcNow;
            DateTime lastTimestamp = DateTime.UtcNow;
            Thread.Sleep(1);

            EventContext.SynchronizeTimestamps(DateTime.UtcNow);

            try
            {
                for (int i = 0; i < 50; i++)
                {
                    Thread.Sleep(1);
                    contextTimestamp = EventContext.Timestamp;
                    Assert.IsTrue(contextTimestamp > lastTimestamp);
                    lastTimestamp = contextTimestamp;
                }
            }
            finally
            {
                EventContext.StopTimestampSynchronization();
            }
        }

        [Test]
        public void EventContextTimestampsAreSubsequentToTheReferenceTimeWhenSynchronized()
        {
            DateTime referenceTime = DateTime.UtcNow;
            EventContext.SynchronizeTimestamps(referenceTime);

            try
            {
                DateTime contextTimestamp = referenceTime;
                DateTime lastTimestamp = referenceTime;

                for (int i = 0; i < 50; i++)
                {
                    Thread.Sleep(1);
                    contextTimestamp = EventContext.Timestamp;
                    Assert.IsTrue(contextTimestamp > referenceTime);
                    Assert.IsTrue(contextTimestamp > lastTimestamp);
                    lastTimestamp = contextTimestamp;
                }
            }
            finally
            {
                EventContext.StopTimestampSynchronization();
            }
        }

        [Test]
        public void EventContextTimestampSynchronizationIsRelevantToTheReferenceTimestampProvided()
        {
            DateTime now = DateTime.UtcNow;
            DateTime referenceTime = DateTime.UtcNow.AddHours(2);
            EventContext.SynchronizeTimestamps(referenceTime);

            try
            {
                Assert.IsTrue(EventContext.Timestamp > now);
                Assert.IsTrue(EventContext.Timestamp > now.AddHours(1));
            }
            finally
            {
                EventContext.StopTimestampSynchronization();
            }
        }

        [Test]
        public void EventContextTimestampsReturnToLocalUtcTimeWhenTimestampSynchronizationIsStopped()
        {
            DateTime now = DateTime.UtcNow;
            DateTime referenceTime = DateTime.UtcNow.AddHours(2);
            EventContext.SynchronizeTimestamps(referenceTime);

            try
            {
                DateTime hourAhead = now.AddHours(1);

                // While time is synchronized against the time in the future, the timestamps are still
                // ahead of the hour.
                Assert.IsTrue(EventContext.Timestamp > hourAhead);

                // Once timestamp synchronization is turned off, context timestamps return
                // to local UTC time (e.g. DateTime.UtcNow).
                EventContext.StopTimestampSynchronization();
                Assert.IsFalse(EventContext.Timestamp > hourAhead);
            }
            finally
            {
                EventContext.StopTimestampSynchronization();
            }
        }

        [Test]
        [Order(1)]
        public void EventContextCreatesTheExpectedEventContextFromPersistedInformation()
        {
            // Activity ID + User
            EventContext context = EventContext.Persist(this.exampleActivityId, userIdentity: this.exampleUserIdentity);
            EventContext persistedContext = EventContext.Persisted();

            Assert.AreEqual(this.exampleActivityId, persistedContext.ActivityId, "Activity ID does not match expected.");
            Assert.AreEqual(this.exampleUserIdentity, persistedContext.UserIdentity, "User identity does not match expected.");
            Assert.AreEqual(context.ActivityId, persistedContext.ActivityId, "Persisted activity ID does not match expected.");
            Assert.AreEqual(Guid.Empty, context.ParentActivityId);

            // With a Parent Activity ID
            context = EventContext.Persist(this.exampleActivityId, this.exampleParentActivityId, this.exampleUserIdentity);
            persistedContext = EventContext.Persisted();

            Assert.AreEqual(this.exampleActivityId, persistedContext.ActivityId, "Activity ID does not match expected.");
            Assert.AreEqual(this.exampleParentActivityId, persistedContext.ParentActivityId, "Parent activity ID does not match expected.");
            Assert.AreEqual(this.exampleUserIdentity, persistedContext.UserIdentity, "User identity does not match expected.");
            Assert.AreEqual(context.ActivityId, persistedContext.ActivityId, "Persisted activity ID does not match expected.");
            Assert.AreEqual(context.ParentActivityId, persistedContext.ParentActivityId, "Persisted parent activity ID does not match expected.");
        }

        [Test]
        [Order(2)]
        public void EventContextCreatesTheExpectedEventContextFromPersistedInformationWhenContextPropertiesAreProvided()
        {
            Dictionary<string, object> expectedContextProperties = new Dictionary<string, object>
            {
                { "property1", 1234 },
                { "property2", "abcd" }
            };

            EventContext.Persist(this.exampleActivityId, userIdentity: this.exampleUserIdentity);
            EventContext persistedContext = EventContext.Persisted(expectedContextProperties);

            Assert.AreEqual(this.exampleActivityId, persistedContext.ActivityId, "Activity ID does not match expected.");
            Assert.AreEqual(this.exampleUserIdentity, persistedContext.UserIdentity, "User identity does not match expected.");
            CollectionAssert.AreEquivalent(expectedContextProperties.ToArray(), persistedContext.Properties.ToArray(), "Context properties do not match expected");
        }

        [Test]
        [Order(3)]
        public void EventContextPersistsParentOrRelatedActivityIdsAssociatedWithACorrelationIdAsExpected()
        {
            Guid correlationId = Guid.NewGuid();

            // Persist an activity ID to mimic a parent activity
            EventContext.Persist(this.exampleParentActivityId, userIdentity: this.exampleUserIdentity);
            EventContext.PersistCorrelation(this.exampleParentActivityId, correlationId);

            // Persist an activity ID to mimic a downstream/child activity
            EventContext.Persist(this.exampleActivityId, userIdentity: this.exampleUserIdentity);
            EventContext persistedContext = EventContext.Persisted(correlationId);

            Assert.AreEqual(this.exampleActivityId, persistedContext.ActivityId, "Activity ID does not match expected.");
            Assert.AreEqual(this.exampleUserIdentity, persistedContext.UserIdentity, "User identity does not match expected.");
            Assert.AreEqual(this.exampleParentActivityId, persistedContext.ParentActivityId, "Related activity ID does not match expected.");
        }

        [Test]
        [Order(4)]
        public void EventContextCorrelationIdentifiersAreConsistentlyTrackedAcrossEventsThatOccurOnASingleThread()
        {
            int iterations = 20;
            Guid expectedActivityId = Guid.NewGuid();
            Guid expectedParentActivityId = Guid.NewGuid();
            Guid expectedClientRequestId = Guid.NewGuid();
            string expectedUserIdentity = "SomeUserInTime";

            // Set global tracking/correlation IDs
            EventContext.Persist(expectedActivityId, expectedParentActivityId, expectedUserIdentity);
            ConcurrentBag<Guid> activityIds = new ConcurrentBag<Guid>();
            ConcurrentBag<Guid> parentActivityIds = new ConcurrentBag<Guid>();

            for (int i = 0; i < iterations; i++)
            {
                EventContext persistedContext = EventContext.Persisted();
                Assert.AreEqual(expectedActivityId, persistedContext.ActivityId, $"Activity ID not tracked successfully.");
                Assert.AreEqual(expectedParentActivityId, persistedContext.ParentActivityId, $"Parent activity ID not tracked successfully.");
                Assert.AreEqual(expectedUserIdentity, persistedContext.UserIdentity, $"User identity not tracked successfully.");

                activityIds.Add(persistedContext.ActivityId);
                parentActivityIds.Add(persistedContext.ParentActivityId);
            }

            Assert.IsFalse(activityIds.Except(new Guid[] { expectedActivityId }).Any());
            Assert.IsFalse(parentActivityIds.Except(new Guid[] { expectedParentActivityId }).Any());
        }

        [Test]
        [Order(5)]
        public void EventContextCorrelationIdentifiersAreConsistentlyTrackedAcrossEventsThatOccurOnMultipleThreads()
        {
            int iterations = 20;
            Guid expectedActivityId = Guid.NewGuid();
            Guid expectedParentActivityId = Guid.NewGuid();
            Guid expectedClientRequestId = Guid.NewGuid();
            string expectedUserIdentity = "SomeUserInTime";

            // Set global tracking/correlation IDs
            EventContext.Persist(expectedActivityId, expectedParentActivityId, expectedUserIdentity);
            ConcurrentBag<Guid> activityIds = new ConcurrentBag<Guid>();
            ConcurrentBag<Guid> parentActivityIds = new ConcurrentBag<Guid>();

            Parallel.For(
                0,
                iterations,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                (i) =>
                {
                    EventContext persistedContext = EventContext.Persisted();
                    Assert.AreEqual(expectedActivityId, persistedContext.ActivityId, $"Activity ID not tracked successfully.");
                    Assert.AreEqual(expectedParentActivityId, persistedContext.ParentActivityId, $"Parent activity ID not tracked successfully.");
                    Assert.AreEqual(expectedUserIdentity, persistedContext.UserIdentity, $"User identity not tracked successfully.");

                    activityIds.Add(persistedContext.ActivityId);
                    parentActivityIds.Add(persistedContext.ParentActivityId);
                });

            Assert.IsFalse(activityIds.Except(new Guid[] { expectedActivityId }).Any());
            Assert.IsFalse(parentActivityIds.Except(new Guid[] { expectedParentActivityId }).Any());
        }

        [Test]
        [Order(6)]
        public void EventContextParentCorrelationIdentifiersAreConsistentlyTrackedAcrossEventsThatOccurOnMultipleThreads()
        {
            int iterations = 20;
            Guid correlationId = Guid.NewGuid();
            Guid parentActivityId = Guid.NewGuid();
            string expectedUserIdentity = "SomeUserInTime";

            // Set global tracking/correlation IDs
            EventContext.PersistCorrelation(parentActivityId, correlationId);

            Parallel.For(
                0,
                iterations,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                (i) =>
                {
                    Guid expectedActivityId = Guid.NewGuid();
                    EventContext.Persist(expectedActivityId, userIdentity: expectedUserIdentity);
                    EventContext persistedContext = EventContext.Persisted(correlationId);

                    Assert.AreEqual(expectedActivityId, persistedContext.ActivityId, $"Activity ID not tracked successfully.");
                    Assert.AreEqual(parentActivityId, persistedContext.ParentActivityId, $"Parent activity ID not tracked successfully.");
                    Assert.AreEqual(expectedUserIdentity, persistedContext.UserIdentity, $"User identity not tracked successfully.");
                });
        }

        [Test]
        [Order(7)]
        public void EventContextPersistentPropertiesAreIncludedWithAllEvents()
        {
            Dictionary<string, object> expectedProperties = new Dictionary<string, object>
            {
                { "Property1", "Any Value" },
                { "Property2", 1234 },
                { "Property3", true }
            };

            EventContext.PersistentProperties.AddRange(expectedProperties);
            EventContext context1 = new EventContext(Guid.NewGuid());

            Assert.IsTrue(context1.Properties.Count == expectedProperties.Count);
            CollectionAssert.AreEquivalent(expectedProperties, context1.Properties);

            Dictionary<string, object> additionalProperties = new Dictionary<string, object>
            {
                { "Property4", 5678 },
                { "Property5", false }
            };

            EventContext context2 = new EventContext(Guid.NewGuid(), additionalProperties);

            Assert.IsTrue(context2.Properties.Count == expectedProperties.Count + additionalProperties.Count);
            CollectionAssert.AreEquivalent(expectedProperties.Union(additionalProperties), context2.Properties);
        }
    }
}
