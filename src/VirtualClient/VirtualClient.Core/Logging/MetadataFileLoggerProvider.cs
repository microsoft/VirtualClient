// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Provides methods for creating <see cref="ILogger"/> instances that can be used
    /// to write metadata/marker files.
    /// </summary>
    [Alias("marker")]
    [Alias("metadata")]
    public sealed class MetadataFileLoggerProvider : ILoggerProvider
    {
        private string filePath;
        private IServiceCollection dependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLoggerProvider"/> class.
        /// </summary>
        public MetadataFileLoggerProvider(IServiceCollection dependencies, string filePath)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            this.filePath = filePath;
            this.dependencies = dependencies;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance that can be used to log events/messages
        /// to an Application Insights endpoint.
        /// </summary>
        /// <param name="categoryName">The logger events category.</param>
        /// <returns>
        /// An <see cref="ILogger"/> instance that can log events/messages to an Application
        /// Insights endpoint.
        /// </returns>
        public ILogger CreateLogger(string categoryName)
        {
            return new MetadataFileLogger(this.dependencies, this.filePath);
        }

        /// <summary>
        /// Disposes of internal resources.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
