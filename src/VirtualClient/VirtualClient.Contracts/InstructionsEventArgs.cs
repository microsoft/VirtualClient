// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Threading;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents instruction arguments passed to background monitors and components.
    /// </summary>
    public class InstructionsEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstructionsEventArgs"/> class.
        /// </summary>
        /// <param name="instructions">The instructions describing the requirements.</param>
        /// <param name="cancellationToken">
        /// A token supplied by the caller that can be used to gracefully terminate the background operations.
        /// </param>
        public InstructionsEventArgs(Instructions instructions, CancellationToken cancellationToken)
        {
            instructions.ThrowIfNull(nameof(instructions));
            this.Id = Guid.NewGuid();
            this.Instructions = instructions;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstructionsEventArgs"/> class.
        /// </summary>
        /// <param name="id">An identifier to use for the instructions.</param>
        /// <param name="instructions">The instructions describing the requirements.</param>
        /// <param name="cancellationToken">
        /// A token supplied by the caller that can be used to gracefully terminate the background operations.
        /// </param>
        public InstructionsEventArgs(Guid id, Instructions instructions, CancellationToken cancellationToken)
        {
            instructions.ThrowIfNull(nameof(instructions));
            this.Id = Guid.NewGuid();
            this.Instructions = instructions;
            this.CancellationToken = cancellationToken;
        }

        /// <summary>
        /// An identifier to use for the instructions.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// The instructions describing the requirements.
        /// </summary>
        public Instructions Instructions { get; }

        /// <summary>
        /// A token supplied by the caller that can be used to gracefully terminate the 
        /// background operations.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
