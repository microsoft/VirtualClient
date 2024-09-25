// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.Serialization;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for the NTttcp workload.
    /// </summary>
    public class NTttcpMetricsParser : MetricsParser
    {
        private readonly bool isClient;
        private NTttcpResult result;
        
        /// <summary>
        /// Parser for the NTttcp workload
        /// </summary>
        /// <param name="rawText">The raw text from the NTttcp process.</param>
        /// <param name="isClient">If role of the current VC is client.</param>
        public NTttcpMetricsParser(string rawText, bool isClient)
            : base(rawText)
        {
            this.isClient = isClient;
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(this.RawText)))
                {
                    string root = this.isClient ? "ntttcps" : "ntttcpr";
                    XmlSerializer serializer = new XmlSerializer(typeof(NTttcpResult), new XmlRootAttribute(root));
                    this.result = (NTttcpResult)serializer.Deserialize(stream);
                }
            }
            catch (Exception exc)
            {
                throw new WorkloadException($"Results not found. The workload 'NTttcp' did not produce any valid results.", exc, ErrorReason.WorkloadFailed);
            }

            NTttcpMetric throughputMetric = this.result.Throughput.First(t => t.Units.Equals("mbps"));
            IList<Metric> metricList = new List<Metric>()
            {
                new Metric("TotalBytesMB", this.result.TotalBytesMB.Value, this.result.TotalBytesMB.Units, MetricRelativity.HigherIsBetter),
                new Metric("AvgBytesPerCompl", this.result.AverageBytesPerCompletion.Value, this.result.AverageBytesPerCompletion.Units, MetricRelativity.Undefined),
                new Metric("AvgFrameSize", this.result.AverageFrameSize.Value, this.result.AverageFrameSize.Units, MetricRelativity.Undefined),
                new Metric("ThroughputMbps", throughputMetric.Value, throughputMetric.Units, MetricRelativity.HigherIsBetter),
                new Metric("AvgPacketsPerInterrupt", this.result.AveragePacketsPerInterrupt.Value, this.result.AveragePacketsPerInterrupt.Units, MetricRelativity.Undefined),
                new Metric("InterruptsPerSec", this.result.Interrupts.Value, this.result.Interrupts.Units, MetricRelativity.Undefined),
                new Metric("PacketsRetransmitted", this.result.PacketsRetransmitted, MetricRelativity.LowerIsBetter),
                new Metric("Errors", this.result.Errors, MetricRelativity.LowerIsBetter),
            };

            if (this.result.TcpAverageRtt != null)
            {
                metricList.Add(new Metric("TcpAverageRtt", this.result.TcpAverageRtt.Value, this.result.TcpAverageRtt.Units, MetricRelativity.LowerIsBetter));
            }

            if (this.result.Cycles != null)
            {
                // Name prior to NTttcp version 1.4.0 -> cycles
                metricList.Add(new Metric("CyclesPerByte", this.result.Cycles.Value, this.result.Cycles.Units, MetricRelativity.LowerIsBetter));
            }
            else if (this.result.CyclesPerByte != null)
            {
                // Name change as of NTttcp version 1.4.0 -> cycles_per_byte
                metricList.Add(new Metric("CyclesPerByte", this.result.CyclesPerByte.Value, this.result.CyclesPerByte.Units, MetricRelativity.LowerIsBetter));
            }

            if (this.result.CpuPercent != null)
            {
                // Name prior to NTttcp version 1.4.0 -> cpu
                metricList.Add(new Metric("AvgCpuPercentage", this.result.CpuPercent.Value, this.result.CpuPercent.Units, MetricRelativity.Undefined));
            }
            else if (this.result.CpuBusyAllPercent != null)
            {
                // Name change as of NTttcp version 1.4.0 -> cpu_busy_all
                metricList.Add(new Metric("AvgCpuPercentage", this.result.CpuBusyAllPercent.Value, this.result.CpuBusyAllPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.IdleCpuPercent != null)
            {
                metricList.Add(new Metric("IdleCpuPercent", this.result.IdleCpuPercent.Value, this.result.IdleCpuPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.IowaitCpuPercent != null)
            {
                metricList.Add(new Metric("IowaitCpuPercent", this.result.IowaitCpuPercent.Value, this.result.IowaitCpuPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.SoftirqCpuPercent != null)
            {
                metricList.Add(new Metric("SoftirqCpuPercent", this.result.SoftirqCpuPercent.Value, this.result.SoftirqCpuPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.SystemCpuPercent != null)
            {
                metricList.Add(new Metric("SystemCpuPercent", this.result.SystemCpuPercent.Value, this.result.SystemCpuPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.UserCpuPercent != null)
            {
                metricList.Add(new Metric("UserCpuPercent", this.result.UserCpuPercent.Value, this.result.UserCpuPercent.Units, MetricRelativity.Undefined));
            }

            if (this.result.CpuCores != null)
            {
                this.Metadata["cpuCores"] = this.result.CpuCores.Value;
            }

            if (this.result.CpuSpeed != null)
            {
                this.Metadata["cpuSpeed"] = this.result.CpuSpeed.Value;
            }

            return metricList;
        }

        /// <summary>
        /// POCO object used for deserializing XML output from the 
        /// NTttcp Executor. 
        /// This contract must be made public for the <see cref="XmlSerializer"/>
        /// do not use outside the context of <see cref="NTttcpMetricsParser"/>
        /// </summary>
        public class NTttcpResult
        { 
            /// <summary>
            /// A collection of key value pairs passed into the NTttcp workload.
            /// </summary>
            [XmlElement("parameters")]
            public NTttcpParameters Parameters { get; set; }

            /// <summary>
            /// Total bytes in Megabytes.
            /// </summary>
            [XmlElement("total_bytes")]
            public NTttcpMetric TotalBytesMB { get; set; }

            /// <summary>
            /// The workload duration.
            /// </summary>
            [XmlElement("realtime")]
            public NTttcpMetric RealTime { get; set; }

            /// <summary>
            /// Average bytes per completion.
            /// </summary>
            [XmlElement("avg_bytes_per_compl")]
            public NTttcpMetric AverageBytesPerCompletion { get; set; }

            /// <summary>
            /// Average bytes per completion per thread.
            /// </summary>
            [XmlElement("threads_avg_bytes_per_compl")]
            public NTttcpMetric ThreadsAverageBytesPerCompletion { get; set; }

            /// <summary>
            /// Average frame size.
            /// </summary>
            [XmlElement("avg_frame_size")]
            public NTttcpMetric AverageFrameSize { get; set; }

            /// <summary>
            /// The throughput described in varying units.
            /// </summary>
            [XmlElement("throughput")]
            public NTttcpMetric[] Throughput { get; set; }

            /// <summary>
            /// The total number of buffers.
            /// </summary>
            [XmlElement("total_buffers")]
            public double TotalBuffers { get; set; }

            /// <summary>
            /// The average packets per interruption.
            /// </summary>
            [XmlElement("avg_packets_per_interrupt")]
            public NTttcpMetric AveragePacketsPerInterrupt { get; set; }

            /// <summary>
            /// Number of interrupts.
            /// </summary>
            [XmlElement("interrupts")]
            public NTttcpMetric Interrupts { get; set; }

            /// <summary>
            /// Number of DPCS.
            /// </summary>
            [XmlElement("dpcs")]
            public NTttcpMetric Dpcs { get; set; }

            /// <summary>
            /// Average number of packets per DPC.
            /// </summary>
            [XmlElement("avg_packets_per_dpc")]
            public NTttcpMetric AveragePacketsPerDpcs { get; set; }

            /// <summary>
            /// Number of cycles.
            /// </summary>
            [XmlElement("cycles")]
            public NTttcpMetric Cycles { get; set; }

            /// <summary>
            /// Number of cycles.
            /// </summary>
            [XmlElement("cycles_per_byte")]
            public NTttcpMetric CyclesPerByte { get; set; }

            /// <summary>
            /// Number of packets sent.
            /// </summary>
            [XmlElement("packets_sent")]
            public long PacketsSent { get; set; }

            /// <summary>
            /// Number of packets received.
            /// </summary>
            [XmlElement("packets_received")]
            public long PacketsReceived { get; set; }

            /// <summary>
            /// Number of packets retransmitted.
            /// </summary>
            [XmlElement("packets_retransmitted")]
            public long PacketsRetransmitted { get; set; }

            /// <summary>
            /// Number of errors.
            /// </summary>
            [XmlElement("errors")]
            public long Errors { get; set; }

            /// <summary>
            /// The number of logical CPU cores on the system.
            /// </summary>
            [XmlElement("cpu_cores")]
            public NTttcpMetric CpuCores { get; set; }

            /// <summary>
            /// The Average Cpu utilization expressed as a percent.
            /// </summary>
            [XmlElement("cpu")]
            public NTttcpMetric CpuPercent { get; set; }

            /// <summary>
            /// The Average Cpu utilization expressed as a percent.
            /// </summary>
            [XmlElement("cpu_busy_all")]
            public NTttcpMetric CpuBusyAllPercent { get; set; }

            /// <summary>
            /// The Average Idle Cpu expressed as a percent.
            /// </summary>
            [XmlElement("idle")]
            public NTttcpMetric IdleCpuPercent { get; set; }

            /// <summary>
            /// The Average Cpu utilization in Iowait expressed as a percent.
            /// </summary>
            [XmlElement("iowait")]
            public NTttcpMetric IowaitCpuPercent { get; set; }

            /// <summary>
            /// The Average Cpu utilization for servicing soft interrupts expressed as a percent.
            /// </summary>
            [XmlElement("softirq")]
            public NTttcpMetric SoftirqCpuPercent { get; set; }

            /// <summary>
            /// The Average System Cpu utilization expressed as a percent.
            /// </summary>
            [XmlElement("system")]
            public NTttcpMetric SystemCpuPercent { get; set; }

            /// <summary>
            /// The Average User Cpu utilization expressed as a percent.
            /// </summary>
            [XmlElement("user")]
            public NTttcpMetric UserCpuPercent { get; set; }

            /// <summary>
            /// The speed of the CPU on the system.
            /// </summary>
            [XmlElement("cpu_speed")]
            public NTttcpMetric CpuSpeed { get; set; }

            /// <summary>
            /// The number of processors.
            /// </summary>
            [XmlElement("num_processors")]
            public long ProcessorCount { get; set; }

            /// <summary>
            /// The buffer count.
            /// </summary>
            [XmlElement("bufferCount")]
            public long BufferCount { get; set; }

            /// <summary>
            /// The length of the buffer.
            /// </summary>
            [XmlElement("bufferLen")]
            public long BufferLength { get; set; }

            /// <summary>
            /// The IO.
            /// </summary>
            [XmlElement("io")]
            public long IO { get; set; }

            /// <summary>
            /// Tcp average RTT.
            /// </summary>
            [XmlElement("tcp_average_rtt")]
            [DefaultValue(null)]
            public NTttcpMetric TcpAverageRtt { get; set; }
        }

        /// <summary>
        /// Thread specific results.
        /// </summary>
        public class NTttcpThread
        {
            /// <summary>
            /// The run duration.
            /// </summary>
            [XmlElement("realtime")]
            public NTttcpMetric RealTime { get; set; }

            /// <summary>
            /// The throughput expressed in different units.
            /// </summary>
            [XmlArray]
            [XmlArrayItem("thoughput", typeof(NTttcpMetric))]
            public NTttcpMetric[] Throughput { get; set; }

            /// <summary>
            /// The average bytes per completion.
            /// </summary>
            [XmlElement("avg_bytes_per_compl")]
            public NTttcpMetric AverageBytesPerCompletion { get; set; }

            /// <summary>
            /// The thread index.
            /// </summary>
            [XmlAttribute("index")]
            public int Id { get; set; }
        }

        /// <summary>
        /// A metric produced by the NTttcp workload.
        /// </summary>
        public class NTttcpMetric
        {
            /// <summary>
            /// The units used to express the metric.
            /// </summary>
            [XmlAttribute("metric")]
            public string Units { get; set; }

            /// <summary>
            /// The value of the metric.
            /// </summary>
            [XmlText]
            public double Value { get; set; }
        }

        /// <summary>
        /// Input parameters given to the NTttcp workload.
        /// </summary>
        public class NTttcpParameters
        {
            /// <summary>
            /// The send socket buffer option.
            /// </summary>
            [XmlElement("send_socket_buff")]
            public int SendSocketBuffer { get; set; }

            /// <summary>
            /// The recieve socket buffer option.
            /// </summary>
            [XmlElement("recv_socket_buff")]
            public int ReceiveSocketBuffer { get; set; }

            /// <summary>
            /// The port option.
            /// </summary>
            [XmlElement("port")]
            public int Port { get; set; }

            /// <summary>
            /// The synchronize port option.
            /// </summary>
            [XmlElement("sync_port")]
            public string SynchronizePort { get; set; }

            /// <summary>
            /// The option to not synchronize.
            /// </summary>
            [XmlElement("no_sync")]
            public string NoSynchronization { get; set; }

            /// <summary>
            /// The wait time in milliseconds.
            /// </summary>
            [XmlElement("wait_timeout_milliseconds")]
            public int Timeout { get; set; }

            /// <summary>
            /// The async option.
            /// </summary>
            [XmlElement("async")]
            public string Async { get; set; }

            /// <summary>
            /// The verbose option.
            /// </summary>
            [XmlElement("verbose")]
            public string Verbose { get; set; }

            /// <summary>
            /// The WSA option.
            /// </summary>
            [XmlElement("wsa")]
            public string Wsa { get; set; }

            /// <summary>
            /// The use ipv6 option.
            /// </summary>
            [XmlElement("use_ipv6")]
            public string UseIpv6 { get; set; }

            /// <summary>
            /// The udp option.
            /// </summary>
            [XmlElement("udp")]
            public string Udp { get; set; }

            /// <summary>
            /// The option to verify data.
            /// </summary>
            [XmlElement("verify_data")]
            public string VerifyData { get; set; }

            /// <summary>
            /// The wait all option.
            /// </summary>
            [XmlElement("wait_all")]
            public string WaitAll { get; set; }

            /// <summary>
            /// The run time option.
            /// </summary>
            [XmlElement("run_time")]
            public int RunTime { get; set; }

            /// <summary>
            /// The warmup time option.
            /// </summary>
            [XmlElement("warmup_time")]
            public int WarmupTime { get; set; }

            /// <summary>
            /// The cooldown time option.
            /// </summary>
            [XmlElement("cooldown_time")]
            public int CooldownTime { get; set; }

            /// <summary>
            /// The dash n timeout option.
            /// </summary>
            [XmlElement("dash_n_timeout")]
            public int DashNTimeout { get; set; }

            /// <summary>
            /// The bind sender option.
            /// </summary>
            [XmlElement("bind_sender")]
            public string BindSender { get; set; }

            /// <summary>
            /// The sender name option.
            /// </summary>
            [XmlElement("sender_name")]
            public string SenderName { get; set; }

            /// <summary>
            /// The max active threads option.
            /// </summary>
            [XmlElement("max_active_threads")]
            public int ActiveThreads { get; set; }
        }
    }
}
