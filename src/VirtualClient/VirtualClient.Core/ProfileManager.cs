// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Storage.Blobs;
    using Polly;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides features for managing profile downloads and related operations.
    /// </summary>
    public class ProfileManager : IProfileManager
    {
        private static readonly IAsyncPolicy DefaultRetryPolicy = Policy.Handle<RequestFailedException>(exc => !ProfileManager.TerminalStatusCodes.Contains(exc.Status))
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2));

        private static readonly List<int> TerminalStatusCodes = new List<int>
        {
            (int)HttpStatusCode.NoContent,
            (int)HttpStatusCode.NotFound,
            (int)HttpStatusCode.Forbidden,
            (int)HttpStatusCode.Gone,
            (int)HttpStatusCode.HttpVersionNotSupported,
            (int)HttpStatusCode.Moved,
            (int)HttpStatusCode.MovedPermanently,
            (int)HttpStatusCode.NetworkAuthenticationRequired,
            (int)HttpStatusCode.Unauthorized
        };

        /// <summary>
        /// Downloads a profile from the target URI endpoint location into the stream provided (e.g. in-memory stream, file stream).
        /// </summary>
        /// <param name="profileUri">The URI to the profile to download.</param>
        /// <param name="downloadStream">The stream into which the blob will be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="credentials">The identity credentials to use for authentication against the store.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        public virtual async Task DownloadProfileAsync(Uri profileUri, Stream downloadStream, CancellationToken cancellationToken, TokenCredential credentials = null, IAsyncPolicy retryPolicy = null)
        {
            profileUri.ThrowIfNull(nameof(profileUri));
            profileUri.ThrowIfInvalid(
                nameof(profileUri),
                uri => uri.Segments.Length >= 3,
                $"Invalid profile URI '{profileUri.AbsolutePath}'. The URI is missing the container name or the name (or virtual path) to the profile itself.");

            downloadStream.ThrowIfNull(nameof(downloadStream));

            string profile = profileUri.AbsolutePath;
            string containerName = profileUri.Segments.FirstOrDefault(s => s != "/");

            try
            {
                await (retryPolicy ?? ProfileManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    Response response = await this.DownloadToStreamAsync(profileUri, downloadStream, credentials, cancellationToken);

                    if (response.Status >= 300)
                    {
                        throw new RequestFailedException(response);
                    }
                });
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Download failed for profile '{profile}' (status code={exc.Status}-{ProfileManager.GetHttpStatusCodeName(exc.Status)}). Access permission was denied.",
                    exc,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Download failed for profile '{profile} '(status code={exc.Status}-{ProfileManager.GetHttpStatusCodeName(exc.Status)}). " +
                    $"The profile or blob container does not exist.",
                    exc,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException exc)
            {
                throw new DependencyException(
                    $"Download failed for profile '{profile}' (status code={exc.Status}-{ProfileManager.GetHttpStatusCodeName(exc.Status)}). {exc.Message.TrimEnd('.')}.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception exc)
            {
                throw new DependencyException(
                    $"Download failed for profile '{profile}'. {exc.Message.TrimEnd('.')}.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

        /// <summary>
        /// Creates the blob container client used to interface with the storage account.
        /// </summary>
        /// <param name="blobUri">URI providing the blob store container reference.</param>
        /// <param name="credentials">Identity credentials to use for authentication against the store.</param>
        protected BlobContainerClient CreateContainerClient(Uri blobUri, TokenCredential credentials = null)
        {
            blobUri.ThrowIfNull(nameof(blobUri));

            // 1) Blob Service SAS URI
            //    Provides the same type of access/restrictions as the Blob service connection string.
            //
            //    e.g. https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...
            //
            // 2) Blob Container SAS URI
            //    Provides access to a specific container within the Blob service. This is the least privileged and most secure option. This
            //    is a good fit for scenarios where all content (e.g. across all monitors) is uploaded to a single container within the blob
            //    store.
            //
            //    e.g. https://anystorageaccount.blob.core.windows.net/content?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...
            //
            // 3) Blob URI with identity/authentication credentials provided (e.g. Azure Managed Identity, Microsoft Entra ID/App).

            BlobContainerClient containerClient = null;

            if (blobUri.AbsolutePath == "/")
            {
                throw new ArgumentException($"Invalid blob URI. The URI provided '{blobUri}' does not define the Storage Account container.");
            }

            Uri containerUri = new Uri($"{blobUri.Scheme}://{blobUri.Host}/{blobUri.Segments.FirstOrDefault(s => s != "/").ToLowerInvariant()}");

            if (EndpointUtility.IsStorageAccountSasUri(blobUri))
            {
                containerClient = new BlobContainerClient(containerUri, new AzureSasCredential(blobUri.Query));
            }
            else if (credentials != null)
            {
                containerClient = new BlobContainerClient(containerUri, credentials);
            }
            else
            {
                containerClient = new BlobContainerClient(containerUri);
            }

            return containerClient;
        }

        /// <summary>
        /// Downloads the profile
        /// </summary>
        protected virtual Task<Response> DownloadToStreamAsync(Uri profileUri, Stream stream, TokenCredential credentials = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // e.g.
            // https://any.blob.core.windows.net/profiles/ANY-PROFILE.json -> ANY.PROFILE.json
            // https://any.blob.core.windows.net/profiles/any/virtual/path/ANY-PROFILE.json -> any/virtual/path/ANY.PROFILE.json

            string profileName = string.Join(string.Empty, profileUri.Segments.Skip(2));
            BlobContainerClient containerClient = this.CreateContainerClient(profileUri, credentials);
            BlobClient blobClient = containerClient.GetBlobClient(profileName);

            return blobClient.DownloadToAsync(stream, cancellationToken);
        }

        private static string GetHttpStatusCodeName(int statusCode)
        {
            return Enum.GetName(typeof(HttpStatusCode), statusCode);
        }
    }
}
