// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Reflection;
    using System.Security;
    using System.Security.Cryptography.X509Certificates;
    using AutoFixture;
    using VirtualClient.Common.Extensions;

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
            fixture.Register<X509Certificate2>(() => AutoFixtureExtensions.CreateCertificate(withPrivateKey: true));

            return fixture;
        }

        /// <summary>
        /// Returns a mock/test <see cref="X509Certificate2"/> instance.
        /// </summary>
        /// <returns>
        /// A mock/test certificate.
        /// </returns>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The certificate password parameter cannot be disposed in this context.")]
        private static X509Certificate2 CreateCertificate(bool withPrivateKey = false)
        {
            X509Certificate2 certificate = null;
            string resourcesDirectory = Path.Combine(Path.GetDirectoryName(AutoFixtureExtensions.thisAssembly.Location), "Resources");

            if (withPrivateKey)
            {
                certificate = new X509Certificate2(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "testcertificate2.private")),
                    string.Empty.ToSecureString(),
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);
            }
            else
            {
                certificate = new X509Certificate2(
                    File.ReadAllBytes(Path.Combine(resourcesDirectory, "testcertificate2.private")));
            }

            return certificate;
        }
    }
}
