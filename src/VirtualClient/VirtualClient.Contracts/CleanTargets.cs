// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtualClient.Contracts
{
    /// <summary>
    /// Provides an indication of a set of target resources to clean (e.g. logs, state, packages).
    /// </summary>
    public class CleanTargets
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
        /// Initializes a new instance of the <see cref="CleanTargets"/> class.
        /// </summary>
        /// <param name="cleanLogs">True if the "logs" directory should be cleaned.</param>
        /// <param name="cleanPackages">True if the "packages" directory should be cleaned (minus built-in packages).</param>
        /// <param name="cleanState">True if the "state" directory should be cleaned.</param>
        /// <param name="cleanTemp">True if the "temp" directory should be cleaned.</param>
        public CleanTargets(bool cleanLogs, bool cleanPackages, bool cleanState, bool cleanTemp)
        {
            this.CleanLogs = cleanLogs;
            this.CleanPackages = cleanPackages;
            this.CleanState = cleanState;
            this.CleanTemp = cleanTemp;
        }

        /// <summary>
        /// True if the "logs" directory should be cleaned.
        /// </summary>
        public bool CleanLogs { get; }

        /// <summary>
        /// True if the "packages" directory should be cleaned (minus built-in packages).
        /// </summary>
        public bool CleanPackages { get; }

        /// <summary>
        /// True if the "state" directory should be cleaned.
        /// </summary>
        public bool CleanState { get; }

        /// <summary>
        /// True if the "temp" directory should be cleaned.
        /// </summary>
        public bool CleanTemp { get; }

        /// <summary>
        /// Creates a <see cref="CleanTargets"/> instance for the cleanup
        /// of all targets.
        /// </summary>
        public static CleanTargets Create()
        {
            return new CleanTargets(cleanLogs: true, cleanPackages: true, cleanState: true, cleanTemp: true);
        }

        /// <summary>
        /// Creates a <see cref="CleanTargets"/> instance from the set of targets.
        /// </summary>
        /// <param name="targets">1 or more targets to clean (e.g logs, packages, state, all)</param>
        public static CleanTargets Create(IEnumerable<string> targets)
        {
            CleanTargets cleanTargets = null;

            bool cleanLogs = false;
            bool cleanPackages = false;
            bool cleanState = false;
            bool cleanTemp = false;

            if (targets?.Any() == true)
            {
                if (targets.Contains(CleanTargets.All, StringComparer.OrdinalIgnoreCase))
                {
                    cleanLogs = true;
                    cleanPackages = true;
                    cleanState = true;
                    cleanTemp = true;
                }
                else
                {
                    cleanLogs = targets.Contains(CleanTargets.Logs, StringComparer.OrdinalIgnoreCase);
                    cleanPackages = targets.Contains(CleanTargets.Packages, StringComparer.OrdinalIgnoreCase);
                    cleanState = targets.Contains(CleanTargets.State, StringComparer.OrdinalIgnoreCase);
                    cleanTemp = targets.Contains(CleanTargets.Temp, StringComparer.OrdinalIgnoreCase);
                }
            }

            if (cleanLogs || cleanPackages || cleanState || cleanTemp)
            {
                cleanTargets = new CleanTargets(cleanLogs, cleanPackages, cleanState, cleanTemp);
            }

            return cleanTargets;
        }
    }
}
