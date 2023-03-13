// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;

    /// <summary>
    /// API client that allows the Virtual Client to upload/download files and packages and to emit telemetry
    /// to/from a proxy API endpoint.
    /// </summary>
    public class VirtualClientProxyApiClient : IProxyApiClient
    {
        private const string BlobsApiRoute = "/api/blobs";
        private const string TelemetryApiRoute = "/api/telemetry";
        private const int DefaultBlobChunkSize = 1024 * 1024;

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpGetRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpGetRetryPolicy(
           (retries) => TimeSpan.FromMilliseconds(retries * 500));

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpPostRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientProxyApiClient"/> class.
        /// </summary>
        /// <param name="restClient">
        /// The REST client that handles REST communications with the proxy API
        /// service.
        /// </param>
        /// <param name="baseUri">
        /// The base URI to the server hosting the proxy API (e.g. https://any.proxy.westUS2.webapps.net:5000).
        /// </param>
        public VirtualClientProxyApiClient(IRestClient restClient, Uri baseUri)
        {
            restClient.ThrowIfNull(nameof(restClient));
            baseUri.ThrowIfNull(nameof(baseUri));

            this.RestClient = restClient;
            this.BaseUri = baseUri;
        }

        /// <summary>
        /// Gets the base URI to the server hosting the proxy API including its port.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Gets or sets the REST client that handles REST communications
        /// with the API service.
        /// </summary>
        protected IRestClient RestClient { get; }

        /// <summary>
        /// The size of an individual chunk of a blob to be downloaded.
        /// </summary>
        protected virtual int BlobChunkSize { get; } = VirtualClientProxyApiClient.DefaultBlobChunkSize;

        /// <summary>
        /// Creates an URI route for the proxy API blob endpoints based on the information defined
        /// in the descriptor.
        /// </summary>
        /// <param name="descriptor">Describes the details of the blob to upload or download.</param>
        /// <returns>The URI route portion of the URI for the blob upload or download (e.g. /api/blobs/anyblob.1.0.0.zip?source=VirtualClient...).</returns>
        public static string CreateBlobApiRoute(ProxyBlobDescriptor descriptor)
        {
            descriptor.ThrowIfNull(nameof(descriptor));

            // e.g.
            // /api/blobs/anypackage.1.0.0.zip?source=VirtualClient&storeType=Packages&containerName=A57214DC-41BA-4211-956D-07095275D73D&contentType=application/octet-stream&contentEncoding=utf-8
            // /api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=A57214DC-41BA-4211-956D-07095275D73D&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=/any/path/to/blob

            string route = $"{VirtualClientProxyApiClient.BlobsApiRoute}/{descriptor.BlobName}?source={descriptor.Source}" +
                $"&storeType={descriptor.StoreType}" +
                $"&containerName={descriptor.ContainerName}" +
                $"&contentType={descriptor.ContentType}" +
                $"&contentEncoding={descriptor.ContentEncoding}";

            if (!string.IsNullOrWhiteSpace(descriptor.BlobPath))
            {
                route += $"&blobPath={descriptor.BlobPath}";
            }

            return HttpUtility.UrlPathEncode(route);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DownloadBlobAsync(ProxyBlobDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            downloadStream.ThrowIfNull(nameof(downloadStream));

            Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.CreateBlobApiRoute(descriptor));
            HttpResponseMessage response = null; 
            HttpResponseMessage headResponse = await this.RestClient.HeadAsync(requestUri, cancellationToken).ConfigureAwait(false);
            
            // If range is not enabled download the file as usual.
            if (!VirtualClientProxyApiClient.IsRangeEnabled(headResponse))
            {
                response = await (retryPolicy ?? defaultHttpGetRetryPolicy).ExecuteAsync(() => this.RestClient.GetAsync(requestUri, cancellationToken));

                Stream httpStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await httpStream.CopyToAsync(downloadStream, cancellationToken).ConfigureAwait(false);
                return response;
            }

            long? fileLength = headResponse.Content.Headers.ContentLength;
            if (!fileLength.HasValue)
            {
                throw new ApiException($"The 'Content-Length' header was not present in the HTTP Response from: '{requestUri}'", ErrorReason.ApiRequestFailed);
            }

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
            {
                for (int latestRequestLength = 0, totalDownloadedLength = 0; totalDownloadedLength < fileLength; totalDownloadedLength += latestRequestLength)
                {
                    request.Headers.Range = new RangeHeaderValue(totalDownloadedLength, totalDownloadedLength + this.BlobChunkSize);
                    response = await (retryPolicy ?? defaultHttpGetRetryPolicy).ExecuteAsync(() => this.RestClient.SendAsync(request, cancellationToken));
                    if (!response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                    
                    byte[] currentContent = await response.Content.ReadAsByteArrayAsync();
                    await downloadStream.WriteAsync(currentContent, cancellationToken);
                    latestRequestLength = currentContent.Length;
                }
            }

            downloadStream.Position = 0;
            return response;
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> UploadBlobAsync(ProxyBlobDescriptor descriptor, Stream uploadStream, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            uploadStream.ThrowIfNull(nameof(uploadStream));

            using (StreamContent requestBody = new StreamContent(uploadStream))
            {
                Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.CreateBlobApiRoute(descriptor));

                return await (retryPolicy ?? defaultHttpPostRetryPolicy).ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);

                    return response;
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> UploadTelemetryAsync(IEnumerable<ProxyTelemetryMessage> messages, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            messages.ThrowIfNull(nameof(messages));

            using (StringContent requestBody = new StringContent(messages.ToJson(), Encoding.UTF8, "application/json"))
            {
                Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.TelemetryApiRoute);

                return await (retryPolicy ?? defaultHttpPostRetryPolicy).ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);

                    return response;
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates the default retry policy for REST GET calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpGetRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of REST status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound,
                HttpStatusCode.Locked,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;
            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        /// <summary>
        /// Creates the default retry policy for REST POST calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpPostRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of REST status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;
            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        private static bool IsRangeEnabled(HttpResponseMessage response)
        {
            return response != null && response.IsSuccessStatusCode && response.Headers.AcceptRanges?.Count > 0;
        }
    }
}