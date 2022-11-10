// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Interface for generic rest client builder.
    /// </summary>
    public interface IRestClientBuilder : IDisposable
    {
        /// <summary>
        /// Build the rest client.
        /// </summary>
        /// <returns>The built rest client.</returns>
        IRestClient Build();

        /// <summary>
        /// Use bearer token for authentication.
        /// </summary>
        /// <param name="authToken">The authentication token.</param>
        /// <returns>Builder itself.</returns>
        IRestClientBuilder WithTokenAuthentication(string authToken);

        /// <summary>
        /// Always trust the server certificate.
        /// Note that this method override the underlying httpclient and previous builder methods, so this builder methods should be used first.
        /// </summary>
        /// <returns>Builder itself.</returns>
        IRestClientBuilder AlwaysTrustServerCertificate();

        /// <summary>
        /// Add accepted media type.
        /// </summary>
        /// <returns>The builder itself</returns>
        IRestClientBuilder AddAcceptedMediaType(MediaType mediaType);
    }
}