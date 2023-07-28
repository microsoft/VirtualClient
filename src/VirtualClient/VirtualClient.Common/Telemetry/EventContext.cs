// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information that can be used to correlate activities with telemetry
    /// events.
    /// </summary>
    public class EventContext
    {
        private static IPersistenceManager defaultPersistenceManager = new CallContextPersistenceManager();
        private static IPersistenceManager persistenceManager;

        private static DateTime? synchronizationReferenceTime;
        private static Stopwatch synchronizationStopwatch = new Stopwatch();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventContext"/> class.
        /// </summary>
        /// <param name="activityId">The activity ID of the event.</param>
        /// <param name="contextProperties">The event context property object (e.g. anonymous type key/value pair object).</param>
        public EventContext(Guid activityId, IEnumerable<KeyValuePair<string, object>> contextProperties = null)
            : this(activityId, Guid.Empty, null, contextProperties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventContext"/> class.
        /// </summary>
        /// <param name="activityId">The activity ID of the event.</param>
        /// <param name="userIdentity">The user identity</param>
        /// <param name="contextProperties">The event context property object (e.g. anonymous type key/value pair object).</param>
        public EventContext(Guid activityId, string userIdentity, IEnumerable<KeyValuePair<string, object>> contextProperties = null)
            : this(activityId, Guid.Empty, userIdentity, contextProperties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventContext"/> class.
        /// </summary>
        /// <param name="activityId">The activity ID of the event.</param>
        /// <param name="parentActivityId">The activity ID of the parent event/operation that preceded the event.</param>
        /// <param name="contextProperties">The event context property object (e.g. anonymous type key/value pair object).</param>
        public EventContext(Guid activityId, Guid parentActivityId, IEnumerable<KeyValuePair<string, object>> contextProperties = null)
            : this(activityId, parentActivityId, null, contextProperties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventContext"/> class.
        /// </summary>
        /// <param name="activityId">The activity ID of the event.</param>
        /// <param name="parentActivityId">The activity ID of the parent event/operation that preceded the event.</param>
        /// <param name="userIdentity">The user identity</param>
        /// <param name="contextProperties">The event context property object (e.g. anonymous type key/value pair object).</param>
        public EventContext(Guid activityId, Guid parentActivityId, string userIdentity, IEnumerable<KeyValuePair<string, object>> contextProperties = null)
        {
            this.ActivityId = activityId;
            this.ParentActivityId = parentActivityId;
            this.UserIdentity = userIdentity;
            this.TransactionId = Guid.NewGuid();
            this.Properties = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (EventContext.PersistentProperties.Any())
            {
                this.Properties.AddRange(EventContext.PersistentProperties);
            }

            if (contextProperties != null)
            {
                this.Properties.AddRange(contextProperties);
            }
        }

        /// <summary>
        /// No event context.
        /// </summary>
        public static EventContext None => new EventContext(Guid.Empty, Guid.Empty, null);

        /// <summary>
        /// Gets the set of global properties that should be included with all
        /// telemetry events.
        /// </summary>
        public static IDictionary<string, object> PersistentProperties { get; } = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets the persistence manager used to track (and relate) event
        /// correlation identifiers.
        /// </summary>
        public static IPersistenceManager Persistence
        {
            get
            {
                return EventContext.persistenceManager ?? EventContext.defaultPersistenceManager;
            }

            set
            {
                EventContext.persistenceManager = value;
            }
        }

        /// <summary>
        /// Gets a timestamp synchronized against an reference timestamp.
        /// </summary>
        public static DateTime Timestamp
        {
            get
            {
                if (EventContext.synchronizationReferenceTime != null)
                {
                    return EventContext.synchronizationReferenceTime.Value.Add(EventContext.synchronizationStopwatch.Elapsed);
                }

                return DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets the activity ID of the event/operation used to group
        /// events in the same call/workflow together.
        /// </summary>
        public Guid ActivityId { get; }

        /// <summary>
        /// Gets or sets the duration (time-to-complete) of the event (in milliseconds).
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets any context properties associated with the event to include
        /// with the event data.
        /// </summary>
        [JsonConverter(typeof(ContextPropertiesJsonConverter))]
        public IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Gets the parent/related activity ID of the event/operation which is often
        /// the parent activity that spawned the operation.
        /// </summary>
        public Guid ParentActivityId { get; set; }

        /// <summary>
        /// Gets the transaction ID of the event/operation which is used to
        /// group a subset of events within a given activity ID together.
        /// </summary>
        /// <returns>Transaction id in GUID.</returns>
        public Guid TransactionId { get; set; }

        /// <summary>
        /// Gets the identity of the user who requested the operation or who
        /// initialized the call/workflow.
        /// </summary>
        public string UserIdentity { get; }

        /// <summary>
        /// Gets the set of properties in serialized representation.
        /// </summary>
        internal string SerializedProperties { get; private set; }

        /// <summary>
        /// Returns an event name containing the identifiers
        /// (e.g. [ ClassName, MethodName ] -> ClassName.MethodName).
        /// </summary>
        /// <param name="identifiers">The event identifiers.</param>
        public static string GetEventName(params string[] identifiers)
        {
            if (identifiers?.Any() != true)
            {
                throw new ArgumentException("One or more identifiers must be provided.", nameof(identifiers));
            }

            return string.Join(".", identifiers);
        }

        /// <summary>
        /// Persists the identifiers so that they can be used at a later point on the callstack.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        /// <param name="parentActivityId">The parent activity ID to persist.</param>
        /// <param name="userIdentity">The user identity to persist.</param>
        public static EventContext Persist(Guid activityId, Guid? parentActivityId = null, string userIdentity = null)
        {
            EventContext.Persistence.PersistActivityId(activityId);

            if (parentActivityId != null)
            {
                EventContext.Persistence.PersistParentActivityId(parentActivityId.Value);
            }

            if (userIdentity != null)
            {
                EventContext.Persistence.PersistUserIdentity(userIdentity);
            }

            return new EventContext(activityId, parentActivityId ?? Guid.Empty, userIdentity);
        }

        /// <summary>
        /// Persists the identifiers so that they can be used at a later point on the callstack.
        /// </summary>
        /// <param name="activityId">The activity ID to persist and associate with the correlation ID.</param>
        /// <param name="correlationId">
        /// A constant correlation ID that can be used to retrieve the persisted activity ID in the future. This is
        /// used to associate parent/related activity IDs with downstream activity IDs.  The correlation ID is typically
        /// a constant/pinned value accessible by all activities down the call stack.
        /// </param>
        public static void PersistCorrelation(Guid activityId, Guid correlationId)
        {
            EventContext.Persistence.PersistActivityId(activityId, correlationId);
        }

        /// <summary>
        /// Creates an <see cref="EventContext"/> object using persisted correlation identifiers.
        /// </summary>
        /// <param name="contextProperties">
        /// The event context property object (e.g. anonymous type key/value pair object).
        /// </param>
        /// <returns>
        /// An <see cref="EventContext"/> object created using persisted correlation identifiers.
        /// </returns>
        public static EventContext Persisted(IEnumerable<KeyValuePair<string, object>> contextProperties = null)
        {
            return new EventContext(
                EventContext.Persistence.GetActivityId(),
                EventContext.Persistence.GetParentActivityId(),
                EventContext.Persistence.GetUserIdentity(),
                contextProperties);
        }

        /// <summary>
        /// Creates an <see cref="EventContext"/> object using persisted correlation identifiers.
        /// </summary>
        /// <param name="correlationId">
        /// The constant/pinned correlation ID that was used to persist an activity ID of a parent activity previously. This is
        /// used to associate parent/related activity IDs with downstream activity IDs.  The correlation ID is typically
        /// a constant/pinned value accessible by all activities down the call stack.
        /// </param>
        /// <param name="contextProperties">
        /// The event context property object (e.g. anonymous type key/value pair object).
        /// </param>
        /// <returns>
        /// An <see cref="EventContext"/> object created using persisted correlation identifiers.
        /// </returns>
        public static EventContext Persisted(Guid correlationId, IEnumerable<KeyValuePair<string, object>> contextProperties = null)
        {
            EventContext context = EventContext.Persisted(contextProperties);
            context.ParentActivityId = EventContext.Persistence.GetActivityId(correlationId);
            return context;
        }

        /// <summary>
        /// Starts/resets the time synchronization tracking using the reference time as the initial
        /// starting point/time.
        /// </summary>
        /// <param name="referenceTimestamp">The reference date/time to use as the starting point for time synchronization.</param>
        public static void SynchronizeTimestamps(DateTime referenceTimestamp)
        {
            EventContext.synchronizationReferenceTime = referenceTimestamp;
            EventContext.synchronizationStopwatch.Restart();
        }

        /// <summary>
        /// Stops the time synchronization tracking.
        /// </summary>
        public static void StopTimestampSynchronization()
        {
            EventContext.synchronizationStopwatch.Stop();
            EventContext.synchronizationReferenceTime = null;
        }

        /// <summary>
        /// Returns a deep clone of the current instance
        /// </summary>
        /// <param name="withProperties">
        /// True/false whether the cloned <see cref="EventContext"/> should include the
        /// context properties</param>
        /// <returns>
        /// A clone of the current instance.
        /// </returns>
        public virtual EventContext Clone(bool withProperties = true)
        {
            EventContext clonedContext = new EventContext(this.ActivityId, this.ParentActivityId, this.UserIdentity)
            {
                DurationMs = this.DurationMs,
                TransactionId = this.TransactionId
            };

            if (withProperties && this.Properties.Any())
            {
                foreach (var entry in this.Properties)
                {
                    // ALWAYS use the indexer here. It is possible that there are persistent properties that
                    // conflict with properties that were added to the EventContext explicitly. With Add or AddRange,
                    // methods, this will cause an error "The key already existed in the dictionary." Entries that are
                    // in the object being cloned take precedence over entries added as globally persistent.
                    clonedContext.Properties[entry.Key] = entry.Value;
                }
            }

            return clonedContext;
        }
    }
}