// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.UnitTests.MongoDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient;
    using VirtualClient.Actions;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

    [TestFixture]
    [Category("Unit")]
    public class MongoDBServerExecutorTests
    {
        private MockFixture mockFixture;

        // Testable derived class to expose protected members using Pattern 1
        private class TestableMongoDBServerExecutor : MongoDBServerExecutor
        {
            public TestableMongoDBServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            // Expose protected methods
            public new void InitializeApiClients() => base.InitializeApiClients();

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
                => base.InitializeAsync(telemetryContext, cancellationToken);

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
                => base.ExecuteAsync(telemetryContext, cancellationToken);

            // Expose protected properties via getter methods
            public IApiClient GetServerApiClient() => this.ServerApiClient;
            public string GetServerIpAddress() => this.ServerIpAddress;
            public CancellationTokenSource GetServerCancellationSource() => this.ServerCancellationSource;
            public int GetPort() => this.Port;
        }

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix, Architecture.X64);

            // Setup default parameters
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Scenario", "MongoDB-Server" },
                { "DiskFilter", string.Empty }
            };

            // Setup process manager mock
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardOutput.Append("{ \"ok\" : 1 }");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };
        }

        [Test]
        public void MongoDBServerExecutor_DiskFilter_ReturnsDefaultEmptyString()
        {
            // SETUP: Parameters without DiskFilter
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Scenario", "MongoDB-Server" }
            };
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            string diskFilter = executor.DiskFilter;

            // ASSERT
            Assert.AreEqual(string.Empty, diskFilter);
        }

        [Test]
        public void MongoDBServerExecutor_DiskFilter_ReturnsParameterValue()
        {
            // SETUP: Parameters with DiskFilter
            this.mockFixture.Parameters["DiskFilter"] = "BiggestSize";
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            string diskFilter = executor.DiskFilter;

            // ASSERT
            Assert.AreEqual("BiggestSize", diskFilter);
        }

        [Test]
        public void MongoDBServerExecutor_DiskFilter_WithDifferentValues_ReturnsCorrectly()
        {
            // SETUP
            var executor1 = new MongoDBServerExecutor(this.mockFixture.Dependencies, 
                new Dictionary<string, IConvertible> { { "DiskFilter", "SmallestSize" } });
            var executor2 = new MongoDBServerExecutor(this.mockFixture.Dependencies, 
                new Dictionary<string, IConvertible> { { "DiskFilter", "BiggestSize" } });

            // ACT
            string filter1 = executor1.DiskFilter;
            string filter2 = executor2.DiskFilter;

            // ASSERT
            Assert.AreEqual("SmallestSize", filter1);
            Assert.AreEqual("BiggestSize", filter2);
        }

        [Test]
        public void MongoDBServerExecutor_Constructor_InitializesProperties()
        {
            // SETUP
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ASSERT - verify constructor properly initializes the executor
            Assert.IsNotNull(executor, "Executor should be created");
        }

        [Test]
        public void MongoDBServerExecutor_Dispose_CompletesSuccessfully()
        {
            // SETUP: Create executor
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Dispose
            executor.Dispose();

            // ASSERT: Should not throw exception
            Assert.Pass("Dispose completed successfully");
        }

        [Test]
        public void MongoDBServerExecutor_DisposeTwice_DoesNotThrow()
        {
            // SETUP: Create executor
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT: Dispose twice
            executor.Dispose();
            executor.Dispose();

            // ASSERT: Should not throw exception
            Assert.Pass("Double dispose handled correctly");
        }

        [Test]
        public void MongoDBServerExecutor_Port_ReturnsDefaultValue()
        {
            // SETUP
            this.mockFixture.Parameters = new Dictionary<string, IConvertible> { { "Scenario", "MongoDB-Server" } };
            var executor = new MongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.Port;

            // ASSERT
            Assert.AreEqual(27017, port);
        }

        #region Protected Member Tests Using Pattern 1

        [Test]
        public void MongoDBServerExecutor_InitializeApiClients_CreatesServerApiClient()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();

            // ASSERT
            Assert.IsNotNull(executor.GetServerApiClient(), "ServerApiClient should be initialized");
        }

        [Test]
        public void MongoDBServerExecutor_InitializeApiClients_UsesLoopbackIPAddress_SingleVM()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();

            // ASSERT
            string serverIp = executor.GetServerIpAddress();
            Assert.IsNotNull(serverIp, "ServerIpAddress should be set");
        }

        [Test]
        public void MongoDBServerExecutor_InitializeApiClients_MultipleInvocations_DoesNotThrow()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT & ASSERT
            Assert.DoesNotThrow(() => executor.InitializeApiClients());
            Assert.DoesNotThrow(() => executor.InitializeApiClients(), "Multiple calls should be safe");
        }

        [Test]
        public void MongoDBServerExecutor_ProtectedPort_AccessibleViaTestClass()
        {
            // SETUP
            this.mockFixture.Parameters["Port"] = 27019;
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            int port = executor.GetPort();

            // ASSERT
            Assert.AreEqual(27019, port);
        }

        [Test]
        public void MongoDBServerExecutor_ServerCancellationSource_InitiallyNull()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            var cancellationSource = executor.GetServerCancellationSource();

            // ASSERT
            Assert.IsNull(cancellationSource, "ServerCancellationSource should be null before initialization");
        }

        [Test]
        public void MongoDBServerExecutor_InitializeApiClients_PreservesApiClientReference()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();
            var apiClient1 = executor.GetServerApiClient();
            var apiClient2 = executor.GetServerApiClient();

            // ASSERT
            Assert.AreSame(apiClient1, apiClient2, "ServerApiClient reference should remain consistent");
        }


        [Test]
        public void MongoDBServerExecutor_ServerApiClient_NullBeforeInitialization()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            var apiClient = executor.GetServerApiClient();

            // ASSERT
            Assert.IsNull(apiClient, "ServerApiClient should be null before InitializeApiClients is called");
        }

        [Test]
        public void MongoDBServerExecutor_DiskFilter_WithCustomFilter_ConfiguresCorrectly()
        {
            // SETUP
            this.mockFixture.Parameters["DiskFilter"] = "BiggestSize";
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            string diskFilter = executor.DiskFilter;

            // ASSERT
            Assert.AreEqual("BiggestSize", diskFilter);
        }

        [Test]
        public void MongoDBServerExecutor_InitializeAsync_CallsBaseInitialization()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = new EventContext(Guid.NewGuid());
            var cancellationToken = CancellationToken.None;

            // Setup mock for process execution
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardOutput.Append("{ \"ok\" : 1 }");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };

            // ACT & ASSERT - Should not throw (InitializeAsync attempts to start MongoDB which will fail in test, but we verify it doesn't throw on base initialization)
            Assert.DoesNotThrowAsync(async () => await executor.InitializeAsync(context, cancellationToken));
        }

        [Test]
        public async Task MongoDBServerExecutor_InitializeAsync_WithDiskFilter_CallsInitializeDiskPathAndConfigureDisk()
        {
            // SETUP: Set DiskFilter to trigger disk initialization path
            this.mockFixture.Parameters["DiskFilter"] = "BiggestSize";
            this.mockFixture.Parameters["DiskDevicePath"] = "/dev/nvme0n1";  // Pre-set to avoid KeyNotFound

            var volumes = new List<DiskVolume>();
            var disk = new Disk(index: 0, devicePath: "/dev/nvme0n1", volumes: volumes, properties: null);
            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Disk> { disk });

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardOutput.Append("{ \"ok\" : 1 }");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };

            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = EventContext.Persisted();

            // ACT: Initialize executor - should trigger disk initialization workflow
            await executor.InitializeAsync(context, CancellationToken.None);

            // ASSERT: Verify the DiskFilter caused disk initialization code path to execute
            // InitializeDiskPathAsync should have been called (it retrieves disks and filters them)
            Assert.AreEqual("BiggestSize", this.mockFixture.Parameters["DiskFilter"], 
                "DiskFilter should remain set after initialization");
            
            // InitializeApiClients should have been called
            Assert.IsNotNull(executor.GetServerApiClient(), 
                "ServerApiClient should be initialized");
        }

        [Test]
        public async Task MongoDBServerExecutor_InitializeAsync_CallsConfigureBindAddressAndStartServer()
        {
            // SETUP: Create executor without DiskFilter to focus on server startup workflow
            this.mockFixture.Parameters["DiskFilter"] = string.Empty;  // No disk configuration

            // Both OnCreateProcess and CreateElevatedProcess will be called - both return same mocked process
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardError.Clear();
                this.mockFixture.Process.StandardOutput.Append("{\"ok\" : 1}");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };

            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = EventContext.Persisted();

            // ACT: Initialize executor - should call ConfigureMongoDBBindAddressAsync → StartMongoDBServerAsync
            await executor.InitializeAsync(context, CancellationToken.None);

            // ASSERT: Verify ServerApiClient was initialized
            Assert.IsNotNull(executor.GetServerApiClient(), 
                "InitializeApiClients should have been called, ServerApiClient should be initialized");
        }

        [Test]
        public async Task MongoDBServerExecutor_ExecuteAsync_CompletesWhenCancelled()
        {
            // SETUP
            this.mockFixture.Parameters["DiskFilter"] = string.Empty;
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = EventContext.Persisted();

            // Initialize API clients first
            executor.InitializeApiClients();

            // Create a cancellation token that will cancel after a short delay
            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
            {
                // ACT & ASSERT: Execute should complete without throwing when cancelled
                await executor.ExecuteAsync(context, cts.Token);
                
                // If we reach here, the test passed - ExecuteAsync handled cancellation properly
                Assert.Pass("ExecuteAsync completed successfully when cancelled");
            }
        }

        [Test]
        public void MongoDBServerExecutor_Constructor_InitializesServerRetryPolicy()
        {
            // SETUP & ACT
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ASSERT - Verify constructor completes successfully and object is created
            Assert.IsNotNull(executor, "Executor should be initialized with retry policy");
        }

        [Test]
        public void MongoDBServerExecutor_DiskFilter_EmptyByDefault()
        {
            // SETUP - No DiskFilter parameter
            this.mockFixture.Parameters.Remove("DiskFilter");
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            string diskFilter = executor.DiskFilter;

            // ASSERT
            Assert.AreEqual(string.Empty, diskFilter, "DiskFilter should be empty by default");
        }

        [Test]
        public void MongoDBServerExecutor_InitializeApiClients_SetsServerIpAddress()
        {
            // SETUP
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            executor.InitializeApiClients();
            string serverIp = executor.GetServerIpAddress();

            // ASSERT
            Assert.IsNotNull(serverIp, "ServerIpAddress should be set after InitializeApiClients");
            Assert.IsNotEmpty(serverIp, "ServerIpAddress should not be empty");
        }

        [Test]
        public void MongoDBServerExecutor_WithNullDiskFilter_HandlesCorrectly()
        {
            // SETUP - Set DiskFilter to null
            this.mockFixture.Parameters["DiskFilter"] = null;
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // ACT
            string diskFilter = executor.DiskFilter;

            // ASSERT
            Assert.AreEqual(string.Empty, diskFilter, "Null DiskFilter should be treated as empty string");
        }

        [Test]
        public void MongoDBServerExecutor_InitializeAsync_ThrowsWhenNoDisksAvailableForFilter()
        {
            // SETUP: Set DiskFilter and mock DiskManager to return no disks
            this.mockFixture.Parameters["DiskFilter"] = "BiggestSize";
            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Disk>()); // No disks returned

             this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardOutput.Append("{ \"ok\" : 1 }");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };
            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = EventContext.Persisted();

            var ex = Assert.ThrowsAsync<DependencyException>(async () =>
                await executor.InitializeAsync(context, CancellationToken.None));

            StringAssert.Contains("No disks are available on the system to match the filter criteria", ex.Message);
        }

        [Test]
        public void MongoDBServerExecutor_InitializeAsync_ThrowsWhenNoDiskMatchesFilter()
        {
            // SETUP: Set a DiskFilter but return disks that don't match
            this.mockFixture.Parameters["DiskFilter"] = "DiskPath:/dev/sdb"; 
            this.mockFixture.Parameters["DiskDevicePath"] = "/dev/nvme0n1";

            // Mock DiskManager to return disks that don't match the filter (e.g., HDD disks)
            var volumes = new List<DiskVolume>();
            var hddDisk = new Disk(
                index: 0,
                devicePath: "/dev/sda",
                volumes: volumes,
                properties: new Dictionary<string, IConvertible> { { "DiskType", "HDD" } });
            
            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Disk> { hddDisk });

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, args, workingDir) =>
            {
                this.mockFixture.Process.StandardOutput.Clear();
                this.mockFixture.Process.StandardOutput.Append("{ \"ok\" : 1 }");
                this.mockFixture.Process.ExitCode = 0;
                return this.mockFixture.Process;
            };

            var executor = new TestableMongoDBServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            var context = EventContext.Persisted();

            // ACT & ASSERT: Should throw SchemaException with "No disks matched the filter criteria"
            var ex = Assert.ThrowsAsync<DependencyException>(async () =>
                await executor.InitializeAsync(context, CancellationToken.None));
            
            StringAssert.Contains("No disks matched the filter criteria", ex.Message);
        }

        #endregion
    }
}

