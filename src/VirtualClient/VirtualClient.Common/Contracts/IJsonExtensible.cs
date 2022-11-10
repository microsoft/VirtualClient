// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Defines the requirement that the contract be JSON extensible.
    /// </summary>
    public interface IJsonExtensible
    {
        /// <summary>
        /// Gets the set of extensions for the contract object/instance.
        /// </summary>
        IDictionary<string, JToken> Extensions { get; }
    }
}