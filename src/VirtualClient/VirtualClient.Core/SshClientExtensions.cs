// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;

    /// <summary>
    /// Extension methods for SSH client instances.
    /// </summary>
    public static class SshClientExtensions
    {
        private static readonly Regex LinuxPathVariableExpression = new Regex("/usr/bin|/home", RegexOptions.IgnoreCase);
        private static readonly Regex Arm64ArchitectureExpression = new Regex("arm64|aarch64", RegexOptions.IgnoreCase);
        private static readonly Regex X64ArchitectureExpression = new Regex("amd64|x64|x86_64", RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns a path fully resolved on the target/remote system (e.g. /home/user/Agent/../logs -> /home/user/logs).
        /// </summary>
        /// <param name="sshClient">The SSH client to use for execution of commands over an SSH session.</param>
        /// <param name="agentInstallationPath">The installation path for the agent.</param>
        /// <param name="relativePath">A relative path to resolve on the target system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The value of the environment variable from the target/remote system.</returns>
        public static async Task<string> ResolvePathOnTargetAsync(this ISshClientProxy sshClient, string agentInstallationPath, string relativePath, CancellationToken cancellationToken)
        {
            string resolvedPath = null;

            ProcessDetails result = await sshClient.ExecuteCommandAsync("echo $PATH", cancellationToken);
            if (result.ExitCode == 0 && LinuxPathVariableExpression.IsMatch(result.StandardOutput))
            {
                // The target is a Linux system.
                result = await sshClient.ExecuteCommandAsync(
                    $"readlink -f  \"{agentInstallationPath.TrimEnd('/', '\\')}/${relativePath}\"", 
                    cancellationToken);

                if (result.ExitCode != 0)
                {
                    throw new NotSupportedException(
                        $"Evaluation failed for relative path '{relativePath}' on the target system: {sshClient.ConnectionInfo.Host}. " +
                        $"{result.ToString()}".Trim());
                }

                resolvedPath = result.StandardOutput.Trim();
            }
            else
            {
                // The target is a Windows system.
                result = await sshClient.ExecuteCommandAsync($"cd \"{agentInstallationPath.TrimEnd('/', '\\')}\"&&echo \"%CD%\\{relativePath}\"", cancellationToken);

                if (result.ExitCode != 0)
                {
                    throw new NotSupportedException(
                        $"Evaluation failed for relative path '{relativePath}' on the target system: {sshClient.ConnectionInfo.Host}. " +
                        $"{result.ToString()}".Trim());
                }

                resolvedPath = result.StandardOutput.Trim();
            }

            return resolvedPath;
        }

        /// <summary>
        /// Returns the platform specifics (e.g. platform/architecture) for the target/remote system.
        /// </summary>
        /// <param name="sshClient">The SSH client to use for execution of commands over an SSH session.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The platform-specifics for the target/remote system.</returns>
        public static async Task<Tuple<PlatformID, Architecture>> GetTargetPlatformArchitectureAsync(this ISshClientProxy sshClient, CancellationToken cancellationToken)
        {
            PlatformID platform = PlatformID.Other;
            Architecture architecture = Architecture.X86;

            ProcessDetails result = await sshClient.ExecuteCommandAsync("echo $PATH", cancellationToken);
            if (result.ExitCode == 0 && LinuxPathVariableExpression.IsMatch(result.StandardOutput))
            {
                // The target is a Linux system.
                platform = PlatformID.Unix;
                result = await sshClient.ExecuteCommandAsync("uname -m", cancellationToken);

                if (Arm64ArchitectureExpression.IsMatch(result.StandardOutput))
                {
                    architecture = Architecture.Arm64;
                }
                else if (X64ArchitectureExpression.IsMatch(result.StandardOutput))
                {
                    architecture = Architecture.X64;
                }
            }
            else
            {
                // The target is a Windows system.
                platform = PlatformID.Win32NT;
                result = await sshClient.ExecuteCommandAsync("echo %PROCESSOR_ARCHITECTURE%", cancellationToken);

                if (Arm64ArchitectureExpression.IsMatch(result.StandardOutput))
                {
                    architecture = Architecture.Arm64;
                }
                else if (X64ArchitectureExpression.IsMatch(result.StandardOutput))
                {
                    architecture = Architecture.X64;
                }
            }

            if (platform == PlatformID.Other)
            {
                throw new NotSupportedException($"Platform-architecture cannot be determined for the target system: {sshClient.ConnectionInfo.Host}.");
            }

            if (architecture == Architecture.X86)
            {
                throw new NotSupportedException($"X86 CPU architecture of target system is not supported: {sshClient.ConnectionInfo.Host}.");
            }

            return new Tuple<PlatformID, Architecture>(platform, architecture);
        }
    }
}
