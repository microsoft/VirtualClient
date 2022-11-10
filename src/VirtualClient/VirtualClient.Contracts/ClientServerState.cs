// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Represents the current state of a client or server component.
    /// </summary>
    public class ClientServerState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientServerState"/> class.
        /// </summary>
        public ClientServerState(ClientServerStatus status, IDictionary<string, IConvertible> properties = null)
            : base(properties)
        {
            this.Status = status;
        }

        /// <summary>
        /// An identifier for the type of instructions (e.g. Profiling).
        /// </summary>
        [JsonProperty(PropertyName = "status", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ClientServerStatus Status { get; set; }
    }
}
