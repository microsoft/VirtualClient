// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Information on the overall state of execution.
    /// </summary>
    public class GlobalState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalState"/> class.
        /// </summary>
        public GlobalState()
        {
            this.IsFirstRun = true;
            this.ProfileIteration = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalState"/> class.
        /// </summary>
        /// <param name="properties">Properties used to describe the state of operations.</param>
        [JsonConstructor]
        public GlobalState(IDictionary<string, IConvertible> properties)
            : base(properties)
        {
        }

        /// <summary>
        /// True if the current operations are the first run for the round of profile
        /// executions. This allows workloads to perform initialization steps that should happen
        /// only on a first run.
        /// </summary>
        public bool IsFirstRun
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(this.IsFirstRun), true);
            }

            set
            {
                this.Properties[nameof(this.IsFirstRun)] = value;
            }
        }

        /// <summary>
        /// The current iteration of the profile execution. This value is incremented
        /// each time a profile finishes a full set of actions and starts over.
        /// </summary>
        public int ProfileIteration
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.ProfileIteration));
            }

            set
            {
                this.Properties[nameof(this.ProfileIteration)] = value;
            }
        }
    }
}
