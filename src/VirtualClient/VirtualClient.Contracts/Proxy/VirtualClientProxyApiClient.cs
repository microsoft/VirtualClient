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
    using System.Text.RegularExpressions;
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

        // Default = 10 MB
        private const int DefaultBlobChunkSize = 1024 * 1024 * 10;

        private static IAsyncPolicy<HttpResponseMessage> defaultGetRetryPolicy = VirtualClientProxyApiClient.GetDefaultRetryPolicy(
            HttpMethod.Get,
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        private static IAsyncPolicy<HttpResponseMessage> defaultPostRetryPolicy = VirtualClientProxyApiClient.GetDefaultRetryPolicy(
           HttpMethod.Post,
           (retries) => TimeSpan.FromMilliseconds(retries * 500));

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientProxyApiClient"/> class.
        /// </summary>
        /// <param name="restClient">
        /// The REST client that handles REST communications with the proxy API service.
        /// </param>
        /// <param name="baseUri">
        /// The base URI to the server hosting the proxy API 
        /// (e.g. https://any.proxy.westUS2.webapps.net:5000, https://any.proxy.westUS2.webapps.net:5000?api-key=123&amp;chunk-size=10000).
        /// </param>
        public VirtualClientProxyApiClient(IRestClient restClient, Uri baseUri)
        {
            restClient.ThrowIfNull(nameof(restClient));
            baseUri.ThrowIfNull(nameof(baseUri));

            this.RestClient = restClient;
            this.BaseUri = baseUri;

            if (!string.IsNullOrWhiteSpace(baseUri.Query))
            {
                Match chunkSize = Regex.Match(baseUri.Query, @"chunk-size=(\d+)", RegexOptions.IgnoreCase);
                if (chunkSize.Success)
                {
                    this.ChunkSize = int.Parse(chunkSize.Groups[1].Value);
                }
            }
        }

        /// <summary>
        /// Gets the base URI to the server hosting the proxy API including its port.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// The size (in bytes) of an individual chunk of a blob to be downloaded when using
        /// download ranges.
        /// </summary>
        public int ChunkSize { get; set; } = VirtualClientProxyApiClient.DefaultBlobChunkSize;

        /// <summary>
        /// Gets or sets the REST client that handles REST communications
        /// with the API service.
        /// </summary>
        protected IRestClient RestClient { get; }

        /// <summary>
        /// Creates an URI route for the proxy API blob endpoints based on the information defined
        /// in the descriptor.
        /// </summary>
        /// <param name="descriptor">Describes the details of the blob to upload or download.</param>
        /// <param name="queryString">Any additional query string properties to include in the route.</param>
        /// <returns>The URI route portion of the URI for the blob upload or download (e.g. /api/blobs/anyblob.1.0.0.zip?source=VirtualClient...).</returns>
        public static string CreateBlobApiRoute(ProxyBlobDescriptor descriptor, string queryString = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));

            // e.g.
            // /api/blobs/anypackage.1.0.0.zip?source=VirtualClient&storeType=Packages&containerName=A57214DC-41BA-4211-956D-07095275D73D&contentType=application/octet-stream&contentEncoding=utf-8
            // /api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=A57214DC-41BA-4211-956D-07095275D73D&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=/any/path/to/blob
            //
            // With API key
            // /api/blobs/anyfile.log?api-key=1234&source=VirtualClient&storeType=Content&containerName=A57214DC-41BA-4211-956D-07095275D73D&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=/any/path/to/blob

            string fullQueryString =
                $"storeType={descriptor.StoreType}" +
                $"&containerName={descriptor.ContainerName}" +
                $"&contentType={descriptor.ContentType}" +
                $"&contentEncoding={descriptor.ContentEncoding}";

            if (!string.IsNullOrWhiteSpace(descriptor.Source))
            {
                fullQueryString = $"source={descriptor.Source}&{fullQueryString}";
            }

            if (!string.IsNullOrWhiteSpace(descriptor.BlobPath))
            {
                fullQueryString += $"&blobPath={descriptor.BlobPath}";
            }

            if (!string.IsNullOrWhiteSpace(queryString))
            {
                fullQueryString = $"{queryString.Trim('?', '&', '/')}&{fullQueryString}";
            }

            string route = $"{VirtualClientProxyApiClient.BlobsApiRoute}/{descriptor.BlobName}?{fullQueryString}";

            return HttpUtility.UrlPathEncode(route);
        }

        /// <summary>
        /// Creates an URI route for the proxy API telemetry endpoints.
        /// </summary>
        /// <param name="queryString">Any additional query string properties to include in the route.</param>
        /// <returns>The URI route portion of the URI for the telemetry endpoint.</returns>
        public static string CreateTelemetryApiRoute(string queryString = null)
        {
            // e.g.
            // /api/telemetry
            // /api/telemetry?api-key=1234

            string route = $"{VirtualClientProxyApiClient.TelemetryApiRoute}";
            if (!string.IsNullOrWhiteSpace(queryString))
            {
                route = $"{route}?{queryString.Trim('?', '&', '/')}";
            }

            return HttpUtility.UrlPathEncode(route);
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> DownloadBlobAsync(ProxyBlobDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            downloadStream.ThrowIfNull(nameof(downloadStream));

            Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.CreateBlobApiRoute(descriptor, this.BaseUri.Query));

            HttpResponseMessage response = null;
            HttpResponseMessage headResponse = await this.RestClient.HeadAsync(requestUri, cancellationToken);

            // If range is not enabled download the file as usual.
            if (!VirtualClientProxyApiClient.IsRangeEnabled(headResponse))
            {
                response = await (retryPolicy ?? defaultGetRetryPolicy).ExecuteAsync(() => this.RestClient.GetAsync(requestUri, cancellationToken));

                Stream httpStream = await response.Content.ReadAsStreamAsync();
                await httpStream.CopyToAsync(downloadStream, cancellationToken);
            }
            else
            {
                long? fileLength = headResponse.Content.Headers.ContentLength;
                if (!fileLength.HasValue)
                {
                    throw new ApiException($"The 'Content-Length' header was not present in the HTTP Response from: '{requestUri}'", ErrorReason.ApiRequestFailed);
                }

                if (fileLength <= 0)
                {
                    throw new ApiException(
                        $"The file length returned by the Proxy API '{fileLength}' is not a valid length. File lengths must be greater than 0.",
                        ErrorReason.ApiRequestFailed);
                }

                for (long latestRequestLength = 0, totalDownloadedLength = 0; totalDownloadedLength < fileLength; totalDownloadedLength += latestRequestLength)
                {
                    response = await (retryPolicy ?? defaultGetRetryPolicy).ExecuteAsync(async () =>
                    {
                        // Note that you CANNOT reuse an HttpRequestMessage for multiple calls. Doing so causes the exception
                        // "Cannot send the same request message multiple times" to be thrown.
                        //
                        // The retry policy is outside of the 'using' to ensure that it is a new HttpRequestMessage for every call including
                        // on initial call failures where retries are employed.
                        using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri))
                        {
                            request.Headers.Range = new RangeHeaderValue(totalDownloadedLength, totalDownloadedLength + this.ChunkSize);
                            using (HttpResponseMessage sequentialResponse = await this.RestClient.SendAsync(request, cancellationToken))
                            {
                                // Return immediately on a non-success status code. The status will be compared against the
                                // set of status codes considered transient and the logic will retry if so.
                                if (!sequentialResponse.IsSuccessStatusCode)
                                {
                                    return sequentialResponse;
                                }

                                byte[] currentContent = await sequentialResponse.Content.ReadAsByteArrayAsync();
                                await downloadStream.WriteAsync(currentContent, cancellationToken);
                                latestRequestLength = currentContent.Length;

                                return new HttpResponseMessage(sequentialResponse.StatusCode);
                            }
                        }
                    });
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
                Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.CreateBlobApiRoute(descriptor, this.BaseUri.Query));

                return await (retryPolicy ?? defaultPostRetryPolicy).ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken);

                    return response;
                });
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> UploadTelemetryAsync(IEnumerable<ProxyTelemetryMessage> messages, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            messages.ThrowIfNull(nameof(messages));

            using (StringContent requestBody = new StringContent(messages.ToJson(), Encoding.UTF8, "application/json"))
            {
                Uri requestUri = new Uri(this.BaseUri, VirtualClientProxyApiClient.CreateTelemetryApiRoute(this.BaseUri.Query));

                return await (retryPolicy ?? defaultPostRetryPolicy).ExecuteAsync(async () =>
                {
                    HttpResponseMessage response = await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken);

                    return response;
                });
            }
        }

        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultRetryPolicy(HttpMethod method, Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of REST status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodesOnGet = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound,
                HttpStatusCode.Locked,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            List<HttpStatusCode> nonTransientErrorCodesOnPost = new List<HttpStatusCode>
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
                bool shouldRetry = false;
                if (!response.IsSuccessStatusCode)
                {
                    IEnumerable<HttpStatusCode> nonTransientCodes = method == HttpMethod.Get
                        ? nonTransientErrorCodesOnGet
                        : nonTransientErrorCodesOnPost;

                    // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                    // code is also not in the list of non-transient status codes.
                    shouldRetry = !response.IsSuccessStatusCode && !nonTransientCodes.Contains(response.StatusCode);
                }

                return shouldRetry;
            }).WaitAndRetryAsync(5, retryWaitInterval);
        }

        private static bool IsRangeEnabled(HttpResponseMessage response)
        {
            return response != null && response.IsSuccessStatusCode && response.Headers.AcceptRanges?.Count > 0;
        }
    }
}