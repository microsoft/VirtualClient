// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    /// Provides methods for persisting identifiers across threads.
    /// </summary>
    public class CallContextPersistenceManager : IPersistenceManager
    {
        private ConcurrentDictionary<Guid, Guid> correlationIds;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallContextPersistenceManager"/> class.
        /// </summary>
        public CallContextPersistenceManager()
        {
            this.correlationIds = new ConcurrentDictionary<Guid, Guid>();
        }

        /// <summary>
        /// Gets or sets the activity ID to persist throughout the call operations.
        /// </summary>
        public static Guid ActivityId
        {
            get
            {
                object activityId = CrossFrameworkCallContext.GetData(nameof(CallContextPersistenceManager.ActivityId));
                return activityId != null ? (Guid)activityId : Guid.Empty;
            }

            private set
            {
                CrossFrameworkCallContext.SetData(nameof(CallContextPersistenceManager.ActivityId), value);
            }
        }

        /// <summary>
        /// Gets or sets the parent activity ID to persist throughout the call operations.
        /// </summary>
        public static Guid ParentActivityId
        {
            get
            {
                object activityId = CrossFrameworkCallContext.GetData(nameof(CallContextPersistenceManager.ParentActivityId));
                return activityId != null ? (Guid)activityId : Guid.Empty;
            }

            private set
            {
                CrossFrameworkCallContext.SetData(nameof(CallContextPersistenceManager.ParentActivityId), value);
            }
        }

        /// <summary>
        /// Gets or sets the user identity to persist throughout the call operations.
        /// </summary>
        public static string UserIdentity
        {
            get
            {
                object user = CrossFrameworkCallContext.GetData(nameof(CallContextPersistenceManager.UserIdentity));

#if !(WINDOWS_UWP) && !(NETCORE) && !(NETSTANDARD)
                if (user == null)
                {
                    user = $"{Environment.UserDomainName}\\{Environment.UserName}";
                }
#endif

                return user?.ToString();
            }

            private set
            {
                CrossFrameworkCallContext.SetData(nameof(CallContextPersistenceManager.UserIdentity), value);
            }
        }

        /// <summary>
        /// Disposes of resources used by the class instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns the activity ID persisted.
        /// </summary>
        /// <returns>
        /// The Guid activity ID persisted by the manager.
        /// </returns>
        public Guid GetActivityId()
        {
            return CallContextPersistenceManager.ActivityId;
        }

        /// <summary>
        /// Returns the activity ID persisted and that is associated with the correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID associated with/linked to the activity ID.</param>
        /// <returns>
        /// The activity ID associated with/linked to the correlation ID.
        /// </returns>
        public Guid GetActivityId(Guid correlationId)
        {
            Guid correlatedActivityId = default(Guid);
            this.correlationIds.TryGetValue(correlationId, out correlatedActivityId);
            return correlatedActivityId;
        }

        /// <summary>
        /// Returns the parent activity ID persisted.
        /// </summary>
        /// <returns>
        /// The Guid parent activity ID persisted by the manager.
        /// </returns>
        public Guid GetParentActivityId()
        {
            return CallContextPersistenceManager.ParentActivityId;
        }

        /// <summary>
        /// Returns the user identitiy persisted.
        /// </summary>
        /// <returns>
        /// The user identity persisted.
        /// </returns>
        public string GetUserIdentity()
        {
            return CallContextPersistenceManager.UserIdentity;
        }

        /// <summary>
        /// Persists the activity ID for later retrieval and associates it with the
        /// correlation ID provided.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        public void PersistActivityId(Guid activityId)
        {
            CallContextPersistenceManager.ActivityId = activityId;
        }

        /// <summary>
        /// Persists the activity ID for later retrieval and associates it with the
        /// correlation ID provided.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        /// <param name="correlationId">The correlation ID to which the activity ID should be associated.</param>
        public void PersistActivityId(Guid activityId, Guid correlationId)
        {
            this.PersistActivityId(activityId);
            this.correlationIds.AddOrUpdate(correlationId, activityId, (existing, other) => activityId);
        }

        /// <summary>
        /// Persists the parent activity ID for later retrieval.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        public void PersistParentActivityId(Guid activityId)
        {
            CallContextPersistenceManager.ParentActivityId = activityId;
        }

        /// <summary>
        /// Persists the user identity for later retrieval.
        /// </summary>
        /// <param name="userIdentity">The user identity to persist.</param>
        public void PersistUserIdentity(string userIdentity)
        {
            CallContextPersistenceManager.UserIdentity = userIdentity;
        }

        /// <summary>
        /// Resets the persistence manager (clears persisted objects).
        /// </summary>
        public void Reset()
        {
            this.correlationIds.Clear();
        }

        /// <summary>
        /// Disposes of resources used by the class instance
        /// </summary>
        /// <param name="disposing">
        /// Defines true/false whether to dispose of managed resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.correlationIds.Clear();
                }

                // Dispose of unmanaged resources here
                this.disposed = true;
            }
        }

        /// <summary>
        /// Cross-framework implementation of the CallContext class available in
        /// full .NET frameworks.
        /// </summary>
        private static class CrossFrameworkCallContext
        {
            private static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

            /// <summary>
            /// Stores a given object and associates it with the specified name.
            /// </summary>
            /// <param name="name">The name with which to associate the new item in the call context.</param>
            /// <param name="data">The object to store in the call context.</param>
            public static void SetData(string name, object data) =>
                CrossFrameworkCallContext.state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

            /// <summary>
            /// Retrieves an object with the specified name from the <see cref="CrossFrameworkCallContext"/>.
            /// </summary>
            /// <param name="name">The name of the item in the call context.</param>
            /// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
            public static object GetData(string name) =>
                CrossFrameworkCallContext.state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
        }
    }
}