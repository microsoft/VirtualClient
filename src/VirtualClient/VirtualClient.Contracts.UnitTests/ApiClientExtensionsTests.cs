// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
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
    using VirtualClient.Core;

    [TestFixture]
    [Category("Unit")]
    public class ApiClientExtensionsTests
    {
        private MockFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            VirtualClientApiClient.DefaultPollingWaitTime = TimeSpan.Zero;
        }

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public async Task GetStateExtensionReturnsTheExpectedStateWhenItExists()
        {
            Item<TestState> existingState = new Item<TestState>(Guid.NewGuid().ToString(), new TestState());

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, existingState))
            {
                this.fixture.ApiClient.OnGetState(existingState.Id).ReturnsAsync(response);

                Item<TestState> actualState = await this.fixture.ApiClient.Object.GetStateAsync<TestState>(existingState.Id, CancellationToken.None);
                Assert.AreEqual(existingState.Id, actualState.Id);
                Assert.IsNotNull(existingState.Definition);
                Assert.IsNotNull(existingState.Definition.Properties);
                Assert.AreEqual(existingState.Definition.Value, actualState.Definition.Value);
            }
        }

        [Test]
        public async Task GetStateExtensionReturnsTheExpectedStateWhenItExists_With_API_JObject_Responses()
        {
            Item<JObject> existingState = new Item<JObject>(Guid.NewGuid().ToString(), JObject.FromObject(new TestState()));

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, existingState))
            {
                this.fixture.ApiClient.OnGetState(existingState.Id).ReturnsAsync(response);

                Item<TestState> actualState = await this.fixture.ApiClient.Object.GetStateAsync<TestState>(existingState.Id, CancellationToken.None);
                Assert.AreEqual(existingState.Id, actualState.Id);
                Assert.IsNotNull(existingState.Definition);
            }
        }

        [Test]
        public async Task GetStateExtensionReturnsTheExpectedStateWhenItDoesNotExist()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient.OnGetState().ReturnsAsync(response);

                Item<TestState> actualState = await this.fixture.ApiClient.Object.GetStateAsync<TestState>("AnyId", CancellationToken.None);
                Assert.IsNull(actualState);
            }
        }

        [Test]
        public void GetStateExtensionThrowsWhenReceivingAnUnexpectedAPIResponse()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.Ambiguous))
            {
                this.fixture.ApiClient.OnGetState().ReturnsAsync(response);

                ApiException error = Assert.ThrowsAsync<ApiException>(
                    () => this.fixture.ApiClient.Object.GetStateAsync<State>("AnyId", CancellationToken.None));

                Assert.AreEqual(ErrorReason.HttpNonSuccessResponse, error.Reason);
            }
        }

        [Test]
        public async Task GetOrCreateStateExtensionReturnsTheExpectedStateWhenItExists()
        {
            Item<TestState> existingState = new Item<TestState>(Guid.NewGuid().ToString(), new TestState());

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, existingState))
            {
                this.fixture.ApiClient.OnGetState(existingState.Id).ReturnsAsync(response);

                Item<TestState> actualState = await this.fixture.ApiClient.Object.GetOrCreateStateAsync<TestState>(existingState.Id, CancellationToken.None);
                Assert.AreEqual(existingState.Id, actualState.Id);
                Assert.IsNotNull(existingState.Definition);
                Assert.IsNotNull(existingState.Definition.Properties);
                Assert.AreEqual(existingState.Definition.Value, actualState.Definition.Value);
            }
        }

        [Test]
        public async Task GetOrCreateStateExtensionReturnsTheExpectedStateWhenItExists_With_API_JObject_Responses()
        {
            Item<JObject> existingState = new Item<JObject>(Guid.NewGuid().ToString(), JObject.FromObject(new TestState()));

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, existingState))
            {
                this.fixture.ApiClient.OnGetState(existingState.Id).ReturnsAsync(response);

                Item<TestState> actualState = await this.fixture.ApiClient.Object.GetOrCreateStateAsync<TestState>(existingState.Id, CancellationToken.None);
                Assert.AreEqual(existingState.Id, actualState.Id);
                Assert.IsNotNull(existingState.Definition);
            }
        }

        [Test]
        public async Task GetOrCreateStateExtensionCreatesADefaultStateWhenOneDoesNotExist()
        {
            Item<TestState> defaultState = new Item<TestState>(Guid.NewGuid().ToString(), new TestState());

            using (HttpResponseMessage getResponse = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                using (HttpResponseMessage postResponse = this.fixture.CreateHttpResponse(HttpStatusCode.Created, defaultState))
                {
                    this.fixture.ApiClient.OnGetState(defaultState.Id).ReturnsAsync(getResponse);
                    this.fixture.ApiClient.OnCreateState<TestState>(defaultState.Id).ReturnsAsync(postResponse);

                    Item<TestState> actualState = await this.fixture.ApiClient.Object.GetOrCreateStateAsync<TestState>(defaultState.Id, CancellationToken.None);
                    Assert.AreEqual(defaultState.Id, actualState.Id);
                    Assert.IsNotNull(defaultState.Definition);
                    Assert.IsNotNull(defaultState.Definition.Properties);
                    Assert.AreEqual(defaultState.Definition.Value, actualState.Definition.Value);
                }
            }
        }

        [Test]
        public void GetOrCreateStateExtensionThrowsWhenReceivingAnUnexpectedAPIResponse()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.Ambiguous))
            {
                this.fixture.ApiClient.OnGetState().ReturnsAsync(response);

                ApiException error = Assert.ThrowsAsync<ApiException>(
                    () => this.fixture.ApiClient.Object.GetOrCreateStateAsync<State>("AnyId", CancellationToken.None));

                Assert.AreEqual(ErrorReason.HttpNonSuccessResponse, error.Reason);
            }
        }

        [Test]
        public async Task PollForExpectedStateExtensionRequestsTheCorrectStateObject()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";
            object expectedState = new
            {
                property1 = "Value",
                property2 = 1234
            };

            Item<object> expectedStateItem = new Item<object>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem))
            {
                string actualStateId = null;

                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<string, CancellationToken, IAsyncPolicy<HttpResponseMessage>> ((stateId, token, retryPolicy) =>
                    {
                        actualStateId = stateId;
                        expectedCallMade = true;
                    })
                    .ReturnsAsync(response);

                await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(expectedStateId, actualStateId);
                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public async Task PollForExpectedStateExtensionRequestsTheCorrectStateObject_Overload2()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            State expectedState = new State();
            expectedState["Identifier"] = "123456";

            Item<State> expectedStateItem = new Item<State>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateItem))
            {
                string actualStateId = null;

                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<string, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((stateId, token, retryPolicy) =>
                    {
                        actualStateId = stateId;
                        expectedCallMade = true;
                    })
                    .ReturnsAsync(response);

                await this.fixture.ApiClient.Object.PollForExpectedStateAsync<State>(
                    expectedStateId,
                    (state) => state["Identifier"].ToString() == "123456",
                    TimeSpan.Zero,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(expectedStateId, actualStateId);
                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public void PollForExpectedStateExtensionCorrectlyIdentifiesWhenTheTargetStateMatchesTheExpectedState()
        {
            string expectedStateId = "State1234";
            object expectedState = new
            {
                property1 = "Value",
                property2 = 1234
            };

            Item<object> expectedStateInstance = new Item<object>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                Assert.DoesNotThrowAsync(() => this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None));
            }
        }

        [Test]
        public void PollForExpectedStateExtensionThrowsWhenAnAttemptToPollForAMatchingStateTimesOut()
        {
            string expectedStateId = "State1234";
            object expectedState = new
            {
                property1 = "Value",
                property2 = 1234
            };

            // The actual state does NOT match the expected state.
            object someOtherState = new
            {
                property1 = "Value",
                property2 = 12345
            };

            Item<object> responseStateItem = new Item<object>(expectedStateId, someOtherState);

            // The target state exists, but it does not match the desired/expected state.
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, responseStateItem))
            {
                this.fixture.ApiClient
                   .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                   .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForExpectedStateAsync(expectedStateId, JObject.FromObject(expectedState), TimeSpan.Zero, DefaultStateComparer.Instance, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PollForExpectedStateExtensionThrowsWhenAnAttemptToPollForAMatchingStateTimesOut_Overload2()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync("AnyStateId", It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => this.fixture.ApiClient.Object.PollForExpectedStateAsync<State>(
                    "AnyStateId",
                    (state) => false,
                    TimeSpan.Zero,
                    CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PollForExpectedStateExtensionThrowsWhenAnUnexpectedNonHttpRequestErrorHappens_Overload2()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync("AnyStateId", It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Throws(new ArgumentException("Serialization error."));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => this.fixture.ApiClient.Object.PollForExpectedStateAsync<State>(
                    "AnyStateId",
                    (state) => false,
                    TimeSpan.Zero,
                    CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiRequestFailed, error.Reason);
                Assert.IsNotNull(error.InnerException);
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);
                Assert.AreEqual($"Serialization error.", error.InnerException.Message);
            }
        }

        [Test]
        public async Task PollForHeartbeatExtensionMakesTheExpectedRequest()
        {
            bool expectedCallMade = false;

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<CancellationToken, IAsyncPolicy<HttpResponseMessage>>((token, retryPolicy) => expectedCallMade = true)
                    .ReturnsAsync(response);

                await this.fixture.ApiClient.Object.PollForHeartbeatAsync(TimeSpan.Zero, CancellationToken.None);

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public void PollForHeartbeatExtensionThrowsWhenAnAttemptToPollForHeartbeatTimesOut()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForHeartbeatAsync(TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PollForHeartbeatExtensionThrowsWhenAnUnexpectedNonHttpRequestErrorHappens()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Throws(new ArgumentException("Unexpected issue."));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForHeartbeatAsync(TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiRequestFailed, error.Reason);
                Assert.IsNotNull(error.InnerException);
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);
                Assert.AreEqual("Unexpected issue.", error.InnerException.Message);
            }
        }

        [Test]
        public async Task PollForServerOnlineExtensionMakesTheExpectedRequest()
        {
            bool expectedCallMade = false;

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<CancellationToken, IAsyncPolicy<HttpResponseMessage>>((token, retryPolicy) => expectedCallMade = true)
                    .ReturnsAsync(response);

                await this.fixture.ApiClient.Object.PollForServerOnlineAsync(TimeSpan.Zero, CancellationToken.None);

                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public void PollForServerOnlineExtensionThrowsWhenAnAttemptToPollForHeartbeatTimesOut()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForServerOnlineAsync(TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PollForServerOnlineExtensionThrowsWhenAnUnexpectedNonHttpRequestErrorHappens()
        {
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Throws(new ArgumentException("Unexpected issue."));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForServerOnlineAsync(TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiRequestFailed, error.Reason);
                Assert.IsNotNull(error.InnerException);
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);
                Assert.AreEqual("Unexpected issue.", error.InnerException.Message);
            }
        }

        [Test]
        public async Task PollForStateDeletedExtensionMakesTheExpectedRequest()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            using (HttpResponseMessage response1 = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                using (HttpResponseMessage response2 = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
                {
                    this.fixture.ApiClient
                        .SetupSequence(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                        .ReturnsAsync(response1)
                        .ReturnsAsync(() => { expectedCallMade = true; return response2; });

                    await this.fixture.ApiClient.Object.PollForStateDeletedAsync(expectedStateId, TimeSpan.FromSeconds(60), CancellationToken.None, pollingInterval: TimeSpan.Zero);

                    Assert.IsTrue(expectedCallMade);
                }
            }
        }

        [Test]
        public void PollForStateDeletedExtensionThrowsWhenAnAttemptToPollForHeartbeatTimesOut()
        {
            string expectedStateId = "State1234";

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForStateDeletedAsync(expectedStateId, TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PollForStateDeletedExtensionThrowsWhenAnUnexpectedNonHttpRequestErrorHappens()
        {
            string expectedStateId = "State1234";

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(expectedStateId, It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Throws(new ArgumentException("Unexpected issue."));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForStateDeletedAsync(expectedStateId, TimeSpan.Zero, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiRequestFailed, error.Reason);
                Assert.IsNotNull(error.InnerException);
                Assert.IsInstanceOf<ArgumentException>(error.InnerException);
                Assert.AreEqual("Unexpected issue.", error.InnerException.Message);
            }
        }

        [Test]
        public async Task VerifyStateExistsExtensionReturnTrueIfStateExists()
        {
            string expectedStateId = "State1234";

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                bool stateExists = await this.fixture.ApiClient.Object.VerifyStateExistsAsync(
                    expectedStateId,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.IsTrue(stateExists);
            }
        }

        [Test]
        public async Task VerifyStateExistsExtensionReturnsFalseIfStateDoesNotExist()
        {
            string stateId = "State1234";


            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                bool stateExists = await this.fixture.ApiClient.Object.VerifyStateExistsAsync(
                    stateId,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.IsFalse(stateExists);
            }
        }

        [Test]

        public async Task GetStateExtensionReturnsExpectedContent()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> properties = new Dictionary<string, IConvertible>();
            properties.Add("Property1", "value1");
            properties.Add("Property2", "value2");

            var parameters = new ClientServerState(ClientServerStatus.Ready, properties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, parameters);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                var stateItem = await this.fixture.ApiClient.Object.GetStateAsync<ClientServerState>(expectedStateId, CancellationToken.None)
                    .ConfigureAwait(false);

                Assert.IsNotNull(stateItem);
                Assert.IsNotNull(stateItem.Definition);
                CollectionAssert.AreEqual(
                    properties.Select(p => $"{p.Key}={p.Value}".ToLowerInvariant()),
                    stateItem.Definition.Properties.Select(p => $"{p.Key}={p.Value}".ToLowerInvariant()));
            }
        }

        [Test]
        public async Task GetStateExtensionReturnsTheExpectedResponseWhenTheStateIsNotFound()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> properties = new Dictionary<string, IConvertible>();
            properties.Add("Property1", "value1");
            properties.Add("Property2", "value2");

            var parameters = new ClientServerState(ClientServerStatus.Ready, properties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, parameters);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                Item<ClientServerState> state = await this.fixture.ApiClient.Object.GetStateAsync<ClientServerState>(expectedStateId, CancellationToken.None);
                Assert.IsNull(state);
            }
        }

        [Test]
        public void GetStateExtensionThrowsIfTheApiReturnsAnUnexpectedNonSuccessResponse()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> properties = new Dictionary<string, IConvertible>();
            properties.Add("Property1", "value1");
            properties.Add("Property2", "value2");

            var parameters = new ClientServerState(ClientServerStatus.Ready, properties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, parameters);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.BadRequest, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                ApiException error = Assert.ThrowsAsync<ApiException>(
                    () => this.fixture.ApiClient.Object.GetStateAsync<ClientServerState>(expectedStateId, CancellationToken.None));

                Assert.AreEqual(ErrorReason.HttpNonSuccessResponse, error.Reason);
            }
        }

        [Test]
        public async Task GetOrCreateStateExtensionGetsStateIfPresent()
        {
            string expectedStateValue = "State1234";

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(expectedState)] = expectedStateValue
            });

            Item<Object> expectedStateInstance = new Item<Object>(nameof(expectedState), expectedStateValue);
            int executed = 0;

            using (HttpResponseMessage getResponse = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(nameof(expectedState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(getResponse);

                using (HttpResponseMessage updateResponse = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
                {
                    this.fixture.ApiClient
                        .Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                        .Callback<string, JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((str, obj, can, pol) =>
                        {
                            State createdState = new State(new Dictionary<string, IConvertible>
                            {
                                [nameof(expectedState)] = expectedStateValue
                            });

                            executed++;

                            State state = obj.ToString().FromJson<Item<State>>().Definition;
                            Assert.IsTrue(JObject.FromObject(state).ToString() == JObject.FromObject(expectedState).ToString());
                        })
                        .ReturnsAsync(updateResponse);

                    HttpResponseMessage response = await this.fixture.ApiClient.Object.GetOrCreateStateAsync(nameof(expectedState), JObject.FromObject(expectedState), CancellationToken.None);

                    Assert.AreEqual(executed, 0);
                    Assert.IsTrue(getResponse.IsSuccessStatusCode);
                }

            }

        }

        [Test]
        public async Task GetOrCreateStateExtensionCreatesStateIfNotPresent()
        {
            string expectedStateValue = "State1234";

            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(expectedState)] = expectedStateValue
            });

            Item<Object> expectedStateInstance = new Item<Object>(nameof(expectedState), expectedStateValue);
            int executed = 0;

            using (HttpResponseMessage getResponse = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(nameof(expectedState), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(getResponse);

                using (HttpResponseMessage updateResponse = this.fixture.CreateHttpResponse(HttpStatusCode.OK))
                {
                    this.fixture.ApiClient
                        .Setup(client => client.CreateStateAsync(It.IsAny<string>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                        .Callback<string, JObject, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((str, obj, can, pol) =>
                        {
                            State createdState = new State(new Dictionary<string, IConvertible>
                            {
                                [nameof(expectedState)] = expectedStateValue
                            });

                            executed++;

                            Assert.IsTrue(obj.ToString() == JObject.FromObject(createdState).ToString());
                        })
                        .ReturnsAsync(updateResponse);

                    HttpResponseMessage response = await this.fixture.ApiClient.Object.GetOrCreateStateAsync(nameof(expectedState), JObject.FromObject(expectedState), CancellationToken.None);

                    Assert.AreEqual(executed, 1);
                    Assert.IsTrue(updateResponse.IsSuccessStatusCode);
                }

            }

        }

        [Test]
        public void SendInstructionsExtensionThrowsIfStateNotFound()
        {
            Item<ClientServerRequest> instructions = new Item<ClientServerRequest>(
                Guid.NewGuid().ToString(),
                new ClientServerRequest(InstructionsType.Profiling, new Dictionary<string, IConvertible>
                {
                    ["property1"] = "value1",
                    ["property2"] = 12345,
                    ["property3"] = true
                }));

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.NotFound))
            {
                this.fixture.ApiClient
                    .Setup(client => client.SendInstructionsAsync(It.IsAny<JObject>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                ApiException error = Assert.ThrowsAsync<ApiException>(
                    () => this.fixture.ApiClient.Object.SendInstructionsAsync(instructions, CancellationToken.None));

                Assert.AreEqual(ErrorReason.HttpNonSuccessResponse, error.Reason);
            }
        }

        [Test]
        public async Task PollForExpectedStateAsyncExtensionRequestsTheCorrectStateObject()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> expectedProperties = new Dictionary<string, IConvertible>();
            expectedProperties.Add("property1", "value1");
            expectedProperties.Add("property2", "value2");

            var expectedState = new ClientServerState(ClientServerStatus.Ready, expectedProperties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                string actualStateId = null;

                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<string, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((stateId, token, retryPolicy) =>
                    {
                        actualStateId = stateId;
                        expectedCallMade = true;
                    })
                    .ReturnsAsync(response);

                await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(expectedStateId, actualStateId);
                Assert.IsTrue(expectedCallMade);
            }
        }

        [Test]
        public void PollForExpectedStateAsyncExtensionCorrectlyIdentifiesWhenTheTargetStateMatchesTheExpectedState()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> expectedProperties = new Dictionary<string, IConvertible>();
            expectedProperties.Add("property1", "value1");
            expectedProperties.Add("property2", "value2");

            var expectedState = new ClientServerState(ClientServerStatus.Ready, expectedProperties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                Assert.DoesNotThrowAsync(() => this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None));
            }
        }

        private class TestState : State
        {
            public TestState()
                : base()
            {
                this[nameof(this.Value)] = -1;
            }

            public TestState(IDictionary<string, IConvertible> properties)
                : base(properties)
            {
            }

            public int Value
            {
                get
                {
                    return this.Properties.GetValue<int>(nameof(this.Value), -1);
                }
            }
        }
    }
}
