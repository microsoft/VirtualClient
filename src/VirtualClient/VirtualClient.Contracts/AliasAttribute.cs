// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines one or more aliases for a given class for reflection support.
    /// </summary>
    [AttributeUsage(validOn: AttributeTargets.Class)]
    public class AliasAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AliasAttribute"/> class.
        /// </summary>
        /// <param name="alias">Alias or aliases delimited by comma.</param>
        public AliasAttribute(string alias)
        {
            alias.ThrowIfNullOrEmpty(nameof(alias));
            this.Aliases = alias.Split(',');
        }

        /// <summary>
        /// The aliases for the logger provider.
        /// </summary>
        public IEnumerable<string> Aliases { get; }
    }
}