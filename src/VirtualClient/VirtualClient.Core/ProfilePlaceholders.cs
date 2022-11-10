// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for applying replacement values to common/well-known
    /// placeholders in profile definitions.
    /// </summary>
    public static class ProfilePlaceholders
    {
        /// <summary>
        /// Replaces the placeholder with the replacement value.
        /// </summary>
        /// <param name="placeholder">The placeholder (e.g. [filesize], [queuedepth]).</param>
        /// <param name="replacement">The replacement value for the placeholder.</param>
        /// <param name="text">The test in which to replace the placeholders.</param>
        /// <returns>String after placeholder replaced.</returns>
        public static string Replace(string placeholder, IConvertible replacement, string text)
        {
            placeholder.ThrowIfNullOrWhiteSpace(nameof(placeholder));
            replacement.ThrowIfNull(nameof(replacement));

            // Normalize the placeholder so that it matches the standard placeholder
            // format in profiles (e.g. [filesize]).
            string effectivePlaceholder = $"[{placeholder.ToLowerInvariant().Trim('[', ']')}]";

            return text.Replace(effectivePlaceholder, replacement.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
