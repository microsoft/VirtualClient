// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
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
        /// <summary>
        /// The default directory to search for installed certificates for use
        /// with the Virtual Client.
        /// </summary>
        public const string DefaultUnixCertificateDirectory = "/home/{0}/.dotnet/corefx/cryptography/x509stores/my";
        private static readonly Regex CommonNameExpression = new Regex("CN=", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateManager"/> class.
        /// </summary>
        public CertificateManager()
            : this(new FileSystem())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateManager"/> class.
        /// </summary>
        /// <param name="fileSystem">Provides features for accessing the file system.</param>
        public CertificateManager(IFileSystem fileSystem)
        {
            this.FileSystem = fileSystem ?? new FileSystem();
        }

        /// <summary>
        /// Provides features for accessing the file system.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Returns true/false whether the issuer for the certificate matches the issuer provided.
        /// </summary>
        /// <param name="certificate">The certificate for which to check the issuer.</param>
        /// <param name="issuer">
        /// The issuer to confirm. This can be a fully qualified name or parts of it
        /// (e.g. CN=ABC Infra CA 01, DC=ABC, DC=COM or ABC Infra CA 01 or ABC).
        /// </param>
        /// <returns>True if the issuer matches or false if not.</returns>
        public static bool IsMatchingIssuer(X509Certificate2 certificate, string issuer)
        {
            return Regex.IsMatch(
                certificate.Issuer.RemoveWhitespace(),
                issuer.RemoveWhitespace(),
                RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns true/false whether the subject name for the certificate matches the 
        /// subject name provided.
        /// </summary>
        /// <param name="certificate">The certificate for which to check the subject name.</param>
        /// <param name="subjectName">
        /// The subject name to confirm. This can be a fully qualified name or parts of it
        /// (e.g. CN=any.service.azure.com or any.service.azure.com).
        /// </param>
        /// <returns>True if the subject name matches or false if not.</returns>
        public static bool IsMatchingSubjectName(X509Certificate2 certificate, string subjectName)
        {
            return Regex.IsMatch(
                certificate.Subject.RemoveWhitespace(),
                subjectName.RemoveWhitespace(),
                RegexOptions.IgnoreCase);
        }

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
        public async Task<X509Certificate2> GetCertificateFromPathAsync(string thumbprint, string directoryPath)
        {
            thumbprint.ThrowIfNullOrWhiteSpace(nameof(thumbprint));
            directoryPath.ThrowIfNullOrWhiteSpace(nameof(directoryPath));

            X509Certificate2 certificate = null;
            string errorMessage = $"Certificate not found. A certificate with matching thumbprint '{thumbprint}' " +
                $"was not found in the expected certificate directory: {directoryPath}";

            try
            {
                if (this.FileSystem.Directory.Exists(directoryPath))
                {
                    List<X509Certificate2> matchingCertificates = new List<X509Certificate2>();
                    string[] certificateFiles = this.FileSystem.Directory.GetFiles(directoryPath);
                    string normalizedThumbprint = CertificateManager.NormalizeThumbprint(thumbprint);

                    foreach (string file in certificateFiles)
                    {
                        try
                        {
                            byte[] certificateBytes = await this.FileSystem.File.ReadAllBytesAsync(file);
                            if (certificateBytes?.Any() == true)
                            {
                                X509Certificate2 certificateFromFile = new X509Certificate2(
                                    certificateBytes,
                                    string.Empty.ToSecureString(),
                                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                                if (certificateFromFile != null)
                                {
                                    if (string.Equals(certificateFromFile.Thumbprint, normalizedThumbprint, StringComparison.OrdinalIgnoreCase))
                                    {
                                        matchingCertificates.Add(certificateFromFile);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // There may be non-certificate files in the directory or certificates that
                            // are not .pfx format.
                        }
                    }

                    // Take the certificate with the latest expiration date that is not 
                    // currently expired.
                    if (matchingCertificates?.Any() == true)
                    {
                        DateTime now = DateTime.Now;
                        certificate = matchingCertificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
                    }
                }
            }
            catch (IOException)
            {
                // Directory access store permissions.
                throw;
            }
            catch (SecurityException)
            {
                // Directory access store permissions.
                throw;
            }

            if (certificate == null)
            {
                throw new CryptographicException(errorMessage);
            }

            return certificate;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> GetCertificateFromPathAsync(string issuer, string subjectName, string directoryPath)
        {
            issuer.ThrowIfNullOrWhiteSpace(nameof(issuer));
            subjectName.ThrowIfNullOrWhiteSpace(nameof(subjectName));
            directoryPath.ThrowIfNullOrWhiteSpace(nameof(directoryPath));

            X509Certificate2 certificate = null;
            string errorMessage = $"Certificate not found. A certificate with matching issuer '{issuer}' and subject '{subjectName}' " +
                $"was not found in the expected certificate directory: {directoryPath}";

            try
            {
                if (this.FileSystem.Directory.Exists(directoryPath))
                {
                    List<X509Certificate2> matchingCertificates = new List<X509Certificate2>();
                    string[] certificateFiles = this.FileSystem.Directory.GetFiles(directoryPath);

                    foreach (string file in certificateFiles)
                    {
                        try
                        {
                            byte[] certificateBytes = await this.FileSystem.File.ReadAllBytesAsync(file);
                            if (certificateBytes?.Any() == true)
                            {
                                X509Certificate2 certificateFromFile = new X509Certificate2(
                                    certificateBytes,
                                    string.Empty.ToSecureString(),
                                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

                                if (certificateFromFile != null)
                                {
                                    if (CertificateManager.IsMatchingIssuer(certificateFromFile, issuer)
                                        && CertificateManager.IsMatchingSubjectName(certificateFromFile, subjectName))
                                    {
                                        matchingCertificates.Add(certificateFromFile);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // There may be non-certificate files in the directory or certificates that
                            // are not .pfx format.
                        }
                    }

                    // Take the certificate with the latest expiration date that is not 
                    // currently expired.
                    if (matchingCertificates?.Any() == true)
                    {
                        DateTime now = DateTime.Now;
                        certificate = matchingCertificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
                    }
                }
            }
            catch (IOException)
            {
                // Directory access store permissions.
                throw;
            }
            catch (SecurityException)
            {
                // Directory access store permissions.
                throw;
            }

            if (certificate == null)
            {
                throw new CryptographicException(errorMessage);
            }

            return certificate;
        }

        /// <inheritdoc/>
        public async Task<X509Certificate2> GetCertificateFromStoreAsync(string thumbprint, IEnumerable<StoreLocation> storeLocations = null, StoreName storeName = StoreName.My)
        {
            thumbprint.ThrowIfNullOrWhiteSpace(nameof(thumbprint));
            if (storeLocations == null)
            {
                storeLocations = new List<StoreLocation>() 
                {
                    StoreLocation.CurrentUser,
                    StoreLocation.LocalMachine
                };
            }

            X509Certificate2 result = null;
            string errorMessage = $"Certificate not found. A certificate for user '{Environment.UserName}' with matching thumbprint '{thumbprint}' " +
                $"was not found in any one of the following expected certificate stores: " +
                $"{string.Join(", ", storeLocations.Select(l => $"{l.ToString()}/{storeName.ToString()}"))}";

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
                storeLocations = new List<StoreLocation>()
                {
                    StoreLocation.CurrentUser,
                    StoreLocation.LocalMachine
                };
            }

            X509Certificate2 result = null;
            string errorMessage = $"Certificate not found. A certificate for user '{Environment.UserName}' with matching issuer '{issuer}' and subject '{subject}' " +
                $"was not found in any one of the following expected certificate stores: " +
                $"{string.Join(", ", storeLocations.Select(l => $"{l.ToString()}/{storeName.ToString()}"))}";

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
        /// <returns>
        /// A set of certificates from the store having a matching thumbprint.
        /// </returns>
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string thumbprint)
        {
            store.ThrowIfNull(nameof(store));
            thumbprint.ThrowIfNullOrWhiteSpace(thumbprint);

            // Note that we found some type of anomaly on Linux systems where valid, unexpired certificates were
            // not returned by the X509Certificate2Collection.Find method when validOnly = true was used. To workaround this
            // we are looking for ALL certificates and then considering those that are not expired in the result set.

            return Task.Run(() =>
            {
                X509Certificate2Collection matchingCertificates = null;
                store.Open(OpenFlags.ReadOnly);

                try
                {
                    string normalizedThumbprint = CertificateManager.NormalizeThumbprint(thumbprint);

                    // There are cases on Unix systems where valid certificates exist but
                    // are not returned when we search for "valid only". We handle the conditions
                    // for validity below based on expiry dates.
                    matchingCertificates = store.Certificates.Find(
                        findType: X509FindType.FindByThumbprint,
                        findValue: normalizedThumbprint,
                        validOnly: false);
                }
                finally
                {
                    store.Close();
                }

                // Take the certificate with the latest expiration date that is not 
                // currently expired.
                X509Certificate2 certificate = null;
                if (matchingCertificates?.Any() == true)
                {
                    DateTime now = DateTime.Now;
                    certificate = matchingCertificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
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
        /// <returns>
        /// A set of certificates from the store having a matching issuer and subject name.
        /// </returns>
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string issuer, string subjectName)
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
                X509Certificate2Collection matchingCertificates = null;
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
                            validOnly: false);
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
                            validOnly: false);
                    }

                    if (certsByIssuer?.Any() == true)
                    {
                        // There are cases on Unix systems where valid certificates exist but
                        // are not returned when we search for "valid only". We handle the conditions
                        // for validity below based on expiry dates.

                        if (CertificateManager.CommonNameExpression.IsMatch(subjectName))
                        {
                            // e.g.
                            // CN=any.service.azure.com
                            matchingCertificates = certsByIssuer.Find(
                                findType: X509FindType.FindBySubjectDistinguishedName,
                                findValue: subjectName,
                                validOnly: false);
                        }
                        else
                        {
                            // e.g.
                            // any.service.azure.com
                            matchingCertificates = certsByIssuer.Find(
                                findType: X509FindType.FindBySubjectName,
                                findValue: subjectName,
                                validOnly: false);
                        }
                    }

                    // Take the certificate with the latest expiration date that is not 
                    // currently expired.
                    if (matchingCertificates?.Any() == true)
                    {
                        DateTime now = DateTime.Now;
                        certificate = matchingCertificates.Where(cert => now >= cert.NotBefore && now < cert.NotAfter)?.OrderByDescending(cert => cert.NotAfter).First();
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