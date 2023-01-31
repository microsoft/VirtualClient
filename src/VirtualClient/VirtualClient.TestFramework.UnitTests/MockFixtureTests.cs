// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Net.Http;
    using System.Threading;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class MockFixtureTests
    {
        [Test]
        public void MockFixtureApiClientResponsesHandleBeingDisposed_CreateStateAsync()
        {
            MockFixture mockFixture = new MockFixture();
            mockFixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = mockFixture.ApiClient.Object;

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = mockClient.CreateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = mockClient.CreateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }

        [Test]
        public void MockFixtureApiClientResponsesHandleBeingDisposed_GetStateAsync()
        {
            MockFixture mockFixture = new MockFixture();
            mockFixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = mockFixture.ApiClient.Object;

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = mockClient.GetStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = mockClient.GetStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }

        [Test]
        public void MockFixtureApiClientResponsesHandleBeingDisposed_UpdateStateAsync()
        {
            MockFixture mockFixture = new MockFixture();
            mockFixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = mockFixture.ApiClient.Object;

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = mockClient.UpdateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = mockClient.UpdateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }
    }
}
