// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an error that occurs while executing commands.
    /// </summary>
    [Serializable]
    public class ProcessExecutionException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutionException"/> class.
        /// </summary>
        public ProcessExecutionException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutionException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ProcessExecutionException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutionException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProcessExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutionException"/> class with
        /// the provided serialization and context information.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        protected ProcessExecutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}