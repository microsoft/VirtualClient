// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.ProcessAffinity;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Bombardier Client executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-x64,win-arm64")]
    public class BombardierExecutor : VirtualClientMultiRoleComponent
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public BombardierExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
               .WaitAndRetryAsync(2, (retries) => TimeSpan.FromSeconds(retries * 2));

            this.ClientRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(2, (retries) => TimeSpan.FromSeconds(retries));

            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// API Client that is used to communicate with server-hosted instance of the Virtual Client Server.
        /// </summary>
        public IApiClient ServerApi { get; set; }

        /// <summary>
        /// API Client that is used to communicate with ReverseProxy instance of the Virtual Client Server.
        /// </summary>
        public IApiClient ReverseProxyApi { get; set; }

        /// <summary>
        /// Provides components and services necessary for interacting with the local system and environment.
        /// </summary>
        public ISystemManagement SystemManagement { get; }

        /// <summary>
        /// Option for testing webserver (default), reverse-proxy (rp), api-gateway (apigw) 
        /// </summary>
        public string TargetService
        {
            get
            {
                switch (this.Parameters.GetValue<string>(nameof(this.TargetService), string.Empty).ToLower())
                {
                    case "reverse-proxy":
                    case "rp":
                        return "rp";
                    case "apiwg":
                    case "apigw":
                    case "api-gateway":
                        return "apigw";
                    case "server":
                        return "server";
                    default:
                        IEnumerable<ClientInstance> reverseProxyInstanceEnumerable = this.GetLayoutClientInstances(ClientRole.ReverseProxy, false);
                        if ((reverseProxyInstanceEnumerable == null) || (!reverseProxyInstanceEnumerable.Any()))
                        {
                            return "server";
                        }
                        else
                        {
                            return "rp";
                        }
                }
            }
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandArguments
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandArguments));
            }
        }

        /// <summary>
        /// Gets or sets whether to bind the workload to specific CPU cores.
        /// </summary>
        public bool BindToCores
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.BindToCores), defaultValue: false);
            }
        }

        /// <summary>
        /// Gets the CPU core affinity specification (e.g., "0-3", "0,2,4,6").
        /// </summary>
        public string CoreAffinity
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.CoreAffinity), out IConvertible value);
                return value?.ToString();
            }
        }

        /// <summary>
        /// Polling Timeout
        /// </summary>
        public TimeSpan Timeout
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Timeout), TimeSpan.FromMinutes(5));
            }
        }

        /// <summary>
        /// Parameter defines true/false whether the action is meant to warm up the server.
        /// We do not capture metrics on warm up operations.
        /// </summary>
        public bool WarmUp
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.WarmUp), false);
            }
        }

        /// <summary>
        /// The path to the Bombardier package.
        /// </summary>
        public string PackageDirectory { get; set; }

        /// <summary>
        /// True/false whether the server instance has been warmed up.
        /// </summary>
        protected bool IsServerWarmedUp { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The retry policy to apply to each Bombardier workload instance when trying to startup
        /// against a target server.
        /// </summary>
        protected IAsyncPolicy ClientRetryPolicy { get; set; }

        /// <summary>
        /// Initializes the executor dependencies, package locations, server api, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath bombardierPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);

            this.PackageDirectory = bombardierPackage.Path;

            // Make bombardier executable on Unix systems
            if (this.Platform == PlatformID.Unix)
            {
                string bombardierPath = this.Combine(this.PackageDirectory, "bombardier");
                await this.SystemManagement.MakeFileExecutableAsync(bombardierPath, this.Platform, cancellationToken).ConfigureAwait(false);
            }

            this.InitializeApiClients();
        }

        /// <summary>
        /// Validates the component parameters.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (this.BindToCores)
            {
                this.ThrowIfParameterNotDefined(nameof(this.CoreAffinity));
            }
        }

        /// <summary>     
        /// Executes component logic
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.WarmUp || !this.IsServerWarmedUp)
            {
                Task clientWorkloadTask;

                clientWorkloadTask = this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");
                        await this.ServerApi.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken);
                        this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                        await this.ServerApi.PollForExpectedStateAsync<State>(nameof(State), (state => state.Online()), this.Timeout, cancellationToken).ConfigureAwait(false);

                        this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");

                        // verify ReverseProxy is online
                        if ((this.ReverseProxyApi != null) && ((this.TargetService == "rp") || (this.TargetService == "apigw")))
                        {
                            this.Logger.LogTraceMessage("Synchronization: Poll ReverseProxy for online signal...");
                            await this.ReverseProxyApi.PollForExpectedStateAsync<State>(nameof(State), (state => state.Online()), TimeSpan.FromMinutes(10), cancellationToken).ConfigureAwait(false);
                            this.Logger.LogTraceMessage("Synchronization: ReverseProxy online signal confirmed...");
                            Item<State> reverseProxyState = await this.ReverseProxyApi.GetStateAsync<State>(nameof(State), cancellationToken).ConfigureAwait(false);
                            telemetryContext.AddContext(nameof(reverseProxyState), reverseProxyState);

                            HttpResponseMessage reverseProxyHttpResponseMessage = await this.ReverseProxyApi.GetStateAsync("version", cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                            telemetryContext.AddResponseContext(reverseProxyHttpResponseMessage);

                            if (reverseProxyHttpResponseMessage.IsSuccessStatusCode)
                            {
                                Item<State> reverseProxyVersion = await reverseProxyHttpResponseMessage.FromContentAsync<Item<State>>();
                                telemetryContext.AddContext(nameof(reverseProxyVersion), reverseProxyVersion.Definition.Properties);
                            }
                        }

                        this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                        string commandArguments = this.GetCommandLineArguments(cancellationToken);
                        await this.ExecuteWorkloadAsync(commandArguments, this.PackageDirectory, telemetryContext, cancellationToken);
                    }
                });

                await Task.WhenAll(clientWorkloadTask);

                if (this.WarmUp)
                {
                    this.IsServerWarmedUp = true;
                }
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();

            if (!this.IsMultiRoleLayout())
            {
                this.ServerApi = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            }
            else
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                this.ServerApi = clientManager.GetOrCreateApiClient(serverInstance.Name, serverInstance);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApi);

                IEnumerable<ClientInstance> reverseProxyInstanceEnumerable = this.GetLayoutClientInstances(ClientRole.ReverseProxy, false);
                if ((reverseProxyInstanceEnumerable == null) || (!reverseProxyInstanceEnumerable.Any()))
                {
                    this.ReverseProxyApi = null;
                }
                else
                {
                    ClientInstance reverseProxyInstance = reverseProxyInstanceEnumerable.FirstOrDefault();
                    this.ReverseProxyApi = clientManager.GetOrCreateApiClient(reverseProxyInstance.Name, reverseProxyInstance);
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ReverseProxyApi);
                }
            }
        }

        /// <summary>
        /// Gets Command Line Argument to start workload.
        /// </summary>
        protected string GetCommandLineArguments(CancellationToken cancellationToken)
        {
            string result = this.CommandArguments;

            Dictionary<string, Regex> roleAndRegexKvp = new Dictionary<string, Regex>()
            {
                { ClientRole.Server, new Regex(@"\{ServerIp\}", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
                { ClientRole.ReverseProxy, new Regex(@"\{ReverseProxyIp\}", RegexOptions.Compiled | RegexOptions.IgnoreCase) },
                { ClientRole.Client,  new Regex(@"\{ClientIp\}", RegexOptions.Compiled | RegexOptions.IgnoreCase) }
            };

            foreach (KeyValuePair<string, Regex> kvp in roleAndRegexKvp)
            {
                MatchCollection matches = kvp.Value.Matches(this.CommandArguments);

                if (matches?.Any() == true)
                {
                    foreach (Match match in matches)
                    {
                        IEnumerable<ClientInstance> instances = this.GetLayoutClientInstances(kvp.Key, throwIfNotExists: false);
                        string ipAddress = instances?.FirstOrDefault()?.IPAddress ?? IPAddress.Loopback.ToString();
                        result = Regex.Replace(result, match.Value, ipAddress);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Execute Bombardier Executor
        /// </summary>
        /// <param name="commandArguments">Command argument to execute on workload</param>
        /// <param name="workingDir">Working Directory</param>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ExecuteWorkloadAsync(string commandArguments, string workingDir, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            commandArguments.ThrowIfNullOrEmpty(nameof(commandArguments));

            string bombardierPath = this.Combine(this.PackageDirectory, this.Platform == PlatformID.Unix ? "bombardier" : "bombardier.exe");

            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(bombardierPath);

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(bombardierPath), bombardierPath)
                .AddContext(nameof(commandArguments), commandArguments)
                .AddContext("bindToCores", this.BindToCores)
                .AddContext("coreAffinity", this.CoreAffinity);

            try
            {
                await (this.ClientRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                {
                    try
                    {
                        DateTime startTime = DateTime.UtcNow;
                        IProcessProxy process = null;
                        ProcessAffinityConfiguration affinityConfig = null;

                        if (this.BindToCores && !string.IsNullOrWhiteSpace(this.CoreAffinity))
                        {
                            affinityConfig = ProcessAffinityConfiguration.Create(
                                this.Platform,
                                this.CoreAffinity);

                            relatedContext.AddContext("affinityMask", affinityConfig.ToString());

                            if (this.Platform == PlatformID.Win32NT)
                            {
                                process = this.SystemManagement.ProcessManager.CreateProcess(
                                    bombardierPath,
                                    commandArguments,
                                    workingDir);
                            }
                            else
                            {
                                LinuxProcessAffinityConfiguration linuxConfig = (LinuxProcessAffinityConfiguration)affinityConfig;
                                string fullCommandLine = $"{bombardierPath} {commandArguments}";
                                string wrappedCommand = linuxConfig.GetCommandWithAffinity(null, fullCommandLine);

                                process = this.SystemManagement.ProcessManager.CreateProcess(
                                    "/bin/bash",
                                    $"-c {wrappedCommand}",
                                    workingDir);
                            }
                        }
                        else
                        {
                            process = await this.ExecuteCommandAsync(bombardierPath, commandArguments, workingDir, telemetryContext, cancellationToken, runElevated: true);
                        }

                        using (process)
                        {
                            if (affinityConfig != null)
                            {
                                if (this.Platform == PlatformID.Win32NT)
                                {
                                    process.Start();
                                    process.ApplyAffinity((WindowsProcessAffinityConfiguration)affinityConfig);
                                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                                }
                                else
                                {
                                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Bombardier", logToFile: true);
                                process.ThrowIfWorkloadFailed();

                                if (process.StandardOutput.Length == 0)
                                {
                                    throw new WorkloadException($"{this.PackageName} did not write metrics to console.", ErrorReason.CriticalWorkloadFailure);
                                }

                                if (!this.WarmUp)
                                {
                                    await this.CaptureMetricsAsync(process, commandArguments, relatedContext, cancellationToken).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.WorkloadStartError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                        throw;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                this.Logger.LogMessage($"{this.TypeName}.OperationCanceledException", LogLevel.Warning, telemetryContext.Clone());
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage($"{this.TypeName}.ExecuteWorkloadError", LogLevel.Error, telemetryContext.Clone().AddError(exc));
                throw;
            }
        }

        /// <summary>
        /// Get Bombardier Version
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>Bombardier Version</returns>
        protected async Task<string> GetBombardierVersionAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string bombardierPath = this.Combine(this.PackageDirectory, this.Platform == PlatformID.Unix ? "bombardier" : "bombardier.exe");
            string bombardierVersion = null;

            try
            {
                this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(bombardierPath);

                string commandArguments = "--version";
                string versionPattern = @"bombardier\s+(?:version\s+)?v?(\d+\.\d+\.\d+)";
                Regex versionRegex = new Regex(versionPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                using (IProcessProxy process = await this.ExecuteCommandAsync(bombardierPath, commandArguments, workingDirectory: this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "BombardierVersion", logToFile: true).ConfigureAwait(false);
                        string output = process.StandardOutput.ToString();
                        Match match = versionRegex.Match(output);

                        if (!match.Success)
                        {
                            output = process.StandardError.ToString();
                            match = versionRegex.Match(output);
                        }

                        if (match.Success)
                        {
                            bombardierVersion = match.Groups[1].Value;
                            telemetryContext.AddContext("BombardierVersion", bombardierVersion);
                            this.Logger.LogMessage($"{this.TypeName}.BombardierVersionCaptured", LogLevel.Information, telemetryContext);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext);
            }

            return bombardierVersion;
        }

        private async Task CaptureMetricsAsync(IProcessProxy workloadProcess, string commandArguments, EventContext context, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (workloadProcess.ExitCode == 0)
                {
                    EventContext telemetryContext = context.Clone();
                    telemetryContext.AddContext(nameof(this.MetricScenario), this.MetricScenario);
                    telemetryContext.AddContext(nameof(this.Scenario), this.Scenario);

                    BombardierMetricsParser resultsParser = new BombardierMetricsParser(workloadProcess.StandardOutput.ToString());
                    IList<Metric> metrics = resultsParser.Parse();

                    foreach (var metric in metrics)
                    {
                        metric.Metadata.Add("bindToCores", this.BindToCores.ToString());
                        if (this.BindToCores && !string.IsNullOrWhiteSpace(this.CoreAffinity))
                        {
                            metric.Metadata.Add("coreAffinity", this.CoreAffinity);
                        }
                    }

                    string bombardierVersion = await this.GetBombardierVersionAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                    this.MetadataContract.AddForScenario(
                       toolName: this.PackageName,
                       toolArguments: commandArguments,
                       toolVersion: bombardierVersion,
                       packageName: this.PackageName,
                       packageVersion: null,
                       additionalMetadata: null);
                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        toolName: this.PackageName,
                        scenarioName: this.MetricScenario ?? this.Scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics: metrics,
                        metricCategorization: null,
                        scenarioArguments: commandArguments,
                        tags: this.Tags,
                        eventContext: telemetryContext);
                }
            }
        }
    }
}
