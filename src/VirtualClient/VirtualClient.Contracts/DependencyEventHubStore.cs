// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Net;
    using Azure.Core;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Represents an Azure Event Hub Namespace store (i.e. telemetry store).
    /// </summary>
    public class DependencyEventHubStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyEventHubStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the Event Hub namespace store (e.g. Telemetry).</param>
        /// <param name="connectionString">The connection string/access policy for the target Event Hub Namespace.</param>
        public DependencyEventHubStore(string storeName, string connectionString)
            : base(storeName, DependencyStore.StoreTypeAzureEventHubNamespace)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyEventHubStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the Event Hub namespace store (e.g. Telemetry).</param>
        /// <param name="endpointUri">The URI/SAS for the target Event Hub Namespace.</param>
        public DependencyEventHubStore(string storeName, Uri endpointUri)
            : base(storeName, DependencyStore.StoreTypeAzureEventHubNamespace)
        {
            endpointUri.ThrowIfNull(nameof(endpointUri));
            this.EndpointUri = endpointUri;
            this.EventHubNamespace = endpointUri.Host;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyEventHubStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the Event Hub namespace store (e.g. Telemetry).</param>
        /// <param name="endpointUri">The URI/SAS for the target Event Hub Namespace.</param>
        /// <param name="credentials">An identity token credential to use for authentication against the Event Hub namespace.</param>
        public DependencyEventHubStore(string storeName, Uri endpointUri, TokenCredential credentials)
            : this(storeName, endpointUri)
        {
            credentials.ThrowIfNull(nameof(credentials));
            this.Credentials = credentials;
        }

        /// <summary>
        /// The connection string/access policy for the target Event Hub Namespace
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// The URI/SAS for the target Event Hub Namespace.
        /// </summary>
        public Uri EndpointUri { get; }

        /// <summary>
        /// The Event Hub namespace.
        /// </summary>
        public string EventHubNamespace { get; }

        /// <summary>
        /// An identity token credential to use for authentication against the Event Hub Namespace. 
        /// </summary>
        public TokenCredential Credentials { get; }
    }
}
