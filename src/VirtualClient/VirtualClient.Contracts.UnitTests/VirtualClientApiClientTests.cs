// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientApiClientTests
    {
        private VirtualClientApiClient apiClient;
        private Mock<IRestClient> mockRestClient;

        [SetUp]
        public void SetupTest()
        {
            this.mockRestClient = new Mock<IRestClient>();
            this.apiClient = new VirtualClientApiClient(this.mockRestClient.Object, new Uri("https://1.2.3.4:5000"));
        }

        [Test]
        public Task VirtualClientApiClientDefaultHttpDeleteRequestRetryPolicyDeterminesWhenToRetryCorrectly()
        {
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpDeleteRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        public Task VirtualClientApiClientDefaultHttpGetRequestRetryPolicyDeterminesWhenToRetryCorrectly()
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

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        public Task VirtualClientApiClientDefaultHttpPostRequestRetryPolicyDeterminesWhenToRetryCorrectly()
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

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        public Task VirtualClientApiClientDefaultHttpPutRequestRetryPolicyDeterminesWhenToRetryCorrectly()
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

            IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPutRetryPolicy(retries => TimeSpan.Zero);

            return VirtualClientApiClientTests.RunDefaultRetryPolicyTests(defaultRetryPolicy, nonTransientErrorCodes);
        }

        [Test]
        public void VirtualClientApiClientMakesTheExpectedCallToGetHeartbeats()
        {
            bool expectedCallMade = false;
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
            {
                this.mockRestClient.Setup(client => client.GetAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<HttpCompletionOption>()))
                    .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, options) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals("/api/heartbeat"));
                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                this.apiClient.GetHeartbeatAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToGetHeartbeats()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
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
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.GetHeartbeatAsync(CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToGetHearbeats(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
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
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.GetHeartbeatAsync(CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
            }
        }

        [Test]
        public async Task VirtualClientApiClientMakesTheExpectedCallToCreateStateObjects()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";
            object expectedState = new
            {
                property1 = "Value",
                property2 = 1234
            };

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Created, expectedState))
            {
                this.mockRestClient.Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>(async (uri, content, token) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals($"/api/state/{expectedStateId}"));

                        Assert.AreEqual(
                            expectedState.ToJson().RemoveWhitespace(),
                            (await content.ReadAsStringAsync().ConfigureAwait(false)).RemoveWhitespace());

                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                await this.apiClient.CreateStateAsync(expectedStateId, JObject.FromObject(expectedState), CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyDefinedOnFailuresToCreateStateObjects()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
            {
                int attempts = 0;
                int expectedRetries = 10;

                this.mockRestClient
                    .Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.CreateStateAsync("anyStateId", JObject.Parse("{ 'any': 'state' }"), CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Conflict)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToCreateStateObject(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
            {
                int attempts = 0;

                this.mockRestClient
                    .Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.CreateStateAsync("anyStateId", JObject.Parse("{ 'any': 'state' }"), CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
            }
        }

        [Test]
        public void VirtualClientApiClientMakesTheExpectedCallToDeleteStateObjects()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.NoContent))
            {
                this.mockRestClient.Setup(client => client.DeleteAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, CancellationToken>((uri, token) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals($"/api/state/{expectedStateId}"));
                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                this.apiClient.DeleteStateAsync(expectedStateId, CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToDeleteStateObjects()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
            {
                int attempts = 0;
                int expectedRetries = 10;

                this.mockRestClient
                    .Setup(client => client.DeleteAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, CancellationToken>((uri, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.DeleteStateAsync("anyStateId", CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToDeleteStateObjects(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
            {
                int attempts = 0;

                this.mockRestClient
                    .Setup(client => client.DeleteAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, CancellationToken>((uri, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpDeleteRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.DeleteStateAsync("anyStateId", CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
            }
        }

        [Test]
        public void VirtualClientApiClientMakesTheExpectedCallToGetStateObjects()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
            {
                this.mockRestClient.Setup(client => client.GetAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<HttpCompletionOption>()))
                    .Callback<Uri, CancellationToken, HttpCompletionOption>((uri, token, options) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals($"/api/state/{expectedStateId}"));
                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                this.apiClient.GetStateAsync(expectedStateId, CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyOnFailuresToGetStateObjects()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
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
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.GetStateAsync("anyStateId", CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToGetStateObjects(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
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
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.GetStateAsync("anyStateId", CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
            }
        }

        [Test]
        public void VirtualClientApiClientMakesTheExpectedCallToSendApplicationExitRequests()
        {
            bool expectedCallMade = false;

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
            {
                this.mockRestClient.Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, CancellationToken>((uri, token) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals($"/api/application/exit"));
                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                this.apiClient.SendApplicationExitInstructionAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public void VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyDefinedOnFailuresToSendApplicationExitRequests()
        {
            int attempts = 0;
            int expectedRetries = 10;

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.InternalServerError))
            {
                this.mockRestClient.Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, CancellationToken>((uri, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                this.apiClient.SendApplicationExitInstructionAsync(CancellationToken.None, defaultRetryPolicy).GetAwaiter().GetResult();

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyDefinedOnFailuresToSendInstructionsRequests()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
            {
                int attempts = 0;
                int expectedRetries = 10;

                this.mockRestClient
                    .Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.SendInstructionsAsync(JObject.Parse("{ 'any': 'instructions' }"), CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Conflict)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToSendInstructionsRequests(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
            {
                int attempts = 0;

                this.mockRestClient
                    .Setup(client => client.PostAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.SendInstructionsAsync(JObject.Parse("{ 'any': 'instructions' }"), CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
            }
        }

        [Test]
        public async Task VirtualClientApiClientMakesTheExpectedCallToUpdateStateObjects()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";
            object expectedState = new
            {
                property1 = "Value",
                property2 = 1234
            };

            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Created, expectedState))
            {
                this.mockRestClient.Setup(client => client.PutAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>(async (uri, content, token) =>
                    {
                        Assert.IsTrue(uri.AbsolutePath.Equals($"/api/state/{expectedStateId}"));

                        Assert.AreEqual(
                            expectedState.ToJson().RemoveWhitespace(),
                            (await content.ReadAsStringAsync().ConfigureAwait(false)).RemoveWhitespace());

                        expectedCallMade = true;
                    })
                    .Returns(Task.FromResult(response));

                await this.apiClient.UpdateStateAsync(expectedStateId, JObject.FromObject(expectedState), CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task VirtualClientApiClientAppliesTheExpectedDefaultRetryPolicyDefinedOnFailuresToUpdateStateObjects()
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.Ambiguous))
            {
                int attempts = 0;
                int expectedRetries = 10;

                this.mockRestClient
                    .Setup(client => client.PutAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPutRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.UpdateStateAsync("anyStateId", JObject.Parse("{ 'any': 'state' }"), CancellationToken.None, defaultRetryPolicy)
                    .ConfigureAwait(false);

                Assert.IsTrue(attempts == expectedRetries + 1);
            }
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Conflict)]
        [TestCase(HttpStatusCode.Forbidden)]
        [TestCase(HttpStatusCode.NetworkAuthenticationRequired)]
        [TestCase(HttpStatusCode.HttpVersionNotSupported)]
        [TestCase(HttpStatusCode.Unauthorized)]
        public async Task VirtualClientApiClientDoesNotRetryOnExpectedNonTransientFailuresToUpdateStateObjects(HttpStatusCode statusCode)
        {
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(statusCode))
            {
                int attempts = 0;

                this.mockRestClient
                    .Setup(client => client.PutAsync(
                        It.IsAny<Uri>(),
                        It.IsAny<HttpContent>(),
                        It.IsAny<CancellationToken>()))
                    .Callback<Uri, HttpContent, CancellationToken>((uri, content, token) => attempts++)
                    .Returns(Task.FromResult(response));

                // Apply the same default policy used by the client (differing only in the retry wait time).
                IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = VirtualClientApiClient.GetDefaultHttpPutRetryPolicy(retries => TimeSpan.Zero);

                await this.apiClient.UpdateStateAsync("anyStateId", JObject.Parse("{ 'any': 'state' }"), CancellationToken.None, defaultRetryPolicy)
                   .ConfigureAwait(false);

                Assert.IsTrue(attempts == 1);
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
            using (HttpResponseMessage response = VirtualClientApiClientTests.CreateResponseMessage(HttpStatusCode.OK))
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
