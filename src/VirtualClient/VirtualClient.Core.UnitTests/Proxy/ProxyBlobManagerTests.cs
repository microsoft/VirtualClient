// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;

    [TestFixture]
    [Category("Unit")]
    internal class ProxyBlobManagerTests
    {
        private MockFixture mockFixture;
        private Mock<IProxyApiClient> mockProxyApiClient;
        private MemoryStream mockStream;
        private DependencyProxyStore mockContentStore;
        private DependencyProxyStore mockPackagesStore;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockProxyApiClient = new Mock<IProxyApiClient>();
            this.mockStream = new MemoryStream(Encoding.UTF8.GetBytes("Any blob"));
            this.mockContentStore = new DependencyProxyStore(DependencyProxyStore.Content, new Uri("http://any.proxy:4600"));
            this.mockPackagesStore = new DependencyProxyStore(DependencyProxyStore.Packages, new Uri("http://any.proxy:4600"));

            // Setup default mock behaviors
            this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [TearDown]
        public void CleanupTest()
        {
            this.mockStream.Dispose();
        }

        [Test]
        public Task ProxyBlobManagerDownloadsUseTheExpectedSourceWhenAnExplicitSourceIsSupplied()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                // Explicit source is defined
                string expectedSource = "AnySpecificSource";
                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object, expectedSource);

                this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.AreEqual(expectedSource, blobDescriptor.Source);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.DownloadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerDownloadsHandlesCasesWhenAnExplicitSourceIsNotSupplied()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                // Explicit source is NOT defined
                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.DownloadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerDownloadsTheExpectedBlobThroughTheProxyApi_Scenario1()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockPackagesStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anypackage.1.0.0.zip", blobDescriptor.BlobName);
                    Assert.AreEqual("packages", blobDescriptor.ContainerName);
                    Assert.AreEqual(Encoding.UTF8.WebName, blobDescriptor.ContentEncoding);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.DownloadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerDownloadsTheExpectedBlobThroughTheProxyApi_Scenario2()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages",
                    ["ContentType"] = "application/octet-stream",
                    ["ContentEncoding"] = Encoding.ASCII.WebName
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockPackagesStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anypackage.1.0.0.zip", blobDescriptor.BlobName);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                    Assert.AreEqual(Encoding.ASCII.WebName, blobDescriptor.ContentEncoding);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.DownloadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public void ProxyBlobManagerThrowsIfTheBlobDownloadFails()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.DownloadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.Forbidden));

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => blobManager.DownloadBlobAsync(descriptor, this.mockStream, CancellationToken.None));
                Assert.AreEqual(ErrorReason.DependencyInstallationFailed, error.Reason);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsUseTheExpectedSourceWhenAnExplicitSourceIsSupplied()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                // Explicit source is defined
                string expectedSource = "AnySpecificSource";
                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object, expectedSource);

                this.mockProxyApiClient
                    .Setup(client => client.UploadBlobAsync(
                        It.IsAny<ProxyBlobDescriptor>(),
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                    {
                        Assert.IsNotNull(blobDescriptor);
                        Assert.AreEqual(expectedSource, blobDescriptor.Source);
                    })
                    .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsHandlesCasesWhenAnExplicitSourceIsNotSupplied()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                // Explicit source is NOT defined
                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockPackagesStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                    .Setup(client => client.UploadBlobAsync(
                        It.IsAny<ProxyBlobDescriptor>(),
                        It.IsAny<Stream>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                    {
                        Assert.IsNotNull(blobDescriptor);
                        Assert.IsNull(blobDescriptor.Source);
                    })
                    .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsABlobAsExpectedThroughTheProxyApi_Scenario1()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anyfile.log",
                    ["ContainerName"] = "logs"
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockContentStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockContentStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anyfile.log", blobDescriptor.BlobName);
                    Assert.AreEqual("logs", blobDescriptor.ContainerName);
                    Assert.AreEqual(Encoding.UTF8.WebName, blobDescriptor.ContentEncoding);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsABlobAsExpectedThroughTheProxyApi_Scenario2()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "/any/path/to/blob/anyfile.log",
                    ["ContainerName"] = "logs"
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockContentStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockContentStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anyfile.log", blobDescriptor.BlobName);
                    Assert.AreEqual("/any/path/to/blob", blobDescriptor.BlobPath);
                    Assert.AreEqual("logs", blobDescriptor.ContainerName);
                    Assert.AreEqual(Encoding.UTF8.WebName, blobDescriptor.ContentEncoding);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsABlobAsExpectedThroughTheProxyApi_Scenario3()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anyfile.log",
                    ["ContainerName"] = "logs",
                    ["ContentType"] = "application/octet-stream",
                    ["ContentEncoding"] = Encoding.ASCII.WebName
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockContentStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockContentStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anyfile.log", blobDescriptor.BlobName);
                    Assert.AreEqual("logs", blobDescriptor.ContainerName);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                    Assert.AreEqual(Encoding.ASCII.WebName, blobDescriptor.ContentEncoding);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public Task ProxyBlobManagerUploadsABlobAsExpectedThroughTheProxyApi_Scenario4()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "/any/path/to/blob/anyfile.log",
                    ["ContainerName"] = "logs",
                    ["ContentType"] = "application/octet-stream",
                    ["ContentEncoding"] = Encoding.ASCII.WebName,
                    ["Source"] = "VirtualClient"
                });


                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockContentStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<ProxyBlobDescriptor, Stream, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((blobDescriptor, stream, token, retryPolicy) =>
                {
                    Assert.IsNotNull(blobDescriptor);
                    Assert.IsNull(blobDescriptor.Source);
                    Assert.AreEqual(this.mockContentStore.StoreName, blobDescriptor.StoreType);
                    Assert.AreEqual("anyfile.log", blobDescriptor.BlobName);
                    Assert.AreEqual("/any/path/to/blob", blobDescriptor.BlobPath);
                    Assert.AreEqual("logs", blobDescriptor.ContainerName);
                    Assert.AreEqual("application/octet-stream", blobDescriptor.ContentType);
                    Assert.AreEqual(Encoding.ASCII.WebName, blobDescriptor.ContentEncoding);
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

                return blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None);
            }
        }

        [Test]
        public void ProxyBlobManagerThrowsIfTheBlobUploadFails()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                DependencyDescriptor descriptor = new DependencyDescriptor(new Dictionary<string, IConvertible>
                {
                    ["Name"] = "anypackage.1.0.0.zip",
                    ["ContainerName"] = "packages"
                });

                ProxyBlobManager blobManager = new ProxyBlobManager(this.mockContentStore, this.mockProxyApiClient.Object);

                this.mockProxyApiClient
                .Setup(client => client.UploadBlobAsync(
                    It.IsAny<ProxyBlobDescriptor>(),
                    It.IsAny<Stream>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.Forbidden));

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => blobManager.UploadBlobAsync(descriptor, this.mockStream, CancellationToken.None));
                Assert.AreEqual(ErrorReason.ApiRequestFailed, error.Reason);
            }
        }
    }
}