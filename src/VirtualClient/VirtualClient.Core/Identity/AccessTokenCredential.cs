namespace VirtualClient.Identity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A <see cref="TokenCredential"/> implementation that uses a pre-acquired 
    /// access token.
    /// </summary>
    public class AccessTokenCredential : TokenCredential
    {
        /// <summary>
        /// Creates a new instance of the <see cref="AccessTokenCredential"/> class.
        /// </summary>
        /// <param name="token">
        /// The credential provider that will be used to get access tokens.
        /// </param>
        public AccessTokenCredential(string token)
        {
            token.ThrowIfNull(nameof(token));
            this.AccessToken = new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
        }

        /// <summary>
        /// The access token to use for authentication.
        /// </summary>
        public AccessToken AccessToken { get; }

        /// <summary>
        /// Gets an access token using the underlying credentials.
        /// </summary>
        /// <param name="requestContext">Context information used when getting the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An access token that can be used to authenticate with Azure resources.
        /// </returns>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return this.AccessToken;
        }

        /// <summary>
        /// Gets an access token using the underlying credentials.
        /// </summary>
        /// <param name="requestContext">Context information used when getting the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An access token that can be used to authenticate with Azure resources.
        /// </returns>
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(this.AccessToken);
        }
    }
}
