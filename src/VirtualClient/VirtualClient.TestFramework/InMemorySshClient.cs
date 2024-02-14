// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Renci.SshNet;
    using VirtualClient.Common;

    /// <summary>
    /// Represents a fake ssh client.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
    public class InMemorySshClient : Dictionary<string, IConvertible>, ISshClientProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshClient"/>
        /// </summary>
        public InMemorySshClient()
        {
            this.SshCommands = new List<ISshCommandProxy>();
        }

        /// <inheritdoc />
        public ConnectionInfo ConnectionInfo { get; set; }

        /// <summary>
        /// The set of ssh commands created by Ssh Client.
        /// </summary>
        public IEnumerable<ISshCommandProxy> SshCommands { get; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Dispose' method is called.
        /// </summary>
        public Action OnDispose { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'HasExited' property is called.
        /// </summary>
        public Action OnConnect { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Kill' method is called.
        /// </summary>
        public Action OnDisconnect { get; set; }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Start' method is called.
        /// </summary>
        public Func<string, ISshCommandProxy> OnCreateCommand { get; set; }

        public void Connect()
        {
            if (this.OnConnect != null)
            {
                this.OnConnect?.Invoke();
            }
        }

        public ISshCommandProxy CreateCommand(string commandText)
        {
            ISshCommandProxy sshCommand = null;
            if (this.OnCreateCommand != null)
            {
                sshCommand = this.OnCreateCommand?.Invoke(commandText);
            }
            else
            {
                sshCommand = new InMemorySshCommand
                {
                    CommandText = commandText
                };
            }

            (this.SshCommands as List<ISshCommandProxy>).Add(sshCommand);

            return sshCommand;
        }

        public void Disconnect()
        {
            if (this.OnDisconnect != null)
            {
                this.OnDisconnect?.Invoke();
            }
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
        public void Dispose()
        {
            this.OnDispose?.Invoke();
        }
    }
}