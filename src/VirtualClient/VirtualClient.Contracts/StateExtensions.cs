// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Globalization;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="State"/> instances.
    /// </summary>
    public static class StateExtensions
    {
        /// <summary>
        /// Gets or sets the 'ErrorMessage' property value from the state.
        /// </summary>
        public static string ErrorMessage(this State state, string error = null)
        {
            state.ThrowIfNull(nameof(state));

            if (error != null)
            {
                state.Properties[nameof(ErrorMessage)] = error;
            }

            state.Properties.TryGetValue(nameof(ErrorMessage), out IConvertible value);
            return value?.ToString();
        }

        /// <summary>
        /// Gets or sets the 'ErrorReason' property value from the state.
        /// </summary>
        public static int? ErrorReason(this State state, int? errorReason = null)
        {
            state.ThrowIfNull(nameof(state));

            if (errorReason != null)
            {
                state.Properties[nameof(ErrorReason)] = errorReason;
            }

            state.Properties.TryGetValue(nameof(ErrorReason), out IConvertible value);
            return value?.ToInt32(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets or sets the 'Online' property value from the state.
        /// </summary>
        public static bool Online(this State state, bool? online = null)
        {
            state.ThrowIfNull(nameof(state));

            if (online != null)
            {
                state.Properties[nameof(Online)] = online.Value;
            }

            return state.Properties.GetValue<bool>(nameof(Online), false);
        }

        /// <summary>
        /// Gets or sets the 'Status' property value from the state.
        /// </summary>
        public static string Status(this State state, string status = null)
        {
            state.ThrowIfNull(nameof(state));

            if (status != null)
            {
                state.Properties[nameof(Status)] = status;
            }

            state.Properties.TryGetValue(nameof(Status), out IConvertible value);
            return value?.ToString();
        }
    }
}
