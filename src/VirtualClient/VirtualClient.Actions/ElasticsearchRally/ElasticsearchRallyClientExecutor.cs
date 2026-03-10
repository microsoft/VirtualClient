// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    public class ElasticsearchRallyClientExecutor : ElasticsearchRallyExecutor
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
        /// The Rally Distribution Version.
        /// If not specified, the Rally latest version will be used.
        /// </summary>
        public string RallyVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.RallyVersion));
            }
        }

        /// <summary>
        /// The track targeted for run by Rally.
        /// </summary>
        public string RallyTrackName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.RallyTrackName));
            }
        }

        /// <summary>
        /// TestMode indicates whether to run Rally in test mode.
        /// </summary>
        public bool RallyTestMode
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(ElasticsearchRallyClientExecutor.RallyTestMode), false);
            }
        }

        /// <summary>
        /// A track consists of one or more challenges. With this flag you can specify which challenge should be run.
        /// https://esrally.readthedocs.io/en/stable/command_line_reference.html#challenge
        /// </summary>
        public string RallyChallenge
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.RallyChallenge), string.Empty);
            }
        }

        /// <summary>
        /// Each challenge consists of one or more tasks.
        /// Use RallyIncludeTasks to specify a comma-separated list of tasks that you want to run.
        /// Only the tasks that match will be executed.
        /// https://esrally.readthedocs.io/en/stable/command_line_reference.html#include-tasks
        /// </summary>
        public string RallyIncludeTasks
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.RallyIncludeTasks), string.Empty);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool RallyCollectAllMetrics
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(ElasticsearchRallyClientExecutor.RallyCollectAllMetrics), false);
            }
        }

        /// <summary>
        /// Command arguments to control Rally
        /// https://esrally.readthedocs.io/en/stable/command_line_reference.html
        /// </summary>
        public string RallyCommandLineArguments
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ElasticsearchRallyClientExecutor.RallyCommandLineArguments), string.Empty);
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

                if (
                    clientInstance == null ||
                    string.IsNullOrEmpty(clientInstance.IPAddress))
                {
                    throw new WorkloadException(
                        $"Elasticsearch Rally Client IP Address must be defined.",
                        ErrorReason.LayoutInvalid);
                }

                string targetHost = clientInstance?.IPAddress;
                if (string.IsNullOrEmpty(targetHost))
                {
                    throw new WorkloadException(
                        $"Elasticsearch Rally Client could not determine the target host from the layout.",
                        ErrorReason.LayoutInvalid);
                }

                string user = this.PlatformSpecifics.GetLoggedInUser();
                int port = this.Port;
                string trackName = this.RallyTrackName;
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

                this.CleanupElasticSearchCluster(
                    telemetryContext.Clone(),
                    cancellationToken,
                    targetHost,
                    port);

                this.RunRallyClient(
                    telemetryContext.Clone(),
                    cancellationToken,
                    user,
                    targetHost,
                    port,
                    trackName,
                    rallySharedStoragePath,
                    rallyUserHomePath);
            });

            return;
        }

        /// <summary>
        /// Reads the contents of the specified report file and returns it as a single string.
        /// </summary>
        /// <param name="reportPath">The full path to the report file to read. Cannot be null or an empty string.</param>
        /// <returns>The contents of the report file as a single string.</returns>
        protected virtual string ReadReportFile(string reportPath)
        {
            return System.IO.File.ReadAllText(reportPath);
        }

        /// <summary>
        /// Checks whether the specified server is available by attempting to connect to the given host and port.
        /// </summary>
        /// <remarks>This method waits up to timeout seconds before performing the availability check. Override
        /// this method to customize the server availability check logic in derived classes.</remarks>
        /// <param name="telemetryContext">The context for telemetry and logging associated with this operation.</param>
        /// <param name="cancellationToken">The cancellation token to observe while waiting for the server to become available.</param>
        /// <param name="targetHost">The DNS name or IP address of the server to check for availability. Cannot be null or empty.</param>
        /// <param name="port">The network port number on the target server to check. Must be a valid TCP port number.</param>
        /// <param name="timeout">The amount of time in milliseconds to wait before performing the availability check. Default is 60000 ms.</param>
        /// <returns>true if the server at the specified host and port is available; otherwise, false.</returns>
        protected virtual bool CheckServerAvailable(
            EventContext telemetryContext,
            CancellationToken cancellationToken,
            string targetHost,
            int port,
            int timeout = 60000)
        {
            Thread.Sleep(timeout); // wait for server to be available

            return this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyUrlServerCall", $"curl {targetHost}:{port}");
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
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyCheckPyhton3", $"python3 --version", true);
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyCheckPip3", $"pip3 --version", true);

            // using pipx to install esrally, prepare the environment and avoid dependency conflicts
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallySetPixPathRoot", $"pipx ensurepath", true);

            string esRallyInstallCommand = "pipx install esrally";
            if (!string.IsNullOrEmpty(this.RallyVersion))
            {
                esRallyInstallCommand = $"{esRallyInstallCommand}=={this.RallyVersion}";
            }

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyInstall", esRallyInstallCommand, true);

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyMakeSharedStorage", $"mkdir -p {rallySharedStoragePath}");

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyChown", $"chown -R {user}:{user} {rallySharedStoragePath}", true);
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallySharedStorageCheck", $"ls -ld {rallySharedStoragePath}", true);
            this.RunCommandAsUser(telemetryContext, cancellationToken, user, "RallyUserTouch", $"echo ok > {rallySharedStoragePath}/test.txt", true);

            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyChownUserHome", $"chown -R {user}:{user} {rallyUserHomePath}");

            this.RunESRallyCommand(telemetryContext, cancellationToken, user, rallyUserHomePath, rallySharedStoragePath, "RallyCheckEsrallyCheck", "--version");
            this.RunESRallyCommand(telemetryContext, cancellationToken, user, rallyUserHomePath, rallySharedStoragePath, "RallyInfo", $"info --track={trackName}");
            this.RunESRallyCommand(telemetryContext, cancellationToken, user, rallyUserHomePath, rallySharedStoragePath, "RallyListTracks", "list tracks");

            // client environment is ready, now we can connect to the Elasticsearch server

            int tries = 0;
            while (!this.CheckServerAvailable(telemetryContext, cancellationToken, targetHost, port))
            {
                int limit = 20;
                if (tries++ >= limit)
                {
                    throw new WorkloadException(
                        $"Elasticsearch Rally Client could not reach the server at {targetHost} after {limit} attempts.",
                        ErrorReason.WorkloadFailed);
                }

                this.Logger.LogMessage($"{this.TypeName}.ElasticsearchServerConnectionAttempt-{tries}-of-{limit}", telemetryContext);
            }

            this.Logger.LogMessage($"{this.TypeName}.ElasticsearchServerIsReady", telemetryContext);
        }

        private void CleanupElasticSearchCluster(EventContext telemetryContext, CancellationToken cancellationToken, string targetHost, int port)
        {
            this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyCleanupElasticSearch", $"curl -X DELETE {targetHost}:{port}/_all");
        }

        private void RunRallyClient(
            EventContext telemetryContext,
            CancellationToken cancellationToken,
            string user,
            string targetHost,
            int port,
            string trackName,
            string rallySharedStoragePath,
            string rallyUserHomePath)
        {
            DateTime start = DateTime.Now;
            string raceId = Guid.NewGuid().ToString();
            string reportPath = $"{rallySharedStoragePath}/report.csv";

            if (this.CheckFileExists(reportPath))
            {
                this.RunCommandAsRoot(telemetryContext, cancellationToken, "RallyRemoveOldReport", $"rm -f {reportPath}", false);
            }

            string rallyCommand = this.RallyCommandLineArguments;

            if (string.IsNullOrEmpty(rallyCommand))
            {
                rallyCommand = this.BuildRallyCommandLineArguments(
                    trackName,
                    raceId,
                    targetHost,
                    port,
                    reportPath);
            }

            this.RunESRallyCommand(telemetryContext, cancellationToken, user, rallyUserHomePath, rallySharedStoragePath, "RallyExecution", rallyCommand);

            this.RunESRallyCommand(telemetryContext, cancellationToken, user, rallyUserHomePath, rallySharedStoragePath, "RallyListRaces", "list races");

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
                    this.CaptureMetrics(reportPath, rallyCommand, raceId, start, DateTime.Now, telemetryContext, cancellationToken);
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

        private string BuildRallyCommandLineArguments(
            string trackName,
            string raceId,
            string targetHost,
            int port,
            string reportPath)
        {
            // using report-file command line to capture results in CSV format
            // https://esrally.readthedocs.io/en/stable/command_line_reference.html#report-file

            string rallyCommand = string.Concat(
                "race ",
                $"--track={trackName} ",
                $"--race-id={raceId} ",
                $"--target-hosts={targetHost}:{port} ",
                $"--show-in-report=available ", // all, all-percentiles, available : using available because "all" thrown null type error
                $"--report-format=csv ",
                $"--report-file={reportPath} ",
                $"--pipeline=benchmark-only ",
                $"--runtime-jdk=bundled");

            if (!string.IsNullOrEmpty(this.RallyChallenge))
            {
                rallyCommand = string.Concat(rallyCommand, $" --challenge={this.RallyChallenge}");
            }

            if (!string.IsNullOrEmpty(this.RallyIncludeTasks))
            {
                rallyCommand = string.Concat(rallyCommand, $" --include-tasks={this.RallyIncludeTasks}");
            }

            if (this.RallyTestMode)
            {
                rallyCommand = string.Concat(rallyCommand, " --test-mode");
            }

            return rallyCommand;
        }

        private void RunESRallyCommand(EventContext telemetryContext, CancellationToken cancellationToken, string user, string rallyUserHomePath, string rallySharedStoragePath, string key, string esRallyCommand)
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

            this.RunCommandAsRoot(telemetryContext, cancellationToken, key, shellCommand, true);
        }

        private void CaptureMetrics(string reportPath, string rallyExecutionArguments, string raceId, DateTime startTime, DateTime exitTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            string reportContents = this.ReadReportFile(reportPath);

            ElasticsearchRallyMetricsParser elasticsearchRallyMetricsParser = new ElasticsearchRallyMetricsParser(
                reportContents,
                new Dictionary<string, IConvertible>
                {
                    ["elasticsearchVersion"] = this.ElasticsearchVersion,
                    ["rallyVersion"] = this.RallyVersion ?? "latest",
                    ["rallyTrack"] = this.RallyTrackName,
                    ["raceId"] = raceId,
                    ["challenge"] = this.RallyChallenge,
                    ["includeTasks"] = this.RallyIncludeTasks,
                },
                this.RallyCollectAllMetrics);

            string[] reportLines = elasticsearchRallyMetricsParser.ReportLines;

            if (reportLines.Length < 2)
            {
                this.Logger.LogMessage($"{this.TypeName}.RallyReportCsvInsufficientData", telemetryContext);
                return;
            }

            telemetryContext.AddContext("RallyReportCsvContents", reportContents.Take(5));
            this.Logger.LogMessage($"{this.TypeName}.RallyReportCsv", telemetryContext);

            this.MetadataContract.AddForScenario(
                "ElasticsearchRally",
                rallyExecutionArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            if (reportLines.Length > 0)
            {
                try
                {
                    IList<Metric> metrics = elasticsearchRallyMetricsParser.Parse();

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
