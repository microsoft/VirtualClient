// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Contracts.Metadata;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The Elasticsearch Rally Client workload executor.
    /// </summary>
    public class ElasticsearchRallyClientExecutor : ElasticsearchRallyBaseExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ElasticsearchRallyClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public ElasticsearchRallyClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The Elasticsearch Distribution Version.
        /// </summary>
        public string DistributionVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.DistributionVersion), "8.0.0");
            }
        }

        /// <summary>
        /// The track targeted for run by Rally.
        /// </summary>
        public string TrackName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.TrackName));
            }
        }

        /// <summary>
        /// TestMode indicates whether to run Rally in test mode.
        /// </summary>
        public bool RallyTestMode
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(ElasticsearchRallyClientExecutor.RallyTestMode));
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteClient", telemetryContext.Clone(), async () =>
            {
                ElasticsearchRallyState state = await this.StateManager.GetStateAsync<ElasticsearchRallyState>(nameof(ElasticsearchRallyState), cancellationToken)
                    ?? new ElasticsearchRallyState();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                ClientInstance clientInstance = this.GetLayoutClientInstances(ClientRole.Server).FirstOrDefault();
                IPAddress.TryParse(clientInstance.IPAddress, out IPAddress serverIPAddress);
                string targetHost = clientInstance?.IPAddress;
                if (string.IsNullOrEmpty(targetHost))
                {
                    throw new WorkloadException(
                        $"Elasticsearch Rally Client could not determine the target host from the layout.",
                        ErrorReason.LayoutInvalid);
                }

                string user = this.PlatformSpecifics.GetLoggedInUser();
                int port = this.Port;                
                string trackName = this.TrackName;
                string dataDirectory = await this.GetDataDirectoryAsync(cancellationToken);

                string rallySharedStoragePath = $"{dataDirectory}/esrally"; // Used for large, shareable, reusable data
                string rallyUserHomePath = $"/home/{user}"; // Used for user‑specific results and metadata

                if (!state.RallyConfigured)
                {
                    this.StartRallyClient(
                        user,
                        targetHost,
                        port,
                        trackName,
                        rallySharedStoragePath,
                        rallyUserHomePath,
                        telemetryContext.Clone(), 
                        cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        state.RallyConfigured = true;
                        await this.StateManager.SaveStateAsync<ElasticsearchRallyState>(nameof(ElasticsearchRallyState), state, cancellationToken);
                    }
                }

                this.RunRallyClient(
                    user,
                    targetHost,
                    port,
                    trackName,
                    rallySharedStoragePath,
                    rallyUserHomePath,
                    telemetryContext.Clone(),
                    cancellationToken);
            });

            return;
        }

        /// <summary>
        /// Reads all lines from the specified report file and returns them as an array of strings.
        /// </summary>
        /// <param name="reportPath">The full path to the report file to read. Cannot be null or an empty string.</param>
        /// <returns>An array of strings, each representing a line from the report file. The array will be empty if the file
        /// contains no lines.</returns>
        protected virtual string[] ReadReportLines(string reportPath)
        {
            return System.IO.File.ReadAllLines(reportPath);
        }

        /// <summary>
        /// Checks whether the specified server is available by attempting to connect to the given host and port.
        /// </summary>
        /// <remarks>This method waits up to timeout seconds before performing the availability check. Override
        /// this method to customize the server availability check logic in derived classes.</remarks>
        /// <param name="telemetryContext">The context for telemetry and logging associated with this operation.</param>
        /// <param name="targetHost">The DNS name or IP address of the server to check for availability. Cannot be null or empty.</param>
        /// <param name="port">The network port number on the target server to check. Must be a valid TCP port number.</param>
        /// <param name="timeout">The amount of time in milliseconds to wait before performing the availability check. Default is 30000 ms.</param>
        /// <returns>true if the server at the specified host and port is available; otherwise, false.</returns>
        protected virtual bool CheckServerAvailable(
            EventContext telemetryContext,
            string targetHost,
            int port,
            int timeout = 30000)
        {
            Thread.Sleep(timeout); // wait for server to be available

            return this.RunCommandAsRoot(telemetryContext, "RallyUrlServerCall", $"curl {targetHost}:{port}");
        }

        private void StartRallyClient(
            string user,
            string targetHost,
            int port,
            string trackName,
            string rallySharedStoragePath,
            string rallyUserHomePath,
            EventContext telemetryContext, 
            CancellationToken cancellationToken)
        {
            // install es rally
            this.RunCommandAsRoot(telemetryContext, "RallyCheckPyhton3", $"python3 --version", true);
            this.RunCommandAsRoot(telemetryContext, "RallyCheckPip3", $"pip3 --version", true);

            // using pipx to install esrally, prepare the environment and avoid dependency conflicts
            this.RunCommandAsRoot(telemetryContext, "RallySetPixPathRoot", $"pipx ensurepath", true);

            this.RunCommandAsRoot(telemetryContext, "RallyInstall", $"pipx install esrally", true);

            this.RunCommandAsRoot(telemetryContext, "RallyMakeSharedStorage", $"mkdir -p {rallySharedStoragePath}");

            this.RunCommandAsRoot(telemetryContext, "RallyChown", $"chown -R {user}:{user} {rallySharedStoragePath}", true);
            this.RunCommandAsRoot(telemetryContext, "RallySharedStorageCheck", $"ls -ld {rallySharedStoragePath}", true);
            this.RunCommandAsUser(telemetryContext, user, "RallyUserTouch", $"echo ok > {rallySharedStoragePath}/test.txt", true);

            this.RunCommandAsRoot(telemetryContext, "RallyChownUserHome", $"chown -R {user}:{user} {rallyUserHomePath}");

            this.RunESRallyCommand(telemetryContext, user, rallyUserHomePath, rallySharedStoragePath, "RallyCheckEsrallyCheck", "--version");
            this.RunESRallyCommand(telemetryContext, user, rallyUserHomePath, rallySharedStoragePath, "RallyInfo", $"info --track={trackName}");
            this.RunESRallyCommand(telemetryContext, user, rallyUserHomePath, rallySharedStoragePath, "RallyListTracks", "list tracks");

            // client environment is ready, now we can connect to the Elasticsearch server

            int tries = 0;
            while (!this.CheckServerAvailable(telemetryContext, targetHost, port))
            {
                if (tries++ > 10)
                {
                    throw new WorkloadException(
                        $"ElasticSearch Rally Client could not reach the server at {targetHost} after multiple attempts.",
                        ErrorReason.WorkloadFailed);
                }

                this.Logger.LogTraceMessage($"ElasticSearch Rally Client waiting for server {targetHost} to be available...");
            }
        }

        private void RunRallyClient(
            string user,
            string targetHost,
            int port,
            string trackName,
            string rallySharedStoragePath,
            string rallyUserHomePath,
            EventContext telemetryContext, 
            CancellationToken cancellationToken)
        {
            DateTime start = DateTime.Now;
            string raceId = Guid.NewGuid().ToString();
            string reportPath = $"{rallySharedStoragePath}/report.csv";

            string rallyCommand = string.Concat(
                "race ",
                $"--track={trackName} ",
                $"--distribution-version={this.DistributionVersion} ",
                $"--target-hosts={targetHost} ",
                $"--race-id={raceId} ",
                $"--target-hosts={targetHost}:{port} ",
                $"--show-in-report=all ", // all, all-percentiles, available
                $"--report-format=csv ",
                $"--report-file={reportPath} ",
                $"--pipeline=benchmark-only ",
                $"--runtime-jdk=bundled");

            if (this.RallyTestMode)
            {
                rallyCommand = string.Concat(rallyCommand, " --test-mode");
            }

            this.RunESRallyCommand(telemetryContext, user, rallyUserHomePath, rallySharedStoragePath, "RallyExecution", rallyCommand);

            this.RunESRallyCommand(telemetryContext, user, rallyUserHomePath, rallySharedStoragePath, "RallyListRaces", "list races");

            // race.json is undocumented and not present in esrally 2.5.0 and later versions by default, so we cannot depend on it.
            string resultsPath = $"{rallySharedStoragePath}/.rally/benchmarks/races/{raceId}/race.json";            
            telemetryContext.AddContext("RallyResultsJsonPath", resultsPath);
            telemetryContext.AddContext("RallyReportCsvPath", reportPath);

            if (!this.CheckFileExists(reportPath))
            {
                throw new WorkloadException(
                    $"{this.TypeName}.RallyReportCsvMissing",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }
            else
            {
                try
                {
                    string[] reportContents = this.ReadReportLines(reportPath);
                    if (reportContents.Length < 2)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.RallyReportCsvInsufficientData", telemetryContext);
                        return;
                    }

                    telemetryContext.AddContext("RallyReportCsvContents", reportContents.Take(5));
                    this.Logger.LogMessage($"{this.TypeName}.RallyReportCsv", telemetryContext);

                    this.CaptureMetrics(reportContents, rallyCommand, raceId, start, DateTime.Now, telemetryContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    throw new WorkloadException(
                        $"{this.TypeName}.RallyReportCsvFailed",
                        ex,
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }
            }
        }

        private void RunESRallyCommand(EventContext telemetryContext, string user, string rallyUserHomePath, string rallySharedStoragePath, string key, string esRallyCommand)
        {
            // hey points of this solution:
            // - avoids dotfiles which are very cumbersome to deal with .net process
            // - wrapper script quirks by calling python3 -m
            // - inlines all required esrally environment arguments via env.

            var pipxBin = $"{rallyUserHomePath}/.local/bin";

            // Build PATH deterministically (prepend pipx bin)
            var basePath = Environment.GetEnvironmentVariable("PATH") ?? "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin";
            var childPath = $"{pipxBin}:{basePath}";

            string shellCommand = string.Concat(
                $"-u {user} -H ", // set user scope
                "env ", // set environment arguments for current process session
                $"HOME={rallyUserHomePath} ", // user storage
                $"RALLY_HOME={rallySharedStoragePath} ", // shared storage
                $"XDG_STATE_HOME={rallyUserHomePath}/.local/state ",
                $"PATH={childPath} ",
                "python3 -m pipx run esrally ", // esrally lives inside the pipx venv, not system Python.
                esRallyCommand);

            this.RunCommandAsRoot(telemetryContext, key, shellCommand, true);
        }

        private void CaptureMetrics(string[] reportContents, string rallyExecutionArguments, string raceId, DateTime startTime, DateTime exitTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.MetadataContract.AddForScenario(
                "ElasticsearchRally",
                rallyExecutionArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            if (reportContents.Length > 0)
            {
                try
                {
                    IList<Metric> metrics = ElasticsearchMetricReader.Read(
                        reportContents,
                        new Dictionary<string, IConvertible>
                        {
                            ["elasticsearchVersion"] = this.DistributionVersion,
                            ["rallyTrack"] = this.TrackName,
                            ["raceId"] = raceId,
                        });

                    if (this.MetricFilters?.Any() == true)
                    {
                        metrics = metrics.FilterBy(this.MetricFilters).ToList();
                    }

                    this.Logger.LogMetrics(
                        toolName: "ElasticsearchRally",
                        scenarioName: this.MetricScenario ?? this.Scenario,
                        startTime,
                        exitTime,
                        metrics,
                        null,
                        scenarioArguments: rallyExecutionArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (Exception exc)
                {
                    throw new WorkloadException(
                        $"Capture metrics failed.",
                        exc, 
                        ErrorReason.InvalidResults);
                }
            }
        }
    }
}
