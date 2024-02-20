// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Attribute is used to decorate <see cref="ILoggerProvider"/> classes that support
    /// specialized logging (e.g. telemetry event logging).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class LoggerSpecializationAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the specialization supported by the logging provider
        /// class instance.
        /// </summary>
        public string Name { get; set; }
    }
}