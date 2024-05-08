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
    public class EventhubAuthenticationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventhubAuthenticationContext"/> class.
        /// </summary>
        /// <param name="connectionString">Eventhub connection string</param>
        public EventhubAuthenticationContext(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventhubAuthenticationContext"/> class.
        /// </summary>
        /// <param name="eventhubNameSpace">Eventhub namespace</param>
        /// <param name="tokenCredential">Token credential to authenticate with eventhub</param>
        public EventhubAuthenticationContext(string eventhubNameSpace, TokenCredential tokenCredential)
        {
            this.EventhubNamespace = eventhubNameSpace;
            this.TokenCredential = tokenCredential;
        }

        /// <summary>
        /// A connection string to authenticate/authorize with eventhub.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// Fully qualified eventhub namespace
        /// </summary>
        public string EventhubNamespace { get; }

        /// <summary>
        /// TokenCredential for Azure Eventhub
        /// </summary>
        public TokenCredential TokenCredential { get; }
    }
}