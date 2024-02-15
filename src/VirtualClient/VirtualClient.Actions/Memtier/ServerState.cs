// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.Memtier
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    internal class ServerState : State
    {
        private const string PortDelimiter = ",,,";

        [JsonConstructor]
        public ServerState(IDictionary<string, IConvertible> properties = null)
            : base(properties)
        {
        }

        internal ServerState(IEnumerable<PortDescription> ports)
          : base()
        {
            if (ports?.Any() == true)
            {
                this[nameof(this.Ports)] = string.Join(ServerState.PortDelimiter, ports.Select(port => $"{port.Port}:{port.CpuAffinity}"));
            }
        }

        /// <summary>
        /// The set of ports on which the Memcached servers are running.
        /// </summary>
        public IEnumerable<PortDescription> Ports
        {
            get
            {
                List<PortDescription> ports = null;
                if (this.Properties.TryGetCollection<string>(nameof(this.Ports), ServerState.PortDelimiter.AsArray(), out IEnumerable<string> portDescriptions))
                {
                    // If CPU affinity is not used, there will only be a list of ports.
                    //
                    // e.g.
                    // 6379,,,6380,,,6381
                    //
                    // The target server instances may each be running on a specific port and also bound to a specific
                    // logical processor. 
                    //
                    // e.g.
                    // 6379:0,,,6380:1,,,6381:2
                    //
                    // For some servers, a signle instance may be bound to all logical processors
                    // 
                    // e.g.
                    // 6379:0,1,2,3,4,5,6,7

                    ports = new List<PortDescription>();
                    foreach (string description in portDescriptions)
                    {
                        string[] parts = description.Split(":", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        ports.Add(new PortDescription
                        {
                            CpuAffinity = parts.Length > 1 ? parts[1] : null,
                            Port = int.Parse(parts[0])
                        });
                    }
                }

                return ports;
            }
        }
    }

    internal class PortDescription
    {
        public string CpuAffinity { get; set; }

        public int Port { get; set; }
    }
}
