// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;

    /// <summary>
    /// A generic attribute for declaring system compatibility.
    /// Intended to be used on VirtualClientComponent types.
    /// </summary>
    public abstract class SystemCompatibilityAttribute : Attribute
    {
        /// <summary>
        /// Tests if the compatibility check passes.
        /// </summary>
        public abstract bool IsSystemCompatible();
    }
}
