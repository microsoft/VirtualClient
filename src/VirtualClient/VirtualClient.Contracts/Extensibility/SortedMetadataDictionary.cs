// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Case-insensitive, sorted dictionary for data point metadata.
    /// </summary>
    public class SortedMetadataDictionary : SortedDictionary<string, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SortedMetadataDictionary"/> class.
        /// </summary>
        public SortedMetadataDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SortedMetadataDictionary"/> class.
        /// </summary>
        public SortedMetadataDictionary(IDictionary<string, object> dictionary)
            : base(dictionary, StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
