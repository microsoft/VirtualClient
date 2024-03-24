namespace VirtualClient
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class ApiClientManagerTests
    {
        [Test]
        public void ApiClientManagerDefaultApiPortMatchesExpected()
        {
            Assert.AreEqual(4500, ApiClientManager.DefaultApiPort);
        }

        [Test]
        public void ApiClientManagerReturnsTheExpectedDefaultApiPort()
        {
            ApiClientManager clientManager = new ApiClientManager();
            int actualPort = clientManager.GetApiPort();

            Assert.AreEqual(ApiClientManager.DefaultApiPort, actualPort);
        }

        [Test]
        public void ApiClientManagerAllowsTheDefaultApiPortToBeOverridden()
        {
            IDictionary<string, int> apiPorts = new Dictionary<string, int>
            {
                [nameof(ApiClientManager.DefaultApiPort)] = 5500
            };

            ApiClientManager clientManager = new ApiClientManager(apiPorts);

            int actualPort = clientManager.GetApiPort();
            Assert.AreEqual(5500, actualPort);

            actualPort = clientManager.GetApiPort(new ClientInstance("AnyClient", "1.2.3.4", ClientRole.Client));
            Assert.AreEqual(5500, actualPort);

            actualPort = clientManager.GetApiPort(new ClientInstance("AnyClient", "1.2.3.5", ClientRole.Server));
            Assert.AreEqual(5500, actualPort);
        }

        [Test]
        public void ApiClientManagerReturnsTheExpectedApiPortWhenRolesExist()
        {
            IDictionary<string, int> apiPorts = new Dictionary<string, int>
            {
                [ClientRole.Client] = 4501,
                [ClientRole.Server] = 4502
            };

            ApiClientManager clientManager = new ApiClientManager(apiPorts);

            int actualPort = clientManager.GetApiPort(new ClientInstance("AnyClient", "1.2.3.4", ClientRole.Client));
            Assert.AreEqual(4501, actualPort);

            actualPort = clientManager.GetApiPort(new ClientInstance("AnyServer", "1.2.3.5", ClientRole.Server));
            Assert.AreEqual(4502, actualPort);
        }

        [Test]
        public void ApiClientManagerCreatesTheExpectedDefaultApiClient()
        {
            ApiClientManager clientManager = new ApiClientManager();

            IApiClient actualClient = clientManager.GetOrCreateApiClient("AnyClient", System.Net.IPAddress.Loopback);
            Assert.AreEqual($"http://localhost:{ApiClientManager.DefaultApiPort}/", actualClient.BaseUri.ToString());

            actualClient = clientManager.GetOrCreateApiClient("AnyServer", new ClientInstance("AnyClient", System.Net.IPAddress.Loopback.ToString()));
            Assert.AreEqual($"http://127.0.0.1:{ApiClientManager.DefaultApiPort}/", actualClient.BaseUri.ToString());
        }

        [Test]
        public void ApiClientManagerCreatesTheExpectedApiClientWhenRolesExistAndTheDefaultPortIsOverridden()
        {
            IDictionary<string, int> apiPorts = new Dictionary<string, int>
            {
                [nameof(ApiClientManager.DefaultApiPort)] = 5500
            };

            ApiClientManager clientManager = new ApiClientManager(apiPorts);

            IApiClient actualClient = clientManager.GetOrCreateApiClient("AnyClient", new ClientInstance("AnyClient", "1.2.3.4", ClientRole.Client));
            Assert.AreEqual($"http://1.2.3.4:5500/", actualClient.BaseUri.ToString());

            actualClient = clientManager.GetOrCreateApiClient("AnyServer", new ClientInstance("AnyServer", "1.2.3.5", ClientRole.Server));
            Assert.AreEqual($"http://1.2.3.5:5500/", actualClient.BaseUri.ToString());
        }

        [Test]
        public void ApiClientManagerCreatesTheExpectedApiClientWhenRolesExist()
        {
            IDictionary<string, int> apiPorts = new Dictionary<string, int>
            {
                [ClientRole.Client] = 4501,
                [ClientRole.Server] = 4502
            };

            ApiClientManager clientManager = new ApiClientManager(apiPorts);

            IApiClient actualClient = clientManager.GetOrCreateApiClient("AnyClient", new ClientInstance("AnyClient", "1.2.3.4", ClientRole.Client));
            Assert.AreEqual($"http://1.2.3.4:4501/", actualClient.BaseUri.ToString());

            actualClient = clientManager.GetOrCreateApiClient("AnyServer", new ClientInstance("AnyServer", "1.2.3.5", ClientRole.Server));
            Assert.AreEqual($"http://1.2.3.5:4502/", actualClient.BaseUri.ToString());
        }
    }
}
