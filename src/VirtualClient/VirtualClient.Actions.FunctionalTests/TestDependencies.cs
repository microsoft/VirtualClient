// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
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
        public static ProfileExecutor CreateProfileExecutor(string profile, IServiceCollection dependencies, bool dependenciesOnly = false)
        {
            ExecutionProfile workloadProfile = ExecutionProfile.ReadProfileAsync(Path.Combine(TestDependencies.ProfileDirectory, profile))
                .GetAwaiter().GetResult();

            workloadProfile.Inline();

            ProfileExecutor profileExecutor = new ProfileExecutor(workloadProfile, dependencies)
            {
                ExecuteActions = !dependenciesOnly,
                ExecuteDependencies = true,
                ExecuteMonitors = !dependenciesOnly
            };

            return profileExecutor;
        }

        public static string GetResourceFileContents(string fileName)
        {
            return File.ReadAllText(Path.Combine(TestDependencies.ResourcesDirectory, fileName));
        }
    }
}
