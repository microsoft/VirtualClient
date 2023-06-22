// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Logging
{
    using System.IO;
    using System.Linq;
    using System.Text;
    using Serilog.Sinks.File;

    /// <summary>
    /// Provides features for initializing metrics CSV files on first write.
    /// </summary>
    public class MetricsCsvFileLifecycleHooks : FileLifecycleHooks
    {
        private static readonly object LockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricsCsvFileLifecycleHooks"/> class.
        /// </summary>
        public MetricsCsvFileLifecycleHooks()
        {
        }

        /// <inheritdoc />
        public override Stream OnFileOpened(Stream underlyingStream, Encoding encoding)
        {
            return this.OnFileOpened(null, underlyingStream, encoding);
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
                    string columnHeaders = string.Join(",", MetricsCsvFileLogger.CsvFields.Select(field => $"\"{field.ColumnName}\""));
                    underlyingStream.Write(encoding.GetBytes(columnHeaders));
                }
            }

            return underlyingStream;
        }
    }
}
