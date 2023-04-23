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
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (ISshClientProxy sshClient = this.systemManagement.SshClientManager.CreateSshClient(this.Host, this.UserName, this.Password))
            {
                sshClient.Connect();

                string sysbenchMemoryCommandParameters = this.ApplyParameter(this.SysbenchMemoryCommandLine, nameof(this.SysbenchTimeout), this.SysbenchTimeout);
                string sysbenchCPUCommandParameters = this.ApplyParameter(this.SysbenchCPUCommandLine, nameof(this.SysbenchTimeout), this.SysbenchTimeout);

                DateTime startTime = DateTime.UtcNow;

                await this.ExecuteSshCommandAsync(
                    sshClient.CreateCommand($"sysbench {sysbenchMemoryCommandParameters} > mem.txt & sysbench {sysbenchCPUCommandParameters} > cpu.txt"),
                    telemetryContext,
                    cancellationToken)
                    .ConfigureAwait(false);

                string memoryResults = await this.ExecuteSshCommandAsync(
                    sshClient.CreateCommand("cat mem.txt"),
                    telemetryContext,
                    cancellationToken)
                    .ConfigureAwait(false);

                string cpuResults = await this.ExecuteSshCommandAsync(
                    sshClient.CreateCommand("cat cpu.txt"),
                    telemetryContext,
                    cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteSshCommandAsync(
                    sshClient.CreateCommand("rm mem.txt & rm cpu.txt"),
                    telemetryContext,
                    cancellationToken)
                    .ConfigureAwait(false);

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
        }

        private async Task<string> ExecuteSshCommandAsync(ISshCommandProxy sshCommand, EventContext telemetryContext,  CancellationToken cancellationToken)
        {
            string result = sshCommand.Execute();

            if (sshCommand.ExitStatus != 0)
            {
                throw new WorkloadException($"ExitCode:{sshCommand.ExitStatus} ErrorMessage:\"{sshCommand.Error}\"", ErrorReason.WorkloadFailed);
            }

            await this.LogSshCommandDetailsAsync(sshCommand, telemetryContext, logToFile: true)
                .ConfigureAwait(false);
            return result;
        }
    }
}
