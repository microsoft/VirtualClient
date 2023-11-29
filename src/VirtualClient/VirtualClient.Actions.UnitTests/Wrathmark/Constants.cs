// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
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