// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;

    /// <summary>
    /// Base class for Wrathmark unit tests.
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public abstract class WrathmarkTestBase
    {
        protected static readonly string ProfilesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(WrathmarkResultParserTests)).Location),
            "Examples",
            "Wrathmark");

        /// <summary>
        /// Gets the example file path for tests.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The full file path.</returns>
        protected static string GetExampleFileForTests(string fileName)
        {
            string retVal = Path.Combine(ProfilesDirectory, fileName);

            Debug.Assert(File.Exists(retVal), $"The specified file '{fileName}' does not exist.");

            return retVal;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WrathmarkWorkloadExecutor" /> class.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <returns>An instance of the <see cref="WrathmarkWorkloadExecutor" /> class based on the <paramref name="fixture"/>.</returns>
        protected WrathmarkWorkloadExecutor WorkloadExecutorFactory(MockFixture fixture)
        {
            return new WrathmarkWorkloadExecutor(fixture.Dependencies, fixture.Parameters);
        }

        internal static class Constants
        {
            public const string BenchmarkResults = "BenchWrathmark.txt";
            public const string FullTestResults = "TestWrathmark.txt";

            public static class Profile
            {
                public static class Parameters
                {
                    public const string PackageName = "wrathmark";
                    public const string DotNetSdkPackageName = "dotnetsdk";
                    public const string TargetFramework = "net8.0";
                    public const string Subfolder = "wrath-sharp-std-arrays";
                    public const string WrathmarkArgs = "xlt ..\\endgame.txt";
                }

                public static class Dependencies
                {
                    public const string DotNetSdkPackageName = "dotnetsdk";
                    public const string WrathmarkPackageName = "wrathmark";
                }

                public static class Actions
                {
                    public static class Parameters
                    {
                        public const string Scenario = "WrathmarkBenchmark";
                    }
                }
            }
        }
    }
}