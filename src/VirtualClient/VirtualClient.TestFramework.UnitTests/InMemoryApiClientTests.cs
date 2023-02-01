// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class InMemoryApiClientTests
    {
        [Test]
        public void InMemoryApiClientTestsResponsesHandleBeingDisposed_CreateStateAsync()
        {
            InMemoryApiClient apiClient = new InMemoryApiClient(IPAddress.Loopback);

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = apiClient.CreateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = apiClient.CreateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }

        [Test]
        public void InMemoryApiClientTestsResponsesHandleBeingDisposed_DeleteStateAsync()
        {
            InMemoryApiClient apiClient = new InMemoryApiClient(IPAddress.Loopback);

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = apiClient.DeleteStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = apiClient.DeleteStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }

        [Test]
        public void InMemoryApiClientTestsResponsesHandleBeingDisposed_GetStateAsync()
        {
            InMemoryApiClient apiClient = new InMemoryApiClient(IPAddress.Loopback);

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = apiClient.GetStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = apiClient.GetStateAsync("AnyState", CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }

        [Test]
        public void InMemoryApiClientTestsResponsesHandleBeingDisposed_UpdateStateAsync()
        {
            InMemoryApiClient apiClient = new InMemoryApiClient(IPAddress.Loopback);

            // The response object will be disposed. The mock framework should not by default be using the same object.
            HttpResponseMessage response1 = apiClient.UpdateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            response1.Dispose();
            HttpResponseMessage response2 = apiClient.UpdateStateAsync("AnyState", JObject.FromObject(new { any = "object" }), CancellationToken.None)
                .GetAwaiter().GetResult();

            // If the HTTP content object is disposed, this will throw an
            string content = null;
            Assert.DoesNotThrow(() => content = response2.Content.ReadAsStringAsync().Result);
            Assert.IsNotNull(content);
        }
    }
}
