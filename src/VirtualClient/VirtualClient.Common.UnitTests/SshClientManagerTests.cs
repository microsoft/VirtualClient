namespace VirtualClient.Common
{
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SshClientManagerTests
    {
        private ISshClientManager sshClientManager;

        [SetUp]
        public void SetupTest()
        {
            this.sshClientManager = new SshClientManager();
        }

        [Test]
        public void SshClientManagerCreatesExpectedSshCommand()
        {
            string mockHost = "mockHost";
            string mockUserName = "username";
            string mockPassword = "password";

            using (ISshClientProxy client = this.sshClientManager.CreateSshClient(mockHost, mockUserName, mockPassword))
            {
                Assert.IsTrue(client.ConnectionInfo.Host.Equals(mockHost));
                Assert.IsTrue(client.ConnectionInfo.Username.Equals(mockUserName));
            }
        }

        [Test]
        public void SshClientManagerCreatesExpectedSftpClient()
        {
            string mockHost = "mockHost";
            string mockUserName = "username";
            string mockPassword = "password";

            using (ISftpClientProxy client = this.sshClientManager.CreateSftpClient(mockHost, mockUserName, mockPassword))
            {
                Assert.IsTrue(client.ConnectionInfo.Host.Equals(mockHost));
                Assert.IsTrue(client.ConnectionInfo.Username.Equals(mockUserName));
            }
        }
    }
}