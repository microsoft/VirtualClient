// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Comparer to use for <see cref="NetworkingWorkloadState"/> object comparisons.
    /// </summary>
    public class StateComparer<TState> : IEqualityComparer<JObject>
    {
        private Func<TState, bool> stateComparison;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateComparer{TState}"/> class.
        /// </summary>
        /// <param name="comparison">The comparison logic</param>
        public StateComparer(Func<TState, bool> comparison)
        {
            comparison.ThrowIfNull(nameof(comparison));
            this.stateComparison = comparison;
        }

        /// <summary>
        /// Returns true if the two <see cref="NetworkingWorkloadState"/> objects are equal.
        /// </summary>
        public bool Equals(JObject x, JObject y)
        {
            TState stateY = y.ToObject<TState>();

            return this.stateComparison.Invoke(stateY);
        }

        /// <summary>
        /// Returns a hash code for the object.
        /// </summary>
        public int GetHashCode(JObject obj)
        {
            return obj.ToString().GetHashCode(StringComparison.Ordinal);
        }
    }
}
