// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Security.Cryptography;
    using System.Text;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Controller;

    // Features available ONLY for SDK Agent builds.
    public abstract partial class CommandBase
    {
        /// <summary>
        /// An identifier that groups a set of experiments together.
        /// </summary>
        public string ExperimentName { get; set; }

        /// <summary>
        /// A subdirectory/folder name within the 'logs' directory to which log files
        /// should be written.
        /// </summary>
        public string LogSubdirectory { get; set; }

        // IMPORTANT:
        // With SDK Agent scenarios, we are not allowing the user to set any of the 
        // directories except the package directory (e.g. logs, state, temp). These are
        // set in stone for now until we can resolve integration issues between the controller
        // and target agent systems (e.g. win-x64 paths vs. linux-arm64 paths when the user specifies
        // a relative path).

        /// <summary>
        /// Applies and default settings/values to the command base.
        /// </summary>
        /// <param name="platformSpecifics">Defines the fundamental directory paths for the application.</param>
        protected void ApplyAgentDefaults(PlatformSpecifics platformSpecifics)
        {
            if (!string.IsNullOrWhiteSpace(this.ExperimentName))
            {
                if (this.Metadata == null)
                {
                    this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
                }

                this.Metadata[MetadataProperty.ExperimentName] = this.ExperimentName;
            }

            // If the user explicitly defines an experiment ID, it takes priority. If the user has defined
            // an experiment name, we use a technique to ensure that the experiment ID created will be exactly 
            // the same each time for a given experiment name.
            if (string.IsNullOrWhiteSpace(this.ExperimentId))
            {
                // When a user provides an experiment name alone, we use an exact experiment ID
                // derived from the experiment name each time. This ensures that output such as
                // telemetry and logs are consistently associated with a single experiment ID regardless
                // of the number of individual executions of the SDK agent.
                if (!string.IsNullOrWhiteSpace(this.ExperimentName))
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        byte[] inputBytes = Encoding.UTF8.GetBytes(this.ExperimentName.ToLowerInvariant());
                        byte[] hashBytes = md5.ComputeHash(inputBytes);

                        this.ExperimentId = new Guid(hashBytes).ToString().ToLowerInvariant();
                    }
                }
                else
                {
                    this.ExperimentId = Guid.NewGuid().ToString().ToLowerInvariant();
                }
            }

            ////string agentId = this.ClientId;
            ////if (this.TryGetLocalIpAddress(out string ipAddress))
            ////{
            ////    agentId = ipAddress;
            ////}

            this.LogToFile = true;
            this.Isolated = true;
            this.LogDirectory = AgentSpecifics.GetLocalLogsPath(platformSpecifics, this.ClientId, this.ExperimentId, this.ExperimentName);
            this.StateDirectory = AgentSpecifics.GetLocalStatePath(platformSpecifics, this.ExperimentId);
            this.TempDirectory = AgentSpecifics.GetLocalTempPath(platformSpecifics, this.ExperimentId);

            // A log subdirectory may be defined as well.
            if (!string.IsNullOrWhiteSpace(this.LogSubdirectory))
            {
                this.LogDirectory = platformSpecifics.Combine(this.LogDirectory, this.LogSubdirectory);
            }

            if (string.IsNullOrWhiteSpace(this.PackageDirectory))
            {
                this.PackageDirectory = AgentSpecifics.GetLocalPackagesPath(platformSpecifics);
            }
        }

        private bool TryGetLocalIpAddress(out string ipAddress)
        {
            ipAddress = null;

            try
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                if (nics?.Any() == true)
                {
                    var these = nics.Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        ?.SelectMany(i => i.GetIPProperties().UnicastAddresses);

                    UnicastIPAddressInformation currentIpAddress = nics.Where(nic => nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        ?.SelectMany(i => i.GetIPProperties().UnicastAddresses)
                        ?.Where(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ?.FirstOrDefault();

                    if (currentIpAddress != null)
                    {
                        ipAddress = currentIpAddress.Address?.ToString();
                    }
                }
            }
            catch
            {
            }

            return !string.IsNullOrWhiteSpace(ipAddress);
        }
    }
}
