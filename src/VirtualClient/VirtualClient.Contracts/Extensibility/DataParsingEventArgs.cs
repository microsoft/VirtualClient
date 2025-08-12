// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;

    /// <summary>
    /// Event arguments for data parsing operation events.
    /// </summary>
    public class DataParsingEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataParsingEventArgs"/> class.
        /// </summary>
        /// <param name="item">The item associated with the parsing operation</param>
        /// <param name="itemIndex">The index of the item in the content.</param>
        /// <param name="error">An error that occurred during parsing.</param>
        public DataParsingEventArgs(string item, int itemIndex, Exception error = null)
        {
            this.ItemIndex = itemIndex;
            this.Error = error;
        }

        /// <summary>
        /// An error that occurred during the parsing operation.
        /// </summary>
        public Exception Error { get; }

        /// <summary>
        /// The item associated with the parsing operation.
        /// </summary>
        public string Item { get; }

        /// <summary>
        /// The item number associated with the parsing operation.
        /// </summary>
        public int ItemIndex { get; }
    }
}
