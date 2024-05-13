// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VirtualClient.Common.Extensions;

namespace VirtualClient
{
    /// <summary>
    /// Manager for certificate management.
    /// </summary>
    public class CertificateManager : ICertificateManager
    {
        /// <summary>
        /// Determines the X509RevocationFlag used by X509Chain to decide how much of the CA chain cert verification will process.
        /// Defaults to X509RevocationFlag.ExcludeRoot.
        /// </summary>
        /// <remarks>see https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509chainpolicy.revocationflag?view=netstandard-2.0 for MS doc reference </remarks>
        public X509RevocationFlag ChainRevocationFlag { get; set; } = X509RevocationFlag.EndCertificateOnly;

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
            string errorMessage = string.Empty;
            foreach (StoreLocation storeLocation in storeLocations)
            {
                try
                {
                    using (X509Store store = new X509Store(storeName, storeLocation))
                    {
                        result = await this.GetCertificateFromStoreAsync(store, thumbprint, false)
                            .ConfigureAwait(false);
                    }
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
                throw new CryptographicException($"Couldn't find certificate with thumbprint {thumbprint} with error: '{errorMessage}'.");
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
            string errorMessage = string.Empty;
            foreach (StoreLocation storeLocation in storeLocations)
            {
                try
                {
                    using (X509Store store = new X509Store(storeName, storeLocation))
                    {
                        result = await this.GetCertificateFromStoreAsync(store, issuer, subject, true)
                            .ConfigureAwait(false);
                    }
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
                throw new CryptographicException($"Couldn't find certificate with issuer [{issuer}] and subject name [{subject}] with error: '{errorMessage}'.");
            }

            return result;
        }

        /// <inheritdoc/>
        public async Task InstallCertificateToStoreAsync(X509Certificate2 certificate, StoreName storeName, StoreLocation storeLocation)
        {
            certificate.ThrowIfNull(nameof(certificate));

            using (X509Store store = new X509Store(storeName, storeLocation))
            {
                X509Certificate2 checkExistence = null;
                try
                {
                    checkExistence = await this.GetCertificateFromStoreAsync(store, certificate.Thumbprint, false).ConfigureAwait(false);
                }
                catch
                { // CertManager throws when not found. So needs to catch it.
                }

                if (checkExistence == null)
                {
                    // Only install if it's not already installed.
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }
            }
        }

        /// <summary>
        /// Verify specified certificate.
        /// </summary>
        /// <param name="certificate">Certificate to verify.</param>
        /// <param name="subject">Expected certificate subject.</param>
        /// <param name="issuer">Expected certificate issuer.</param>
        /// <param name="thumbprint">Expected certificate thumbprint.</param>
        /// <returns>True if verification passes; false if it fails.</returns>
        public bool VerifyCertificate(X509Certificate2 certificate, string subject, string issuer, string thumbprint)
        {
            string normalizedThumbprint = CertificateManager.NormalizeThumbprint(thumbprint);

            using (X509Chain chain = new X509Chain())
            {
                // Use the default values of chain.ChainPolicy including:
                //   RevocationFlag = X509RevocationFlag.ExcludeRoot
                //   VerificationFlags = X509VerificationFlags.NoFlag
                //   VerificationTime = DateTime.Now
                //   UrlRetrievalTimeout = new TimeSpan(0, 0, 0)
                // RevocationMode has now been returned to the default value of X509RevocationMode.Online to
                // enable Cert revocation list checking (CRL)
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.RevocationFlag = this.ChainRevocationFlag;

                return certificate != null &&
                    chain.Build(certificate) &&
                    CertificateManager.AreFieldsEqual(certificate.Issuer, issuer) &&
                    CertificateManager.AreFieldsEqual(certificate.Subject, subject) &&
                    certificate.Thumbprint.Equals(normalizedThumbprint, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Checks whether two certificate fields (e.g. Issuer, Subject, etc.) are equal.
        /// "Equal" means that the fields have the same attribute keys and values.
        /// Order in which they occur does not matter.
        /// </summary>
        /// <param name="field1">First field to compare.</param>
        /// <param name="field2">Second field to compare.</param>
        /// <returns>True if they are equal, False otherwise.</returns>
        protected static bool AreFieldsEqual(string field1, string field2)
        {
            field1.ThrowIfNullOrWhiteSpace(nameof(field1));
            field2.ThrowIfNullOrWhiteSpace(nameof(field2));

            bool areEqual = false;
            var parsedField1 = CertificateManager.ParseField(field1);
            var parsedField2 = CertificateManager.ParseField(field2);

            if (parsedField1.Count == parsedField2.Count)
            {
                areEqual = true;
                foreach (string attributeKey in parsedField1.Keys)
                {
                    string field1Value;
                    string field2Value;
                    if (parsedField1.TryGetValue(attributeKey, out field1Value)
                        && parsedField2.TryGetValue(attributeKey, out field2Value)
                        && string.Equals(field1Value, field2Value, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    areEqual = false;
                    break;
                }
            }

            return areEqual;
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
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string thumbprint, bool validCertificateOnly = true)
        {
            store.ThrowIfNull(nameof(store));
            thumbprint.ThrowIfNullOrWhiteSpace(thumbprint);

            X509Certificate2Collection certificates = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
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

            if (certificates.Count == 0)
            {
                throw new CryptographicException($"Certificate that matches thumbprint [{thumbprint}] not found in store [Name: {store.Name}, Location: {store.Location}].");
            }

            return Task.FromResult(certificates[0]);
        }

        /// <summary>
        /// Returns a set of certificates from the store having a matching thumbprint.
        /// </summary>
        /// <param name="store">The certificate store in which to search.</param>
        /// <param name="issuer">The name of the issuer for the certificate within the certificate store.</param>
        /// <param name="subjectName">The subject name for the certificate within the certificate store.</param>
        /// <param name="validCertificateOnly">True if only valid (i.e. not expired/revoked) certificates should be returned.</param>
        /// <returns>
        /// A set of certificates from the store having a matching issuer and subject name.
        /// </returns>
        protected virtual Task<X509Certificate2> GetCertificateFromStoreAsync(X509Store store, string issuer, string subjectName, bool validCertificateOnly = true)
        {
            store.ThrowIfNull(nameof(store));
            issuer.ThrowIfNullOrWhiteSpace(issuer);
            subjectName.ThrowIfNullOrWhiteSpace(subjectName);

            X509Certificate2Collection certificates = null;

            try
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certsByIssuer = store.Certificates.Find(
                    findType: X509FindType.FindByIssuerName,
                    findValue: issuer,
                    validOnly: validCertificateOnly);

                certificates = certsByIssuer.Find(
                    findType: X509FindType.FindBySubjectName,
                    findValue: subjectName,
                    validOnly: validCertificateOnly);
            }
            finally
            {
                store.Close();
            }

            if (certificates.Count == 0)
            {
                throw new CryptographicException($"Certificate that matches issuer [{issuer}] and subject name [{subjectName}] not found in store [Name: {store.Name}, Location: {store.Location}].");
            }

            return Task.FromResult(certificates[0]);
        }

        /// <summary>
        /// Parse certificate fields so we can extract attributes individually.
        /// </summary>
        /// <param name="field">
        /// Field to parse, e.g. Issuer
        /// Issuer: "CN=CommonName, OU=OrgUnit, O=Org, L=Location, S=State, C=C0untry"
        /// </param>
        /// <returns>Dictionary of attribute keys and values.</returns>
        private static IDictionary<string, string> ParseField(string field)
        {
            var parsedField = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string attribute in field.Split(','))
            {
                string[] attributeParts = attribute.Split('=').Select(part => part.Trim()).ToArray<string>();

                if (parsedField.ContainsKey(attributeParts[0]))
                {
                    parsedField[attributeParts[0]] = parsedField[attributeParts[1]];
                }
                else
                {
                    parsedField.Add(attributeParts[0], attributeParts[1]);
                }
            }

            return parsedField;
        }
    }
}