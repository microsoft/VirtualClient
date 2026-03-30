// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text.Json.Nodes;
    using VirtualClient.Contracts;

    /// <summary>
    /// Methods for extending the functionality of the 
    /// file system class, and related classes. (i.e. IFile, IPath, etc.)
    /// </summary>
    internal static class DockerRuntimeExtensions
    {
        /// <summary>
        /// Parses JSON output from 'docker image inspect' and returns platform-specific information.
        /// </summary>
        public static PlatformSpecifics GetPlatform(string dockerImageInspectJson)
        {
            var array = JsonNode.Parse(dockerImageInspectJson)?.AsArray()
                ?? throw new ArgumentException("Invalid docker inspect JSON output.");

            var root = array[0]
                ?? throw new ArgumentException("Docker inspect output is empty.");

            string os = root["Os"]?.GetValue<string>() ?? string.Empty;
            string arch = root["Architecture"]?.GetValue<string>() ?? string.Empty;
            string variant = root["Variant"]?.GetValue<string>() ?? string.Empty;

            PlatformID platform = ParsePlatform(os);
            Architecture architecture = ParseArchitecture(arch, variant);

            return new PlatformSpecifics(platform, architecture);
        }

        private static PlatformID ParsePlatform(string os)
        {
            return os.ToLowerInvariant() switch
            {
                "linux" => PlatformID.Unix,
                "windows" => PlatformID.Win32NT,
                _ => throw new NotSupportedException($"The OS/system platform '{os}' is not supported.")
            };
        }

        private static Architecture ParseArchitecture(string arch, string variant)
        {
            return arch.ToLowerInvariant() switch
            {
                "amd64" or "x86_64" => Architecture.X64,
                "arm64" or "aarch64" => Architecture.Arm64,
                "arm" when variant.ToLowerInvariant() == "v8" => Architecture.Arm64,
                _ => throw new NotSupportedException($"The CPU/processor architecture '{arch}' is not supported.")
            };
        }

    }
}
