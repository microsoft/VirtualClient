// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;
    using System.Linq;

    /// <summary>
    /// Tests compatibility with a specific OS (PlatformID).
    /// </summary>
    public abstract class SystemOSCompatibilityAttribute : SystemCompatibilityAttribute
    {
        private PlatformID[] platforms;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemOSCompatibilityAttribute"/> class.
        /// </summary>
        /// <param name="platforms">The platform to be tested against.</param>
        protected SystemOSCompatibilityAttribute(params PlatformID[] platforms)
        {
            this.platforms = platforms;
        }

        /// <inheritdoc/>
        public override bool IsSystemCompatible()
        {
            return this.platforms.Contains(PlatformUtil.CurrentPlatform);
        }
    }
}