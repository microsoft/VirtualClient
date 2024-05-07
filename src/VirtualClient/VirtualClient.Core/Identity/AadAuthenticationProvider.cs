namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Identity.Client;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Provides methods for authenticating with Azure Active Directory (AAD) applications
    /// that use access tokens. Use the following documentation as reference:
    /// <list type="bullet">
    /// <item>
    /// <a href='https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.4.1/sdk/identity/Azure.Identity/README.md'>Azure.Identity README</a>
    /// </item>
    /// <item>
    /// <a href='https://docs.microsoft.com/en-us/dotnet/api/azure.identity?view=azure-dotnet'>Azure.Identity Namespace</a>
    /// </item>
    /// <item>
    /// <a href='https://www.nuget.org/packages/Azure.Identity/'>Azure.Identity NuGet Package(s)</a>
    /// </item>
    /// <item>
    /// <a href='https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet'>Using Managed Identity</a>
    /// </item>
    /// </list>
    /// </summary>
    public class AadAuthenticationProvider : IHttpAuthentication, IAuthenticationProvider<AuthenticationResult>, IDisposable
    {
        private static TimeSpan tokenExpirationRefreshBuffer = TimeSpan.FromMinutes(10);
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        private string tenantId;
        private string clientId;
        private string managedIdentityClientId;
        private string resourceId;
        private string certificateThumbprint;
        private string certificateIssuer;
        private string certificateSubject;
        private bool disposed = false;

        private AuthenticationResult latestAuthResult;
        private ICertificateManager certManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="AadAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="clientId">The client Id</param>
        /// <param name="resourceId">The resource Id.</param>
        /// <param name="certificateThumbprint">The certificate thumbprint</param>
        /// <param name="certManager">Certificate manager.</param>
        /// <param name="logger">Logger used to emit messages from the system.</param>
        public AadAuthenticationProvider(string tenantId, string clientId, string resourceId, string certificateThumbprint, ICertificateManager certManager = null, ILogger logger = null)
        {
            tenantId.ThrowIfNull(nameof(tenantId));
            clientId.ThrowIfNull(nameof(clientId));
            resourceId.ThrowIfNull(nameof(resourceId));
            certificateThumbprint.ThrowIfNull(nameof(certificateThumbprint));

            this.tenantId = tenantId;
            this.clientId = clientId;
            this.resourceId = resourceId;
            this.certificateThumbprint = certificateThumbprint;
            this.certManager = certManager ?? new CertificateManager();
            this.Scopes = new List<string> { $"{this.resourceId}/.default" };
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AadAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="managedIdentityClientId">Id of managed identity.</param>
        /// <param name="resourceId">The resource Id.</param>
        /// <param name="logger">Logger used to emit messages from the system.</param>
        public AadAuthenticationProvider(string tenantId, string managedIdentityClientId, string resourceId, ILogger logger = null)
        {
            tenantId.ThrowIfNull(nameof(tenantId));
            managedIdentityClientId.ThrowIfNull(nameof(managedIdentityClientId));
            resourceId.ThrowIfNull(nameof(resourceId));

            this.resourceId = resourceId;
            this.tenantId = tenantId;
            this.managedIdentityClientId = managedIdentityClientId;
            this.Scopes = new List<string> { $"{this.resourceId}/.default" };
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AadAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="tenantId">Tenant id.</param>
        /// <param name="clientId">The client Id</param>
        /// <param name="resourceId">The resource Id.</param>
        /// <param name="issuerName">The certificate issuer</param>
        /// <param name="subjectName">The certificate subject name.</param>
        /// <param name="certManager">Certificate manager.</param>
        /// <param name="logger">Logger used to emit messages from the system.</param>
        public AadAuthenticationProvider(string tenantId, string clientId, string resourceId, string issuerName, string subjectName, ICertificateManager certManager = null, ILogger logger = null)
        {
            tenantId.ThrowIfNull(nameof(tenantId));
            issuerName.ThrowIfNull(nameof(issuerName));
            subjectName.ThrowIfNull(nameof(subjectName));

            this.tenantId = tenantId;
            this.clientId = clientId;
            this.resourceId = resourceId;
            this.certificateIssuer = issuerName;
            this.certificateSubject = subjectName;
            this.certManager = certManager ?? new CertificateManager();
            this.Scopes = new List<string> { $"{this.resourceId}/.default" };
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// The scopes to use when requesting access tokens.
        /// </summary>
        public List<string> Scopes { get; }

        /// <summary>
        /// The logger used to emit telemetry from the system.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Token credential used in AAD authentication.
        /// </summary>
        protected TokenCredential TokenCredential { get; set; }

        /// <inheritdoc/>
        public async Task AuthenticateAsync(HttpClient httpClient, CancellationToken cancellationToken, bool autoRefresh = true)
        {
            httpClient.ThrowIfNull(nameof(httpClient));

            EventContext telemetryContext = EventContext.Persist(Guid.NewGuid());

            await this.Logger.LogTelemetryAsync($"{nameof(AadAuthenticationProvider)}.Authenticate", telemetryContext, async () =>
            {
                // The Authentication provider follows these steps:
                // 1. Check if the current thread has an invalid token.
                //    If valid, set header and exit, if not perform the below.
                // 2. Waits to enter the critical code section.
                // 3. Checks if the current thread has an invalid token. The purpose of the second
                //    check is to validate that another thread did not previsouly update the authentication result
                //    while the currently blocked thread was waiting enterance to the semaphore.
                // 4. Exit semaphore, update authentication result, and exit.
                bool shouldRefresh = this.ShouldRefresh(autoRefresh);

                telemetryContext.AddContext(nameof(shouldRefresh), shouldRefresh);
                telemetryContext.AddContext("initialAuth", this.latestAuthResult == null);
                if (shouldRefresh)
                {
                    try
                    {
                        await this.semaphore.WaitAsync().ConfigureAwait(false);
                        if (this.ShouldRefresh(autoRefresh))
                        {
                            telemetryContext.AddContext("invalidAuthenticationResult", this.AuthenticationResultToTelemetry());
                            this.latestAuthResult = await this.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                        }
                    }
                    finally
                    {
                        this.semaphore.Release();
                    }
                }

                telemetryContext.AddContext("authenticationResult", this.AuthenticationResultToTelemetry());
            }).ConfigureAwait(false);

            if (this.latestAuthResult == null)
            {
                throw new AuthenticationException("Invalid authentication result.");
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.latestAuthResult.AccessToken);
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AuthenticateAsync(CancellationToken cancellationToken)
        {
            if (this.TokenCredential == null)
            {
                this.TokenCredential = await TokenCredentialExtensions.GetTokenCredentialAsync(
                    this.tenantId, 
                    this.clientId, 
                    this.managedIdentityClientId, 
                    this.certificateThumbprint, 
                    this.certificateIssuer, 
                    this.certificateSubject, 
                    this.certManager).ConfigureAwait(false);
            }

            AuthenticationResult authenticationResult;

            try
            {
                AccessToken token = await this.TokenCredential.GetTokenAsync(new TokenRequestContext(this.Scopes.ToArray()), cancellationToken).ConfigureAwait(false);

                authenticationResult = new AuthenticationResult(
                    accessToken: token.Token,
                    isExtendedLifeTimeToken: false,
                    uniqueId: null,
                    expiresOn: token.ExpiresOn,
                    extendedExpiresOn: token.ExpiresOn,
                    tenantId: this.tenantId,
                    account: null,
                    idToken: null,
                    scopes: this.Scopes,
                    correlationId: Guid.NewGuid());
            }
            catch (Exception exc)
            {
                string errorMessage = string.Empty;
                if (!string.IsNullOrWhiteSpace(this.managedIdentityClientId))
                {
                    errorMessage = $"Failed to get token for managedIdentity clientId: '{this.managedIdentityClientId}'.";
                }
                else if (!string.IsNullOrEmpty(this.certificateIssuer) && !string.IsNullOrEmpty(this.certificateSubject))
                {
                    errorMessage = $"Failed to get token for clientId: '{this.clientId}'" +
                        $"on resourceId:'{this.resourceId}' " +
                        $"with certificate issuer:'{this.certificateIssuer}' and certificate subject: '{this.certificateSubject}'" +
                        $" on authority:'{this.tenantId}'";
                }
                else
                {
                    errorMessage = $"Failed to get token for clientId: '{this.clientId}'" +
                        $"on resourceId:'{this.resourceId}' " +
                        $"with certificateThumbprint:'{this.certificateThumbprint}' on authority:'{this.tenantId}'";
                }

                throw new AuthenticationException(errorMessage, exc);
            }

            return authenticationResult;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this.latestAuthResult = null;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all unmanaged resources.
        /// </summary>
        /// <param name="disposing">If this object is currently being disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed && disposing)
            {
                this.semaphore.Dispose();
            }

            this.disposed = true;
        }

        private bool ShouldRefresh(bool autoRefresh)
        {
            return this.latestAuthResult == null ||
                        (autoRefresh && this.latestAuthResult.ExpiresOn < DateTime.UtcNow + AadAuthenticationProvider.tokenExpirationRefreshBuffer);
        }

        private object AuthenticationResultToTelemetry()
        {
            if (this.latestAuthResult == null)
            {
                return null;
            }

            return new
            {
                CorrelationId = this.latestAuthResult.CorrelationId,
                Scopes = this.latestAuthResult.Scopes,
                Username = this.latestAuthResult.Account?.Username,
                ExpiresOn = this.latestAuthResult.ExpiresOn,
                TenantId = this.latestAuthResult.TenantId,
                Claims = this.latestAuthResult.ClaimsPrincipal?.Claims.Select(c => new { c.Type, c.Value })
            };
        }
    }
}