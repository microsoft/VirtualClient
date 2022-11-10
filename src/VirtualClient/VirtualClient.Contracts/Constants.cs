// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
}
