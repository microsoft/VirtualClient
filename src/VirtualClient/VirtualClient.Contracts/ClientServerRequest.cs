// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a request/instructions sent between a client and a server component.
    /// </summary>
    public class ClientServerRequest : Instructions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientServerRequest"/> class.
        /// </summary>
        [JsonConstructor]
        public ClientServerRequest(InstructionsType type, IDictionary<string, IConvertible> properties = null)
            : base(type, properties)
        {
        }
    }
}
