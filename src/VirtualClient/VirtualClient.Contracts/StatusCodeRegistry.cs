namespace VirtualClient.Contracts
{
    /// <summary>
    /// Provides a registry of exit/status codes.
    /// <list type="bullet">
    /// <item>
    /// <description><b>CodeError</b>: 110000 - 119999</description>
    /// </item>
    /// <item>
    /// <description><b>OrchestrationError</b>: 210000 - 219999</description>
    /// </item>
    /// <item>
    /// <description><b>ToolsetError</b>: 211000 - 211999<br/>(Note that this is a subset of OrchestrationError)</description>
    /// </item>
    /// <item>
    /// <description><b>ConfigurationError</b>: 310000 - 319999</description>
    /// </item>
    /// <item>
    /// <description><b>UsageError</b>: 311000 - 311999<br/>(Note that this is a subset of ConfigurationError)</description>
    /// </item>
    /// <item>
    /// <description><b>SystemError</b>: 410000 - 419999</description>
    /// </item>
    /// <item>
    /// <description><b>ThresholdOrKpiError</b>: 510000 - 519999</description>
    /// </item>
    /// </list>
    /// </summary>
    internal static class StatusCodeRegistry
    {
        /// <summary>
        /// Returns a status code to represent the exit code provided.
        /// </summary>
        public static int GetStatusCode(int exitCode)
        {
            int statusCode = 0;

            switch (exitCode)
            {
                // e.g.
                // ProfileNotDefined = 500
                // 
                // Exit Code = 500
                // Status Code Base = UsageError = 311000
                //
                // Status Code = 311500
                case (int)ErrorReason.CriticalWorkloadFailure:
                case (int)ErrorReason.InvalidResults:
                case (int)ErrorReason.MonitorFailed:
                case (int)ErrorReason.MonitorUnexpectedAnomaly:
                case (int)ErrorReason.WorkloadFailed:
                case (int)ErrorReason.WorkloadResultsNotFound:
                case (int)ErrorReason.WorkloadResultsParsingFailed:
                case (int)ErrorReason.WorkloadUnexpectedAnomaly:
                    statusCode = StatusCodeBase.ToolsetError + exitCode;
                    break;

                case (int)ErrorReason.ContentStoreNotDefined:
                case (int)ErrorReason.DiskFilterNotSupported:
                case (int)ErrorReason.InstructionsNotProvided:
                case (int)ErrorReason.InstructionsNotValid:
                case (int)ErrorReason.InvalidCertificate:
                case (int)ErrorReason.LayoutInvalid:
                case (int)ErrorReason.LayoutNotDefined:
                case (int)ErrorReason.PackageStoreNotDefined:
                case (int)ErrorReason.ProfileNotFound:
                    statusCode = StatusCodeBase.UsageError + exitCode;
                    break;

                case (int)ErrorReason.DependencyDescriptionInvalid:
                case (int)ErrorReason.DuplicateExtensionsFound:
                case (int)ErrorReason.DuplicatePackagesFound:
                case (int)ErrorReason.EnvironmentIsInsufficient:
                case (int)ErrorReason.ExtensionAssemblyInvalid:
                case (int)ErrorReason.InvalidOrMissingLicense:
                case (int)ErrorReason.InvalidProfileDefinition:
                case (int)ErrorReason.LinuxDistributionNotSupported:
                case (int)ErrorReason.NotSupported:
                case (int)ErrorReason.PlatformNotSupported:
                case (int)ErrorReason.ProcessorArchitectureNotSupported:
                case (int)ErrorReason.Unauthorized:
                case (int)ErrorReason.VersionNotSupported:
                    statusCode = StatusCodeBase.ConfigurationError + exitCode;
                    break;

                case (int)ErrorReason.ApiRequestFailed:
                case (int)ErrorReason.ApiStartupFailed:
                case (int)ErrorReason.ApiStatePollingTimeout:
                case (int)ErrorReason.DependencyInstallationFailed:
                case (int)ErrorReason.DependencyNotFound:
                case (int)ErrorReason.Http400BadRequestResponse:
                case (int)ErrorReason.Http403ForbiddenResponse:
                case (int)ErrorReason.Http404NotFoundResponse:
                case (int)ErrorReason.Http409ConflictResponse:
                case (int)ErrorReason.Http412PreconditionFailedResponse:
                case (int)ErrorReason.HttpNonSuccessResponse:
                case (int)ErrorReason.NetworkTargetDoesNotExist:
                case (int)ErrorReason.WorkloadDependencyMissing:
                case (int)ErrorReason.WorkloadNotFound:
                    statusCode = StatusCodeBase.OrchestrationError + exitCode;
                    break;

                case (int)ErrorReason.DiskFormatFailed:
                case (int)ErrorReason.DiskInformationNotAvailable:
                case (int)ErrorReason.DiskMountFailed:
                case (int)ErrorReason.FileUploadNotificationCreationFailed:
                case (int)ErrorReason.PerformanceCounterNotFound:
                case (int)ErrorReason.SystemMemoryReadFailed:
                case (int)ErrorReason.SystemOperationFailed:
                    statusCode = StatusCodeBase.SystemError + exitCode;
                    break;

                default:
                    statusCode = StatusCodeBase.CodeError + 1;
                    break;
            }

            return statusCode;
        }

        /// <summary>
        /// Defines application exit status codes for Virtual Client. Virtual Client status codes are as follows:
        /// </summary>
        internal static class StatusCodeBase
        {
            /// <summary>
            /// Error due to code bug or issue.
            /// </summary>
            public const int CodeError = 110000;

            /// <summary>
            /// Error due to orchestration issue.
            /// </summary>
            public const int OrchestrationError = 210000;

            /// <summary>
            /// Error due to a toolset issue. Note that this is a subset of the OrchestrationError.
            /// </summary>
            public const int ToolsetError = 211000;

            /// <summary>
            /// Error due to configuration issue.
            /// </summary>
            public const int ConfigurationError = 310000;

            /// <summary>
            /// Error due to usage issue. Note that this is a subset of the ConfigurationError.
            /// </summary>
            public const int UsageError = 311000;

            /// <summary>
            /// Error due to System issue.
            /// </summary>
            public const int SystemError = 410000;

            /// <summary>
            /// Error due to threshold or KPI issue.
            /// </summary>
            public const int ThresholdOrKpiError = 510000;
        }
    }
}
