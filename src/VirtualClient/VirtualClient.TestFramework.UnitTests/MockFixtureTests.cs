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
        [TestCase(null, null)]
        [TestCase("  ", null)]
        [TestCase("C:", null)]
        [TestCase(@"C:\", null)]
        [TestCase(@"C:\path", @"C:\")]
        [TestCase(@"C:\any\path", @"C:\any")]
        [TestCase(@"C:\any/path", @"C:\any")]
        [TestCase(@"C:\any\path\file1.log", @"C:\any\path")]
        [TestCase(@"any\path\file1.log", @"any\path")]
        [TestCase(@"\any\path\file1.log", @"\any\path")]
        [TestCase(@"~\any\path\file1.log", @"~\any\path")]
        public void MockFixtureGetDirectoryNameHandlesWindowsStylePaths(string path, string expectedDirectoryName)
        {
            string actualDirectoryName = MockFixture.GetDirectoryName(path);
            Assert.AreEqual(expectedDirectoryName, actualDirectoryName);
        }

        [Test]
        [TestCase(null, null)]
        [TestCase("  ", null)]
        [TestCase("/", null)]
        [TestCase("/home", null)]
        [TestCase("/home/path", "/home")]
        [TestCase("/home/path/", "/home")]
        [TestCase("/home/path/file1.log", "/home/path")]
        [TestCase("~/path/file1.log", "~/path")]
        [TestCase(@"/home\path\file1.log", @"/home\path")]
        public void MockFixtureGetDirectoryNameHandlesUnixStylePaths(string path, string expectedDirectoryName)
        {
            string actualDirectoryName = MockFixture.GetDirectoryName(path);
            Assert.AreEqual(expectedDirectoryName, actualDirectoryName);
        }

        [Test]
        public void MockFixtureApiClientResponsesHandleBeingDisposed_CreateStateAsync()
        {
            MockFixture fixture = new MockFixture();
            fixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = fixture.ApiClient.Object;

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
            MockFixture fixture = new MockFixture();
            fixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = fixture.ApiClient.Object;

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
            MockFixture fixture = new MockFixture();
            fixture.Setup(System.PlatformID.Win32NT);
            IApiClient mockClient = fixture.ApiClient.Object;

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
