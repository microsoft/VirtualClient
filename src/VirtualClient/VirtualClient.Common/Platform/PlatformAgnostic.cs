// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;

    /// <summary>
    /// Used to designate a class as being compatible with all platforms.
    /// </summary>
    public class PlatformAgnostic : SystemCompatibilityAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformAgnostic"/> class.
        /// </summary>
        public PlatformAgnostic()
            : base(PlatformID.Win32NT, PlatformID.Unix, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE, PlatformID.Xbox, PlatformID.MacOSX)
        {
        }
    }
}