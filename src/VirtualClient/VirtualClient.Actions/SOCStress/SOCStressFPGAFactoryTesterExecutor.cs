namespace VirtualClient.Actions.SOCStress
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public class SOCStressFPGAFactoryTesterExecutor : VirtualClientComponent
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="SOCStressFPGAFactoryTesterExecutor"/>.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public SOCStressFPGAFactoryTesterExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The Host name.
        /// </summary>
        public string Host
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressFPGAFactoryTesterExecutor.Host));
            }
        }

        /// <summary>
        /// The username for Host.
        /// </summary>
        public string UserName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressFPGAFactoryTesterExecutor.UserName));
            }
        }

        /// <summary>
        /// The password for Host.
        /// </summary>
        public string Password
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressFPGAFactoryTesterExecutor.Password));
            }
        }

        /// <summary>
        /// The Host name.
        /// </summary>
        public bool DisableSOCPCIe
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPCIe));
            }
        }

        /// <summary>
        /// The Host name.
        /// </summary>
        public bool DisableSOCDRAM
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCDRAM));
            }
        }

        /// <summary>
        /// The Host name.
        /// </summary>
        public bool DisableSOCPowerVirus
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SOCStressFPGAFactoryTesterExecutor.DisableSOCPowerVirus));
            }
        }

        /// <summary>
        /// The Host name.
        /// </summary>
        public bool FPGAStressVerbose
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SOCStressFPGAFactoryTesterExecutor.FPGAStressVerbose));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FPGAFacotryTesterTimeout
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressFPGAFactoryTesterExecutor.FPGAFacotryTesterTimeout));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using (ISshClientProxy sshClient = this.systemManagement.SshClientManager.CreateSshClient(this.Host, this.UserName, this.Password))
                {
                    sshClient.Connect();

                    string fpgaFactoryTesterCommand = $"fpgafactorytester -duration {this.FPGAFacotryTesterTimeout}";
                    if (this.DisableSOCPCIe)
                    {
                        fpgaFactoryTesterCommand += " -pcie 0";
                    }

                    if (this.DisableSOCDRAM)
                    {
                        fpgaFactoryTesterCommand += " -dram 0";
                    }

                    if (this.DisableSOCPowerVirus)
                    {
                        fpgaFactoryTesterCommand += " -virus 0";
                    }

                    if (this.FPGAStressVerbose)
                    {
                        fpgaFactoryTesterCommand += " -verbose";
                    }

                    string fpgaFactoryTesterOutput = sshClient.CreateCommand(fpgaFactoryTesterCommand).Execute();

                    this.Logger.LogMessage(
                       $"{typeof(SOCStressFPGAFactoryTesterExecutor)}.ProcessDetails",
                       (Microsoft.Extensions.Logging.LogLevel)LogLevel.Information,
                       telemetryContext.Clone().AddContext("processDetails", fpgaFactoryTesterOutput));

                    sshClient.Disconnect();
                }

            });
        }
    }
}
