// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;

    /// <summary>
    /// Represents output from an SSH session/stream command execution.
    /// </summary>
    public class SshCommandOutputInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SshCommandOutputInfo"/> class.
        /// </summary>
        /// <param name="output">A line of output from the SSH command stream.</param>
        /// <param name="elapsed">The command execution time elapsed.</param>
        public SshCommandOutputInfo(string output, TimeSpan elapsed)
        {
            this.Output = output;
            this.ElapsedTime = elapsed;
        }

        /// <summary>
        /// The total elapsed time for the command execution.
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Output from the stream.
        /// </summary>
        public string Output { get; }
    }
}
