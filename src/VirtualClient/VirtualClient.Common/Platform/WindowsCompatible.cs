// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;

    /// <summary>
    /// Used to designate a class as being compatible with Windows. (PlatformID.Win32NT)
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public sealed class WindowsCompatible : SystemCompatibilityAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsCompatible"/> class.
        /// </summary>
        public WindowsCompatible()
            : base(PlatformID.Win32NT)
        {
        }
    }
}