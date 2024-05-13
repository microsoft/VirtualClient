// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace VirtualClient
{
    /// <summary>
    /// Interface for certificate manager
    /// </summary>
    public interface ICertificateManager
    {
        /// <summary>
        /// Get certificate from specified store that matches the specified thumbprint.
        /// </summary>
        /// <param name="storeName">The certificate store in which the certificates matching the thumbprint are stored (default = My/Personal).</param>
        /// <param name="storeLocations">
        /// The certificate store location in which the certificates matching the thumbprint are stored. Default is to look at current user first, if not found, look at localmachine next.
        /// </param>
        /// <param name="thumbprint">Certificate thumbprint.</param>
        /// <returns>Certificate.</returns>
        Task<X509Certificate2> GetCertificateFromStoreAsync(string thumbprint, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My);

        /// <summary>
        /// Get certificate from specified store that matches the specified thumbprint.
        /// </summary>
        /// <param name="storeName">The certificate store in which the certificates matching the thumbprint are stored (default = My/Personal).</param>
        /// <param name="storeLocations">
        /// The certificate store location in which the certificates matching the thumbprint are stored. Default is to look at current user first, if not found, look at localmachine next.
        /// </param>
        /// <param name="issuer">Certificate issuer.</param>
        /// <param name="subjectName">Certificate subject name.</param>
        /// <returns>Certificate.</returns>
        Task<X509Certificate2> GetCertificateFromStoreAsync(string issuer, string subjectName, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My);

        /// <summary>
        /// Verify specified certificate.
        /// </summary>
        /// <param name="certificate">Certificate to verify.</param>
        /// <param name="subject">Expected certificate subject.</param>
        /// <param name="issuer">Expected certificate issuer.</param>
        /// <param name="thumbprint">Expected certificate thumbprint.</param>
        /// <returns>True if verification passes; false if it fails.</returns>
        bool VerifyCertificate(X509Certificate2 certificate, string subject, string issuer, string thumbprint);
    }
}