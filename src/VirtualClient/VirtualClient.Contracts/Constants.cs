// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Constants defining different types roles that can be present in a layout.
    /// </summary>
    public static class ClientRole
    {
        /// <summary>
        /// Client role.
        /// </summary>
        public const string Client = "Client";

        /// <summary>
        /// Reverse Proxy role.
        /// </summary>
        public const string ReverseProxy = "ReverseProxy";

        /// <summary>
        /// Server role.
        /// </summary>
        public const string Server = "Server";
    }

    /// <summary>
    /// Constants used to describe parameters used in resource connection definitions
    /// (e.g. storage accounts, Event Hub namespaces).
    /// </summary>
    public static class ConnectionParameter
    {
        /// <summary>
        /// Parameter = BlobEndpoint. A storage account blob endpoint/URL (e.g. https://anystorage.blob.core.windows.net).
        /// </summary>
        public const string BlobEndpoint = nameof(BlobEndpoint);

        /// <summary>
        /// Parameter = ClientId. A Microsoft Entra ID/App client ID.
        /// </summary>
        public const string ClientId = nameof(ClientId);

        /// <summary>
        /// Parameter = CertificateIssuer. The issuer for a certificate (e.g. CN=ABC Infra AC, DC=ABC, DC=COM).
        /// </summary>
        public const string CertificateIssuer = nameof(CertificateIssuer);

        /// <summary>
        /// Parameter = CertificateSubject. The subject name for a certificate (e.g. CN=anyservice.abc.com).
        /// </summary>
        public const string CertificateSubject = nameof(CertificateSubject);

        /// <summary>
        /// Parameter = CertificateThumbprint. The SHA1 thumbprint for a certificate.
        /// </summary>
        public const string CertificateThumbprint = nameof(CertificateThumbprint);

        /// <summary>
        /// Parameter = DefaultEndpointsProtocol. The default communication protocol for a storage account (e.g. https).
        /// </summary>
        public const string DefaultEndpointsProtocol = nameof(DefaultEndpointsProtocol);

        /// <summary>
        /// Parameter = Directory. The directory/path.
        /// </summary>
        public const string Directory = nameof(Directory);

        /// <summary>
        /// Parameter = Endpoint. The target endpoint/URI (e.g. https://anystorage.blob.core.windows.net).
        /// </summary>
        public const string Endpoint = nameof(Endpoint);

        /// <summary>
        /// Parameter = EndpointUrl. The target endpoint URL (e.g. https://anystorage.blob.core.windows.net).
        /// </summary>
        public const string EndpointUrl = nameof(EndpointUrl);

        /// <summary>
        /// Parameter = EventHubNamespace. The target Event Hub Namespace or URI (e.g. any.servicebus.windows.net, sb://any.servicebus.windows.net).
        /// </summary>
        public const string EventHubNamespace = nameof(EventHubNamespace);

        /// <summary>
        /// Parameter = ManagedIdentityId. The client ID for an Azure Managed Identity.
        /// </summary>
        public const string ManagedIdentityId = nameof(ManagedIdentityId);

        /// <summary>
        /// Parameter = SharedAccessKeyName. A name for a storage account shared access key.
        /// </summary>
        public const string SharedAccessKeyName = nameof(SharedAccessKeyName);

        /// <summary>
        /// Parameter = TenantId. The Microsoft Entra tenant/directory ID.
        /// </summary>
        public const string TenantId = nameof(TenantId);
    }

    /// <summary>
    /// Constants that define the type of disks on a system.
    /// </summary>
    public static class DiskType
    {
        /// <summary>
        /// The disk is a system/OS disk.
        /// </summary>
        public const string OSDisk = "os_disk";

        /// <summary>
        /// The disk is a system/OS disk.
        /// </summary>
        public const string SystemDisk = "system_disk";

        /// <summary>
        /// The disk is a remote disk.
        /// </summary>
        public const string RemoteDisk = "remote_disk";

        /// <summary>
        /// The default disk type.
        /// </summary>
        public const string DefaultDisk = "disk";
    }

    /// <summary>
    /// Common environment variable names.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Represents common environment variable naming conventsions.")]
    public static class EnvironmentVariable
    {
        /// <summary>
        /// Name = JAVA_HOME
        /// </summary>
        public const string JAVA_HOME = nameof(JAVA_HOME);

        /// <summary>
        /// Name = JAVA_EXE
        /// </summary>
        public const string JAVA_EXE = nameof(JAVA_EXE);

        /// <summary>
        /// Name = LD_LIBRARY_PATH
        /// </summary>
        public const string LD_LIBRARY_PATH = nameof(LD_LIBRARY_PATH);

        /// <summary>
        /// Name = PATH
        /// </summary>
        public const string PATH = nameof(PATH);

        /// <summary>
        /// Name = SDK_EVENTHUB_CONNECTION
        /// </summary>
        public const string SDK_EVENTHUB_CONNECTION = nameof(SDK_EVENTHUB_CONNECTION);

        /// <summary>
        /// Name = SDK_PACKAGES_CONNECTION
        /// </summary>
        public const string SDK_PACKAGES_CONNECTION = nameof(SDK_PACKAGES_CONNECTION);

        /// <summary>
        /// Name = SDK_PACKAGES_DIR
        /// </summary>
        public const string SDK_PACKAGES_DIR = nameof(SDK_PACKAGES_DIR);

        /// <summary>
        /// Name = SUDO_USER
        /// </summary>
        public const string SUDO_USER = nameof(SUDO_USER);

        /// <summary>
        /// Name = USER
        /// </summary>
        public const string USER = nameof(USER);

        /// <summary>
        /// Name = VC_PASSWORD
        /// </summary>
        public const string VC_PASSWORD = nameof(VC_PASSWORD);

        /// <summary>
        /// Name = VC_LIBRARY_PATH
        /// </summary>
        public const string VC_LIBRARY_PATH = nameof(VC_LIBRARY_PATH);

        /// <summary>
        /// Name = VC_PACKAGES_DIR
        /// </summary>
        public const string VC_PACKAGES_DIR = nameof(VC_PACKAGES_DIR);

        /// <summary>
        /// Name = VC_SUDO_USER
        /// </summary>
        public const string VC_SUDO_USER = nameof(VC_SUDO_USER);
    }

    /// <summary>
    /// Global or well-known parameters available for use on the Virtual Client command line.
    /// </summary>
    public class GlobalParameter
    {
        /// <summary>
        /// ContentStoreSource
        /// </summary>
        public const string ContestStoreSource = "ContentStoreSource";

        /// <summary>
        /// PackageStoreSource
        /// </summary>
        public const string PackageStoreSource = "PackageStoreSource";

        /// <summary>
        /// TelemetrySource
        /// </summary>
        public const string TelemetrySource = "TelemetrySource";
    }

    /// <summary>
    /// Common HTTP content types.
    /// </summary>
    public static class HttpContentType
    {
        /// <summary>
        /// text/plain
        /// </summary>
        public const string PlainText = "text/plain";

        /// <summary>
        /// application/octet-stream
        /// </summary>
        public const string Binary = "application/octet-stream";

        /// <summary>
        /// application/json
        /// </summary>
        public const string Json = "application/json";
    }

    /// <summary>
    /// Common profile metadata property names.
    /// </summary>
    public class ProfileMetadata
    {
        /// <summary>
        /// Metadata = SupportsIterations
        /// </summary>
        public const string SupportsIterations = nameof(SupportsIterations);
    }

    /// <summary>
    /// Constants used to describe parameters used in resource connection definitions
    /// (e.g. storage accounts, Event Hub namespaces).
    /// </summary>
    public static class UriParameter
    {
        /// <summary>
        /// URI Parameter = crtt. The issuer for a certificate (e.g. CN=ABC Infra AC, DC=ABC, DC=COM).
        /// </summary>
        public const string CertificateIssuer = "crti";

        /// <summary>
        /// URI Parameter = crts. The subject name for a certificate (e.g. CN=anyservice.abc.com).
        /// </summary>
        public const string CertificateSubject = "crts";

        /// <summary>
        /// URI Parameter = crtt. The SHA1 thumbprint for a certificate.
        /// </summary>
        public const string CertificateThumbprint = "crtt";

        /// <summary>
        /// URI Parameter = cid. The Microsoft Entra ID/App client ID.
        /// </summary>
        public const string ClientId = "cid";

        /// <summary>
        /// URI Parameter = miid. The Azure Managed Identity client ID.
        /// </summary>
        public const string ManagedIdentityId = "miid";

        /// <summary>
        /// URI Parameter = tid. The Azure tenant/directory ID.
        /// </summary>
        public const string TenantId = "tid";
    }
}
