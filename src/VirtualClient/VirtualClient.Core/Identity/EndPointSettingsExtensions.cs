using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VirtualClient.Contracts;

namespace VirtualClient.Identity
{
    internal static class EndPointSettingsExtensions
    {
        internal const string AllowedPackageUri = "https://packages.virtualclient.microsoft.com";

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
        /// Parses the subject name and issuer from the provided uri. If the uri does not contain the correctly formatted certificate subject name
        /// and issuer information the method will return false, and keep the two out parameters as null.
        /// Ex. https://vegaprod01proxyapi.azurewebsites.net?crti=issuerName&amp;crts=certSubject
        /// </summary>
        /// <param name="uri">The uri to attempt to parse the values from.</param>
        /// <param name="issuer">The issuer of the certificate.</param>
        /// <param name="subject">The subject of the certificate.</param>
        /// <returns>True/False if the method was able to successfully parse both the subject name and the issuer of the certificate.</returns>
        public static bool TryParseCertificateReference(Uri uri, out string issuer, out string subject)
        {
            string queryString = Uri.UnescapeDataString(uri.Query).Trim('?').Replace("&", ",,,");

            IDictionary<string, string> queryParameters = TextParsingExtensions.ParseDelimitedValues(queryString)?.ToDictionary(
                entry => entry.Key,
                entry => entry.Value?.ToString(),
                StringComparer.OrdinalIgnoreCase);

            return TryGetCertificateReferenceForUri(queryParameters, out issuer, out subject);
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

        internal static bool TryGetEndpointForConnection(IDictionary<string, string> connectionParameters, out string endpoint)
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

        internal static bool TryGetCertificateReferenceForConnection(IDictionary<string, string> uriParameters, out string certificateThumbPrint)
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

        internal static bool TryGetCertificateReferenceForConnection(IDictionary<string, string> connectionParameters, out string certificateIssuer, out string certificateSubject)
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

        internal static bool TryGetManagedIdentityReferenceForConnection(IDictionary<string, string> connectionParameters, out string managedIdentityId)
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

        internal static bool TryGetMicrosoftEntraReferenceForConnection(IDictionary<string, string> connectionParameters, out string clientId, out string tenantId)
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

        internal static bool TryGetCertificateReferenceForUri(IDictionary<string, string> uriParameters, out string certificateThumbPrint)
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

        internal static bool TryGetCertificateReferenceForUri(IDictionary<string, string> uriParameters, out string certificateIssuer, out string certificateSubject)
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

        internal static bool TryGetManagedIdentityReferenceForUri(IDictionary<string, string> uriParameters, out string managedIdentityId)
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

        internal static bool TryGetMicrosoftEntraReferenceForUri(IDictionary<string, string> uriParameters, out string clientId, out string tenantId)
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
