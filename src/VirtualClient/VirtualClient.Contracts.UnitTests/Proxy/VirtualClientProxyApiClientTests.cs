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
    using System.Security.Policy;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Net.Http.Headers;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Rest;

    using RangeHeaderValue = System.Net.Http.Headers.RangeHeaderValue;
    using RangeItemHeaderValue = System.Net.Http.Headers.RangeItemHeaderValue;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientProxyApiClientTests
    {
        private TestProxyClient apiClient;
        private Mock<IRestClient> mockRestClient;
        private Fixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockRestClient = new Mock<IRestClient>();
            this.apiClient = new TestProxyClient(this.mockRestClient.Object, new Uri("https://1.2.3.4:5000"));
            this.fixture = new Fixture();
            this.fixture.Register(VirtualClientProxyApiClientTests.CreateMockProxyTelemetryMessage);
        }

        [Test]
        public Task VirtualClientProxyApiClientDefaultHttpGetRequestRetryPolicyDeterminesWhenToRetryCorrectly()
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
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
                {
                    this.mockRestClient.Setup(client => client.HeadAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

                    this.mockRestClient.Setup(client => client.GetAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<HttpCompletionOption>()))
                        .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, option) =>
                        {
                            Assert.AreEqual(uri.PathAndQuery, VirtualClientProxyApiClientTests.GetExpectedBlobPathAndQuery(descriptor));
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.DownloadBlobAsync(descriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);

                    this.mockRestClient.Verify(client => client.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>(), It.IsAny<HttpCompletionOption>()), Times.Once());
                }
            }
        }

        [Test]
        public async Task VirtualClientProxyApiClientMakesTheExpectedCallToDownloadBlobs_2()
        {
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
            using (Stream stream = new InMemoryStream())
            {
                using (HttpResponseMessage response = VirtualClientProxyApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
                {
                    this.mockRestClient.Setup(client => client.HeadAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

                    this.mockRestClient.Setup(client => client.GetAsync(
                            It.IsAny<Uri>(),
                            It.IsAny<CancellationToken>(),
                            It.IsAny<HttpCompletionOption>()))
                        .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, option) =>
                        {
                            Assert.AreEqual(uri.PathAndQuery, VirtualClientProxyApiClientTests.GetExpectedBlobPathAndQuery(descriptor));
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.DownloadBlobAsync(descriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);

                    this.mockRestClient.Verify(client => client.GetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>(), It.IsAny<HttpCompletionOption>()), Times.Once());
                }
            }
        }

        [Test]
        [TestCase(1024, 16, 64)]
        [TestCase(1032, 16, 65)]
        [TestCase(16, 32, 1)]
        [TestCase(99, 7, 15)]
        public async Task VirtualClientProxyApiClientMakesTheExpectedCallToDownloadBlobsWhenTheBlobHasRangeEnabled(int contentLength, int increment, int expectedInvocations)
        {
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
            using Stream stream = new InMemoryStream();
            using HttpResponseMessage headResponse = new HttpResponseMessage(HttpStatusCode.OK);
            headResponse.Headers.Add("Accept-Ranges", "bytes");
            headResponse.Content.Headers.Add("Content-Length", contentLength.ToString());

            byte[] expectedBytes = new byte[contentLength];
            Random.Shared.NextBytes(expectedBytes);

            this.mockRestClient.Setup(rc => rc.HeadAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Callback<Uri, CancellationToken>((actualUri, cancellationToken) =>
                {
                    Assert.AreEqual(actualUri.PathAndQuery, VirtualClientProxyApiClientTests.GetExpectedBlobPathAndQuery(descriptor));
                }).ReturnsAsync(headResponse);

            int expectedFrom = 0;
            int expectedTo = increment;
            this.mockRestClient.Setup(rc => rc.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()))
                .Callback<HttpRequestMessage, CancellationToken>((request, cancellationToken) =>
                {
                    Uri actualUri = request.RequestUri;
                    Assert.AreEqual(actualUri.PathAndQuery, VirtualClientProxyApiClientTests.GetExpectedBlobPathAndQuery(descriptor));
                    Assert.AreEqual(HttpMethod.Get, request.Method);

                    RangeHeaderValue range = request.Headers.Range;
                    Assert.IsNotNull(range);
                    Assert.AreEqual(1, range.Ranges.Count);

                    RangeItemHeaderValue rangeItem = range.Ranges.First();
                    Assert.AreEqual(expectedFrom, rangeItem.From);
                    Assert.AreEqual(expectedTo, rangeItem.To);
                })
                .ReturnsAsync(() =>
                {
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(expectedBytes[expectedFrom..Math.Min(expectedTo, contentLength)])
                    };

                    expectedFrom = expectedTo;
                    expectedTo = expectedTo + increment;
                    return response;
                });

            this.apiClient.PublicBlobChunkSize = increment;
            await this.apiClient.DownloadBlobAsync(descriptor, stream, CancellationToken.None);

            this.mockRestClient.Verify(rc => rc.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(expectedInvocations));

            byte[] actualBytes = new byte[stream.Length];
            await stream.ReadAsync(actualBytes, CancellationToken.None);

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [Test]
        public async Task VirtualClientProxyApiClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToDownloadBlobs()
        {
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
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

                    await this.apiClient.DownloadBlobAsync(descriptor, stream, CancellationToken.None, defaultRetryPolicy);

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
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
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

                    await this.apiClient.DownloadBlobAsync(descriptor, stream, CancellationToken.None, defaultRetryPolicy);

                    Assert.IsTrue(attempts == 1);
                }
            }
        }

        [Test]
        public async Task VirtualClientProxyApiClientMakesTheExpectedCallToUploadBlobs()
        {
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor(withPath: true);
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
                            Assert.AreEqual(uri.PathAndQuery, VirtualClientProxyApiClientTests.GetExpectedBlobPathAndQuery(descriptor));

                            Assert.IsNotNull(content);
                        })
                        .Returns(Task.FromResult(response));

                    await this.apiClient.UploadBlobAsync(descriptor, stream, CancellationToken.None)
                        .ConfigureAwait(false);

                    this.mockRestClient.Verify(client => client.PostAsync(It.IsAny<Uri>(), It.IsAny<HttpContent>(), It.IsAny<CancellationToken>()), Times.Once());
                }
            }
        }

        [Test]
        public async Task VirtualClientApiProxyClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToUploadBlobs()
        {
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
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

                    await this.apiClient.UploadBlobAsync(descriptor, stream, CancellationToken.None, defaultRetryPolicy)
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
            ProxyBlobDescriptor descriptor = VirtualClientProxyApiClientTests.GetBlobDescriptor();
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

                    await this.apiClient.UploadBlobAsync(descriptor, stream, CancellationToken.None, defaultRetryPolicy)
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

                    await this.apiClient.UploadTelemetryAsync(this.fixture.CreateMany<ProxyTelemetryMessage>(), CancellationToken.None).ConfigureAwait(false);

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

                    await this.apiClient.UploadTelemetryAsync(this.fixture.CreateMany<ProxyTelemetryMessage>(), CancellationToken.None, defaultRetryPolicy).ConfigureAwait(false);

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

                    await this.apiClient.UploadTelemetryAsync(this.fixture.CreateMany<ProxyTelemetryMessage>(), CancellationToken.None, defaultRetryPolicy).ConfigureAwait(false);

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

        private static ProxyTelemetryMessage CreateMockProxyTelemetryMessage()
        {
            return new ProxyTelemetryMessage
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
            };
        }

        private static ProxyBlobDescriptor GetBlobDescriptor(bool withPath = false)
        {
            return new ProxyBlobDescriptor(
                "VirtualClient",
                "Packages",
                "blobname.1.0.0.zip",
                "packages",
                "application/octet-stream",
                Encoding.UTF8.WebName,
                withPath ? "/path/to/blob" : null);
        }

        private static string GetExpectedBlobPathAndQuery(ProxyBlobDescriptor descriptor)
        {
            string expectedSource = descriptor.Source;
            string expectedStoreType = descriptor.StoreType;
            string expectedBlobName = descriptor.BlobName;
            string expectedContainerName = descriptor.ContainerName;
            string expectedContentType = descriptor.ContentType;
            string expectedContentEncoding = descriptor.ContentEncoding;
            string expectedBlobPath = descriptor.BlobPath;

            return $"/api/blobs/{expectedBlobName}?source={expectedSource}" +
                $"&storeType={expectedStoreType}" +
                $"&containerName={expectedContainerName}" +
                $"&contentType={expectedContentType}" +
                $"&contentEncoding={expectedContentEncoding}" +
                $"{(!string.IsNullOrEmpty(expectedBlobPath) ? $"&blobPath={expectedBlobPath}" : string.Empty)}";
        }

        private class TestProxyClient : VirtualClientProxyApiClient
        {
            public int PublicBlobChunkSize = 1024;

            public TestProxyClient(IRestClient restClient, Uri baseUri) 
                : base(restClient, baseUri)
            {
            }

            protected override int BlobChunkSize 
            { 
                get 
                {
                    return this.PublicBlobChunkSize;
                } 
            }
        }
    }
}
