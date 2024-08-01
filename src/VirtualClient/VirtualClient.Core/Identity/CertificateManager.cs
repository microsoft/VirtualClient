// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Manager for certificate management.
    /// </summary>
    public class CertificateManager : ICertificateManager
    {
        private static readonly Regex CommonNameExpression = new Regex("CN=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Normalizes the certificate thumbprint ensuring that the string value has no
        /// non-printable/control characters.
        /// </summary>
        /// <param name="thumbprint">The certificate thumbprint to normalize.</param>
        /// <returns>
        /// A valid certificate thumbprint value containing only printable characters.
        /// </returns>
        public static string NormalizeThumbprint(string thumbprint)
        {
            // Depending upon how the thumbprint is provided, it is possible for it to have
            // invisible characters in the string.
            // https://stackoverflow.com/questions/8448147/problems-with-x509store-certificates-find-findbythumbprint
            return Regex.Replace(thumbprint, @"[^\da-zA-z]", string.Empty).ToUpperInvariant();
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> GetCertificateFromStoreAsync(string thumbprint, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My)
        {
            thumbprint.ThrowIfNullOrWhiteSpace(nameof(thumbprint));
            if (storeLocations == null)
            {
                storeLocations = new List<StoreLocation>() { StoreLocation.CurrentUser, StoreLocation.LocalMachine };
            }

            X509Certificate2 result = null;
            string errorMessage = $"Certificate not found. A certificate for user '{Environment.UserName}' with matching thumbprint '{thumbprint}' " +
                $"was not found in any one of the following expected certificate stores: ";
            foreach (StoreLocation storeLocation in storeLocations)
            {
                try
                {
                    using (X509Store store = new X509Store(storeName, storeLocation))
                    {
                        result = await this.GetCertificateFromStoreAsync(store, thumbprint);
                    }
                }
                catch (CryptographicException)
                {
                    // Certificate store permissions issue
                    throw;
                }
                catch (SecurityException)
                {
                    // Certificate access permissions issue
                    throw;
                }
                catch (Exception exc)
                {
                    errorMessage = errorMessage + $"Message from location '{storeLocation.ToString()}': {exc.Message}.";
                }

                if (result != null)
                {
                    break;
                }
            }

            if (result == null)
            {
                throw new CryptographicException(errorMessage);
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> GetCertificateFromStoreAsync(string issuer, string subject, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My)
        {
            issuer.ThrowIfNullOrWhiteSpace(nameof(issuer));
            subject.ThrowIfNullOrWhiteSpace(nameof(subject));

            if (storeLocations == null)
            {
                storeLocations = new List<StoreLocation>() { StoreLocation.CurrentUser, StoreLocation.LocalMachine };
            }

            X509Certificate2 result = null;
            string errorMessage = $"Certificate not found. A certificate for user '{Environment.UserName}' with matching issuer '{issuer}' and subject '{subject}' " +
                $"was not found in any one of the following expected certificate stores: ";
            foreach (StoreLocation storeLocation in storeLocations)
            {
                try
                {
                    using (X509Store store = new X509Store(storeName, storeLocation))
                    {
                        result = await this.GetCertificateFromStoreAsync(store, issuer, subject);
                    }
                }
                catch (CryptographicException)
                {
                    // Certificate store permissions issue
                    throw;
                }
                catch (SecurityException)
                {
                    // Certificate access permissions issue
                    throw;
                }
                catch (Exception exc)
                {
                    errorMessage = errorMessage + $"Message from location '{storeLocation.ToString()}': {exc.Message}.";
                }

                if (result != null)
                {
                    break;
                }
            }

            if (result == null)
            {
                throw new CryptographicException(errorMessage);
            }

            return result;
        }

        /// <summary>
        /// Returns a set of certificates from the store having a matching thumbprint.
        /// </summary>
        /// <param name="store">The certificate store in which to search.</param>
        /// <param name="thumbprint">The thumbprint of the certificate in the store.</param>
        /// <param name="validCertificateOnly">True if only valid (i.e. not expired/revoked) certificates should be returned.</param>
        /// <returns>
        /// A set of certificates from the store having a matching thumbprint.
        /// </returns>
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string thumbprint, bool validCertificateOnly = false)
        {
            store.ThrowIfNull(nameof(store));
            thumbprint.ThrowIfNullOrWhiteSpace(thumbprint);

            // Note that we found some type of anomaly on Linux systems where valid, unexpired certificates were
            // not returned by the X509Certificate2Collection.Find method when validOnly = true was used. To workaround this
            // we are looking for ALL certificates and then considering those that are not expired in the result set.

            return Task.Run(() =>
            {
                X509Certificate2Collection certificates = null;
                store.Open(OpenFlags.ReadOnly);

                try
                {
                    string normalizedThumbprint = CertificateManager.NormalizeThumbprint(thumbprint);

                    certificates = store.Certificates.Find(
                        findType: X509FindType.FindByThumbprint,
                        findValue: normalizedThumbprint,
                        validOnly: validCertificateOnly);
                }
                finally
                {
                    store.Close();
                }

                X509Certificate2 certificate = null;
                if (certificates?.Any() == true)
                {
                    DateTime now = DateTime.Now;
                    certificate = certificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
                }

                return certificate;
            });
        }

        /// <summary>
        /// Returns a set of certificates from the store having a matching thumbprint.
        /// </summary>
        /// <param name="store">The certificate store in which to search.</param>
        /// <param name="issuer">
        /// The name of the issuer for the certificate within the certificate store. Both issuer and distinguished name formats are supported 
        /// (e.g. ABC Infra CA 01 or CN=ABC Infra CA 01, DC=ABC, DC=COM). Any part of a distinguished name can be used (e.g. ABC Infra CA 01, ABC or COM).
        /// </param>
        /// <param name="subjectName">
        /// The subject name for the certificate within the certificate store. Both subject name and distinguished name formats are supported 
        /// (e.g. any.service.azure.com or CN=any.service.azure.com).
        /// </param>
        /// <param name="validCertificateOnly">True if only valid (i.e. not expired/revoked) certificates should be returned.</param>
        /// <returns>
        /// A set of certificates from the store having a matching issuer and subject name.
        /// </returns>
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string issuer, string subjectName, bool validCertificateOnly = false)
        {
            store.ThrowIfNull(nameof(store));
            issuer.ThrowIfNullOrWhiteSpace(issuer);
            subjectName.ThrowIfNullOrWhiteSpace(subjectName);

            // Note that we found some type of anomaly on Linux systems where valid, unexpired certificates were
            // not returned by the X509Certificate2Collection.Find method when validOnly = true was used. To workaround this
            // we are looking for ALL certificates and then considering those that are not expired in the result set.

            return Task.Run(() =>
            {
                X509Certificate2 certificate = null;
                X509Certificate2Collection certificates = null;
                X509Certificate2Collection certsByIssuer = null;

                store.Open(OpenFlags.ReadOnly);

                try
                {
                    if (CertificateManager.CommonNameExpression.IsMatch(issuer))
                    {
                        // e.g.
                        // CN=ABC Infra CA 01, DC=ABC, DC=COM
                        certsByIssuer = store.Certificates.Find(
                            findType: X509FindType.FindByIssuerDistinguishedName,
                            findValue: issuer,
                            validOnly: validCertificateOnly);
                    }
                    else
                    {
                        // e.g.
                        // Any one of the parts of 'CN=ABC Infra CA 01, DC=ABC, DC=COM' without
                        // the tokens. Each of the following are valid for example:
                        //
                        // COM
                        // ABC
                        // ABC Infra CA 01
                        certsByIssuer = store.Certificates.Find(
                            findType: X509FindType.FindByIssuerName,
                            findValue: issuer,
                            validOnly: validCertificateOnly);
                    }

                    if (certsByIssuer?.Any() == true)
                    {
                        if (CertificateManager.CommonNameExpression.IsMatch(subjectName))
                        {
                            // e.g.
                            // CN=any.service.azure.com
                            certificates = certsByIssuer.Find(
                                findType: X509FindType.FindBySubjectDistinguishedName,
                                findValue: subjectName,
                                validOnly: validCertificateOnly);
                        }
                        else
                        {
                            // e.g.
                            // any.service.azure.com
                            certificates = certsByIssuer.Find(
                                findType: X509FindType.FindBySubjectName,
                                findValue: subjectName,
                                validOnly: validCertificateOnly);
                        }
                    }

                    if (certificates?.Any() == true)
                    {
                        DateTime now = DateTime.Now;
                        certificate = certificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
                    }
                }
                finally
                {
                    store.Close();
                }

                return certificate;
            });
        }
    }
}