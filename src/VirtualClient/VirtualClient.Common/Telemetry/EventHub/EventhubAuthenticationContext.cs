// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using Azure.Core;
    using global::Azure.Messaging.EventHubs;

    /// <summary>
    /// Represents context to authenticate with Azure Event Hub.
    /// </summary>
    public class EventHubAuthenticationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubAuthenticationContext"/> class.
        /// </summary>
        /// <param name="connectionString">Eventhub connection string</param>
        public EventHubAuthenticationContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventHubAuthenticationContext"/> class.
        /// </summary>
        /// <param name="eventHubNameSpace">Eventhub namespace</param>
        /// <param name="tokenCredential">Token credential to authenticate with eventhub</param>
        public EventHubAuthenticationContext(string eventHubNameSpace, TokenCredential tokenCredential)
        {
            this.EventHubNamespace = eventHubNameSpace;
            this.TokenCredential = tokenCredential;
        }

        /// <summary>
        /// A connection string to authenticate/authorize with eventhub.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Fully qualified eventhub namespace
        /// </summary>
        public string EventHubNamespace { get; }

        /// <summary>
        /// TokenCredential for Azure Eventhub
        /// </summary>
        public TokenCredential TokenCredential { get; }
    }
}