// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;

    /// <summary>
    /// Represents the possible types of modifications that
    /// could be performed on an event payload to be within ETW restrictions
    /// or size limitations.
    /// </summary>
    [Flags]
    public enum ContextModifications
    {
        /// <summary>
        /// No modifications have been made to the payload
        /// </summary>
        None = 0,

        /// <summary>
        /// The payload exceeded ETW size limits and was constrained
        /// </summary>
        ConstrainPayload = 1,

        /// <summary>
        /// The payload exceeded ETW size limits but the attempt to constrain
        /// the payload failed.
        /// </summary>
        ConstrainPayloadFailed = 2,
    }
}