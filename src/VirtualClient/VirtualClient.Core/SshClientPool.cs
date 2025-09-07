// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a set of SSH clients/targets.
    /// </summary>
    public class SshClientPool : List<ISshClientProxy>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SshClientPool"/> class.
        /// </summary>
        public SshClientPool()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SshClientPool"/> class.
        /// </summary>
        public SshClientPool(IEnumerable<ISshClientProxy> clients)
            : base(clients)
        {
        }
    }
}
