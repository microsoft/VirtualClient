// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Contains utilities regarding the platform VirtualClient is running on.
    /// </summary>
    public static class PlatformUtil
    {
        /// <summary>
        /// The current platform
        /// </summary>
        private static PlatformID? overrideCurrentPlatform = null;

        /// <summary>
        /// Gets the current platform
        /// </summary>
        public static PlatformID CurrentPlatform
        {
            get
            {
                if (PlatformUtil.overrideCurrentPlatform == null)
                {
                    return Environment.OSVersion.Platform;
                }
                else
                {
                    return PlatformUtil.overrideCurrentPlatform.Value;
                }
            }

            set // overridable for unit testing
            {
                PlatformUtil.overrideCurrentPlatform = value;
            }
        }

        /// <summary>
        /// Tests whether or not a VirtualClientComponent is compatible with the operating system.
        /// </summary>
        /// <param name="component">The component to test.</param>
        /// <returns>IsSystemCompatible</returns>
        public static bool IsSystemCompatible(object component)
        {
            return PlatformUtil.IsSystemCompatible(component?.GetType()) == true;
        }

        /// <summary>
        /// Tests whether or not a Type is compatible with the operating system.
        /// </summary>
        /// <param name="componentType">The component type to test.</param>
        /// <returns>IsSystemCompatible</returns>
        public static bool IsSystemCompatible(Type componentType)
        {
            if (componentType == null)
            {
                return false;
            }

            IEnumerable<SystemCompatibilityAttribute> foundAttributes = componentType.GetCustomAttributes<SystemCompatibilityAttribute>();
            return foundAttributes?.Any(attr => attr.IsSystemCompatible()) == true;
        }
    }
}
