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
    /// Redis Cli Executor.
    /// </summary>
    public class RedisCliExecutor : RedisExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCliExecutor"/> class.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public RedisCliExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The command line to be executed.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine), string.Empty);
            }
        }

        /// <summary>
        /// The number of Redis Server instances running.
        /// </summary>
        public int ServerInstances
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerInstances), 1);
            }
        }

        /// <summary>
        /// The port number of the first Redis Server instance.
        /// </summary>
        public int ServerPort
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerPort), 6379);
            }
        }

        /// <summary>
        /// Parameter for the the CommandArguments.
        /// </summary>
        public string CommandArguments
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandArguments));
            }
        }

        /// <summary>
        /// Executes Redis cli Command.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);
                foreach (ClientInstance server in targetServers)
                {
                    string ipAddress = IPAddress.Parse(server.IPAddress).ToString();
                    int serverPort = this.ServerPort;
                    for (int instance = 0; instance < this.ServerInstances; instance++)
                    {
                        int port = serverPort + instance;
                        string portnumber = port.ToString();
                        string commandArguments = this.GetCommandLine(ipAddress, portnumber);

                        await this.ExecuteCommandAsync("redis-cli", commandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            }
        }

        private string GetCommandLine(string ipAddress, string portnumber)
        {
            string command = this.CommandLine;
            command = command.Replace("{ServerIpAddress}", ipAddress);
            command = command.Replace("{ServerPortNumber}", portnumber);
            return command;
        }
    }
}