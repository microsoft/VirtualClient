// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ApiClientExtensionsTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
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
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<string, CancellationToken, IAsyncPolicy<HttpResponseMessage>> ((stateId, token, retryPolicy) =>
                    {
                        actualStateId = stateId;
                        expectedCallMade = true;
                    })
                    .ReturnsAsync(response);

                HttpResponseMessage actualResponse = await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
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
        public async Task PollForExpectedStateExtensionCorrectlyIdentifiesWhenTheTargetStateMatchesTheExpectedState()
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
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                HttpResponseMessage actualResponse = await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(actualResponse);
                Assert.IsTrue(actualResponse.StatusCode == HttpStatusCode.OK);
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
                   .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                   .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.PollForExpectedStateAsync(expectedStateId, JObject.FromObject(expectedState), TimeSpan.Zero, DefaultStateComparer.Instance, CancellationToken.None));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
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
                Assert.AreEqual(properties, stateItem.Definition.Properties);
            }
        }

        [Test]
        public void GetStateExtensionThrowsIfStateNotFound()
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
        public async Task SynchronizeStateExtensionRequestsTheCorrectStateObject()
        {
            bool expectedCallMade = false;
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> expectedProperties = new Dictionary<string, IConvertible>();
            expectedProperties.Add("Property1", "value1");
            expectedProperties.Add("Property2", "value2");

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

                HttpResponseMessage actualResponse = await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
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
        public async Task SynchronizeStateExtensionCorrectlyIdentifiesWhenTheTargetStateMatchesTheExpectedState()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> expectedProperties = new Dictionary<string, IConvertible>();
            expectedProperties.Add("Property1", "value1");
            expectedProperties.Add("Property2", "value2");

            var expectedState = new ClientServerState(ClientServerStatus.Ready, expectedProperties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, expectedState);

            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, expectedStateInstance))
            {
                this.fixture.ApiClient
                    .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .ReturnsAsync(response);

                HttpResponseMessage actualResponse = await this.fixture.ApiClient.Object.PollForExpectedStateAsync(
                    expectedStateId,
                    JObject.FromObject(expectedState),
                    TimeSpan.Zero,
                    DefaultStateComparer.Instance,
                    CancellationToken.None).ConfigureAwait(false);

                Assert.IsNotNull(actualResponse);
                Assert.IsTrue(actualResponse.StatusCode == HttpStatusCode.OK);
            }
        }

        [Test]
        public void SynchronizeStateExtensionThrowsWhenAnAttemptToPollForAMatchingStateTimesOut()
        {
            string expectedStateId = "State1234";

            IDictionary<string, IConvertible> expectedProperties = new Dictionary<string, IConvertible>();
            expectedProperties.Add("Property1", "value1");
            expectedProperties.Add("Property2", "value2");

            IDictionary<string, IConvertible> someOtherProperties = new Dictionary<string, IConvertible>();
            someOtherProperties.Add("Property1", "value1");
            someOtherProperties.Add("Property2", "value3");

            var expectedState = new ClientServerState(ClientServerStatus.Ready, expectedProperties);
            var someOtherState = new ClientServerState(ClientServerStatus.Ready, someOtherProperties);

            Item<ClientServerState> expectedStateInstance = new Item<ClientServerState>(expectedStateId, expectedState);
            Item<ClientServerState> responseStateItem = new Item<ClientServerState>(expectedStateId, someOtherState);

            // The target state exists, but it does not match the desired/expected state.
            using (HttpResponseMessage response = this.fixture.CreateHttpResponse(HttpStatusCode.OK, responseStateItem))
            {
                this.fixture.ApiClient
                   .Setup(client => client.GetStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                   .ReturnsAsync(response);

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(
                    () => this.fixture.ApiClient.Object.SynchronizeStateAsync(expectedStateInstance, DefaultStateComparer.Instance, CancellationToken.None, TimeSpan.Zero));

                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }
    }

}
