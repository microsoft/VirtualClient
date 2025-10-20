// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides an <see cref="ILogger"/> implementation that writes a 'metadata/marker' file 
    /// to the file system.
    /// </summary>
    public class MetadataFileLogger : ILogger
    {
        private readonly object lockObject = new object();
        private ISystemInfo systemInfo;
        private IFileSystem fileSystem;
        private PlatformSpecifics platformSpecifics;
        private bool fileWritten;
        
        /// <summary>
        ///  Initializes a new instance of the <see cref="MetadataFileLogger"/> class.
        /// </summary>
        public MetadataFileLogger(IServiceCollection dependencies, string filePath)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.Dependencies = dependencies;
            this.FilePath = filePath;
            this.systemInfo = this.Dependencies.GetService<ISystemInfo>();
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();
            this.platformSpecifics = this.Dependencies.GetService<PlatformSpecifics>();
        }

        /// <summary>
        /// Provides dependencies to the logger.
        /// </summary>
        protected IServiceCollection Dependencies { get; }

        /// <summary>
        /// The path to the marker file (e.g. /home/user/virtualclient/logs/metadata.log).
        /// </summary>
        protected string FilePath { get; }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state) 
            where TState : notnull
        {
            return state as IDisposable;
        }

        /// <summary>
        /// Returns 'true' always.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Required implementation for ILogger
        /// </summary>
        /// <typeparam name="TState">The data type for the event/message object.</typeparam>
        /// <param name="logLevel">The log level/severity of the event/message.</param>
        /// <param name="eventId">The event ID of the event/message.</param>
        /// <param name="state">The event/message object or text.</param>
        /// <param name="exception">An exception associated with the event/message.</param>
        /// <param name="formatter">Given an exception is provided, a delegate to format the message output for that exception.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!this.fileWritten)
            {
                lock (this.lockObject)
                {
                    // Race condition prevention technique.
                    if (!this.fileWritten)
                    {
                        try
                        {
                            IDictionary<string, IConvertible> metadata = this.GetMetadata();
                            this.WriteMetadataFile(metadata);
                        }
                        catch
                        {
                            // Best effort.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a set of metadata to write to the file.
        /// </summary>
        protected virtual IDictionary<string, IConvertible> GetMetadata()
        {
            IDictionary<string, IConvertible> metadata = new SortedDictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "clientId", this.systemInfo.AgentId },
                { "experimentId", this.systemInfo.ExperimentId },
                { "machineName", Environment.MachineName },
                { "platformArchitecture", this.platformSpecifics.PlatformArchitectureName },
                { "operatingSystemVersion", Environment.OSVersion.ToString() },
                { "operatingSystemDescription", RuntimeInformation.OSDescription },
                { "timestamp", DateTime.UtcNow.ToString("o") },
                { "timezone", TimeZoneInfo.Local.StandardName }
            };

            if (VirtualClientRuntime.CommandLineMetadata?.Any() == true)
            {
                metadata.AddRange(VirtualClientRuntime.CommandLineMetadata);
            }

            return metadata;
        }

        /// <summary>
        /// Writes the metadata file at the path specified.
        /// </summary>
        /// <param name="metadata">The metadata to write to the file.</param>
        protected virtual void WriteMetadataFile(IDictionary<string, IConvertible> metadata)
        {
            string effectiveFilePath = null;
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                // File name not defined.
                effectiveFilePath = this.platformSpecifics.GetLogsPath("metadata.log");
            }
            else if (string.IsNullOrWhiteSpace(this.fileSystem.Path.GetDirectoryName(this.FilePath)))
            {
                // File name only provided (e.g. --logger=metadata;marker.log).
                effectiveFilePath = this.platformSpecifics.GetLogsPath(this.FilePath);
            }
            else
            {
                // File path provided (e.g. --logger=metadata;/home/user/logs/marker.log, --logger=metadata;./logs/marker.log).
                effectiveFilePath = this.fileSystem.Path.GetFullPath(this.FilePath);
            }

            // Ensure a fixed-width for ALL metadata key/value pairs.
            int maxKeyLength = metadata.Max(item => item.Key.Length);

            StringBuilder outputBuilder = new StringBuilder();
            foreach (var entry in metadata)
            {
                outputBuilder.AppendLine(string.Format($"{{0,-{maxKeyLength}}} : {{1}}", entry.Key.CamelCased(), entry.Value));
            }

            RetryPolicies.Synchronous.FileOperations.Execute(() =>
            {
                string directory = this.fileSystem.Path.GetDirectoryName(effectiveFilePath);
                if (!this.fileSystem.Directory.Exists(directory))
                {
                    this.fileSystem.Directory.CreateDirectory(directory);
                }

                this.fileSystem.File.WriteAllText(effectiveFilePath, outputBuilder.ToString());
                this.fileWritten = true;
            });
        }
    }
}
