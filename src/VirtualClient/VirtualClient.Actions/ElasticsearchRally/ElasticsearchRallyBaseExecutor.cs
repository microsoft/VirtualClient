// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Base class for all Elasticsearch Rally workload executors.
    /// </summary>
    public abstract class ElasticsearchRallyBaseExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Constructor for <see cref="ElasticsearchRallyBaseExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public ElasticsearchRallyBaseExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server
            };
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "osdisk:false&biggestsize");
            }
        }

        /// <summary>
        /// Elasticsearch Node Port Number
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 9200);
            }
        }

        /// <summary>
        /// Manages the state of the system.
        /// </summary>
        protected IStateManager StateManager => this.Dependencies.GetService<IStateManager>();

        /// <summary>
        /// Initializes the environment for execution of the Rally workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.IsMultiRoleLayout())
            {
                throw new WorkloadException(
                    $"{this.PackageName} Client/Server requires at least 2 nodes to run",
                    ErrorReason.LayoutInvalid);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
                
                ClientInstance instance = this.GetLayoutClientInstance();
                string layoutIPAddress = instance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(instance.Role);

                ClientInstance clientInstance = this.GetLayoutClientInstances(ClientRole.Client).First();

                IPAddress.TryParse(clientInstance.IPAddress, out IPAddress clientIpAddress);
                telemetryContext.AddContext("ClientIpAddress", clientIpAddress.ToString());

                IEnumerable<ClientInstance> serverInstances = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance serverInstance in serverInstances)
                {
                    IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                    IApiClient apiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", apiClient);
                }
            }

            await Task.CompletedTask;

            return;
        }

        /// <summary>
        /// Get filtered data directory
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected async Task<string> GetDataDirectoryAsync(CancellationToken cancellationToken)
        {
            string diskPath = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                ISystemManagement systemManager = this.Dependencies.GetService<ISystemManagement>();

                IEnumerable<Disk> disks = await systemManager.DiskManager.GetDisksAsync(cancellationToken);

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                diskPath = $"{disksToTest.First().GetPreferredAccessPath(this.Platform)}";
            }

            return diskPath;
        }

        /// <summary>
        /// Determines whether a file exists at the specified path.
        /// </summary>
        /// <remarks>The method does not check whether the caller has permission to access the file.
        /// Passing an invalid path may result in false being returned.</remarks>
        /// <param name="path">The path to the file to check. This can be either a relative or an absolute path.</param>
        /// <returns>true if a file exists at the specified path; otherwise, false.</returns>
        protected virtual bool CheckFileExists(string path)
        {
            return System.IO.File.Exists(path);
        }

        /// <summary>
        /// Runs a bash script command.
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="key">Task identifier</param>
        /// <param name="script"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        protected bool RunCommandScript(EventContext telemetryContext, string key, string script, bool throwOnError = false)
        {
            bool ok = this.RunCommand("/bin/bash", BuildBashScript(script), out string output, out string error);

            this.HandleTelemetry(telemetryContext, key, script, throwOnError, ok, output, error);

            return ok;
        }

        /// <summary>
        /// Runs a command as root.
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="key">Task identifier</param>
        /// <param name="command"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        protected bool RunCommandAsRoot(EventContext telemetryContext, string key, string command, bool throwOnError = false)
        {
            return this.RunCommandAsUser(telemetryContext, null, key, command, throwOnError);
        }

        /// <summary>
        /// Runs a command as a specific user.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        protected bool RunCommandAsUser(string user, string command, out string output, out string error)
        {
            return
                this.RunCommand(
                    "/usr/bin/sudo",
                    string.IsNullOrEmpty(user) ? command : $"-u {user} -H bash {BuildBashScript(command)}",
                    out output,
                    out error);
        }

        /// <summary>
        /// Runs a command as a specific user.
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="user"></param>
        /// <param name="key">Task identifier</param>
        /// <param name="command"></param>
        /// <param name="throwOnError"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected bool RunCommandAsUser(EventContext telemetryContext, string user, string key, string command, bool throwOnError = false)
        {
            bool ok = this.RunCommandAsUser(user, command, out string output, out string error);

            this.HandleTelemetry(telemetryContext, key, command, throwOnError, ok, output, error);

            return ok;
        }

        /// <summary>
        /// Runs a command.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="arguments"></param>
        /// <param name="output"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        protected virtual bool RunCommand(string command, string arguments, out string output, out string error)
        {
            output = null;
            error = null;

            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using (var p = Process.Start(psi))
            {
                p.WaitForExit();
                output = p.StandardOutput.ReadToEnd().Trim();
                error = p.StandardError.ReadToEnd().Trim();
                return p.ExitCode == 0;
            }
        }

        private static string BuildBashScript(string script)
        {
            return string.Concat("-lc \"", script.Replace("\"", "\\\""), "\"");
        }

        private void HandleTelemetry(EventContext telemetryContext, string key, string command, bool throwOnError, bool ok, string output, string error)
        {
            telemetryContext.AddContext($"{this.TypeName}.{key}Command", command);
            telemetryContext.AddContext($"{this.TypeName}.{key}Output", output);
            telemetryContext.AddContext($"{this.TypeName}.{key}Error", error);
            telemetryContext.AddContext($"{this.TypeName}.{key}Ok", ok);
            this.Logger.LogMessage($"{this.TypeName}.{key}", telemetryContext);

            if (!ok)
            {
                this.Logger.LogMessage($"{this.TypeName}.{key}Failed", telemetryContext);

                if (throwOnError)
                {
                    throw new WorkloadException(
                        $"Rally server configuration failed. Output: {output}; Error: {error}",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }
            }
        }
    }
}
