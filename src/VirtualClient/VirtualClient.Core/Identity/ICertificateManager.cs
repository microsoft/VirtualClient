// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for certificate manager
    /// </summary>
    public interface ICertificateManager
    {
        /// <summary>
        /// Get certificate from specified directory path that matches the specified thumbprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint/hash (e.g. SHA1) for the certificate.</param>
        /// <param name="directoryPath">The directory path location in which to search for the certificate.</param>
        Task<X509Certificate2> GetCertificateFromPathAsync(string thumbprint, string directoryPath);

        /// <summary>
        /// Get certificate from specified directory path that matches the specified issuer and subject.
        /// </summary>
        /// <param name="issuer">
        /// The issuer of the certificate. This can be a fully qualified name or parts of it (e.g. ABC Infra CA 01 or CN=ABC Infra CA 01, DC=ABC, DC=COM).
        /// </param>
        /// <param name="subjectName">
        /// The subject name for the certificate. This can be a fully qualified name or parts of it (e.g. any.service.azure.com or CN=any.service.azure.com).
        /// </param>
        /// <param name="directoryPath">The directory path location in which to search for the certificate.</param>
        Task<X509Certificate2> GetCertificateFromPathAsync(string issuer, string subjectName, string directoryPath);

        /// <summary>
        /// Get certificate from specified store that matches the specified thumbprint.
        /// </summary>
        /// <param name="thumbprint">The thumbprint/hash (e.g. SHA1) for the certificate.</param>
        /// <param name="storeName">The certificate store in which the certificates matching the thumbprint are stored (default = My/Personal).</param>
        /// <param name="storeLocations">
        /// The certificate store location in which the certificates matching the thumbprint are stored. Default is to look at current user first, if not found, look at localmachine next.
        /// </param>
        Task<X509Certificate2> GetCertificateFromStoreAsync(string thumbprint, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My);

        /// <summary>
        /// Get certificate from specified store that matches the specified issuer and subject.
        /// </summary>
        /// <param name="storeName">The certificate store in which the certificates matching the thumbprint are stored (default = My/Personal).</param>
        /// <param name="storeLocations">
        /// The certificate store location in which the certificates matching the thumbprint are stored. Default is to look at current user first, if not found, look at localmachine next.
        /// </param>
        /// <param name="issuer">
        /// The issuer of the certificate. This can be a fully qualified name or parts of it (e.g. ABC Infra CA 01 or CN=ABC Infra CA 01, DC=ABC, DC=COM).
        /// </param>
        /// <param name="subjectName">
        /// The subject name for the certificate. This can be a fully qualified name or parts of it (e.g. any.service.azure.com or CN=any.service.azure.com).
        /// </param>
        Task<X509Certificate2> GetCertificateFromStoreAsync(string issuer, string subjectName, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My);
    }
}