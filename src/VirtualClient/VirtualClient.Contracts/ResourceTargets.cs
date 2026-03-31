// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualClient.Contracts
{
    /// <summary>
    /// Provides an indication of a set of target resources (e.g. logs, state, packages).
    /// </summary>
    public class ResourceTargets
    {
        /// <summary>
        /// Target = all
        /// </summary>
        public const string All = "all";

        /// <summary>
        /// Target = logs
        /// </summary>
        public const string Logs = "logs";

        /// <summary>
        /// Target = packages
        /// </summary>
        public const string Packages = "packages";

        /// <summary>
        /// Target = state
        /// </summary>
        public const string State = "state";

        /// <summary>
        /// Target = temp
        /// </summary>
        public const string Temp = "temp";

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceTargets"/> class.
        /// </summary>
        /// <param name="targetLogs">True if the "logs" directory should be targeted.</param>
        /// <param name="targetPackages">True if the "packages" directory should be targeted (minus built-in packages).</param>
        /// <param name="targetState">True if the "state" directory should be targeted.</param>
        /// <param name="targetTemp">True if the "temp" directory should be targeted.</param>
        public ResourceTargets(bool targetLogs, bool targetPackages, bool targetState, bool targetTemp)
        {
            this.TargetLogs = targetLogs;
            this.TargetPackages = targetPackages;
            this.TargetState = targetState;
            this.TargetTemp = targetTemp;
        }

        /// <summary>
        /// True if the "logs" directory should be targeted.
        /// </summary>
        public bool TargetLogs { get; }

        /// <summary>
        /// True if the "packages" directory should be targeted.
        /// </summary>
        public bool TargetPackages { get; }

        /// <summary>
        /// True if the "state" directory should be targeted.
        /// </summary>
        public bool TargetState { get; }

        /// <summary>
        /// True if the "temp" directory should be targeted.
        /// </summary>
        public bool TargetTemp { get; }

        /// <summary>
        /// Creates a <see cref="ResourceTargets"/> instance for all targets.
        /// </summary>
        public static ResourceTargets Create()
        {
            return new ResourceTargets(targetLogs: true, targetPackages: true, targetState: true, targetTemp: true);
        }

        /// <summary>
        /// Creates a <see cref="ResourceTargets"/> instance from the set of targets.
        /// </summary>
        /// <param name="targets">1 or more targets (e.g logs, packages, state, all)</param>
        public static ResourceTargets Create(IEnumerable<string> targets)
        {
            ResourceTargets resourceTargets = null;

            bool targetLogs = false;
            bool targetPackages = false;
            bool targetState = false;
            bool targetTemp = false;

            if (targets?.Any() == true)
            {
                if (targets.Contains(ResourceTargets.All, StringComparer.OrdinalIgnoreCase))
                {
                    targetLogs = true;
                    targetPackages = true;
                    targetState = true;
                    targetTemp = true;
                }
                else
                {
                    targetLogs = targets.Contains(ResourceTargets.Logs, StringComparer.OrdinalIgnoreCase);
                    targetPackages = targets.Contains(ResourceTargets.Packages, StringComparer.OrdinalIgnoreCase);
                    targetState = targets.Contains(ResourceTargets.State, StringComparer.OrdinalIgnoreCase);
                    targetTemp = targets.Contains(ResourceTargets.Temp, StringComparer.OrdinalIgnoreCase);
                }
            }

            if (targetLogs || targetPackages || targetState || targetTemp)
            {
                resourceTargets = new ResourceTargets(targetLogs, targetPackages, targetState, targetTemp);
            }

            return resourceTargets;
        }
    }
}
