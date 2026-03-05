// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace VirtualClient.Identity
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate Loader  to cleanly handle differences in .NET versions for loading certificates from byte arrays.
    /// </summary>
    internal static class CertificateLoaderHelper
    {
        internal static X509Certificate2 LoadPublic(byte[] cerBytes)
        {
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadCertificate(cerBytes);
#else
        return new X509Certificate2(cerBytes);
#endif
        }

        internal static X509Certificate2 LoadPkcs12(
            byte[] pfxBytes,
            string password,
            X509KeyStorageFlags flags)
        {
#if NET9_0_OR_GREATER
            return X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                password,
                flags);
#else
        return new X509Certificate2(
            pfxBytes,
            password,
            flags);
#endif
        }
    }
}
