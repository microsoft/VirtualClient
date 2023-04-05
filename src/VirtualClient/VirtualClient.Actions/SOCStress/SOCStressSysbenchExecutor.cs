using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using VirtualClient.Common;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    /// <summary>
    /// SOC stress running using sysbench workload.
    /// It stresses out CPU and Memory.
    /// </summary>
    public class SOCStressSysbenchExecutor : VirtualClientComponent
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="SOCStressSysbenchExecutor"/>.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public SOCStressSysbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) 
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
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.Host));
            }
        }

        /// <summary>
        /// The username for Host.
        /// </summary>
        public string UserName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.UserName));
            }
        }

        /// <summary>
        /// The password for Host.
        /// </summary>
        public string Password
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.Password));
            }
        }

        /// <summary>
        /// Sysbench memory command line.
        /// </summary>
        public string SysbenchMemoryCommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.SysbenchMemoryCommandLine));
            }
        }

        /// <summary>
        /// Sysbench CPU command line.
        /// </summary>
        public string SysbenchCPUCommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.SysbenchCPUCommandLine));
            }
        }

        /// <summary>
        /// Sysbench timeout.
        /// </summary>
        public string SysbenchTimeout
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.SysbenchTimeout));
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using (ISshClientProxy sshClient = this.systemManagement.SshClientManager.CreateSshClient(this.Host, this.UserName, this.Password))
                {
                    sshClient.Connect();

                    string sysbenchMemoryCommandParameters = this.ApplyParameter(this.SysbenchMemoryCommandLine, nameof(this.SysbenchTimeout), this.SysbenchTimeout);
                    string sysbenchCPUCommandParameters = this.ApplyParameter(this.SysbenchCPUCommandLine, nameof(this.SysbenchTimeout), this.SysbenchTimeout);

                    DateTime startTime = DateTime.UtcNow;

                    this.ExecuteSshCommand(sshClient.CreateCommand($"sysbench {sysbenchMemoryCommandParameters} > mem.txt & sysbench {sysbenchCPUCommandParameters} > cpu.txt"));
                    string memoryResults = this.ExecuteSshCommand(sshClient.CreateCommand("cat mem.txt"));
                    string cpuResults = this.ExecuteSshCommand(sshClient.CreateCommand("cat cpu.txt"));

                    this.Logger.LogMessage(
                        $"{typeof(SOCStressSysbenchExecutor)}Memory.ProcessDetails",
                        (Microsoft.Extensions.Logging.LogLevel)LogLevel.Info,
                        telemetryContext.Clone().AddContext("processDetails", memoryResults));

                    this.Logger.LogMessage(
                       $"{typeof(SOCStressSysbenchExecutor)}CPU.ProcessDetails",
                       (Microsoft.Extensions.Logging.LogLevel)LogLevel.Info,
                       telemetryContext.Clone().AddContext("processDetails", cpuResults));

                    this.ExecuteSshCommand(sshClient.CreateCommand("rm mem.txt & rm cpu.txt"));

                    SysbenchMetricsParser sysbenchMemoryMetricsParser = new SysbenchMetricsParser(memoryResults);
                    IList<Metric> sysbenchMemoryMetrics = sysbenchMemoryMetricsParser.Parse().ToList();

                    SysbenchMetricsParser sysbenchCPUMetricsParser = new SysbenchMetricsParser(cpuResults);
                    IList<Metric> sysbenchCPUMetrics = sysbenchCPUMetricsParser.Parse().ToList();

                    DateTime endTime = DateTime.UtcNow;

                    sshClient.Disconnect();

                    this.Logger.LogMetrics(
                        "SOC Stress Sysbench",
                        "SOC Stress Sysbench Memory",
                        startTime,
                        endTime,
                        sysbenchMemoryMetrics,
                        null,
                        sysbenchMemoryCommandParameters,
                        this.Tags,
                        telemetryContext);

                    this.Logger.LogMetrics(
                        "SOC Stress Sysbench",
                        "SOC Stress Sysbench CPU",
                        startTime,
                        endTime,
                        sysbenchCPUMetrics,
                        null,
                        sysbenchCPUCommandParameters,
                        this.Tags,
                        telemetryContext);
                }
            });
        }

        private string ExecuteSshCommand(ISshCommandProxy sshCommand)
        {
            string result = sshCommand.Execute();

            if (sshCommand.ExitStatus != 0)
            {
                throw new WorkloadException($"ExitCode:{sshCommand.ExitStatus} ErrorMessage:\"{sshCommand.Error}\"", ErrorReason.WorkloadFailed);
            }

            return result;
        }
    }
}
