// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension method for Nginx Workload.
    /// </summary>
    public static class NginxExtensions
    {
        /// <summary>
        /// Checks if workload has expired from the state's "Timeout" property
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static bool IsExpired(this State state)
        {
            return state.Timeout() < DateTime.UtcNow;
        }

        /// <summary>
        /// Gets or sets the 'Timeout' property value from the state.
        /// </summary>
        public static DateTime Timeout(this State state, DateTime? value = null)
        {
            state.ThrowIfNull("state");
            if (value != null)
            {
                state.Properties["Timeout"] = value.Value;
            }

            return state.Properties.GetValue<DateTime>("Timeout");
        }

        /// <summary>
        /// Returns nginx commands to start the process.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        public static string ConvertToCommandArgs(this NginxCommand command)
        {
            switch (command)
            {
                case NginxCommand.Start:
                    return "systemctl restart nginx";

                case NginxCommand.Stop:
                    return "systemctl disable nginx";

                case NginxCommand.GetVersion:
                    return "nginx -V";

                case NginxCommand.GetConfig:
                    return "nginx -T";

                default:
                    throw new WorkloadException($"Unable to convert {nameof(NginxCommand)} enum into string value. Value: {Enum.GetName(command)} - {command}");
            }
        }
    }
}
