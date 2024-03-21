// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using NUnit.Framework;

    /// <summary>
    /// This class is used to determine if the integration tests will be executed or not. To execute integration
    /// tests, a specific environment variable must be set on the system (i.e. VC_ENABLE_INTEGRATION_TESTS = true).
    /// If this environment variable is not set, none of the integration tests will be executed. They will each be
    /// ignored instead. This is a safety mechanism to ensure that users can safely run all tests with an intent to 
    /// run just unit or functional tests. Integration tests run against live resources and are generally meant for
    /// 1-off debugging of code at the local desktop (vs. automated test verification).
    /// </summary>
    [SetUpFixture]
    internal class GlobalSetup
    {
        [OneTimeSetUp]
        public static void IsEnabled()
        {
            // const string variableName = "VC_ENABLE_INTEGRATION_TESTS";

            //if (!string.Equals(Environment.GetEnvironmentVariable(variableName), "true", StringComparison.OrdinalIgnoreCase))
            //{
            //    Assert.Ignore(
            //        $"Integration tests ignored. Set the environment variable '{variableName}' to true to enable support " +
            //        $"for running integrations tests locally. The environment variable can be set in the user-level or system-level variables on " +
            //        $"the operating system. See the 'GlobalSetup.cs' file for details.");
            //}
        }
    }
}
