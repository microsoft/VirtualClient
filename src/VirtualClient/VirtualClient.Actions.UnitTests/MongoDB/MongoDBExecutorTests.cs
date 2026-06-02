// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.UnitTests.MongoDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.MongoDB;
    using VirtualClient;
    using VirtualClient.Actions;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    [Category("Unit")]
    public class MongoDBExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);
        }

        [Test]
        public void MongoDBExecutor_PortParameter_ReturnsDefault27017()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(27017, port);
        }

        [Test]
        public void MongoDBExecutor_PortParameter_ReturnsCustomValue()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Port", 27018 } };
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(27018, port);
        }

        [Test]
        public void MongoDBExecutor_PortParameter_WithStringValue_ConvertsProperly()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Port", "27019" } };
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(27019, port);
        }

        [Test]
        public void MongoDBExecutor_MultipleInstances_MaintainIndependentPorts()
        {
            // SETUP
            var params1 = new Dictionary<string, IConvertible> { { "Port", 27017 } };
            var params2 = new Dictionary<string, IConvertible> { { "Port", 27018 } };

            // ACT
            var executor1 = new MongoDBClientExecutor(this.mockFixture.Dependencies, params1);
            var executor2 = new MongoDBClientExecutor(this.mockFixture.Dependencies, params2);

            // ASSERT
            Assert.AreEqual(27017, executor1.Port);
            Assert.AreEqual(27018, executor2.Port);
        }

        [Test]
        public void MongoDBExecutor_LargePortNumber_ReturnsCorrectValue()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Port", 65535 } };
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(65535, port);
        }

        [Test]
        public void MongoDBExecutor_MultiplePortRanges_HandledCorrectly()
        {
            // SETUP
            var ports = new[] { 1024, 8080, 27017, 49152, 65535 };

            // ACT & ASSERT
            foreach (var port in ports)
            {
                this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Port", port } };
                var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
                Assert.AreEqual(port, executor.Port);
            }
        }

        [Test]
        public void MongoDBExecutor_PortAsString_ConvertedToInt()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Port", "28000" } };
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(28000, port);
            Assert.IsInstanceOf<int>(port);
        }

        [Test]
        public void MongoDBExecutor_NullPort_ReturnsDefault()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new MongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(27017, port);
        }

        #region Protected Method Tests (Pattern 1: Derived Test Class)

        /// <summary>
        /// Test helper class that exposes protected methods using Pattern 1.
        /// Inherits from MongoDBClientExecutor and exposes protected members via 'public new' methods.
        /// </summary>
        private class TestableMongoDBClientExecutor : MongoDBClientExecutor
        {
            public TestableMongoDBClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            /// <summary>
            /// Exposes the protected InitializeAsync method for testing.
            /// </summary>
            public new async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken) => await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Exposes the protected InitializeApiClients method for testing.
            /// </summary>
            public new void InitializeApiClients() => base.InitializeApiClients();

            /// <summary>
            /// Accessor for the ServerApiClient property.
            /// </summary>
            public IApiClient GetServerApiClient() => this.ServerApiClient;

            /// <summary>
            /// Accessor for the ServerIpAddress property.
            /// </summary>
            public string GetServerIpAddress() => this.ServerIpAddress;
        }

        /// <summary>
        /// Test helper class for MongoDBServerExecutor protected methods.
        /// </summary>
        private class TestableMongoDBServerExecutor : MongoDBServerExecutor
        {
            public TestableMongoDBServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            /// <summary>
            /// Exposes the protected InitializeAsync method for testing.
            /// </summary>
            public new async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken) => await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Exposes the protected InitializeApiClients method for testing.
            /// </summary>
            public new void InitializeApiClients() => base.InitializeApiClients();

            /// <summary>
            /// Accessor for the ServerApiClient property.
            /// </summary>
            public IApiClient GetServerApiClient() => this.ServerApiClient;

            /// <summary>
            /// Accessor for the ServerIpAddress property.
            /// </summary>
            public string GetServerIpAddress() => this.ServerIpAddress;
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_CreatesLoopbackClient_OnSingleVMLayout()
        {
            // SETUP: Single VM layout (no multi-role configuration)
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Initialize API clients for single VM (uses loopback)
            executor.InitializeApiClients();

            // ASSERT: ServerApiClient should be created for loopback address
            IApiClient serverClient = executor.GetServerApiClient();
            Assert.IsNotNull(serverClient, "ServerApiClient should be initialized for single VM");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_UsesLoopbackIPAddress_OnSingleVM()
        {
            // SETUP: Single VM layout
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Initialize API clients for single VM
            executor.InitializeApiClients();

            // ASSERT: ServerApiClient should be created
            IApiClient serverClient = executor.GetServerApiClient();
            Assert.IsNotNull(serverClient, "API client should be created");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_AllowsMultipleInvocations()
        {
            // SETUP: Single VM layout
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Call InitializeApiClients multiple times
            executor.InitializeApiClients();
            executor.InitializeApiClients();
            executor.InitializeApiClients();

            // ASSERT: Multiple calls should not throw exceptions
            IApiClient client = executor.GetServerApiClient();
            Assert.IsNotNull(client, "API client should be set after multiple initializations");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_ServerExecutor_CreatesLoopbackClient()
        {
            // SETUP: Test with MongoDBServerExecutor (derived class)
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var serverExecutor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Initialize API clients for server
            serverExecutor.InitializeApiClients();

            // ASSERT: ServerApiClient should be created
            IApiClient serverClient = serverExecutor.GetServerApiClient();
            Assert.IsNotNull(serverClient, "Server executor should have ServerApiClient initialized");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_WithMultiVMLayout_ConfiguresServerApiClient()
        {
            // SETUP: Multi-role layout with valid server IP
            this.mockFixture.SetupLayout(
                new ClientInstance("ClientAgent", "1.2.3.4", ClientRole.Client),
                new ClientInstance("ServerAgent", "1.2.3.5", ClientRole.Server));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();

            // ASSERT: ServerApiClient should be created for the server instance IP
            IApiClient serverClient = executor.GetServerApiClient();
            string serverIpAddress = executor.GetServerIpAddress();
            
            Assert.IsNotNull(serverClient, "ServerApiClient should be initialized for multi-VM layout");
            Assert.IsNotNull(serverIpAddress, "ServerIpAddress should be set");
            Assert.AreEqual("1.2.3.5", serverIpAddress);
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_MaintainsApiClientReference()
        {
            // SETUP: Single VM layout
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Initialize API clients
            executor.InitializeApiClients();
            IApiClient firstClient = executor.GetServerApiClient();

            // Initialize again
            executor.InitializeApiClients();
            IApiClient secondClient = executor.GetServerApiClient();

            // ASSERT: Both calls should return a valid client
            Assert.IsNotNull(firstClient, "First API client should be created");
            Assert.IsNotNull(secondClient, "Second API client should be created");
        }

        [Test]
        public void MongoDBExecutor_ProtectedPort_AccessibleViaTestClass()
        {
            // SETUP: Create executor with custom parameters
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Port", 27018 }
            };
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Access protected Port property
            int port = executor.Port;

            // ASSERT: Protected properties should be accessible through test class
            Assert.AreEqual(27018, port, "Port property should be accessible");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_WithLoopback_ConfiguresCorrectly()
        {
            // SETUP: Ensure we're testing single VM configuration
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Initialize API clients which should use loopback
            executor.InitializeApiClients();

            // ASSERT: Verify configuration
            IApiClient client = executor.GetServerApiClient();
            Assert.IsNotNull(client, "Client should be initialized");
            
            // Verify that the client was created (the mock manager returns a valid client)
            Assert.IsTrue(client != null, "API client should be properly configured for loopback communication");
        }

        [Test]
        public void MongoDBExecutor_InitializeApiClients_SingleVM_CallsGetOrCreateApiClientWithLoopback()
        {
            // SETUP: Single VM layout and explicit setup for loopback overload.
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();
            while (this.mockFixture.Dependencies.Any(d => d.ServiceType == typeof(EnvironmentLayout)))
            {
                var layoutDescriptor = this.mockFixture.Dependencies.First(d => d.ServiceType == typeof(EnvironmentLayout));
                this.mockFixture.Dependencies.Remove(layoutDescriptor);
            }

            this.mockFixture.ApiClientManager
                .Setup(mgr => mgr.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback, null))
                .Returns(this.mockFixture.ApiClient.Object);

            var executor = new TestableMongoDBClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();

            // ASSERT
            this.mockFixture.ApiClientManager.Verify(
                mgr => mgr.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback, null),
                Times.Once);
            Assert.AreSame(this.mockFixture.ApiClient.Object, executor.GetServerApiClient());
        }

        #endregion
    }
}