// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Redis Benchmark Client Executor.
    /// </summary>
    public class RedisFlushAllExecutor : RedisExecutor
    {

        public static string CommandLine = @"redis-cli -h {0} -p {1} FLUSHALL";

        public RedisFlushAllExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {

        }

        public int ServerInstances
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerInstances), 1);
            }
        }

        public int StartPort
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.StartPort), 6379);
            }
        }

        /// <summary>
        /// Parameter defines the Memtier benchmark toolset command line to execute.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }




        /// <summary>
        /// Executes  client side.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);
                foreach (ClientInstance server in targetServers)
                {
                    string ipAddress = IPAddress.Parse(server.IPAddress).ToString();
                    int startport = this.StartPort;
                    for (int instance = 0; i < this.ServerInstances; instance++)
                    {
                        int port = startport + instance;
                        string portnumber = port.ToString();
                        string fullcommand = GetCommandLine(ipAddress, portnumber);

                        await this.ExecuteCommandAsync("", fullcommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private string GetCommandLine(string ipAddress, string portnumber)
        {
            string command = string.Format(this.CommandLine, ipAddress, portnumber);
            return command;
        }

    }
}