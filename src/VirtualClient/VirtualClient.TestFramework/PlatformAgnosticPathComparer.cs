// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Platform-agnostic comparer for file paths.
    /// </summary>
    public class PlatformAgnosticPathComparer : IEqualityComparer<string>
    {
        private PlatformAgnosticPathComparer()
        {
        }

        /// <summary>
        /// The singleton instance of the comparer.
        /// </summary>
        public static PlatformAgnosticPathComparer Instance { get; } = new PlatformAgnosticPathComparer();

        /// <summary>
        /// Returns true if the 2 paths are equal (case-sensitive).
        /// </summary>
        public bool Equals(string path1, string path2)
        {
            if (path1 == null && path2 == null)
            {
                return true;
            }

            if ((path1 == null && path2 != null) || (path1 != null && path2 == null))
            {
                return false;
            }

            return string.Equals(path1.Replace("\\", "/"), path2.Replace("\\", "/"), StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns a hash code for the path.
        /// </summary>
        public int GetHashCode(string path)
        {
            return path.GetHashCode();
        }
    }
}
