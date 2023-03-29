using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using Renci.SshNet;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

namespace VirtualClient.Actions
{
    /// <summary>
    /// 
    /// </summary>
    public class SOCStressSysbenchExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Constructor for <see cref="SOCStressSysbenchExecutor"/>.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public SOCStressSysbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) 
            : base(dependencies, parameters)
        {
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
        /// Parameters for sysbench memory.
        /// </summary>
        public string SysbenchMemoryParameters
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.SysbenchMemoryParameters));
            }
        }

        /// <summary>
        /// Parameters for sysbench CPU.
        /// </summary>
        public string SysbenchCPUParameters
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SOCStressSysbenchExecutor.SysbenchCPUParameters));
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
        /// Executes SOC stress using sysbench workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                using (SshClient sshClient = new SshClient(this.Host, this.UserName, this.Password))
                {
                    sshClient.Connect();

                    string sysbenchMemoryCommandParameters = this.ApplyParameter(this.SysbenchMemoryParameters, nameof(this.SysbenchTimeout), this.SysbenchTimeout);
                    string sysbenchCPUCommandParameters = this.ApplyParameter(this.SysbenchCPUParameters, nameof(this.SysbenchTimeout), this.SysbenchTimeout);

                    DateTime startTime = DateTime.UtcNow;

                    sshClient.CreateCommand($"sysbench {sysbenchMemoryCommandParameters} > mem.txt & sysbench {sysbenchCPUCommandParameters} > cpu.txt").Execute();
                    string memoryResults = sshClient.CreateCommand("cat mem.txt").Execute();
                    string cpuResults = sshClient.CreateCommand("cat cpu.txt").Execute();

                    this.Logger.LogMessage(
                        $"{typeof(SOCStressSysbenchExecutor)}Memory.ProcessDetails",
                        (Microsoft.Extensions.Logging.LogLevel)LogLevel.Info,
                        telemetryContext.Clone().AddContext("processDetails", memoryResults));

                    this.Logger.LogMessage(
                       $"{typeof(SOCStressSysbenchExecutor)}CPU.ProcessDetails",
                       (Microsoft.Extensions.Logging.LogLevel)LogLevel.Info,
                       telemetryContext.Clone().AddContext("processDetails", cpuResults));

                    sshClient.CreateCommand("rm mem.txt & rm cpu.txt").Execute();

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
    }
}
