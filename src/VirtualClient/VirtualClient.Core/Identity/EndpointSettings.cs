namespace VirtualClient.Identity
{
    using System;
    using VirtualClient.Common.Extensions;

    internal class EndpointSettings
    {
        private static readonly char[] TrimChars = { '\'', '"', ' ' };

        public EndpointSettings(string endpoint) 
        {
            endpoint.ThrowIfNullOrWhiteSpace(nameof(endpoint));

            endpoint = EndPointSettingsExtensions.ValidateAndFormatPackageUri(endpoint);
            string argumentValue = endpoint.Trim(EndpointSettings.TrimChars);

            // this.IsBlobStoreConnectionString = EndPointSettingsExtensions.IsBlobStoreConnectionString(argumentValue);

            // this.EndPoint = new Uri(endpoint);
            this.IsCustomConnectionString = EndPointSettingsExtensions.IsCustomConnectionString(endpoint);

            this.IsEventHubConnectionString = EndPointSettingsExtensions.IsEventHubConnectionString(endpoint);
            // this.IsKeyVaultConnectionString = EndPointSettingsExtensions.IsKeyVaultConnectionString(endpoint);

        }

        public Uri EndPoint { get; set; }

        public bool IsCustomConnectionString { get; set; }

        public bool IsBlobStoreConnectionString { get; set; }

        public bool IsEventHubConnectionString { get; set; }

        public bool IsKeyVaultConnectionString { get; set; }

        public string TenantId { get; set; }

        public string ClientId { get; set; }

        public string ManagedIdentityId { get; set; }

        public string CertificateThumbprint { get; set; }

        public string CertificateSubjectName { get; set; }

        public string CertificateIssuerName { get; set; }
    }
}
