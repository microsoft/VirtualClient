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

        /// <summary>
        /// Constructor for <see cref="MetricsParser"/>
        /// </summary>
        /// <param name="rawText">Text to be parsed.</param>
        /// <param name="logger">ILogger for logging in parser.</param>
        /// <param name="eventContext">Provided correlation identifiers and context properties for the metric.</param>
        public MetricsParser(string rawText, ILogger logger, EventContext eventContext)
            : base(rawText)
        {
        }
    }
}
