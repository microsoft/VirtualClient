// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines the compatibility with specific platform/platforms (e.g. linux-arm64,linux-x64).
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class SupportedPlatformsAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SupportedPlatformsAttribute"/> class.
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