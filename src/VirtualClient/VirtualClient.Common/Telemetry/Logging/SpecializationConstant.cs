// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    /// <summary>
    /// Constants that define logging specializations.
    /// </summary>
    public static class SpecializationConstant
    {
        /// <summary>
        /// The specialization name for telemetry logging.
        /// </summary>
        public const string Telemetry = nameof(SpecializationConstant.Telemetry);

        /// <summary>
        /// The specialization name for structured logging, usually to file.
        /// </summary>
        public const string StructuredLogging = nameof(SpecializationConstant.StructuredLogging);
    }
}