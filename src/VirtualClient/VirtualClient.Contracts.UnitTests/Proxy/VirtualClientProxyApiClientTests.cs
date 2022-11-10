// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Rest;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientProxyApiClientTests
    {
        private VirtualClientProxyApiClient apiClient;
        private Mock<IRestClient> mockRestClient;
        private ProxyBlobDescriptor mockPackageDescriptor;
        private ProxyBlobDescriptor mockContentDescriptor;

        [SetUp]
        public void SetupTest()
        {
            this.mockRestClient = new Mock<IRestClient>();
            this.apiClient = new VirtualClientProxyApiClient(this.mockRestClient.Object, new Uri("https://1.2.3.4:5000"));

            this.mockContentDescriptor = new ProxyBlobDescriptor(
                "VirtualClient",
                "Content",
                "blobname.txt",
                Guid.NewGuid().ToString(),
                "application/octet-stream",
                Encoding.UTF8.WebName,
                "/any/path/in/the/container");

            this.mockPackageDescriptor = new ProxyBlobDescriptor(
                "VirtualClient",
                "Packages",
                "blobname.1.0.0.zip",
                "packages",
                "application/octet-stream",
                Encoding.UTF8.WebName);
        }

        [Test]
        public Task VirtualClientProxyApiClienDefaultHttpGetRequestRetryPolicyDeterminesWhenToRetryCorrectly()
        {
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound,
                HttpStatusCode.Locked,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientProxyApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        public Task VirtualClientProxyApiClientDefaultHttpPostRequestRetryPolicyDeterminesWhenToRetryCorrectly()
        {
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientProxyApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        [TestCase("VirtualClient", "Content", "anyfile.log", "anycontainer", "application/octet-stream", "utf-8", null,
            "/api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=anycontainer&contentType=application/octet-stream&contentEncoding=utf-8")]
        //
        [TestCase("VirtualClient", "Content", "anyfile.log", "anycontainer", "application/octet-stream", "utf-8", "/any/path/to/blob",
            "/api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=anycontainer&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=/any/path/to/blob")]
        //
        [TestCase("VirtualClient", "Content", "anyfile.log", "anycontainer", "application/octet-stream", "utf-8", "any/path/to/blob/",
            "/api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=anycontainer&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=any/path/to/blob")]
        //
        [TestCase("VirtualClient", "Content", "anyfile.log", "anycontainer", "application/octet-stream", "utf-8", "/any/path/to/blob/",
            "/api/blobs/anyfile.log?source=VirtualClient&storeType=Content&containerName=anycontainer&contentType=application/octet-stream&contentEncoding=utf-8&blobPath=/any/path/to/blob")]
        public void VirtualClientApiClientFormsTheCorrectUriRouteForAGivenDescriptor(string source, string storeType, string blobName, string containerName, string contentType, string contentEncoding, string blobPath, string expectedRoute)
        {
            string encodin = Encoding.UTF8.WebName;
            ProxyBlobDescriptor descriptor = new ProxyBlobDescriptor(source, storeType, blobName, containerName, contentType, contentEncoding, blobPath);

            string actualRoute = VirtualClientProxyApiClient.CreateBlobApiRoute(descriptor);
            Assert.AreEqual(expectedRoute, actualRoute);
        }

        [Test]
        public async Task VirtualClientProxyApiClientMakesTheExpectedCallToDownloadBlobs()
        {
            bool expectedCallMade = false;
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
                {
                    this.mockRestClient.Setup(client => client.GetAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<HttpCompletionOption>()))
                        .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, option) =>
                        {
                            string expectedSource = this.mockPackageDescriptor.Source;
                            string expectedStoreType = this.mockPackageDescriptor.StoreType;
                            string expectedBlobName = this.mockPackageDescriptor.BlobName;
                            string expectedContainerName = this.mockPackageDescriptor.ContainerName;
                            string expectedContentType = this.mockPackageDescriptor.ContentType;
                            string expectedContentEncoding = this.mockPackageDescriptor.ContentEncoding;

                            Assert.IsTrue(uri.PathAndQuery.Equals(
                                $"/api/blobs/{expectedBlobName}?source={expectedSource}&storeType={expectedStoreType}&containerName={expectedContainerName}&contentType={expectedContentType}&contentEncoding={expectedContentEncoding}"));

                            expectedCallMade = true;
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.DownloadBlobAsync(this.mockPackageDescriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.IsTrue(expectedCallMade);
                }
            }
        }

        [Test]
        public async Task VirtualClientProxyApiClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToDownloadBlobs()
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
                {
                    int attempts = 0;
                    int expectedRetries = 10;

                    this.mockRestClient
                        .Setup(client => client.GetAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<HttpCompletionOption>()))
                        .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, options) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.DownloadBlobAsync(this.mockPackageDescriptor, stream, CancellationToken.None, defaultRetryPolicy)
                        .ConfigureAwait(false);

                    Assert.IsTrue(attempts == expectedRetries + 1);
                }
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientProxyApiClientDoesNotRetryOnExpectedNonTransientFailuresToDownloadBlobs(HttpStatusCode statusCode)
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(statusCode))
                {
                    int attempts = 0;

                    this.mockRestClient
                        .Setup(client => client.GetAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<HttpCompletionOption>()))
                        .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, options) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.DownloadBlobAsync(this.mockPackageDescriptor, stream, CancellationToken.None, defaultRetryPolicy)
                       .ConfigureAwait(false);

                    Assert.IsTrue(attempts == 1);
                }
            }
        }

        [Test]
        public async Task VirtualClientProxyApiClientMakesTheExpectedCallToUploadBlobs()
        {
            bool expectedCallMade = false;
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
                {
                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) =>
                        {
                            string expectedSource = this.mockContentDescriptor.Source;
                            string expectedStoreType = this.mockContentDescriptor.StoreType;
                            string expectedBlobName = this.mockContentDescriptor.BlobName;
                            string expectedContainerName = this.mockContentDescriptor.ContainerName;
                            string expectedContentType = this.mockContentDescriptor.ContentType;
                            string expectedContentEncoding = this.mockContentDescriptor.ContentEncoding;
                            string expectedBlobPath = this.mockContentDescriptor.BlobPath;

                            Assert.IsTrue(uri.PathAndQuery.Equals(
                                $"/api/blobs/{expectedBlobName}?source={expectedSource}&storeType={expectedStoreType}&containerName={expectedContainerName}" +
                                $"&contentType={expectedContentType}&contentEncoding={expectedContentEncoding}&blobPath={expectedBlobPath}"));

                            Assert.IsNotNull(content);

                            expectedCallMade = true;
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.UploadBlobAsync(this.mockContentDescriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);

                    Assert.IsTrue(expectedCallMade);
                }
            }
        }

        [Test]
        public async Task VirtualClientApiProxyClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToUploadBlobs()
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
                {
                    int attempts = 0;
                    int expectedRetries = 10;

                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.UploadBlobAsync(this.mockContentDescriptor, stream, CancellationToken.None, defaultRetryPolicy)
                        .ConfigureAwait(false);

                    Assert.IsTrue(attempts == expectedRetries + 1);
                }
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Conflict)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiProxyClientDoesNotRetryOnExpectedNonTransientFailuresToUploadBlobs(HttpStatusCode statusCode)
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(statusCode))
                {
                    int attempts = 0;

                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.UploadBlobAsync(this.mockContentDescriptor, stream, CancellationToken.None, defaultRetryPolicy)
                        .ConfigureAwait(false);

                    Assert.IsTrue(attempts == 1);
                }
            }
        }

        [Test]
        public async Task VirtualClientApiProxyClientMakesTheExpectedCallToUploadTelemetry()
        {
            bool expectedCallMade = false;
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
                {
                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) =>
                        {
                            Assert.IsTrue(uri.AbsolutePath.Equals("/api/telemetry"));
                            Assert.IsNotNull(content);
                            Assert.DoesNotThrowAsync(() => content.ReadAsJsonAsync<IEnumerable<ProxyTelemetryMessage>>());

                            expectedCallMade = true;
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.UploadTelemetryAsync(new List<ProxyTelemetryMessage>()
                    {
                        new ProxyTelemetryMessage
                        {
                            Source = "VirtualClient",
                            EventType = "Traces",
                            Message = "Executor.Start",
                            ItemType = "traces",
                            SeverityLevel = LogLevel.Information,
                            OperationId = Guid.NewGuid().ToString(),
                            OperationParentId = Guid.NewGuid().ToString(),
                            AppHost = "AnyHost",
                            AppName = "VirtualClient",
                            SdkVersion = "1.10.0.0",
                            CustomDimensions = new Dictionary<string, object>
                            {
                                ["anyProperty1"] = "anyValue",
                                ["anyProperty2"] = 1234,
                                ["anyProperty3"] = true,
                                ["anyProperty4"] = new List<int> { 1, 2, 3, 4 },
                                ["anyProperty5"] = new Dictionary<string, object>
                                {
                                    ["anyChildProperty1"] = "anyOtherValue",
                                    ["anyChildProperty2"] = 9876
                                }
                            }
                        }
                    },
                    CancellationToken.None).ConfigureAwait(false);

                    Assert.IsTrue(expectedCallMade);
                }
            }
        }

        [Test]
        public async Task VirtualClientApiProxyClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToUploadTelemetry()
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
                {
                    int attempts = 0;
                    int expectedRetries = 10;

                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.UploadTelemetryAsync(new List<ProxyTelemetryMessage>()
                    {
                        new ProxyTelemetryMessage
                        {
                            Source = "VirtualClient",
                            EventType = "Traces",
                            Message = "Executor.Start",
                            ItemType = "traces",
                            SeverityLevel = LogLevel.Information,
                            OperationId = Guid.NewGuid().ToString(),
                            OperationParentId = Guid.NewGuid().ToString(),
                            AppHost = "AnyHost",
                            AppName = "VirtualClient",
                            SdkVersion = "1.10.0.0",
                            CustomDimensions = new Dictionary<string, object>
                            {
                                ["anyProperty1"] = "anyValue",
                                ["anyProperty2"] = 1234,
                                ["anyProperty3"] = true,
                                ["anyProperty4"] = new List<int> { 1, 2, 3, 4 },
                                ["anyProperty5"] = new Dictionary<string, object>
                                {
                                    ["anyChildProperty1"] = "anyOtherValue",
                                    ["anyChildProperty2"] = 9876
                                }
                            }
                        }
                    },
                    CancellationToken.None,
                    defaultRetryPolicy).ConfigureAwait(false);

                    Assert.IsTrue(attempts == expectedRetries + 1);
                }
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Conflict)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiProxyClientDoesNotRetryOnExpectedNonTransientFailuresToUploadTelemetry(HttpStatusCode statusCode)
        {
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(statusCode))
                {
                    int attempts = 0;

                    this.mockRestClient.Setup(client => client.PostAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<HttpContent>(),
                            It.IsAny<CancellationToken>()))
                        .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                        .Returns(Task.FromResult(response));

                    // Apply the same default policy used by the client (differing only in the retry wait time).
                    IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientProxyApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                    await this.apiClient.UploadTelemetryAsync(new List<ProxyTelemetryMessage>()
                    {
                        new ProxyTelemetryMessage
                        {
                            Source = "VirtualClient",
                            EventType = "Traces",
                            Message = "Executor.Start",
                            ItemType = "traces",
                            SeverityLevel = LogLevel.Information,
                            OperationId = Guid.NewGuid().ToString(),
                            OperationParentId = Guid.NewGuid().ToString(),
                            AppHost = "AnyHost",
                            AppName = "VirtualClient",
                            SdkVersion = "1.10.0.0",
                            CustomDimensions = new Dictionary<string, object>
                            {
                                ["anyProperty1"] = "anyValue",
                                ["anyProperty2"] = 1234,
                                ["anyProperty3"] = true,
                                ["anyProperty4"] = new List<int> { 1, 2, 3, 4 },
                                ["anyProperty5"] = new Dictionary<string, object>
                                {
                                    ["anyChildProperty1"] = "anyOtherValue",
                                    ["anyChildProperty2"] = 9876
                                }
                            }
                        }
                    },
                    CancellationToken.None,
                    defaultRetryPolicy).ConfigureAwait(false);

                    Assert.IsTrue(attempts == 1);
                }
            }
        }

        private static HttpResponseMessage CreateResponseMessage(HttpStatusCode expectedStatusCode, object expectedContent = null)
        {
            HttpResponseMessage mockResponse = new HttpResponseMessage(expectedStatusCode);

            if (expectedContent != null)
            {
                mockResponse.Content = new StringContent(expectedContent.ToJson());
            }

            return mockResponse;
        }

        private static async Task RunDefaultRetryPolicyTests(IAsyncPolicy<HttpResponseMessage> retryPolicy, IEnumerable<HttpStatusCode> nonTransientErrorCodes)
        {
            using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
            {
                int attempts = 0;

                // HTTP responses with success status codes should not cause retries
                foreach (HttpStatusCode statusCode in Enum.GetValues<HttpStatusCode>())
                {
                    int statusCodeValue = (int)statusCode;
                    if (statusCodeValue < 200)
                    {
                        continue;
                    }

                    attempts = 0;
                    response.StatusCode = statusCode;
                    if (statusCodeValue < 300)
                    {
                        await retryPolicy.ExecuteAsync(() =>
                        {
                            attempts++;
                            return Task.FromResult(response);
                        }).ConfigureAwait(false);

                        Assert.IsTrue(attempts == 1);
                    }
                    else if (nonTransientErrorCodes.Contains(statusCode))
                    {
                        await retryPolicy.ExecuteAsync(() =>
                        {
                            attempts++;
                            return Task.FromResult(response);
                        }).ConfigureAwait(false);

                        Assert.IsTrue(attempts == 1);
                    }
                    else
                    {
                        await retryPolicy.ExecuteAsync(() =>
                        {
                            attempts++;
                            return Task.FromResult(response);
                        }).ConfigureAwait(false);

                        // First attempt + 10 retries
                        Assert.IsTrue(attempts == 11);
                    }
                }
            }
        }
    }
}
