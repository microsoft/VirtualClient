// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Identity;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for uploading and downloading blobs from an Azure storage account
    /// blob store.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Some error messages are too long to fit on a single line.")]
    public class BlobManager : IBlobManager
    {
        internal static readonly List<int> RetryableCodes = new List<int>
        {
            (int)HttpStatusCode.BadGateway,
            (int)HttpStatusCode.GatewayTimeout,
            (int)HttpStatusCode.ServiceUnavailable,
            (int)HttpStatusCode.GatewayTimeout,
            (int)HttpStatusCode.InternalServerError
        };

        private static IAsyncPolicy defaultRetryPolicy = Policy.Handle<RequestFailedException>(error =>
        {
            return error.Status < 400 || BlobManager.RetryableCodes.Contains(error.Status)
                // When the file is still being written, the signature would change during upload. Retry in this case.
                || error.Message.Contains("is not the same as any computed signature", StringComparison.CurrentCultureIgnoreCase);
        }).WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries + 1));

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobManager"/> class.
        /// </summary>
        /// <param name="storeDescription">Provides the store details and requirement for the blob manager.</param>
        public BlobManager(DependencyBlobStore storeDescription)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));
            this.StoreDescription = storeDescription;
        }

        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        public DependencyStore StoreDescription { get; }

        /// <summary>
        /// Certificate manager to read certificate from local cert store.
        /// </summary>
        internal ICertificateManager CertificateManger { get; set; }

        /// <inheritdoc />
        public async Task<DependencyDescriptor> DownloadBlobAsync(DependencyDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            downloadStream.ThrowIfNull(nameof(downloadStream));

            BlobDescriptor blobDescriptor = (descriptor as BlobDescriptor) ?? new BlobDescriptor(descriptor);
            blobDescriptor.Validate(nameof(BlobDescriptor.Name), nameof(BlobDescriptor.ContainerName));

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                d => !d.ContainsNullEmptyOrWhiteSpace(d.Name, d.ContainerName),
                $"Invalid blob details. The blob and container names are required.");

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                d => BlobManager.IsBlobNameValid(blobDescriptor.Name),
                $"The blob name provided '{blobDescriptor.Name}' is not a valid name for Azure storage account blob.");

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                d => BlobManager.IsContainerNameValid(blobDescriptor.ContainerName),
                $"The blob container name provided '{blobDescriptor.ContainerName}' is not a valid name for Azure storage account blob container.");

            try
            {
                return await (retryPolicy ?? BlobManager.defaultRetryPolicy).ExecuteAsync(async () =>
                {
                    BlobDescriptor blobInfo = new BlobDescriptor(descriptor);
                    Response response = await this.DownloadToStreamAsync(blobDescriptor, downloadStream, cancellationToken)
                        .ConfigureAwait(false);

                    if (response.Status >= 300)
                    {
                        throw new RequestFailedException(
                            response.Status,
                            $"Blob download failed for blob '{blobDescriptor.Name}' (status code={response.Status}-{BlobManager.GetHttpStatusCodeName(response.Status)}).");
                    }

                    if (response.Headers.ETag != null)
                    {
                        blobInfo.ETag = response.Headers.ETag.Value.ToString();
                    }

                    downloadStream.Position = 0;

                    return blobInfo;

                }).ConfigureAwait(false);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Download failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). Access permission was denied.",
                    exc,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Download failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). The blob or blob container does " +
                    $"not exist.",
                    exc,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException exc)
            {
                throw new DependencyException(
                    $"Download failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). {exc.Message}.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception exc)
            {
                throw new DependencyException(
                    $"Download failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}'.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

        /// <inheritdoc />
        public async Task<DependencyDescriptor> UploadBlobAsync(DependencyDescriptor descriptor, Stream uploadStream, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            uploadStream.ThrowIfNull(nameof(uploadStream));

            BlobDescriptor blobDescriptor = (descriptor as BlobDescriptor) ?? new BlobDescriptor(descriptor);
            blobDescriptor.Validate(nameof(BlobDescriptor.Name), nameof(BlobDescriptor.ContainerName), nameof(BlobDescriptor.ContentType));

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                addr => !addr.ContainsNullEmptyOrWhiteSpace(addr.Name, addr.ContainerName),
                $"Invalid blob details. The blob and container names are required.");

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                d => BlobManager.IsBlobNameValid(blobDescriptor.Name),
                $"The blob name provided '{blobDescriptor.Name}' is not a valid name for Azure storage account blob.");

            blobDescriptor.ThrowIfInvalid(
                nameof(descriptor),
                d => BlobManager.IsContainerNameValid(blobDescriptor.ContainerName),
                $"The blob container name provided '{blobDescriptor.ContainerName}' is not a valid name for Azure storage account blob container.");

            try
            {
                return await (retryPolicy ?? BlobManager.defaultRetryPolicy).ExecuteAsync(async () =>
                {
                    BlobDescriptor blobInfo = new BlobDescriptor(descriptor);

                    uploadStream.Position = 0;
                    BlobRequestConditions uploadConditions = new BlobRequestConditions();

                    if (blobDescriptor.ETag != null)
                    {
                        uploadConditions.IfMatch = new ETag(blobDescriptor.ETag);
                    }

                    Response<BlobContentInfo> response = await this.UploadFromStreamAsync(
                        blobDescriptor,
                        uploadStream,
                        new BlobUploadOptions
                        {
                            Conditions = uploadConditions,
                            HttpHeaders = new BlobHttpHeaders
                            {
                                ContentEncoding = blobDescriptor.ContentEncoding.WebName,
                                ContentType = blobDescriptor.ContentType
                            }
                        },
                        cancellationToken).ConfigureAwait(false);

                    Response rawResponse = response.GetRawResponse();
                    if (rawResponse.Status >= 300)
                    {
                        throw new RequestFailedException(
                            rawResponse.Status,
                            $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                            $"(status code={rawResponse.Status}-{BlobManager.GetHttpStatusCodeName(rawResponse.Status)}).");
                    }

                    if (rawResponse.Headers.ETag != null)
                    {
                        blobInfo.ETag = rawResponse.Headers.ETag.Value.ToString();
                    }

                    return blobInfo;

                }).ConfigureAwait(false);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). Access permission was denied.",
                    exc,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). The blob or blob container does " +
                    $"not exist.",
                    exc,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException exc) when (exc.Status == (int)HttpStatusCode.PreconditionFailed)
            {
                throw new DependencyException(
                    $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). The eTag provided " +
                    $"does not match the eTag existing for the blob indicating the blob was updated by another process.",
                    exc,
                    ErrorReason.Http412PreconditionFailedResponse);
            }
            catch (RequestFailedException exc)
            {
                throw new DependencyException(
                    $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}' " +
                    $"(status code={exc.Status}-{BlobManager.GetHttpStatusCodeName(exc.Status)}). {exc.Message}.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception exc)
            {
                throw new DependencyException(
                    $"Upload failed for blob '{blobDescriptor.Name}' and container '{blobDescriptor.ContainerName}'.",
                    exc,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

        /// <summary>
        /// Downloads the blob to the stream provided.
        /// </summary>
        protected virtual Task<Response> DownloadToStreamAsync(BlobDescriptor descriptor, Stream stream, CancellationToken cancellationToken)
        {
            DependencyBlobStore blobStore = this.StoreDescription as DependencyBlobStore;
            BlobContainerClient containerClient = this.CreateContainerClient(descriptor, blobStore, out bool hasContainerPrivileges);
            BlobClient blobClient = containerClient.GetBlobClient(descriptor.Name);

            return blobClient.DownloadToAsync(stream, cancellationToken);
        }

        /// <summary>
        /// Uploads the blob from the stream provided.
        /// </summary>
        protected virtual async Task<Response<BlobContentInfo>> UploadFromStreamAsync(BlobDescriptor descriptor, Stream stream, BlobUploadOptions uploadOptions, CancellationToken cancellationToken)
        {
            DependencyBlobStore blobStore = this.StoreDescription as DependencyBlobStore;
            BlobContainerClient containerClient = this.CreateContainerClient(descriptor, blobStore, out bool hasContainerPrivileges);
            BlobClient blobClient = containerClient.GetBlobClient(descriptor.Name);

            if (hasContainerPrivileges)
            {
                // Container-specific SAS URIs do not allow the client to access container existence, properties or
                // to create the container. Furthermore, the container MUST already exist in order for this type of
                // SAS URI to be created from it.
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None)
                    .ConfigureAwait(false);
            }

            return await blobClient.UploadAsync(stream, uploadOptions, cancellationToken)
                .ConfigureAwait(false);
        }

        private BlobContainerClient CreateContainerClient(BlobDescriptor descriptor, DependencyBlobStore blobStore, out bool hasContainerPrivileges)
        {
            // [Authentication Options]
            // 1) Storage Account connection string
            //    The primary or secondary connection string to the Azure storage account. This provides full access privileges to the entire
            //    storage account but the least amount of security. This is generally recommended only for testing scenarios. The use of a
            //    SAS URI or connection string is preferred because it enables finer grained control of the exact resources within the storage
            //    account that the application should be able to access.
            //
            //    e.g.
            //    DefaultEndpointsProtocol=https;AccountName=anystorageaccount;AccountKey=w7Q+BxLw...;EndpointSuffix=core.windows.net
            //
            // 2) Blob Service connection string
            //    User-defined/restricted access to all containers within the blob store. This is a good fit for scenarios where
            //    content (e.g. from different monitors) is uploaded to different containers within the blob store and thus the application
            //    needs access to all containers.
            //
            //    e.g. BlobEndpoint=https://anystorageaccount.blob.core.windows.net/;SharedAccessSignature=sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...
            //
            // 3) Blob Service SAS URI
            //    Provides the same type of access/restrictions as the Blob service connection string.
            //
            //    e.g. https://anystorageaccount.blob.core.windows.net/?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...
            //
            // 4) Blob Container SAS URI
            //    Provides access to a specific container within the Blob service. This is the least privileged and most secure option. This
            //    is a good fit for scenarios where all content (e.g. across all monitors) is uploaded to a single container within the blob
            //    store.
            //
            //    e.g. https://anystorageaccount.blob.core.windows.net/content?sv=2020-08-04&ss=b&srt=c&sp=rwlacx&se=2021-11-23T14:30:18Z&st=2021-11-23T02:19:18Z&spr=https&sig=jcql6El...

            hasContainerPrivileges = false;
            BlobContainerClient containerClient = null;
            if (!string.IsNullOrEmpty(blobStore.ConnectionToken))
            {
                if (Uri.TryCreate(blobStore.ConnectionToken, UriKind.Absolute, out Uri sasUri))
                {
                    Uri containerUri = sasUri;
                    if (!sasUri.AbsolutePath.Contains(descriptor.ContainerName, StringComparison.OrdinalIgnoreCase))
                    {
                        // The connection authentication token is a blob service-specific SAS URI.
                        containerUri = new Uri($"{sasUri.Scheme}://{sasUri.Host}/{descriptor.ContainerName.ToLowerInvariant()}{sasUri.Query}");
                        hasContainerPrivileges = true;
                    }

                    containerClient = new BlobContainerClient(containerUri);
                }
                else
                {
                    // The connection authentication token is either a storage account-level connection string
                    // or a Blob service-level connection string.
                    containerClient = new BlobContainerClient(blobStore.ConnectionToken, descriptor.ContainerName.ToLowerInvariant());
                    hasContainerPrivileges = true;
                }
            }
            else if (blobStore.UseCertificate == true)
            {
                X509Certificate2 certificate = this.CertificateManger
                containerClient = new BlobContainerClient(new Uri(blobStore.EndpointUrl), credential: new StorageCredentials(certificate);
            }
            else if (blobStore.UseManagedIdentity == true)
            {
                containerClient = new BlobContainerClient(new Uri(blobStore.EndpointUrl), credential: new DefaultAzureCredential());
            }
            

            return containerClient;
        }

        private static string GetHttpStatusCodeName(int statusCode)
        {
            return Enum.GetName(typeof(HttpStatusCode), statusCode);
        }

        private static bool IsBlobNameValid(string blobName)
        {
            bool isValid = true;

            // https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata
            //
            // Blob names cannot be longer than 1024 characters.
            if (blobName.Length > 1024)
            {
                isValid = false;
            }

            return isValid;
        }

        private static bool IsContainerNameValid(string containerName)
        {
            bool isValid = true;

            // https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata
            //
            // Container names must be between 3 and 63 characters.
            if (containerName.Length < 3 || containerName.Length > 63)
            {
                isValid = false;
            }

            if (!Regex.IsMatch(containerName, "^[a-z0-9-]+$", RegexOptions.IgnoreCase))
            {
                isValid = false;
            }

            return isValid;
        }
    }
}
