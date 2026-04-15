// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides dependencies used in the various functional tests in this project.
    /// </summary>
    public static class TestDependencies
    {
        static TestDependencies()
        {
            TestDependencies.TestDirectory = Path.GetDirectoryName(Assembly.GetAssembly(typeof(TestDependencies)).Location);
            TestDependencies.ProfileDirectory = Path.Combine(TestDependencies.TestDirectory, "Profiles");
            TestDependencies.ResourcesDirectory = Path.Combine(TestDependencies.TestDirectory, "Resources");
        }

        /// <summary>
        /// The directory where the workload profiles exist.
        /// </summary>
        public static string ProfileDirectory { get; }

        /// <summary>
        /// The directory where the workload resources exist.
        /// </summary>
        public static string ResourcesDirectory { get; }

        /// <summary>
        /// The directory where the test binaries exist (e.g. the build output directory).
        /// </summary>
        public static string TestDirectory { get; }

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
            ExecutionProfile workloadProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(TestDependencies.ProfileDirectory, profile))
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

        public static string GetResourceFileContents(string fileName)
        {
            return File.ReadAllText(Path.Combine(TestDependencies.ResourcesDirectory, fileName));
        }
    }
}
