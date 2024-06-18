// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;

    /// <summary>
    /// References to extensions discovered that can be used by the 
    /// Virtual Client runtime.
    /// </summary>
    public class PlatformExtensions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformExtensions"/> class.
        /// </summary>
        /// <param name="binaries"></param>
        /// <param name="profiles"></param>
        public PlatformExtensions(IEnumerable<IFileInfo> binaries = null, IEnumerable<IFileInfo> profiles = null)
        {
            if (binaries?.Any() == true)
            {
                this.Binaries = new List<IFileInfo>(binaries);
            }

            if (profiles?.Any() == true)
            {
                this.Profiles = new List<IFileInfo>(profiles);
            }
        }

        /// <summary>
        /// Binary/assembly extensions discovered that can be loaded into the
        /// runtime and used to expose new features.
        /// </summary>
        public IList<IFileInfo> Binaries { get; }

        /// <summary>
        /// Profile extensions discovered that used at runtime.
        /// </summary>
        public IList<IFileInfo> Profiles { get; }
    }
}
