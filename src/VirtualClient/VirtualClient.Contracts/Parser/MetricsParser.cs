// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Abstract parser for various VC documents
    /// </summary>
    public abstract class MetricsParser : TextParser<IList<Metric>>
    {
        /// <summary>
        /// Constructor for <see cref="MetricsParser"/>
        /// </summary>
        /// <param name="rawText">Text to be parsed.</param>
        public MetricsParser(string rawText)
            : base(rawText)
        {
        }
    }
}
