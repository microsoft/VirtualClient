// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    /// <summary>
    /// Holds Suffixes for common event names.
    /// </summary>
    public static class EventNameSuffix
    {
        /// <summary>
        /// Ex:  GetItemsError
        /// </summary>
        public static string Error { get; } = nameof(EventNameSuffix.Error);

        /// <summary>
        /// Ex:  GetItemsInvalidUsage
        /// </summary>
        public static string InvalidUsage { get; } = nameof(EventNameSuffix.InvalidUsage);

        /// <summary>
        /// Ex:  GetItemsReturns
        /// </summary>
        public static string Returns { get; } = nameof(EventNameSuffix.Returns);

        /// <summary>
        /// Ex:  GetItemsStart
        /// </summary>
        public static string Start { get; } = nameof(EventNameSuffix.Start);

        /// <summary>
        /// Ex:  GetItemsStop
        /// </summary>
        public static string Stop { get; } = nameof(EventNameSuffix.Stop);

        /// <summary>
        /// Ex:  GetItemsSuccess
        /// </summary>
        public static string Success { get; } = nameof(EventNameSuffix.Success);
    }
}