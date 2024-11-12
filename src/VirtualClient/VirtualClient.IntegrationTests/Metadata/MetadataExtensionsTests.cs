// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using NUnit.Framework;
    using PEFile;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    [TestFixture]
    [Category("Integration")]
    internal class MetadataExtensionsTests
    {
        private string eventHubAccessPolicy;

        [SetUp]
        public void SetupTest()
        {
            const string variableName = "VC_EVENT_HUBS_ACCESS_POLICY";
            this.eventHubAccessPolicy = Environment.GetEnvironmentVariable(variableName);

            if (string.IsNullOrWhiteSpace(this.eventHubAccessPolicy))
            {
                Assert.Ignore(
                    $"Integration tests ignored. Set the environment variable '{variableName}' to true to enable support " +
                    $"for emitting telemetry to a target Event Hub. The environment variable can be set in the user-level or system-level variables on " +
                    $"the operating system.");
            }
        }

        [Test]
        public void MetadataContractTelemetryExample()
        {
            // NOTE:
            // Make sure to set the Event Hubs access policy in the environment variable as noted above.
            EventHubTelemetryChannel channel = DependencyFactory.CreateEventHubTelemetryChannel(
                this.eventHubAccessPolicy,
                "telemetry-logs");

            EventHubTelemetryLogger logger = new EventHubTelemetryLogger(channel, LogLevel.Trace);

            string experimentId = Guid.NewGuid().ToString();
            string profile = "DOES-NOT-EXIST-INTEGRATION-TEST-3.json";
            string platformArchitecture = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                Environment.MachineName,
                experimentId,
                new PlatformSpecifics(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture),
                NullLogger.Instance);

            // Persisted properties and metadata contracts are applied to the telemetry output
            // by the Virtual Client. The properties within are included in the custom dimensions
            // section of the telemetry.
            EventContext.PersistentProperties.AddRange(new Dictionary<string, object>
            {
                ["clientId"] = Environment.MachineName.ToLowerInvariant(),
                ["experimentId"] = experimentId.ToLowerInvariant(),
                ["appVersion"] = "1.0.00000.0000",
                ["appPlatformVersion"] = "1.0.0",
                ["executionArguments"] = $"--profile={profile}",
                ["executionSystem"] = "Test",
                ["operatingSystemPlatform"] = Environment.OSVersion.Platform.ToString(),
                ["platformArchitecture"] = platformArchitecture,
                ["executionProfile"] = $"{profile} ({platformArchitecture})",
                ["executionProfileName"] = profile,
                ["executionProfilePath"] = $"S:\\any\\path\\to\\{profile}",
                ["executionProfileParameters"] = $"Parameter1=Value1,,,Parameter2=Value2",
                ["executionProfileDescription"] = "Profile does not exist. This is a test.",
                ["profileFriendlyName"] = "Profile does not exist.",
            });

            // Metadata that is added to the output telemetry structure.
            IDictionary<string, object> metadata = new Dictionary<string, object>
            {
                { "agentId", Environment.MachineName },
                { "experimentId", experimentId },
                { "integrationTest", true },
                { "testName", nameof(this.MetadataContractTelemetryExample) },
                { "testRevision", 2 }
            };

            IDictionary<string, object> hostMetadata = systemManagement.GetHostMetadataAsync(logger)
                .GetAwaiter().GetResult();

            IDictionary<string, object> hwPartsMetadata = systemManagement.GetHardwarePartsMetadataAsync(logger)
                .GetAwaiter().GetResult();

            hostMetadata.AddRange(hwPartsMetadata);

            MetadataContract.Persist(
                metadata,
                MetadataContractCategory.Default);

            MetadataContract.Persist(
                hostMetadata,
                MetadataContractCategory.Host);

            MetadataContract.Persist(
                new Dictionary<string, object>
                {
                    { "exitWait", TimeSpan.FromMinutes(30).ToString() },
                    { "layout", null },
                    { "logToFile", true },
                    { "iterations", null },
                    { "profiles", "PERF-" },
                    { "timeout", TimeSpan.FromDays(1).ToString() },
                    { "timeoutScope", DeterminismScope.IndividualAction.ToString() },
                    { "scenarios", "Scenario1,Scenario2,-Dependency1" },
                },
                MetadataContractCategory.Runtime);

            MetadataContract.Persist(
                new Dictionary<string, object>
                {
                    { "compilerVersion_cc", "10.5.0" },
                    { "compilerVersion_gcc", "10.5.0" },
                    { "compilerVersion_gfortran", "10.5.0" },
                    { "package_speccpu2017", "speccpu.2017.1.1.8.zip" },
                },
                MetadataContractCategory.Dependencies,
                true);

            MetadataContract contract = new MetadataContract();

            contract.Add(
                new Dictionary<string, object>
                {
                    { "scenario", "ExecuteSPECBenchmark" },
                    { "compilerVersion", "10" },
                    { "specProfile", "intrate" },
                    { "packageName", "speccpu2017" },
                    { "runPeak", false },
                    { "baseOptimizingFlags", "-g -O3 -march=native" },
                    { "peakOptimizingFlags", "-g -Ofast -march=native -flto" },
                },
                MetadataContractCategory.Scenario,
                true);

            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            contract.Apply(telemetryContext);

            for (int i = 0; i < 10; i++)
            {
                logger.LogMessage($"IntegrationTests.{nameof(this.MetadataContractTelemetryExample)}", telemetryContext);
            }

            (logger as IFlushableChannel).Flush(TimeSpan.FromMinutes(2));
        }
    }
}
