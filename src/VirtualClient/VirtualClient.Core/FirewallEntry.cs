// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a firewall entry/rule.
    /// </summary>
    public class FirewallEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the firewall entry.</param>
        /// <param name="description">The description of the firewall entry.</param>
        /// <param name="protocol">The communication protocol for the firewall entry (e.g. TCP, UDP *).</param>
        /// <param name="ports">The ports associated with the firewall entry.</param>
        public FirewallEntry(string name, string description, string protocol, IEnumerable<int> ports)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            description.ThrowIfNullOrWhiteSpace(nameof(description));
            protocol.ThrowIfNullOrWhiteSpace(nameof(protocol));
            ports.ThrowIfEmpty(nameof(ports));

            this.Name = name;
            this.Description = description;
            this.Protocol = protocol;
            this.Ports = new ReadOnlyCollection<int>(ports.ToList());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the firewall entry.</param>
        /// <param name="description">The description of the firewall entry.</param>
        /// <param name="protocol">The communication protocol for the firewall entry (e.g. TCP, UDP *).</param>
        /// <param name="ports">The ports associated with the firewall entry.</param>
        /// <param name="remotePorts">The remote ports associated with the firewall entry.</param>
        public FirewallEntry(string name, string description, string protocol, IEnumerable<int> ports, IEnumerable<int> remotePorts)
            : this(name, description, protocol, ports)
        {
            remotePorts.ThrowIfEmpty(nameof(remotePorts));
            this.RemotePorts = new ReadOnlyCollection<int>(remotePorts.ToList());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the firewall entry.</param>
        /// <param name="description">The description of the firewall entry.</param>
        /// <param name="protocol">The communication protocol for the firewall entry (e.g. TCP, UDP *).</param>
        /// <param name="portRange">The range of ports associated with the firewall entry.</param>
        public FirewallEntry(string name, string description, string protocol, Range portRange)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            description.ThrowIfNullOrWhiteSpace(nameof(description));
            protocol.ThrowIfNullOrWhiteSpace(nameof(protocol));
            portRange.ThrowIfNull(nameof(portRange));

            this.Name = name;
            this.Description = description;
            this.Protocol = protocol;
            this.PortRange = portRange;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the firewall entry.</param>
        /// <param name="description">The description of the firewall entry.</param>
        /// <param name="protocol">The communication protocol for the firewall entry (e.g. TCP, UDP *).</param>
        /// <param name="portRange">The range of ports associated with the firewall entry.</param>
        /// <param name="remotePortRange">The range of remote ports associated with the firewall entry.</param>
        public FirewallEntry(string name, string description, string protocol, Range portRange, Range remotePortRange)
            : this(name, description, protocol, portRange)
        {
            remotePortRange.ThrowIfNull(nameof(remotePortRange));
            this.RemotePortRange = remotePortRange;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirewallEntry"/> class.
        /// </summary>
        /// <param name="name">The name of the firewall entry.</param>
        /// <param name="description">The description of the firewall entry.</param>
        /// <param name="appPath">The path to the application.</param>
        public FirewallEntry(string name, string description, string appPath)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            description.ThrowIfNullOrWhiteSpace(nameof(description));
            appPath.ThrowIfNullOrWhiteSpace(nameof(appPath));

            this.Name = name;
            this.Description = description;
            this.AppPath = appPath;
        }

        /// <summary>
        /// The description of the firewall entry.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The name of the firewall entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The communication protocol for the firewall entry (e.g. TCP, UDP *).
        /// </summary>
        public string Protocol { get; }

        /// <summary>
        /// The ports associated with the firewall entry.
        /// </summary>
        public IEnumerable<int> Ports { get; }

        /// <summary>
        /// The remote ports associated with the firewall entry.
        /// </summary>
        public IEnumerable<int> RemotePorts { get; }

        /// <summary>
        /// The range of ports associated with the firewall entry.
        /// </summary>
        public Range PortRange { get; }

        /// <summary>
        /// The range of remote ports associated with the firewall entry. The default
        /// is all ports.
        /// </summary>
        public Range RemotePortRange { get; }

        /// <summary>
        /// Path of app/program associated with the firewall entry. 
        /// </summary>
        public string AppPath { get; }
    }
}
