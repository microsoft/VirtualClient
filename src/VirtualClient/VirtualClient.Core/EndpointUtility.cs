// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    /// <summary>
    /// Provides features for managing requirements for remote endpoint access.
    /// </summary>
    public static class EndpointUtility
    {
        private const string AllowedPackageUri = "https://packages.virtualclient.microsoft.com";

        /// <summary>
        /// Creates a <see cref="DependencyBlobStore"/> definition from the connection properties provided.
        /// <list>
        /// <item>The following type of connection strings are supported:</item>
        /// <list type="bullet">
        /// <item>Storage account connection string<br/>(e.g. DefaultEndpointsProtocol=https;AccountName=anystorage01;AccountKey=...;EndpointSuffix=core.windows.net).</item>
        /// <item>Storage account blob container connection string<br/>(e.g. BlobEndpoint=https://anystorage01.blob.core.windows.net/;SharedAccessSignature=sv=2022-11-02...).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="storeName">The name of the dependency store (e.g. Content, Packages).</param>
        /// <param name="endpoint">A connection string or URI describing the target storage endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager"></param>
        public static DependencyBlobStore CreateBlobStoreReference(string storeName, string endpoint, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyBlobStore store = null;
            endpoint = ValidateAndFormatPackageUri(endpoint);
            string argumentValue = endpoint.Trim(new char[] { '\'', '"', ' ' });

            if (EndpointUtility.IsStorageAccountConnectionString(argumentValue))
            {
                // e.g.
                // DefaultEndpointsProtocol=https;AccountName=anystorage01;AccountKey=...;EndpointSuffix=core.windows.net
                store = EndpointUtility.CreateBlobStoreReference(storeName, argumentValue);
            }
            else if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // EndpointUrl=anystorage01.blob.core.windows.net;ClientId=307591a4-abb2...;TenantId=985bbc17...
                IDictionary<string, string> connectionProperties = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                store = EndpointUtility.CreateBlobStoreReference(storeName, connectionProperties, certificateManager);
            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri))
            {
                // e.g.
                // SAS URI
                // https://any.service.azure.com?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https
                // or
                // Custom URI
                // https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d
                // or
                // Package URI
                // https://packages.virtualclient.microsoft.com

                store = EndpointUtility.CreateBlobStoreReference(storeName, endpointUri, certificateManager);
            }

            // If the certificate is not found, the certificate manager will throw and exception. The logic that follows
            // here would happen if the user provided invalid information that precedes the search for the actual certificate.
            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Storage Account endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return store;
        }

        /// <summary>
        /// Creates a <see cref="DependencyEventHubStore"/> definition from the connection string provided.
        /// <list>
        /// <item>The following type of connection strings are supported:</item>
        /// <list type="bullet">
        /// <item>Event Hub namespace access policy/connection string<br/>(e.g. Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=...).</item>
        /// <item>Event Hub instance access policy/connection string<br/>(e.g. Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=...;EntityPath=telemetry-logs).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="storeName">The name of the dependency store (e.g. Content, Packages).</param>
        /// <param name="endpoint">A connection string or URI describing the target Event Hub endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager"></param>
        public static DependencyEventHubStore CreateEventHubStoreReference(string storeName, string endpoint, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyEventHubStore store = null;
            string argumentValue = endpoint.Trim(new char[] { '\'', '\"', ' ' });

            if (EndpointUtility.IsEventHubConnectionString(argumentValue))
            {
                // e.g.
                // --eventhub="Endpoint=sb://xxx.servicebus.windows.net/;SharedAccessKeyName=xxx"

                store = new DependencyEventHubStore(storeName, argumentValue);
            }
            else if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // Endpoint=sb://any.servicebus.windows.net;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4
                // EventHubNamespace=any.servicebus.windows.net;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4

                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                // We support an 'EventHubNamespace' property in custom connection strings. To ensure consistency downstream,
                // we define the endpoint to be a proper Event Hub namespace URI.
                if (connectionParameters.TryGetValue(ConnectionParameter.EventHubNamespace, out string eventHubNamespace))
                {
                    connectionParameters[ConnectionParameter.EndpointUrl] = eventHubNamespace;
                    if (!eventHubNamespace.Trim().StartsWith("sb://"))
                    {
                        connectionParameters[ConnectionParameter.EndpointUrl] = $"sb://{eventHubNamespace}";
                    }
                }

                store = EndpointUtility.CreateEventHubStoreReference(storeName, connectionParameters, certificateManager);
            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri) && EndpointUtility.IsCustomUri(endpointUri))
            {
                // e.g.
                // sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=123456789

                store = EndpointUtility.CreateEventHubStoreReference(storeName, endpointUri, certificateManager);
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Event Hub endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Event Hub namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0610-integration-event-hub/{Environment.NewLine}");
            }

            return store;
        }

        /// <summary>
        /// Creates a <see cref="DependencyKeyVaultStore"/> definition from the connection string provided.
        /// </summary>
        /// <param name="storeName">The name of the dependency store (e.g. KeyVault, Packages).</param>
        /// <param name="endpoint">A connection string or URI describing the target Key Vault endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager"></param>
        public static DependencyKeyVaultStore CreateKeyVaultStoreReference(string storeName, string endpoint, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyKeyVaultStore store = null;
            string argumentValue = endpoint.Trim(new char[] { '\'', '\"', ' ' });

            if (EndpointUtility.IsCustomConnectionString(argumentValue))
            {
                // e.g.
                // Endpoint=https://my-keyvault.vault.azure.net/;CertificateThumbprint=1234567;ClientId=985bbc17;TenantId=307591a4
                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(argumentValue)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                store = EndpointUtility.CreateKeyVaultStoreReference(storeName, connectionParameters, certificateManager);
            }
            else if (Uri.TryCreate(argumentValue, UriKind.Absolute, out Uri endpointUri) && EndpointUtility.IsCustomUri(endpointUri))
            {
                // e.g.
                // https://my-keyvault.vault.azure.net/?cid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&tid=307591a4-abb2-4559-af59-b47177d140cf&crtt=123456789

                store = EndpointUtility.CreateKeyVaultStoreReference(storeName, endpointUri, certificateManager);
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Key Vault endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Key Vault namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}");
            }

            return store;
        }

        /// <summary>
        /// Creates a <see cref="DependencyProfileReference"/> definition from the endpoint URI provided. 
        /// <list>
        /// <item>The following type of URIs are supported:</item>
        /// <list type="bullet">
        /// <item>Storage account or blob container SAS URI<br/>(e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&amp;ss=b&amp;srt=co&amp;sp=rtf&amp;se=2024-07-02T05:15:29Z&amp;st=2024-07-01T21:15:29Z&amp;spr=https).</item>
        /// <item>Microsoft Entra or Managed Identity referencing URI<br/>(e.g. https://any.service.azure.com/profiles/ANY-PROFILE.json?cid=307591a4-abb2-4559-af59-b47177d140cf&amp;tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&amp;crti=ABC&amp;crts=any.service.com).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="endpoint">A connection string or URI describing the target profile storage endpoint and any identity/authentication information.</param>
        /// <param name="certificateManager">Provides features for reading certificates from the local system certificate stores.</param>
        public static DependencyProfileReference CreateProfileReference(string endpoint, ICertificateManager certificateManager)
        {
            DependencyProfileReference profile = null;

            if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri profileUri))
            {
                profile = EndpointUtility.CreateProfileReference(profileUri, certificateManager);
            }
            else if (EndpointUtility.IsCustomConnectionString(endpoint))
            {
                // e.g.
                // EndpointUrl=anystorage01.blob.core.windows.net/profile/ANY-PROFILE.json;ClientId=307591a4-abb2...;TenantId=985bbc17...
                IDictionary<string, string> connectionParameters = TextParsingExtensions.ParseDelimitedValues(endpoint)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                profile = EndpointUtility.CreateProfileReference(connectionParameters, certificateManager);
            }
            else
            {
                profile = new DependencyProfileReference(endpoint);
            }

            return profile;
        }

        /// <summary>
        /// Returns true/false whether the value is a custom Virtual Client connection string 
        /// (e.g. EndpointUrl=https://any.blob.core.windows.net;ManagedIdentityId=307591a4-abb2-4559-af59-b47177d140cf).
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>True if the value is a custom Virtual Client connection string. False if not.</returns>
        public static bool IsCustomConnectionString(string value)
        {
            bool isConnectionString = false;
            StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;

            if (value.Contains($"{ConnectionParameter.EndpointUrl}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.EventHubNamespace}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.ClientId}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.TenantId}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.ManagedIdentityId}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.CertificateIssuer}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.CertificateSubject}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.CertificateThumbprint}=", ignoreCase))
            {
                isConnectionString = true;
            }

            return isConnectionString;
        }

        /// <summary>
        /// Returns true/false whether the value is a custom Virtual Client URI
        /// (e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&amp;tid=985bbc17-E3a5-4fec-b0cb-40dbb8bc5959&amp;crti=ABC&amp;crts=any.service.com).
        /// </summary>
        /// <param name="endpointUri">The URI to evaluate.</param>
        /// <returns>True if the URI is a custom Virtual Client URI. False if not.</returns>
        public static bool IsCustomUri(Uri endpointUri)
        {
            return Regex.IsMatch(
                endpointUri.Query,
                $"{UriParameter.CertificateIssuer}=|{UriParameter.CertificateSubject}=|{UriParameter.CertificateThumbprint}=|{UriParameter.ClientId}=|{UriParameter.ManagedIdentityId}=|{UriParameter.TenantId}=",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the value is an Event Hub namespace access policy/connection string.
        /// (e.g. Endpoint=sb://any.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=...).
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>True if the value is an Event Hub namespace connection string. False if not.</returns>
        public static bool IsEventHubConnectionString(string value)
        {
            return Regex.IsMatch(
                value,
                $"{ConnectionParameter.Endpoint}=|{ConnectionParameter.SharedAccessKeyName}=",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the value is a file path.
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>True if the value is a fully qualified file path. False if not.</returns>
        public static bool IsFullyQualifiedFilePath(string value)
        {
            return Regex.IsMatch(value, "[A-Z]+:\\\\|^/", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the value is a standard Storage Account connection string.
        /// (e.g. DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net...).
        /// </summary>
        /// <param name="value">The value to evaluate.</param>
        /// <returns>True if the value is a  Storage Account connection string. False if not.</returns>
        public static bool IsStorageAccountConnectionString(string value)
        {
            return Regex.IsMatch(
                value,
                $"{ConnectionParameter.DefaultEndpointsProtocol}=|{ConnectionParameter.BlobEndpoint}=",
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the value is a standard Storage Account connection string.
        /// (e.g. DefaultEndpointsProtocol=https;AccountName=anystorage;EndpointSuffix=core.windows.net...).
        /// </summary>
        /// <param name="endpointUri">The URI to evaluate.</param>
        /// <returns>True if the value is a  Storage Account connection string. False if not.</returns>
        public static bool IsStorageAccountSasUri(Uri endpointUri)
        {
            return Regex.IsMatch(endpointUri.Query, "sv=|se=|spr=|sig=", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the provided endpoint uri is allowed to access package.
        /// (e.g. https://packages.virtualclient.microsoft.com)
        /// </summary>
        /// <param name="endpointUri">The URI to evaluate.</param>
        /// <param name="storeName"></param>
        /// <returns></returns>
        /// <exception cref="DependencyException"></exception>
        public static bool IsPackageUri(Uri endpointUri, string storeName)
        {
            bool packageUri = new Uri(AllowedPackageUri).Host.Equals(endpointUri.Host, StringComparison.OrdinalIgnoreCase);
            if (storeName == DependencyStore.Content && packageUri)
            {
                throw new SchemaException(
                    $"The URI provided for '--{storeName}' is not supported. {Environment.NewLine}. The value must be one of the following supported identifiers: {Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return storeName == DependencyStore.Packages && packageUri;
        }

        /// <summary>
        /// Returns the endpoint by verifying package uri checks.
        /// if the endpoint is a package uri without http or https protocols then append the protocol else return the endpoint value.
        /// </summary>
        /// <param name="endpoint">endpoint to verify and format</param>
        /// <returns></returns>
        public static string ValidateAndFormatPackageUri(string endpoint)
        {
            string packageUri = new Uri(AllowedPackageUri).Host;
            return packageUri == endpoint ? $"https://{endpoint}" : endpoint;
        }

        private static DependencyBlobStore CreateBlobStoreReference(string storeName, string connectionString)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            DependencyBlobStore store = null;
            if (EndpointUtility.IsStorageAccountConnectionString(connectionString))
            {
                // #1 - Storage account-level or container-level connection string
                store = new DependencyBlobStore(storeName, connectionString);
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Storage Account endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyBlobStore CreateBlobStoreReference(string storeName, Uri endpointUri, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyBlobStore store = null;

            if (string.IsNullOrWhiteSpace(endpointUri.Query))
            {
                // Basic URI without any query parameters
                // 1) If the given endpoint uri is a package uri (e.g. https://packages.virtualclient.microsoft.com ) then the package is retrieved from storage via CDN
                // 2) If the given endpoint uri is a blob storage (e.g https://any.blob.core.windows.net) then the packages is retrieved from blob storage 
                store = IsPackageUri(endpointUri, storeName) 
                    ? new DependencyBlobStore(storeName, endpointUri, DependencyStore.StoreTypeAzureCDN)
                    : new DependencyBlobStore(storeName, endpointUri);
            }
            else if (EndpointUtility.IsStorageAccountSasUri(endpointUri))
            {
                // 2) Storage account or blob container SAS Uri
                //    e.g. https://any.blob.core.windows.net/?sv=2022-11-02&ss=b&srt=co&sp=rtf&se=2024-07-02T05:15:29Z&st=2024-07-01T21:15:29Z&spr=https

                store = new DependencyBlobStore(storeName, endpointUri);
            }
            else if (EndpointUtility.IsCustomUri(endpointUri))
            {
                // 3) URI for Microsoft Entra or Managed Identity
                //    e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetManagedIdentityReferenceForUri(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraReferenceForUri(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                            .GetAwaiter().GetResult();
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://any.service.azure.com/

                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    store = new DependencyBlobStore(storeName, baseUri, credential);
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Storage Account endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyBlobStore CreateBlobStoreReference(string storeName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyBlobStore store = null;

            if (EndpointUtility.TryGetEndpointForConnection(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetManagedIdentityReferenceForConnection(connectionParameters, out string managedIdentityId))
                    {
                        credential = new ManagedIdentityCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetMicrosoftEntraReferenceForConnection(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateThumbprint))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                                .GetAwaiter().GetResult();
                        }
                        else if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                                .GetAwaiter().GetResult();
                        }
                    }

                    if (credential != null)
                    {
                        store = new DependencyBlobStore(storeName, endpointUri, credential);
                    }
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Storage Account endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyEventHubStore CreateEventHubStoreReference(string storeName, string connectionString)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            DependencyEventHubStore store = null;

            if (EndpointUtility.IsEventHubConnectionString(connectionString))
            {
                // #1 - Storage account-level or container-level connection string
                store = new DependencyEventHubStore(storeName, connectionString);
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Event Hub endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Event Hub namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0610-integration-event-hub/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyEventHubStore CreateEventHubStoreReference(string storeName, Uri endpointUri, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyEventHubStore store = null;

            if (EndpointUtility.IsCustomUri(endpointUri))
            {
                // URI for Microsoft Entra or Managed Identity
                // e.g. sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetManagedIdentityReferenceForUri(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraReferenceForUri(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                            .GetAwaiter().GetResult();
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // sb://any.servicebus.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> sb://any.servicebus.azure.com/

                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    store = new DependencyEventHubStore(storeName, baseUri, credential);
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Event Hub endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Event Hub namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0610-integration-event-hub/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyEventHubStore CreateEventHubStoreReference(string storeName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyEventHubStore store = null;

            if (EndpointUtility.TryGetEndpointForConnection(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetManagedIdentityReferenceForConnection(connectionParameters, out string managedIdentityId))
                    {
                        credential = new ManagedIdentityCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetMicrosoftEntraReferenceForConnection(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateThumbprint))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                                .GetAwaiter().GetResult();
                        }
                        else if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                                .GetAwaiter().GetResult();
                        }
                    }

                    if (credential != null)
                    {
                        store = new DependencyEventHubStore(storeName, endpointUri, credential);
                    }
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Event Hub endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Event Hub namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0610-integration-event-hub/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyKeyVaultStore CreateKeyVaultStoreReference(string storeName, Uri endpointUri, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyKeyVaultStore store = null;

            if (EndpointUtility.IsCustomUri(endpointUri))
            {
                // URI for Microsoft Entra or Managed Identity
                // e.g. https://my-keyvault.vault.azure.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crti=ABC&crts=any.service.com)

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(endpointUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetManagedIdentityReferenceForUri(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraReferenceForUri(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                            .GetAwaiter().GetResult();
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://my-keyvault.vault.azure.net/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://my-keyvault.vault.azure.net/

                    Uri baseUri = new Uri(endpointUri.OriginalString.Substring(0, endpointUri.OriginalString.IndexOf("?")));
                    store = new DependencyKeyVaultStore(storeName, baseUri, credential);
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Key Vault endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Key Vault access policy/connection string{Environment.NewLine}" +
                    $"2) A URI with Microsoft Entra ID/App identity information(e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}");
            }

            return store;
        }

        private static DependencyKeyVaultStore CreateKeyVaultStoreReference(string storeName, IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyKeyVaultStore store = null;

            if (EndpointUtility.TryGetEndpointForConnection(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetManagedIdentityReferenceForConnection(connectionParameters, out string managedIdentityId))
                    {
                        credential = new ManagedIdentityCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetMicrosoftEntraReferenceForConnection(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateThumbprint))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                                .GetAwaiter().GetResult();
                        }
                        else if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                                .GetAwaiter().GetResult();
                        }
                    }

                    if (credential != null)
                    {
                        store = new DependencyKeyVaultStore(storeName, endpointUri, credential);
                    }
                }
            }

            if (store == null)
            {
                throw new SchemaException(
                    $"The value provided for the Key Vault endpoint is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid Key Vault namespace access policy/connection string{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}");
            }

            return store;
        }

        private static async Task<TokenCredential> CreateIdentityTokenCredentialAsync(ICertificateManager certificateManager, string clientId, string tenantId, string certificateThumbprint)
        {
            certificateManager.ThrowIfNull(nameof(certificateManager));
            clientId.ThrowIfNullOrWhiteSpace(nameof(clientId));
            tenantId.ThrowIfNullOrWhiteSpace(nameof(tenantId));
            certificateThumbprint.ThrowIfNullOrWhiteSpace(nameof(certificateThumbprint));

            // Always search CurrentUser/My store first.
            PlatformID platform = Environment.OSVersion.Platform;
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (platform == PlatformID.Win32NT)
            {
                // There is no local machine store on Unix/Linux systems. This store is available on
                // Windows only.
                storeLocations.Add(StoreLocation.LocalMachine);
            }

            ClientCertificateCredentialOptions credentialOptions = new ClientCertificateCredentialOptions
            {
                // Required to support integration with Microsoft Entra ID/Apps for certificate subject names defined
                // in the "trustedCertificateSubjects" section of the Microsoft Entra ID/App manifest.
                SendCertificateChain = true
            };

            X509Certificate2 certificate = null;

            if (platform == PlatformID.Unix)
            {
                string currentUser = Environment.UserName;

                try
                {
                    certificate = await certificateManager.GetCertificateFromStoreAsync(
                        certificateThumbprint,
                        storeLocations,
                        storeName);
                }
                catch (CryptographicException) when (currentUser?.ToLowerInvariant() == "root")
                {
                    // Backup:
                    // We are likely running as sudo/root. The .NET SDK will
                    // look for the certificate in the location specific to 'root'
                    // by default. We want to try the current user location as well.
                    PlatformSpecifics platformSpecifics = new PlatformSpecifics(
                        Environment.OSVersion.Platform,
                        RuntimeInformation.ProcessArchitecture);

                    currentUser = platformSpecifics.GetLoggedInUser();

                    certificate = await certificateManager.GetCertificateFromPathAsync(
                        certificateThumbprint,
                        string.Format(CertificateManager.DefaultUnixCertificateDirectory, currentUser));
                }
            }
            else
            {
                certificate = await certificateManager.GetCertificateFromStoreAsync(certificateThumbprint, storeLocations, storeName);
            }

            return new ClientCertificateCredential(tenantId, clientId, certificate, credentialOptions);
        }

        private static async Task<TokenCredential> CreateIdentityTokenCredentialAsync(ICertificateManager certificateManager, string clientId, string tenantId, string certificateIssuer, string certificateSubject)
        {
            certificateManager.ThrowIfNull(nameof(certificateManager));
            clientId.ThrowIfNullOrWhiteSpace(nameof(clientId));
            tenantId.ThrowIfNullOrWhiteSpace(nameof(tenantId));
            certificateIssuer.ThrowIfNullOrWhiteSpace(nameof(certificateIssuer));
            certificateSubject.ThrowIfNullOrWhiteSpace(nameof(certificateSubject));

            // Always search CurrentUser/My store first.
            PlatformID platform = Environment.OSVersion.Platform;
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (platform == PlatformID.Win32NT)
            {
                // There is no local machine store on Unix/Linux systems. This store is available on
                // Windows only.
                storeLocations.Add(StoreLocation.LocalMachine);
            }

            ClientCertificateCredentialOptions credentialOptions = new ClientCertificateCredentialOptions
            {
                // Required to support integration with Microsoft Entra ID/Apps for certificate subject names defined
                // in the "trustedCertificateSubjects" section of the Microsoft Entra ID/App manifest.
                SendCertificateChain = true
            };

            X509Certificate2 certificate = null;

            if (platform == PlatformID.Unix)
            {
                string currentUser = Environment.UserName;

                try
                {
                    certificate = await certificateManager.GetCertificateFromStoreAsync(
                        certificateIssuer,
                        certificateSubject,
                        storeLocations,
                        storeName);
                }
                catch (CryptographicException) when (currentUser?.ToLowerInvariant() == "root")
                {
                    // Backup:
                    // We are likely running as sudo/root. The .NET SDK will
                    // look for the certificate in the location specific to 'root'
                    // by default. We want to try the current user location as well.
                    PlatformSpecifics platformSpecifics = new PlatformSpecifics(
                        Environment.OSVersion.Platform,
                        RuntimeInformation.ProcessArchitecture);

                    currentUser = platformSpecifics.GetLoggedInUser();

                    certificate = await certificateManager.GetCertificateFromPathAsync(
                        certificateIssuer,
                        certificateSubject,
                        string.Format(CertificateManager.DefaultUnixCertificateDirectory, currentUser));
                }
            }
            else
            {
                certificate = await certificateManager.GetCertificateFromStoreAsync(
                    certificateIssuer, 
                    certificateSubject, 
                    storeLocations, 
                    storeName);
            }

            return new ClientCertificateCredential(tenantId, clientId, certificate, credentialOptions);
        }

        private static DependencyProfileReference CreateProfileReference(Uri profileUri, ICertificateManager certificateManager)
        {
            profileUri.ThrowIfNull(nameof(profileUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyProfileReference reference = null;

            // Remote profile for download.
            string profileName = profileUri.Segments.Last();

            if (string.IsNullOrWhiteSpace(profileUri.Query))
            {
                // A URI to a profile that does not require any specific authentication (e.g. anonymous auth).
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json
                //
                // or a SAS URI
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https

                reference = new DependencyProfileReference(profileUri);
            }
            else if (EndpointUtility.IsStorageAccountSasUri(profileUri))
            {
                // or a SAS URI
                // e.g. https://anystorage.blob.core.windows.net/profiles/ANY-PROFILE.json?sv=2022-11-02&ss=b&srt=co&sp=rt&se=2024-07-02T22:26:42Z&st=2024-07-02T14:26:42Z&spr=https

                reference = new DependencyProfileReference(profileUri);
            }
            else if (EndpointUtility.IsCustomUri(profileUri))
            {
                // Custom URI
                // e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&crtt=1753429a8bc4f91d

                // We unescape any URI-encoded characters (e.g. spaces -> %20).
                string queryString = Uri.UnescapeDataString(profileUri.Query).Trim('?').Replace("&", ",,,");

                IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value?.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                TokenCredential credential = null;
                if (EndpointUtility.TryGetManagedIdentityReferenceForUri(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraReferenceForUri(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateReferenceForUri(queryParameters, out string certificateIssuer, out string certificateSubject))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                            .GetAwaiter().GetResult();
                    }
                }

                if (credential != null)
                {
                    // e.g.
                    // https://any.service.azure.com/?miid=307591a4-abb2-4559-af59-b47177d140cf -> https://any.service.azure.com/

                    Uri baseUri = new Uri(profileUri.OriginalString.Substring(0, profileUri.OriginalString.IndexOf("?")));
                    reference = new DependencyProfileReference(baseUri, credential);
                }
            }

            if (reference == null)
            {
                throw new SchemaException(
                    $"The value provided for the profile reference is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"2) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"4) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return reference;
        }

        private static DependencyProfileReference CreateProfileReference(IDictionary<string, string> connectionParameters, ICertificateManager certificateManager)
        {
            connectionParameters.ThrowIfNullOrEmpty(nameof(connectionParameters));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyProfileReference profile = null;

            if (EndpointUtility.TryGetEndpointForConnection(connectionParameters, out string endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri endpointUri))
                {
                    TokenCredential credential = null;
                    if (EndpointUtility.TryGetManagedIdentityReferenceForConnection(connectionParameters, out string managedIdentityId))
                    {
                        credential = new ManagedIdentityCredential(managedIdentityId);
                    }
                    else if (EndpointUtility.TryGetMicrosoftEntraReferenceForConnection(connectionParameters, out string clientId, out string tenantId))
                    {
                        if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateThumbprint))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                                .GetAwaiter().GetResult();
                        }
                        else if (EndpointUtility.TryGetCertificateReferenceForConnection(connectionParameters, out string certificateIssuer, out string certificateSubject))
                        {
                            credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateIssuer, certificateSubject)
                                .GetAwaiter().GetResult();
                        }
                    }

                    if (credential != null)
                    {
                        profile = new DependencyProfileReference(endpointUri, credential);
                    }
                }
            }

            if (profile == null)
            {
                throw new SchemaException(
                    $"The value provided for the profile reference is invalid. The value must be one of the following supported identifiers:{Environment.NewLine}" +
                    $"1) The name of an out-of-box profile {Environment.NewLine}" +
                    $"2) A valid storage account or blob container SAS URI{Environment.NewLine}" +
                    $"3) A connection string or URI with Microsoft Entra ID/App information (e.g. using certificate-based authentication){Environment.NewLine}" +
                    $"4) A connection string or URI with Microsoft Azure Managed Identity information{Environment.NewLine}" +
                    $"5) A directory path that exists on the system.{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}" +
                    $"See the following documentation for additional details and examples:{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0010-command-line/{Environment.NewLine}" +
                    $"- https://microsoft.github.io/VirtualClient/docs/guides/0600-integration-blob-storage/{Environment.NewLine}");
            }

            return profile;
        }

        private static bool TryGetEndpointForConnection(IDictionary<string, string> connectionParameters, out string endpoint)
        {
            bool endpointDefined = false;
            endpoint = null;

            if (connectionParameters?.Any() == true)
            {
                if ((connectionParameters.TryGetValue(ConnectionParameter.Endpoint, out endpoint) || connectionParameters.TryGetValue(ConnectionParameter.EndpointUrl, out endpoint))
                    && !string.IsNullOrWhiteSpace(endpoint))
                {
                    endpointDefined = true;
                }
            }

            return endpointDefined;
        }

        private static bool TryGetCertificateReferenceForConnection(IDictionary<string, string> uriParameters, out string certificateThumbPrint)
        {
            bool parametersDefined = false;
            certificateThumbPrint = null;

            if (uriParameters?.Any() == true)
            {
                if (uriParameters.TryGetValue(ConnectionParameter.CertificateThumbprint, out string thumbprint)
                    && !string.IsNullOrWhiteSpace(thumbprint))
                {
                    certificateThumbPrint = thumbprint;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetCertificateReferenceForConnection(IDictionary<string, string> connectionParameters, out string certificateIssuer, out string certificateSubject)
        {
            bool parametersDefined = false;
            certificateIssuer = null;
            certificateSubject = null;

            if (connectionParameters?.Any() == true)
            {
                if (connectionParameters.TryGetValue(ConnectionParameter.CertificateIssuer, out string issuer)
                    && connectionParameters.TryGetValue(ConnectionParameter.CertificateSubject, out string subject)
                    && !string.IsNullOrWhiteSpace(issuer)
                    && !string.IsNullOrWhiteSpace(subject))
                {
                    certificateIssuer = issuer;
                    certificateSubject = subject;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetManagedIdentityReferenceForConnection(IDictionary<string, string> connectionParameters, out string managedIdentityId)
        {
            bool parametersDefined = false;
            managedIdentityId = null;

            if (connectionParameters?.Any() == true)
            {
                if (connectionParameters.TryGetValue(ConnectionParameter.ManagedIdentityId, out string managedIdentityClientId)
                    && !string.IsNullOrWhiteSpace(managedIdentityClientId))
                {
                    managedIdentityId = managedIdentityClientId;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetMicrosoftEntraReferenceForConnection(IDictionary<string, string> connectionParameters, out string clientId, out string tenantId)
        {
            bool parametersDefined = false;
            clientId = null;
            tenantId = null;

            if (connectionParameters?.Any() == true)
            {
                if (connectionParameters.TryGetValue(ConnectionParameter.ClientId, out string microsoftEntraClientId)
                    && connectionParameters.TryGetValue(ConnectionParameter.TenantId, out string microsoftEntraTenantId)
                    && !string.IsNullOrWhiteSpace(microsoftEntraClientId)
                    && !string.IsNullOrWhiteSpace(microsoftEntraTenantId))
                {
                    clientId = microsoftEntraClientId;
                    tenantId = microsoftEntraTenantId;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetCertificateReferenceForUri(IDictionary<string, string> uriParameters, out string certificateThumbPrint)
        {
            bool parametersDefined = false;
            certificateThumbPrint = null;

            if (uriParameters?.Any() == true)
            {
                if (uriParameters.TryGetValue(UriParameter.CertificateThumbprint, out string thumbprint)
                    && !string.IsNullOrWhiteSpace(thumbprint))
                {
                    certificateThumbPrint = thumbprint;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetCertificateReferenceForUri(IDictionary<string, string> uriParameters, out string certificateIssuer, out string certificateSubject)
        {
            bool parametersDefined = false;
            certificateIssuer = null;
            certificateSubject = null;

            if (uriParameters?.Any() == true)
            {
                if (uriParameters.TryGetValue(UriParameter.CertificateIssuer, out string issuer)
                    && uriParameters.TryGetValue(UriParameter.CertificateSubject, out string subject)
                    && !string.IsNullOrWhiteSpace(issuer)
                    && !string.IsNullOrWhiteSpace(subject))
                {
                    certificateIssuer = issuer;
                    certificateSubject = subject;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetManagedIdentityReferenceForUri(IDictionary<string, string> uriParameters, out string managedIdentityId)
        {
            bool parametersDefined = false;
            managedIdentityId = null;

            if (uriParameters?.Any() == true)
            {
                if (uriParameters.TryGetValue(UriParameter.ManagedIdentityId, out string managedIdentityClientId)
                    && !string.IsNullOrWhiteSpace(managedIdentityClientId))
                {
                    managedIdentityId = managedIdentityClientId;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetMicrosoftEntraReferenceForUri(IDictionary<string, string> uriParameters, out string clientId, out string tenantId)
        {
            bool parametersDefined = false;
            clientId = null;
            tenantId = null;

            if (uriParameters?.Any() == true)
            {
                if (uriParameters.TryGetValue(UriParameter.ClientId, out string microsoftEntraClientId)
                    && uriParameters.TryGetValue(UriParameter.TenantId, out string microsoftEntraTenantId)
                    && !string.IsNullOrWhiteSpace(microsoftEntraClientId)
                    && !string.IsNullOrWhiteSpace(microsoftEntraTenantId))
                {
                    clientId = microsoftEntraClientId;
                    tenantId = microsoftEntraTenantId;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }
    }
}
