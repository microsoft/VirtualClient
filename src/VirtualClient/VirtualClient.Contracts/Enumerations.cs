// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// Enumeration defines the type of profile component.
    /// </summary>
    public enum ComponentType : int
    {
        /// <summary>
        /// Undefined component type.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Profile action.
        /// </summary>
        Action = 1,

        /// <summary>
        /// Profile dependency.
        /// </summary>
        Dependency = 2,

        /// <summary>
        /// Profile monitor.
        /// </summary>
        Monitor = 3
    }

    /// <summary>
    /// Enumeration describes error reasons. Note that error reasons with
    /// a value greater than or equal to 500 indicate failures for which will
    /// prevent the application from functioning correctly at any point.
    /// </summary>
    public enum ErrorReason : int
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// The performance counter does not exist.
        /// </summary>
        PerformanceCounterNotFound = 100,

        /// <summary>
        /// An unexpected anomaly/error happened in the execution of the
        /// monitor. These are entirely unexpected scenarios.
        /// </summary>
        MonitorUnexpectedAnomaly = 101,

        /// <summary>
        /// Disk information unavailable.
        /// </summary>
        DiskInformationNotAvailable = 300,

        /// <summary>
        /// Disk filter not supported.
        /// </summary>
        DiskFilterNotSupported = 301,

        /// <summary>
        /// The workload failed during execution.
        /// </summary>
        WorkloadFailed = 315,

        /// <summary>
        /// The workload results/results file was not found.
        /// </summary>
        WorkloadResultsNotFound = 314,

        /// <summary>
        /// The workload failed during execution.
        /// </summary>
        WorkloadResultsParsingFailed = 316,

        /// <summary>
        /// The monitor failed during execution.
        /// </summary>
        MonitorFailed = 318,

        /// <summary>
        /// The HTTP server/service returned a non-success response.
        /// </summary>
        HttpNonSuccessResponse = 320,

        /// <summary>
        /// The HTTP server/service returned a BadRequest/400 response.
        /// </summary>
        Http400BadRequestResponse = 321,

        /// <summary>
        /// The HTTP server/service returned a NotFound/404 response.
        /// </summary>
        Http404NotFoundResponse = 322,

        /// <summary>
        /// The HTTP server/service returned a Conflict/409 response.
        /// </summary>
        Http409ConflictResponse = 323,

        /// <summary>
        /// The HTTP server/service returned a Forbidden/403 response.
        /// </summary>
        Http403ForbiddenResponse = 324,

        /// <summary>
        /// The HTTP server/service returned a PreconditionFailed/412 response.
        /// </summary>
        Http412PreconditionFailedResponse = 325,

        // VC Error Handling (400s):
        // --------------------------------------------------------------------------
        // Exceptions with an error value between 400 - 499 are serious errors that
        // prevent VC from running workloads correctly. However, they also represent
        // situations that may be transient and from which can be recovered in a period
        // of time.

        /// <summary>
        /// Invalid test results.
        /// </summary>
        InvalidResults = 400,

        /// <summary>
        /// Polling for a given state failed.
        /// </summary>
        ApiStatePollingTimeout = 410,

        /// <summary>
        /// An API call/request failed.
        /// </summary>
        ApiRequestFailed = 411,

        /// <summary>
        /// An attempt to get the total system/OS memory failed.
        /// </summary>
        SystemMemoryReadFailed = 420,

        /// <summary>
        /// An unexpected anomaly/error happened in the execution of the
        /// workload. These are entirely unexpected scenarios.
        /// </summary>
        WorkloadUnexpectedAnomaly = 430,

        /// <summary>
        /// A file upload notification write failed.
        /// </summary>
        FileUploadNotificationCreationFailed = 440,

        // VC Error Handling (500s):
        // --------------------------------------------------------------------------
        // Exceptions with an error value >= 500 are exceptions from which the
        // VC cannot recover at any point (i.e. they are not transient). Errors
        // in this category will cause the VC to exit promptly.

        /// <summary>
        /// The workload profile does not exist.
        /// </summary>
        ProfileNotFound = 500,

        /// <summary>
        /// The workload profile definition is invalid.
        /// </summary>
        InvalidProfileDefinition = 501,

        /// <summary>
        /// The feature/option is not supported.
        /// </summary>
        NotSupported = 502,

        /// <summary>
        /// The OS/system platform is not supported.
        /// </summary>
        PlatformNotSupported = 503,

        /// <summary>
        /// The processor architecture is not supported.
        /// </summary>
        ProcessorArchitectureNotSupported = 504,

        /// <summary>
        /// A required dependency description is invalid/incomplete.
        /// </summary>
        DependencyDescriptionInvalid = 505,

        /// <summary>
        /// A required dependency failed installation.
        /// </summary>
        DependencyInstallationFailed = 506,

        /// <summary>
        /// A required dependency not found.
        /// </summary>
        DependencyNotFound = 507,

        /// <summary>
        /// The environment that the Virtual Client is executing on
        /// does not have sufficient resources to run.
        /// </summary>
        EnvironmentIsInsufficent = 508,

        /// <summary>
        /// One or more instructions provided are invalid/incorrect.
        /// </summary>
        InstructionsNotValid = 510,

        /// <summary>
        /// One or more instructions required for the execution of the profile
        /// were not included.
        /// </summary>
        InstructionsNotProvided = 511,

        /// <summary>
        /// Required license information is either missing or does not exist.
        /// </summary>
        InvalidOrMissingLicense = 512,

        /// <summary>
        /// Disk format operations failed.
        /// </summary>
        DiskFormatFailed = 515,

        /// <summary>
        /// Disk mount operations failed.
        /// </summary>
        DiskMountFailed = 516,

        /// <summary>
        /// A system-required operation failed.
        /// </summary>
        SystemOperationFailed = 517,

        /// <summary>
        /// Not a supported Linux distribution.
        /// </summary>
        LinuxDistributionNotSupported = 518,

        /// <summary>
        /// The network target/endpoint does not exist.
        /// </summary>
        NetworkTargetDoesNotExist = 520,

        /// <summary>
        /// An expected workload executable was not found.
        /// </summary>
        WorkloadNotFound = 525,

        /// <summary>
        /// A required dependency for a workload is missing and the workload cannot
        /// run without it.
        /// </summary>
        WorkloadDependencyMissing = 526,

        /// <summary>
        /// A workload or functional test considered to be critical to the outcome of the
        /// run failed.
        /// </summary>
        CriticalWorkloadFailure = 527,

        /// <summary>
        /// The package store was not defined on the command line.
        /// </summary>
        PackageStoreNotDefined = 530,

        /// <summary>
        /// The Virtual Client API service failed to startup.
        /// </summary>
        ApiStartupFailed = 535,

        /// <summary>
        /// Permissions issue. The agent/caller is not authorized to perform the action.
        /// </summary>
        Unauthorized = 540,

        /// <summary>
        /// The environment layout is null.
        /// </summary>
        EnvironmentLayoutNotDefined = 550,

        /// <summary>
        /// Environment layout is not valid or is missing required information.
        /// </summary>
        LayoutInvalid = 551,

        /// <summary>
        /// IP address present on layout does not matches with the IPAddress of the machine.
        /// </summary>
        LayoutIPAddressDoesNotMatch = 552,

        /// <summary>
        /// The layout does not contain any instance whose name matches with agent id.
        /// </summary>
        EnvironmentLayoutClientInstancesNotFound = 553,

        /// <summary>
        /// The layout contains multiple instances whose name matches with agent id.
        /// </summary>
        EnvironmentLayoutClientInstanceDuplicates = 554,

        /// <summary>
        /// An extensions assembly is not valid for the current Virtual Client
        /// runtime.
        /// </summary>
        ExtensionAssemblyInvalid = 580,

        /// <summary>
        /// Duplicate extensions binaries or profiles were found during startup.
        /// </summary>
        DuplicateExtensionsFound = 581,

        /// <summary>
        /// Duplicate packages were found during startup.
        /// </summary>
        DuplicatePackagesFound = 582,

        /// <summary>
        /// Not a supported version.
        /// </summary>
        VersionNotSupported = 590
    }

    /// <summary>
    /// The type of aggregation to apply to a set of metrics.
    /// </summary>
    public enum MetricAggregateType
    {
        /// <summary>
        /// Capture the average of the metric values.
        /// </summary>
        Average,

        /// <summary>
        /// Captures the maximum of the metric values.
        /// </summary>
        Max,

        /// <summary>
        /// Captures the median of the metric values.
        /// </summary>
        Median,

        /// <summary>
        /// Captures the minimum of the metric values.
        /// </summary>
        Min,

        /// <summary>
        /// Simply capture the metric value.
        /// </summary>
        Raw,

        /// <summary>
        /// Capture the sum of the metric values.
        /// </summary>
        Sum
    }

    /// <summary>
    /// Types of archive files.
    /// </summary>
    public enum ArchiveType
    {
        /// <summary>
        /// The archive type is not defined.
        /// </summary>
        Undefined,

        /// <summary>
        /// File extension .tar
        /// </summary>
        Tar,

        /// <summary>
        /// File extension .tar.gz, .tgz or .tar.gzip
        /// </summary>
        Tgz,

        /// <summary>
        /// File extension .zip
        /// </summary>
        Zip,
    }

    /// <summary>
    /// Represents the current status of a client-side or server-side component.
    /// </summary>
    public enum ClientServerStatus
    {
        /// <summary>
        /// Status = undefined/unknown.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Status = ready to receive instructions.
        /// </summary>
        Ready,

        /// <summary>
        /// Status = execution of instructions/workload started.
        /// </summary>
        ExecutionStarted,

        /// <summary>
        /// Status = execution of instructions/workload completed.
        /// </summary>
        ExecutionCompleted,

        /// <summary>
        /// Status = execution of instructions/workload failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Status = operational reset completed.
        /// </summary>
        ResetCompleted,

        /// <summary>
        /// Status = operational reset in progress.
        /// </summary>
        ResetInProgress
    }

    /// <summary>
    /// Disk file system types (e.g. MSDOS, NTFS).
    /// </summary>
    public enum FileSystemType
    {
        /// <summary>
        /// No file system type
        /// </summary>
        None,

        /// <summary>
        /// bfs
        /// </summary>
        Bfs,

        /// <summary>
        /// ext2
        /// </summary>
        Ext2,

        /// <summary>
        /// ext3
        /// </summary>
        Ext3,

        /// <summary>
        /// ext4
        /// </summary>
        Ext4,

        /// <summary>
        /// minix
        /// </summary>
        Minix,

        /// <summary>
        /// msdos
        /// </summary>
        MsDos,

        /// <summary>
        /// ntfs
        /// </summary>
        Ntfs,

        /// <summary>
        /// vFat
        /// </summary>
        VFat,

        /// <summary>
        /// xfs
        /// </summary>
        Xfs,

        /// <summary>
        /// xiafs
        /// </summary>
        Xiafs
    }

    /// <summary>
    /// Defines a type of instructions that is sent or received within the
    /// runtime of a Virtual Client process.
    /// </summary>
    public enum InstructionsType
    {
        /// <summary>
        /// Instruction type not defined.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Instructions to execute profiling.
        /// </summary>
        Profiling = 1,

        /// <summary>
        /// Instructions that represent a client/server reset request.
        /// </summary>
        ClientServerReset = 2,

        /// <summary>
        /// Instructions that represent a client/server request to start an operation/process.
        /// </summary>
        ClientServerStartExecution = 3,

        /// <summary>
        /// Instructions that represent a client/server request to stop an operation/process.
        /// </summary>
        ClientServerStartStopExecution = 4,

        /// <summary>
        /// Instructions that represent a client/server request to exit the server-side proxy/process.
        /// </summary>
        ClientServerExit = 5
    }

    /// <summary>
    /// Defines different Linux distribution
    /// </summary>
    public enum LinuxDistribution
    {
        /// <summary>
        /// Unkwown distribution.
        /// </summary>
        Unknown,

        /// <summary>
        /// Ubuntu
        /// </summary>
        Ubuntu,

        /// <summary>
        /// RHEL7
        /// </summary>
        RHEL7,

        /// <summary>
        /// RHEL8
        /// </summary>
        RHEL8,

        /// <summary>
        /// Debian
        /// </summary>
        Debian,

        /// <summary>
        /// CentOS7
        /// </summary>
        CentOS7,

        /// <summary>
        /// CentOS8
        /// </summary>
        CentOS8,

        /// <summary>
        /// SUSE
        /// </summary>
        SUSE,

        /// <summary>
        /// Flatcar
        /// https://www.flatcar.org/
        /// </summary>
        Flatcar,

        /// <summary>
        /// Fedora
        /// </summary>
        Fedora,

        /// <summary>
        /// MSFT internal CentOS based distro AzLinux (Previously Mariner)
        /// </summary>
        AzLinux,

        /// <summary>
        /// AwsLinux
        /// </summary>
        AwsLinux
    }

    /// <summary>
    /// Defines the type of data that the log represents.
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// Undefined data type.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Standard log/trace data.
        /// </summary>
        Trace = 101,

        /// <summary>
        /// Error data.
        /// </summary>
        Error = 102,

        /// <summary>
        /// System events data.
        /// </summary>
        SystemEvent = 103,

        /// <summary>
        /// Test metrics/results data.
        /// </summary>
        Metric = 105,

        /// <summary>
        /// Collection of metrics.
        /// </summary>
        MetricsCollection = 106
    }

    /// <summary>
    /// Defines a category of the Virtual Client metadata data contract
    /// (e.g. CPU, Memory, System).
    /// </summary>
    public enum MetadataContractCategory
    {
        /// <summary>
        /// The default metadata category. In telemetry output, the metadata section 
        /// name will be: 'metadata'.
        /// </summary>
        Default,

        /// <summary>
        /// Metadata related to dependencies/packages that are installed on the system.
        /// In telemetry output, the metadata section name will be: 'metadata_dependencies'.
        /// </summary>
        Dependencies,

        /// <summary>
        /// Metadata related to the the host and operating system. In telemetry output, the metadata 
        /// section name will be: 'metadata_host'.
        /// </summary>
        Host,

        /// <summary>
        /// Metadata related to the Virtual Client platform runtime
        /// (e.g. profile, timeout, iterations). In telemetry output, the metadata section 
        /// name will be: 'metadata_runtime'.
        /// </summary>
        Runtime,

        /// <summary>
        /// Metadata related to the Virtual Client runtime scenario 
        /// (e.g. workload, monitor, dependency scenario). In telemetry output, 
        /// the metadata section name will be: 'metadata_scenario'.
        /// </summary>
        Scenario,

        /// <summary>
        /// Metadata related to the Virtual Client runtime scenario 
        /// (e.g. workload, monitor, dependency scenario) as "extensions" to
        /// that scenario definition. In telemetry output, the metadata section name 
        /// will be: 'metadata_scenario_ext'.
        /// </summary>
        ScenarioExtensions
    }

    /// <summary>
    /// Defines the scope of a metadata contract property (e.g. the lifetime of the property
    /// within the execution of the application).
    /// </summary>
    public enum MetadataContractScope
    {
        /// <summary>
        /// The metadata property should be persisted only during the execution of a single
        /// component within a profile.
        /// </summary>
        Component,

        /// <summary>
        /// The metadata property should be persisted throughout the entire lifetime
        /// of the application's runtime.
        /// </summary>
        Lifetime
    }

    /// <summary>
    /// Defines the relationship of the metric value and it's relative meaning.
    /// </summary>
    public enum MetricRelativity
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// HigherIsBetter (i.e. higher values are considered better outcomes).
        /// </summary>
        HigherIsBetter = 1,

        /// <summary>
        /// LowerIsBetter (i.e. lower values are considered better outcomes).
        /// </summary>
        LowerIsBetter = 2
    }

    /// <summary>
    /// Disk partition types (e.g. GPT, MSDOS).
    /// </summary>
    public enum PartitionType
    {
        /// <summary>
        /// bsd
        /// </summary>
        Bsd,

        /// <summary>
        /// msdos
        /// </summary>
        MsDos,

        /// <summary>
        /// gpt
        /// </summary>
        Gpt,
    }

    /// <summary>
    /// Defines different modes that can be used when profiling the system.
    /// </summary>
    public enum ProfilingMode
    {
        /// <summary>
        /// None - Do not profile the system.
        /// </summary>
        None,

        /// <summary>
        /// Interval - Profile the system on a consistent interval.
        /// </summary>
        Interval,

        /// <summary>
        /// OnDemand - Profile the system only when requested.
        /// </summary>
        OnDemand
    }
}
