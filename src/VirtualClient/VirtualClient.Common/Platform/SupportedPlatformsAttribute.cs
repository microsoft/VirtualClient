// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Tests compatibility with specific platform/platforms
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class SupportedPlatformsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SystemCompatibilityAttribute"/> class.
        /// </summary>
        /// <param name="throwError">Throw error when not supported.</param>
        /// <param name="platforms">The platform to be tested against.</param>
        public SupportedPlatformsAttribute(string platforms, bool throwError = false)
        {
            platforms.ThrowIfNullOrEmpty(nameof(platforms));
            this.CompatiblePlatforms = platforms.Split(',');
            this.ThrowError = throwError;
        }

        /// <summary>
        /// The platforms for which the component is compatible.
        /// </summary>
        public IEnumerable<string> CompatiblePlatforms { get; }

        /// <summary>
        /// Throw error when not supported.
        /// </summary>
        public bool ThrowError { get; }
    }
}