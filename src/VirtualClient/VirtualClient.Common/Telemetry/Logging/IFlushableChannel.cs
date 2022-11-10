// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;

    /// <summary>
    /// Provides a method that can be called to flush buffered content
    /// from the logger instance.
    /// </summary>
    public interface IFlushableChannel
    {
        /// <summary>
        /// Flushes buffered content from the logger instance.
        /// </summary>
        /// <param name="timeout">A timeout for the flush operation.</param>
        /// <returns>
        /// A task that can be used to flush buffered content from the logger
        /// instance.
        /// </returns>
        void Flush(TimeSpan? timeout = null);
    }
}