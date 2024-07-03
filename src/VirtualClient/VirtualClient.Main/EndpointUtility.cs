// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// 
    /// </summary>
    internal static class EndpointUtility
    {
        /// <summary>
        /// Converts the custom connection properties into a URI form.
        /// </summary>
        /// <param name="connectionProperties">A set of properties describing and endpoint connection to convert into a URI-style endpoint definition.</param>
        /// <returns>A URI that represents the connection and authentication details of a target endpoint.</returns>
        public static Uri ConvertToUri(IDictionary<string, string> connectionProperties)
        {
            connectionProperties.ThrowIfNullOrEmpty(nameof(connectionProperties));

            Uri endpointUri = null;

            string endpoint = null;
            if ((connectionProperties.TryGetValue(ConnectionParameter.Endpoint, out endpoint) || connectionProperties.TryGetValue(ConnectionParameter.EndpointUrl, out endpoint))
                && !string.IsNullOrWhiteSpace(endpoint))
            {
                if (Uri.TryCreate(endpoint, UriKind.Absolute, out Uri baseEndpoint))
                {
                    IDictionary<string, string> queryStringParameters = new Dictionary<string, string>();

                    if (connectionProperties.TryGetValue(ConnectionParameter.ManagedIdentityId, out string managedIdentityId))
                    {
                        queryStringParameters.Add(UriParameter.ManagedIdentityId, managedIdentityId);

                    }
                    else if (connectionProperties.TryGetValue(ConnectionParameter.ClientId, out string clientId)
                        && connectionProperties.TryGetValue(ConnectionParameter.TenantId, out string tenantId)
                        && !string.IsNullOrWhiteSpace(clientId)
                        && !string.IsNullOrWhiteSpace(tenantId))
                    {
                        if (connectionProperties.TryGetValue(ConnectionParameter.CertificateThumbprint, out string certificateThumbprint)
                            && !string.IsNullOrWhiteSpace(certificateThumbprint))
                        {
                            queryStringParameters.Add(UriParameter.ClientId, clientId);
                            queryStringParameters.Add(UriParameter.TenantId, tenantId);
                            queryStringParameters.Add(UriParameter.CertificateThumbprint, certificateThumbprint);
                        }
                        else if (connectionProperties.TryGetValue(ConnectionParameter.CertificateIssuer, out string certificateIssuer)
                            && connectionProperties.TryGetValue(ConnectionParameter.CertificateSubject, out string certificatesubject)
                            && !string.IsNullOrWhiteSpace(certificateIssuer)
                            && !string.IsNullOrWhiteSpace(certificatesubject))
                        {
                            queryStringParameters.Add(UriParameter.ClientId, clientId);
                            queryStringParameters.Add(UriParameter.TenantId, tenantId);
                            queryStringParameters.Add(UriParameter.CertificateIssuer, certificateIssuer);
                            queryStringParameters.Add(UriParameter.CertificateSubject, certificatesubject);
                        }
                    }

                    if (queryStringParameters.Any())
                    {
                        string baseUri = baseEndpoint.ToString().TrimEnd(new char[] { '/', '?' });
                        string queryString = string.Join("&", queryStringParameters.Select(entry => $"{entry.Key}={entry.Value}"));

                        endpointUri = new Uri($"{baseUri}?{queryString}");
                    }
                    else
                    {
                        endpointUri = baseEndpoint;
                    }
                }
            }

            if (endpointUri == null)
            {
                throw new SchemaException(
                    $"Invalid connection definition. Valid connection definitions must include an endpoint URI at a minimum. There may be additional " +
                    $"properties that define authentication details (e.g. Azure Managed Identities, Microsoft Entra ID/Apps, certificate references).");
            }

            return endpointUri;
        }

        /// <summary>
        /// Returns true/false whether the connection parameters contains valid identity parameters.
        /// </summary>
        /// <param name="connectionProperties">The connection properties to evaluate.</param>
        /// <returns>True if valid identity parameters are provided.</returns>
        internal static bool ContainsValidIdentityParameters(IDictionary<string, string> connectionProperties)
        {
            // We support the following types of identities:
            // 1) Azure Managed Identities
            // 2) Microsoft Entra ID/Apps with certificates

            bool parametersAreValid = false;
            if (EndpointUtility.TryGetManagedIdentityParameters(connectionProperties, out string managedIdentityId))
            {
                parametersAreValid = true;
            }
            else if (EndpointUtility.TryGetMicrosoftEntraIdParameters(connectionProperties, out string clientId, out string tenantId))
            {
                if (EndpointUtility.TryGetCertificateThumbprintParameters(connectionProperties, out string certificateThumbprint)
                    || EndpointUtility.TryGetCertificateIssuerAndSubjectParameters(connectionProperties, out string certificateIssuer, out string certificateSubject))
                {
                    parametersAreValid = true;
                }
            }

            return parametersAreValid;
        }

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
        /// <param name="connectionString">A connection string describing the target storage endpoint and identity/authentication information.</param>
        public static DependencyBlobStore CreateBlobStoreReference(string storeName, string connectionString)
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

        /// <summary>
        /// Creates a <see cref="DependencyBlobStore"/> definition from the endpoint URI provided. 
        /// <list>
        /// <item>The following type of URIs are supported:</item>
        /// <list type="bullet">
        /// <item>Storage account or blob container SAS URI<br/>(e.g. https://anystorage.blob.core.windows.net/?sv=2022-11-02&amp;ss=b&amp;srt=co&amp;sp=rtf&amp;se=2024-07-02T05:15:29Z&amp;st=2024-07-01T21:15:29Z&amp;spr=https).</item>
        /// <item>Microsoft Entra or Managed Identity referencing URI<br/>(e.g. https://any.service.azure.com/?cid=307591a4-abb2-4559-af59-b47177d140cf&amp;tid=985bbc17-E3A5-4fec-b0cb-40dbb8bc5959&amp;crti=ABC&amp;crts=any.service.com).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="storeName">The name of the dependency store (e.g. Content, Packages).</param>
        /// <param name="endpointUri">A URI describing the target storage endpoint and identity/authentication information (e.g. https://any.blob.core.windows.net/?miid=307591a4-abb2-4559-af59-b47177d140cf).</param>
        /// <param name="certificateManager">Provides features for reading certificates from the local system certificate stores.</param>
        public static DependencyBlobStore CreateBlobStoreReference(string storeName, Uri endpointUri, ICertificateManager certificateManager)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            endpointUri.ThrowIfNull(nameof(endpointUri));
            certificateManager.ThrowIfNull(nameof(certificateManager));

            DependencyBlobStore store = null;

            if (string.IsNullOrWhiteSpace(endpointUri.Query))
            {
                // 1) Basic URI without any query parameters
                //    e.g. https://any.blob.core.windows.net

                store = new DependencyBlobStore(storeName, endpointUri);
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
                if (EndpointUtility.TryGetManagedIdentityParameters(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraIdParameters(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateThumbprintParameters(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateIssuerAndSubjectParameters(queryParameters, out string certificateIssuer, out string certificateSubject))
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
        /// <param name="connectionString">A connection string describing the target storage endpoint and identity/authentication information.</param>
        public static DependencyEventHubStore CreateEventHubStoreReference(string storeName, string connectionString)
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

        /// <summary>
        /// Creates a <see cref="DependencyEventHubStore"/> definition from the endpoint URI provided.
        /// <list>
        /// <item>The following type of URIs are supported:</item>
        /// <list type="bullet">
        /// <item>Microsoft Entra or Managed Identity referencing URI<br/>(e.g. sb://any.servicebus.windows.net/?cid=307591a4-abb2-4559-af59-b47177d140cf&amp;tid=985bbc17-e3a5-4fec-b0cb-40dbb8bc5959&amp;crti=ABC&amp;crts=any.service.com).</item>
        /// </list>
        /// </list>
        /// </summary>
        /// <param name="storeName">The name of the dependency store (e.g. Content, Packages).</param>
        /// <param name="endpointUri">A URI describing the target storage endpoint and identity/authentication information (e.g. sb://any.servicebus.windows.net/?miid=307591a4-abb2-4559-af59-b47177d140cf).</param>
        /// <param name="certificateManager">Provides features for reading certificates from the local system certificate stores.</param>
        public static DependencyEventHubStore CreateEventHubStoreReference(string storeName, Uri endpointUri, ICertificateManager certificateManager)
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
                if (EndpointUtility.TryGetManagedIdentityParameters(queryParameters, out string managedIdentityId))
                {
                    credential = new ManagedIdentityCredential(managedIdentityId);
                }
                else if (EndpointUtility.TryGetMicrosoftEntraIdParameters(queryParameters, out string clientId, out string tenantId))
                {
                    if (EndpointUtility.TryGetCertificateThumbprintParameters(queryParameters, out string certificateThumbprint))
                    {
                        credential = EndpointUtility.CreateIdentityTokenCredentialAsync(certificateManager, clientId, tenantId, certificateThumbprint)
                            .GetAwaiter().GetResult();
                    }
                    else if (EndpointUtility.TryGetCertificateIssuerAndSubjectParameters(queryParameters, out string certificateIssuer, out string certificateSubject))
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

            if (value.Contains($"{ConnectionParameter.Endpoint}=", ignoreCase)
                || value.Contains($"{ConnectionParameter.EndpointUrl}=", ignoreCase)
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

        private static async Task<TokenCredential> CreateIdentityTokenCredentialAsync(ICertificateManager certificateManager, string clientId, string tenantId, string certificateThumbprint)
        {
            certificateManager.ThrowIfNull(nameof(certificateManager));
            clientId.ThrowIfNullOrWhiteSpace(nameof(clientId));
            tenantId.ThrowIfNullOrWhiteSpace(nameof(tenantId));
            certificateThumbprint.ThrowIfNullOrWhiteSpace(nameof(certificateThumbprint));

            // Always search CurrentUser/My store first.
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

            X509Certificate2 certificate = await certificateManager.GetCertificateFromStoreAsync(certificateThumbprint, storeLocations, storeName);

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
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
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

            X509Certificate2 certificate = await certificateManager.GetCertificateFromStoreAsync(certificateIssuer, certificateSubject, storeLocations, storeName);

            return new ClientCertificateCredential(tenantId, clientId, certificate, credentialOptions);
        }

        private static bool TryGetEndpoint(IDictionary<string, string> connectionProperties, out string endpoint)
        {
            bool endpointDefined = false;
            endpoint = null;

            if (connectionProperties?.Any() == true)
            {
                if ((connectionProperties.TryGetValue(ConnectionParameter.Endpoint, out endpoint) || connectionProperties.TryGetValue(ConnectionParameter.EndpointUrl, out endpoint))
                    && !string.IsNullOrWhiteSpace(endpoint))
                {
                    endpointDefined = true;
                }
            }

            return endpointDefined;
        }

        private static bool TryGetCertificateIssuerAndSubjectParameters(IDictionary<string, string> queryParameters, out string certificateIssuer, out string certificateSubject)
        {
            bool parametersDefined = false;
            certificateIssuer = null;
            certificateSubject = null;

            if (queryParameters?.Any() == true)
            {
                if (queryParameters.TryGetValue(UriParameter.CertificateIssuer, out string issuer)
                    && queryParameters.TryGetValue(UriParameter.CertificateSubject, out string subject)
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

        private static bool TryGetCertificateThumbprintParameters(IDictionary<string, string> queryParameters, out string certificateThumbPrint)
        {
            bool parametersDefined = false;
            certificateThumbPrint = null;

            if (queryParameters?.Any() == true)
            {
                if (queryParameters.TryGetValue(UriParameter.CertificateThumbprint, out string thumbprint)
                    && !string.IsNullOrWhiteSpace(thumbprint))
                {
                    certificateThumbPrint = thumbprint;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetManagedIdentityParameters(IDictionary<string, string> queryParameters, out string managedIdentityId)
        {
            bool parametersDefined = false;
            managedIdentityId = null;

            if (queryParameters?.Any() == true)
            {
                if (queryParameters.TryGetValue(UriParameter.ManagedIdentityId, out string managedIdentityClientId)
                    && !string.IsNullOrWhiteSpace(managedIdentityClientId))
                {
                    managedIdentityId = managedIdentityClientId;
                    parametersDefined = true;
                }
            }

            return parametersDefined;
        }

        private static bool TryGetMicrosoftEntraIdParameters(IDictionary<string, string> queryParameters, out string clientId, out string tenantId)
        {
            bool parametersDefined = false;
            clientId = null;
            tenantId = null;

            if (queryParameters?.Any() == true)
            {
                if (queryParameters.TryGetValue(UriParameter.ClientId, out string microsoftEntraClientId)
                    && queryParameters.TryGetValue(UriParameter.TenantId, out string microsoftEntraTenantId)
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
