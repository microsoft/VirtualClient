// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;

    /// <summary>
    /// Provides methods for persisting identifiers used in telemetry
    /// events and for event correlation.
    /// </summary>
    public interface IPersistenceManager : IDisposable
    {
        /// <summary>
        /// Returns the activity ID persisted.
        /// </summary>
        /// <returns>
        /// The Guid activity ID persisted by the manager.
        /// </returns>
        Guid GetActivityId();

        /// <summary>
        /// Returns the activity ID persisted and that is associated with the correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID associated with/linked to the activity ID.</param>
        /// <returns>
        /// The activity ID associated with/linked to the correlation ID.
        /// </returns>
        Guid GetActivityId(Guid correlationId);

        /// <summary>
        /// Returns the parent activity ID persisted.
        /// </summary>
        /// <returns>
        /// The Guid parent activity ID persisted by the manager.
        /// </returns>
        Guid GetParentActivityId();

        /// <summary>
        /// Returns the user identitiy persisted.
        /// </summary>
        /// <returns>
        /// The user identity persisted.
        /// </returns>
        string GetUserIdentity();

        /// <summary>
        /// Persists the activity ID for later retrieval.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        void PersistActivityId(Guid activityId);

        /// <summary>
        /// Persists the parent activity ID for later retrieval.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        void PersistParentActivityId(Guid activityId);

        /// <summary>
        /// Persists the activity ID for later retrieval and associates it with the
        /// correlation ID provided.
        /// </summary>
        /// <param name="activityId">The activity ID to persist.</param>
        /// <param name="correlationId">The correlation ID to which the activity ID should be associated.</param>
        void PersistActivityId(Guid activityId, Guid correlationId);

        /// <summary>
        /// Persists the user identity for later retrieval.
        /// </summary>
        /// <param name="userIdentity">The user identity to persist.</param>
        void PersistUserIdentity(string userIdentity);

        /// <summary>
        /// Resets the persistence manager (clears persisted objects).
        /// </summary>
        void Reset();
    }
}