// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Used to compare 2 state objects with each other.
    /// </summary>
    public class DefaultStateComparer : IEqualityComparer<JObject>
    {
        private DefaultStateComparer()
        {
        }

        /// <summary>
        /// The singleton instance of the <see cref="DefaultStateComparer"/>;
        /// </summary>
        public static DefaultStateComparer Instance { get; } = new DefaultStateComparer();

        /// <summary>
        /// Returns true if the two objects are equal.
        /// </summary>
        public bool Equals(JObject x, JObject y)
        {
            return JToken.DeepEquals(x, y);
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
