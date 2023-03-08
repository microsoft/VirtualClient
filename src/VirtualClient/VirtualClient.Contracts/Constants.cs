// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

namespace VirtualClient.Contracts
{
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
        /// Name = SUDO_USER
        /// </summary>
        public const string SUDO_USER = nameof(SUDO_USER);

        /// <summary>
        /// Name = PATH
        /// </summary>
        public const string PATH = nameof(PATH);

        /// <summary>
        /// Name = USER
        /// </summary>
        public const string USER = nameof(USER);
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
}
