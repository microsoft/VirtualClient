// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Platform
{
    using System;

    /// <summary>
    /// Used to designate a class as being compatible with Linux. (PlatformID.Unix)
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public sealed class UnixCompatible : SystemCompatibilityAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnixCompatible"/> class.
        /// </summary>
        public UnixCompatible()
            : base(PlatformID.Unix)
        {
        }
    }
}
