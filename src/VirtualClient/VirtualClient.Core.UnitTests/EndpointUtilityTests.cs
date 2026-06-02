// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Policy;
    using AutoFixture;
    using Azure.Identity;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    internal class EndpointUtilityTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Initialize()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupCertificateMocks();
        }
    }
}
