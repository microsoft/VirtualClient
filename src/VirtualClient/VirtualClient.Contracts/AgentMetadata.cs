// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Special metadata commonly used with SDK agent workflows.
    /// </summary>
    public class AgentMetadata : ReadOnlyDictionary<string, IConvertible>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentMetadata"/> class.
        /// </summary>
        /// <param name="metadata">The full set of metadata provided to the SDK agent.</param>
        public AgentMetadata(IDictionary<string, IConvertible> metadata)
            : base(new Dictionary<string, IConvertible>(metadata, StringComparer.OrdinalIgnoreCase))
        {
        }

        /// <summary>
        /// An identifier used to identify a cycle #.
        /// </summary>
        public string Cycle
        {
            get
            {
                this.TryGetValue(nameof(this.Cycle), out IConvertible cycle);
                return cycle?.ToString();
            }
        }

        /// <summary>
        /// An identifier used to group a set of N-number of experiments (e.g. DC_Cycling).
        /// </summary>
        public string ExperimentName
        {
            get
            {
                this.TryGetValue(nameof(this.ExperimentName), out IConvertible experimentName);
                return experimentName?.ToString();
            }
        }
    }
}
