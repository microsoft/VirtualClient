// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// The performance capture strategy to apply to aggregations of
    /// metrics.
    /// </summary>
    public enum CaptureStrategy
    {
        /// <summary>
        /// Capture the average of the metric values.
        /// </summary>
        Average,

        /// <summary>
        /// Simply capture the metric value.
        /// </summary>
        Raw
    }

    /// <summary>
    /// Defines the level of determinism before an application timeout is honored.
    /// </summary>
    public enum DeterminismScope
    {
        /// <summary>
        /// Scope not defined.
        /// </summary>
        Undefined,

        /// <summary>
        /// Allow the current action to complete.
        /// </summary>
        IndividualAction,

        /// <summary>
        /// Allow all actions in the profile to complete.
        /// </summary>
        AllActions
    }
}
