// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using AutoFixture;

    /// <summary>
    /// Extension methods for <see cref="Fixture"/> instances and for general
    /// testing classes.
    /// </summary>
    /// <remarks>
    /// The <see cref="Fixture"/> class is part of a library called Autofixture
    /// which is used to help ease the creation of mock objects that are commonly 
    /// used in VirtualClient.TestExtensions project tests (e.g. unit, functional).
    /// 
    /// Source Code:
    /// https://github.com/AutoFixture/AutoFixture"
    /// 
    /// Cheat Sheet:
    /// https://github.com/AutoFixture/AutoFixture/wiki/Cheat-Sheet
    /// 
    /// </remarks>
    public static class AutoFixtureExtensions
    {
        private static Assembly thisAssembly = Assembly.GetAssembly(typeof(AutoFixtureExtensions));

        /// <summary>
        /// Registers a factory method with the <see cref="Fixture"/> provided that
        /// can be used to create a mock <see cref="X509Certificate2"/> instances.
        /// </summary>
        /// <param name="fixture">The test/auto fixture.</param>
        public static Fixture SetupCertificateMocks(this Fixture fixture)
        {
            fixture.Register<X509Certificate2>(() => fixture.CreateCertificate(withPrivateKey: true));

            return fixture;
        }

        /// <summary>
        /// Returns a mock/test <see cref="X509Certificate2"/> instance.
        /// </summary>
        /// <returns>
        /// A mock/test certificate.
        /// </returns>
        public static X509Certificate2 CreateCertificate(this Fixture fixture, bool withPrivateKey = false)
        {
            // IMPORTANT:
            // The certificate that is used to support unit/functional testing scenarios is a self-signed
            // certificate. Whereas it is a valid certificate, it cannot be used for any authentication or
            // authorization. It is only useful for development/testing purposes.
            //
            // This certificate was created on a local developer system using the 'New-TestCertificate.ps1' 
            // commandlet in the solution directory.
            //
            // e.g.
            // PS> New-TestCertificate.ps1 -OutputPath 'C:\Users\User\Documents'

            X509Certificate2 certificate = null;
            string resourcesDirectory = Path.Combine(Path.GetDirectoryName(AutoFixtureExtensions.thisAssembly.Location), "Resources");

#if NET9_0_OR_GREATER
            if (withPrivateKey)
            {
                certificate = X509CertificateLoader.LoadPkcs12(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "test-certificate.private")),
                    "CRC",
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
            else
            {
                certificate = X509CertificateLoader.LoadCertificate(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "test-certificate.public")));
            }
#elif NET8_0_OR_GREATER
            if (withPrivateKey)
            {
                certificate = new X509Certificate2(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "test-certificate.private")),
                    "CRC",
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
            else
            {
                certificate = new X509Certificate2(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "test-certificate.public")));
            }
#endif

            return certificate;
        }
    }
}
