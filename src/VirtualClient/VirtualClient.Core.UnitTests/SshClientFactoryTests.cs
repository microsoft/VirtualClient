// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Linq;
    using NUnit.Framework;
    using Renci.SshNet;

    [TestFixture]
    [Category("Unit")]
    public class SshClientFactoryTests
    {
        [Test]
        public void SshClientManagerCreatesExpectedSshCommand()
        {
            string mockHost = "mockHost";
            string mockUserName = "username";
            string mockPassword = "password";

            SshClientFactory factory = new SshClientFactory();
            using (ISshClientProxy client = factory.CreateClient(mockHost, mockUserName, mockPassword))
            {
                Assert.IsNotNull(client);
                Assert.IsNotNull(client.ConnectionInfo);
                Assert.IsInstanceOf<PasswordAuthenticationMethod>(client.ConnectionInfo.AuthenticationMethods.FirstOrDefault());
                Assert.IsTrue(client.ConnectionInfo.Host.Equals(mockHost));
                Assert.IsTrue(client.ConnectionInfo.Username.Equals(mockUserName));
            }
        }
    }
}