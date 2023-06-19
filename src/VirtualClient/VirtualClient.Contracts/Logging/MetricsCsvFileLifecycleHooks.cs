// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Logging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Serilog.Sinks.File;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides features for initializing CSV files on first write.
    /// </summary>
    public class MetricsCsvFileLifecycleHooks : FileLifecycleHooks
    {
        private static readonly HashSet<string> FileInitialization = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object LockObject = new object();

        private IEnumerable<string> csvFileHeaders;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLifecycleHooks"/> class.
        /// </summary>
        /// <param name="csvHeaders">The fields/headers to add to the top of each CSV file.</param>
        public MetricsCsvFileLifecycleHooks(IEnumerable<string> csvHeaders)
        {
            csvHeaders.ThrowIfNullOrEmpty(nameof(csvHeaders));
            this.csvFileHeaders = csvHeaders;
        }

        /// <inheritdoc />
        public override Stream OnFileOpened(string path, Stream underlyingStream, Encoding encoding)
        {
            // We need to ensure that the CSV headers are written to the file first. Here, we are using
            // a simple technique to check if the file (at the path provided) was initialized or not.
            // The very first time the file is opened, the stream will have a length of zero bytes.
            if (underlyingStream != null && underlyingStream.Length == 0)
            {
                lock (MetricsCsvFileLifecycleHooks.LockObject)
                {
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (StreamWriter writer = new StreamWriter(path))
                    {
                        int propertyIndex = 0;
                        foreach (string header in this.csvFileHeaders)
                        {
                            if (propertyIndex > 0)
                            {
                                writer.Write(",");
                            }

                            writer.Write(header);
                            propertyIndex++;
                        }

                        writer.Write(Environment.NewLine);
                    }

                    MetricsCsvFileLifecycleHooks.FileInitialization.Add(path);
                }
            }

            return underlyingStream;
        }
    }
}
