// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BlobManagerExtensionsTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public void TryGetContentStoreReturnsTheExpectedResponseWhenTheStoreExists()
        {
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsTrue(component.TryGetContentStoreManager(out actualStore));
                Assert.IsNotNull(actualStore);
                Assert.IsTrue(object.ReferenceEquals(this.fixture.ContentBlobManager.Object, actualStore));

                // Method overload 2
                Assert.IsTrue(component.Dependencies.TryGetContentStoreManager(out actualStore));
                Assert.IsNotNull(actualStore);
                Assert.IsTrue(object.ReferenceEquals(this.fixture.ContentBlobManager.Object, actualStore));
            }
        }

        [Test]
        public void TryGetContentStoreReturnsTheExpectedResponseWhenTheStoreDoesNotExist_Scenario1()
        {
            // We have not added any blob manager instances to the dependencies.
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsFalse(component.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);

                // Method overload 2
                Assert.IsFalse(component.Dependencies.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);
            }
        }

        [Test]
        public void TryGetContentStoreReturnsTheExpectedResponseWhenTheStoreDoesNotExist_Scenario2()
        {
            // blob manager instances exist but none for the content store.
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
            this.fixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager> { this.fixture.PackagesBlobManager.Object });

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsFalse(component.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);

                // Method overload 2
                Assert.IsFalse(component.Dependencies.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);
            }
        }

        [Test]
        public void TryGetPackagesStoreReturnsTheExpectedResponseWhenTheStoreExists()
        {
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsTrue(component.TryGetPackageStoreManager(out actualStore));
                Assert.IsNotNull(actualStore);
                Assert.IsTrue(object.ReferenceEquals(this.fixture.PackagesBlobManager.Object, actualStore));

                // Method overload 2
                Assert.IsTrue(component.Dependencies.TryGetPackageStoreManager(out actualStore));
                Assert.IsNotNull(actualStore);
                Assert.IsTrue(object.ReferenceEquals(this.fixture.PackagesBlobManager.Object, actualStore));
            }
        }

        [Test]
        public void TryGetPackagesStoreReturnsTheExpectedResponseWhenTheStoreDoesNotExist_Scenario1()
        {
            // We have not added any blob manager instances to the dependencies.
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsFalse(component.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);

                // Method overload 2
                Assert.IsFalse(component.Dependencies.TryGetContentStoreManager(out actualStore));
                Assert.IsNull(actualStore);
            }
        }

        [Test]
        public void TryGetPackagesStoreReturnsTheExpectedResponseWhenTheStoreDoesNotExist_Scenario2()
        {
            // blob manager instances exist but none for the packages store.
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
            this.fixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(new List<IBlobManager> { this.fixture.ContentBlobManager.Object });

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                // Method overload 1
                IBlobManager actualStore;
                Assert.IsFalse(component.TryGetPackageStoreManager(out actualStore));
                Assert.IsNull(actualStore);

                // Method overload 2
                Assert.IsFalse(component.Dependencies.TryGetPackageStoreManager(out actualStore));
                Assert.IsNull(actualStore);
            }
        }

        private class TestVirtualClientComponent : VirtualClientComponent
        {
            public TestVirtualClientComponent(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new IEnumerable<string> SupportedRoles
            {
                get
                {
                    return base.SupportedRoles;
                }

                set
                {
                    base.SupportedRoles = value;
                }
            }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
