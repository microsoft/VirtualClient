// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides dependencies used in the various functional tests in this project.
    /// </summary>
    public static class TestProfileResources
    {
        /// <summary>
        /// Creates a <see cref="ProfileExecutor"/> for the workload profile provided (e.g. PERF-IO-FIO-STRESS.json).
        /// </summary>
        /// <param name="profile">The name of the workload profile file (e.g. PERF-IO-DISKSPD.json).</param>
        /// <param name="dependencies">Service dependencies required by the profile executor.</param>
        /// <param name="dependenciesOnly">True to execute only profile dependencies, not actions.</param>
        /// <param name="parameterOverrides">
        /// Optional parameter overrides applied to every non-disk-fill action in the profile after inlining
        /// (e.g. <c>new Dictionary&lt;string, IConvertible&gt; {{ "DiskFilter", "DiskIndex:6,7" }}</c>).
        /// Useful for testing scenarios that would normally be driven by CLI <c>--parameters</c>.
        /// </param>
        public static ProfileExecutor CreateProfileExecutor(
            string profile,
            IServiceCollection dependencies,
            bool dependenciesOnly = false,
            IDictionary<string, IConvertible> parameterOverrides = null)
        {
            ExecutionProfile workloadProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(MockFixture.TestResourcesDirectory, "profiles", profile))
                .GetAwaiter().GetResult();

            workloadProfile.Inline();

            if (parameterOverrides?.Count > 0)
            {
                foreach (ExecutionProfileElement action in workloadProfile.Actions)
                {
                    // Do not apply overrides to disk-fill actions — DiskFill is incompatible with
                    // DiskIndex: targeting and would fail validation if both are set.
                    bool isDiskFill = action.Parameters.TryGetValue("DiskFill", out IConvertible df)
                        && bool.TryParse(df?.ToString(), out bool dfValue) && dfValue;

                    if (!isDiskFill)
                    {
                        foreach (KeyValuePair<string, IConvertible> kvp in parameterOverrides)
                        {
                            action.Parameters[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            ComponentSettings settings = new ComponentSettings
            {
                ExitWait = TimeSpan.Zero
            };

            ProfileExecutor profileExecutor = new ProfileExecutor(workloadProfile, dependencies, settings)
            {
                ExecuteActions = !dependenciesOnly,
                ExecuteDependencies = true,
                ExecuteMonitors = !dependenciesOnly,
            };

            return profileExecutor;
        }
    }
}
