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
    public abstract class SystemCompatibilityAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCompatibilityAttribute"/> class.
        /// </summary>
        /// <param name="platforms">The platform to be tested against.</param>
        protected SystemCompatibilityAttribute(params PlatformID[] platforms)
        {
            platforms.ThrowIfNullOrEmpty(nameof(platforms));
            this.CompatiblePlatforms = new List<PlatformID>(platforms);
        }

        /// <summary>
        /// The platforms for which the component is compatible.
        /// </summary>
        public IEnumerable<PlatformID> CompatiblePlatforms { get; }
    }
}