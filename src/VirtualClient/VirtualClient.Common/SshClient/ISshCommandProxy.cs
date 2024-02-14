// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;

    /// <summary>
    /// Acts as a limited proxy to provide information about a SSH command.
    /// </summary>
    public interface ISshCommandProxy : IDisposable
    {
        /// <summary>
        /// Gets the command exit status.
        /// </summary>
        int ExitStatus { get; }

        /// <summary>
        /// Log Results.
        /// </summary>
        ProcessDetails ProcessDetails { get; }

        /// <summary>
        /// Gets the command execution result.
        /// </summary>
        string Result { get; }

        /// <summary>
        /// Gets the command execution error.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// Gets the command text.
        /// </summary>
        string CommandText { get; }

        /// <summary>
        /// Executes command specified by Renci.SshNet.SshCommand.CommandText property.
        /// </summary>
        /// <returns>Command execution result</returns>
        string Execute();
    }
}