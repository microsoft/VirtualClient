// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Buffers.Binary;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    /// <summary>
    /// Helper for RSA cryptography.
    /// </summary>
    public static class RsaCrypto
    {
        /// <summary>
        /// Create a self signed certificate using RSA.
        /// </summary>
        /// <param name="cName">Common name of the domain</param>
        /// <param name="keySize">Key size of the RSA crypto</param>
        /// <returns>Self signed certificate</returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string cName, int keySize)
        {
            RSA rsa = RSA.Create(keySize);
            CertificateRequest certRequest = new CertificateRequest(cName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return certRequest.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        }
    }
}