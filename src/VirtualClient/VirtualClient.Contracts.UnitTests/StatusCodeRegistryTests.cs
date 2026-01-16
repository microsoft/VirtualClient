namespace VirtualClient.Contracts
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class StatusCodeRegistryTests
    {
        [Test]
        [TestCase(ErrorReason.DependencyDescriptionInvalid, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.DuplicateExtensionsFound, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.DuplicatePackagesFound, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.EnvironmentIsInsufficient, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.ExtensionAssemblyInvalid, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.InvalidOrMissingLicense, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.InvalidProfileDefinition, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.LinuxDistributionNotSupported, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.NotSupported, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.PlatformNotSupported, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.ProcessorArchitectureNotSupported, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.Unauthorized, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.VersionNotSupported, StatusCodeRegistry.StatusCodeBase.ConfigurationError)]
        [TestCase(ErrorReason.ApiRequestFailed, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.ApiStartupFailed, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.ApiStatePollingTimeout, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.DependencyInstallationFailed, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.DependencyNotFound, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.Http400BadRequestResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.Http403ForbiddenResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.Http404NotFoundResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.Http409ConflictResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.Http412PreconditionFailedResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.HttpNonSuccessResponse, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.NetworkTargetDoesNotExist, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.WorkloadDependencyMissing, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.WorkloadNotFound, StatusCodeRegistry.StatusCodeBase.OrchestrationError)]
        [TestCase(ErrorReason.DiskFormatFailed, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.DiskInformationNotAvailable, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.DiskMountFailed, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.FileUploadNotificationCreationFailed, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.PerformanceCounterNotFound, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.SystemMemoryReadFailed, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.SystemOperationFailed, StatusCodeRegistry.StatusCodeBase.SystemError)]
        [TestCase(ErrorReason.CriticalWorkloadFailure, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.InvalidResults, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.MonitorFailed, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.MonitorUnexpectedAnomaly, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.WorkloadFailed, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.WorkloadResultsNotFound, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.WorkloadResultsParsingFailed, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.WorkloadUnexpectedAnomaly, StatusCodeRegistry.StatusCodeBase.ToolsetError)]
        [TestCase(ErrorReason.ContentStoreNotDefined, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.DiskFilterNotSupported, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.InstructionsNotProvided, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.InstructionsNotValid, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.InvalidCertificate, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.LayoutInvalid, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.LayoutNotDefined, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.PackageStoreNotDefined, StatusCodeRegistry.StatusCodeBase.UsageError)]
        [TestCase(ErrorReason.ProfileNotFound, StatusCodeRegistry.StatusCodeBase.UsageError)]
        public void StatusCodeRegistryMapsExistingErrorCodesToExpectedStatusCodes(int errorCode, int expectedStatusCodeBase)
        {
            int expectedStatusCode = expectedStatusCodeBase + errorCode;
            int actualStatusCode = StatusCodeRegistry.GetStatusCode(errorCode);

            Assert.AreEqual(
                expectedStatusCode,
                actualStatusCode,
                $"Status code does not match. Status code for error/exit code '{errorCode}' is expected to be '{expectedStatusCode}' but was '{actualStatusCode}' instead.");
        }

        [Test]
        [TestCase(1)]
        [TestCase(-1)]
        [TestCase(12345678)]
        [TestCase(-12345678)]
        public void StatusCodeRegistryEmitsTheExpectedStatusCodeForUnmappedExitCodes_1(int unmappedExitCode)
        {
            int expectedStatusCode = StatusCodeRegistry.StatusCodeBase.CodeError + 1;
            int actualStatusCode = StatusCodeRegistry.GetStatusCode(unmappedExitCode);

            Assert.AreEqual(
                expectedStatusCode,
                actualStatusCode,
                $"Status code does not match. Status code for error/exit code '{unmappedExitCode}' is expected to be '{expectedStatusCode}' but was '{actualStatusCode}' instead.");
        }

        [Test]
        public void StatusCodeRegistryEmitsTheExpectedStatusCodeForUnmappedExitCodes_2()
        {
            int expectedStatusCode = StatusCodeRegistry.StatusCodeBase.CodeError + 1;
            int actualStatusCode = StatusCodeRegistry.GetStatusCode((int)ErrorReason.Undefined);

            Assert.AreEqual(
                expectedStatusCode,
                actualStatusCode,
                $"Status code does not match. Status code for undefined error reasons should be mapped as a code error.");
        }

        [Test]
        public void StatusCodeRegistryHasStatusCodesMappedForAllErrorReasons()
        {
            // Note to the Developer:
            // Every VirtualClient.Contracts -> Enumerations.cs (ErrorReason) MUST have a status code mapped to the
            // error reason. Status code mappings are defined in the VirtualClient.Contracts -> StatusCodeRegistry.cs
            // file.
            //
            // Additionally note that the default mapping should be StatusCodeBase.CodeError + 1. No specific exit codes
            // should be mapped to this particular status code base. Leave it as-is.

            ErrorReason[] allErrorReasons = Enum.GetValues<ErrorReason>();
            foreach (ErrorReason reason in allErrorReasons)
            {
                if (reason != ErrorReason.Undefined)
                {
                    int mappedStatusCode = StatusCodeRegistry.GetStatusCode((int)reason);

                    Assert.IsTrue(
                        mappedStatusCode > 0,
                        $"Status Code not mapped correctly for '{reason.ToString()}'.");

                    Assert.IsTrue(
                        mappedStatusCode != StatusCodeRegistry.StatusCodeBase.CodeError + 1,
                        $"Status Code not mapped correctly for '{reason.ToString()}'. ALL code errors should be left as default.");
                }
            }
        }
    }
}
