// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an error that occurs during a REST operation.
    /// </summary>
    [Serializable]
    public class RestException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestException"/> class.
        /// </summary>
        public RestException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public RestException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public RestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestException"/> class with
        /// the provided serialization and context information.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        protected RestException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}