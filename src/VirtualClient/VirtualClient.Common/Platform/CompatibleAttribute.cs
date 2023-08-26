// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Tests compatibility with a specific OS (PlatformID).
    /// </summary>
    public abstract class CompatibleAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCompatibilityAttribute"/> class.
        /// </summary>
        /// <param name="platformArchitectures">The platform to be tested against.</param>
        protected CompatibleAttribute(params string[] platformArchitectures)
        {
            platformArchitectures.ThrowIfNullOrEmpty(nameof(platformArchitectures));
            this.CompatiblePlatformArchitectures = new List<string>(platformArchitectures);
        }

        /// <summary>
        /// The platforms for which the component is compatible.
        /// </summary>
        public IEnumerable<string> CompatiblePlatformArchitectures { get; }
    }
}