// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an exception/error that occurred during the execution of a monitoring process
    /// on the system.
    /// </summary>
    public class VirtualClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientException"/> class.
        /// </summary>
        protected VirtualClientException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        protected VirtualClientException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        protected VirtualClientException(string message, ErrorReason reason)
            : base(message)
        {
            this.Reason = reason;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        protected VirtualClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        protected VirtualClientException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException)
        {
            this.Reason = reason;
        }

        /// <summary>
        /// Defines the error reason/category.
        /// </summary>
        public ErrorReason Reason { get; internal set; }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the execution of an API
    /// operation.
    /// </summary>
    public class ApiException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class.
        /// </summary>
        public ApiException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ApiException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public ApiException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ApiException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public ApiException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the execution of a monitoring process
    /// on the system.
    /// </summary>
    public class MonitorException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorException"/> class.
        /// </summary>
        public MonitorException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public MonitorException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public MonitorException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public MonitorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public MonitorException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the initialization of the
    /// Virtual Client workload runtime environment.
    /// </summary>
    public class EnvironmentSetupException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSetupException"/> class.
        /// </summary>
        public EnvironmentSetupException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSetupException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public EnvironmentSetupException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSetupException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public EnvironmentSetupException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSetupException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public EnvironmentSetupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentSetupException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public EnvironmentSetupException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the execution of a workload
    /// on the system.
    /// </summary>
    public class WorkloadException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class.
        /// </summary>
        public WorkloadException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public WorkloadException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public WorkloadException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public WorkloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public WorkloadException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the preparation or parsing 
    /// of results from a workload.
    /// </summary>
    public class WorkloadResultsException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadException"/> class.
        /// </summary>
        public WorkloadResultsException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadResultsException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public WorkloadResultsException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadResultsException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public WorkloadResultsException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadResultsException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public WorkloadResultsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkloadResultsException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public WorkloadResultsException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the installation or configuration
    /// of a required dependency.
    /// </summary>
    public class DependencyException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyException"/> class.
        /// </summary>
        public DependencyException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public DependencyException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public DependencyException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public DependencyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public DependencyException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the execution of a process
    /// on the system (i.e. an OS process).
    /// </summary>
    public class ProcessException : VirtualClientException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessException"/> class.
        /// </summary>
        public ProcessException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ProcessException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="reason">The error reason/category.</param>
        public ProcessException(string message, ErrorReason reason)
            : base(message, reason)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProcessException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="reason">The error reason/category.</param>
        public ProcessException(string message, Exception innerException, ErrorReason reason)
            : base(message, innerException, reason)
        {
        }
    }

    /// <summary>
    /// Represents an exception/error that occurred during the execution of a process
    /// on the system (i.e. an OS process).
    /// </summary>
    public class StartupException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class.
        /// </summary>
        public StartupException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public StartupException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StartupException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public StartupException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents the base exception for errors that occurr in relation to the
    /// experiment schema.
    /// </summary>
    [Serializable]
    public class SchemaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaException"/> class.
        /// </summary>
        public SchemaException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaException"/> class with
        /// the provided message.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public SchemaException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaException"/> class with
        /// the provided message and inner exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public SchemaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
