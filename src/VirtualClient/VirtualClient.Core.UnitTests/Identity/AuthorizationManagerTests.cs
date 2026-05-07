// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class AuthorizationManagerTests
    {
        [Test]
        public async Task AuthorizationManagerAttemptsAnInteractiveWorkflowFirstToGetAnAccessToken()
        {
            int interactiveCalls = 0;
            int deviceCodeCalls = 0;

            var authManager = new TestAuthorizationManager();

            authManager.OnAttemptTokenDeviceCodeFlow = (context) =>
            {
                deviceCodeCalls++;
                return "any-token";
            };

            authManager.OnAttemptTokenInteractiveFlow = (context) =>
            {
                interactiveCalls++;
                return "any-token";
            };

            await authManager.GetAccessTokenAsync(new Uri("https://any.vault.azure.com"), Guid.NewGuid().ToString(), CancellationToken.None);

            Assert.AreEqual(1, interactiveCalls);
            Assert.AreEqual(0, deviceCodeCalls);
        }

        [Test]
        public async Task AuthorizationManagerAttemptsADeviceCodeFlowIfTheInteractiveFlowCannotHappen()
        {
            int deviceCodeCalls = 0;
            var authManager = new TestAuthorizationManager();

            authManager.OnAttemptTokenDeviceCodeFlow = (context) =>
            {
                deviceCodeCalls++;
                return "any-token";
            };

            authManager.OnAttemptTokenInteractiveFlow = (context) => throw new AuthenticationFailedException("Unable to open a web page");

            await authManager.GetAccessTokenAsync(
                new Uri("https://any.vault.azure.net"), 
                Guid.NewGuid().ToString(), 
                CancellationToken.None);

            Assert.AreEqual(1, deviceCodeCalls);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("     ")]
        public void AuthorizationManagerThrowsIfAnInvalidAccessTokenIsAcquired(string invalidToken)
        {
            var authManager = new TestAuthorizationManager();

            authManager.OnAttemptTokenInteractiveFlow = (context) => invalidToken;

            Assert.ThrowsAsync<AuthenticationFailedException>(() => authManager.GetAccessTokenAsync(
                new Uri("https://any.vault.azure.com"),
                Guid.NewGuid().ToString(),
                CancellationToken.None));
        }

        [Test]
        [TestCase("https://any.vault.azure.net")]
        [TestCase("https://any.blob.core.windows.net")]
        public void AuthorizationManagerUsesTheExpectedScopeForKeyVaultAccessTokenRequests(string resourceUri)
        {
            string expectedScope = $"{resourceUri}/.default";
            string expectedTenantId = Guid.NewGuid().ToString();

            var authManager = new TestAuthorizationManager();
            TokenRequestContext requestContext = authManager.CreateRequestContext(new Uri(resourceUri), expectedTenantId);

            Assert.IsNotEmpty(requestContext.Scopes);
            Assert.IsTrue(requestContext.Scopes.Count() == 1);
            Assert.AreEqual(requestContext.Scopes[0], expectedScope);
            Assert.AreEqual(requestContext.TenantId, expectedTenantId);
        }

        private class TestAuthorizationManager : AuthorizationManager
        {
            public Func<TokenRequestContext, string> OnAttemptTokenDeviceCodeFlow { get; set; }

            public Func<TokenRequestContext, string> OnAttemptTokenInteractiveFlow { get; set; }

            public new TokenRequestContext CreateRequestContext(Uri resourceUri, string tenantId)
            {
                return base.CreateRequestContext(resourceUri, tenantId);
            }

            protected override Task<string> AttemptTokenDeviceCodeFlowAsync(TokenRequestContext context, CancellationToken cancellationToken)
            {
                string accessToken = "any-token";
                if (this.OnAttemptTokenDeviceCodeFlow != null)
                {
                    accessToken = this.OnAttemptTokenDeviceCodeFlow.Invoke(context);
                }

                return Task.FromResult(accessToken);
            }

            protected override Task<string> AttemptTokenInteractiveFlowAsync(TokenRequestContext context, CancellationToken cancellationToken)
            {
                string accessToken = "any-token";
                if (this.OnAttemptTokenInteractiveFlow != null)
                {
                    accessToken = this.OnAttemptTokenInteractiveFlow.Invoke(context);
                }

                return Task.FromResult(accessToken);
            }
        }
    }
}
