// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    /// <summary>
    /// Metadata properties that can be associated with dependency packages.
    /// </summary>
    public static class PackageMetadata
    {
        /// <summary>
        /// The executable path.
        /// </summary>
        public const string ExecutablePath = "ExecutablePath";

        /// <summary>
        /// Represents a package that contains extensions.
        /// </summary>
        public const string Extensions = "Extensions";

        /// <summary>
        /// Represents the dependency specifics key that defines the path where an application
        /// will be/is installed (e.g. the path to the Java runtime executable, java.exe).
        /// </summary>
        public const string InstallationPath = "InstallationPath";

        /// <summary>
        /// Represents the dependency specifics key that defines the name of
        /// an installer (e.g. the name of the Java runtime installer).
        /// </summary>
        public const string InstallerName = "InstallerName";
    }
}
