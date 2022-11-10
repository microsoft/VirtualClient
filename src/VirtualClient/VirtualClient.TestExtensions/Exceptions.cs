// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an equality comparison test assertion failure.
    /// </summary>
    [Serializable]
    public class EqualityAssertFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityAssertFailedException"/> class
        /// </summary>
        public EqualityAssertFailedException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public EqualityAssertFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">A related or underlying error root cause exception</param>
        public EqualityAssertFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EqualityAssertFailedException"/> class
        /// </summary>
        /// <param name="info">Serialized error properties</param>
        /// <param name="context">The serialization streaming context</param>
        protected EqualityAssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Represents an exception test assertion failure.
    /// </summary>
    [Serializable]
    public class ExceptionAssertFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionAssertFailedException"/> class
        /// </summary>
        public ExceptionAssertFailedException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public ExceptionAssertFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">A related or underlying error root cause exception</param>
        public ExceptionAssertFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionAssertFailedException"/> class
        /// </summary>
        /// <param name="info">Serialized error properties</param>
        /// <param name="context">The serialization streaming context</param>
        protected ExceptionAssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// Represents a serialization test assertion failure.
    /// </summary>
    [Serializable]
    public class SerializationAssertFailedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationAssertFailedException"/> class
        /// </summary>
        public SerializationAssertFailedException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        public SerializationAssertFailedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationAssertFailedException"/> class
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">A related or underlying error root cause exception</param>
        public SerializationAssertFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationAssertFailedException"/> class
        /// </summary>
        /// <param name="info">Serialized error properties</param>
        /// <param name="context">The serialization streaming context</param>
        protected SerializationAssertFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
