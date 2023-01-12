// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Networking Workload Tool Name.
    /// </summary>
    public enum NetworkingWorkloadTool
    {
        /// <summary>
        /// Undefined.
        /// </summary>
        Undefined,

        /// <summary>
        /// NTttcp Tool.
        /// </summary>
        NTttcp,
        
        /// <summary>
        /// Latte Tool. 
        /// </summary>
        Latte,
        
        /// <summary>
        /// CPS Tool.
        /// </summary>
        CPS,

        /// <summary>
        /// Socket Performance Tool.
        /// </summary>
        SockPerf
    }

    /// <summary>
    /// Networking Workload Tool State.
    /// </summary>
    public enum NetworkingWorkloadToolState
    {
        /// <summary>
        /// Stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// Running.
        /// </summary>
        Running,

        /// <summary>
        /// Start.
        /// </summary>
        Start,

        /// <summary>
        /// Stop.
        /// </summary>
        Stop
    }

    /// <summary>
    /// Networking Workload State.
    /// </summary>
    public class NetworkingWorkloadState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadState"/> class.
        /// </summary>
        public NetworkingWorkloadState()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadState"/> class.
        /// </summary>
        [JsonConstructor]
        public NetworkingWorkloadState(IDictionary<string, IConvertible> properties)
            : base(properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadState"/> class.
        /// </summary>
        public NetworkingWorkloadState(
            string packageName,
            string scenario,
            NetworkingWorkloadTool tool,
            NetworkingWorkloadToolState toolState,
            string protocol = null,
            int? threadCount = null,
            string bufferSizeClient = null,
            string bufferSizeServer = null,
            int? connections = null,
            int? testDuration = null,
            int? warmupTime = null,
            int? delayTime = null,
            string testMode = null,
            int? messageSize = null,
            int? port = null,
            bool? receiverMultiClientMode = null,
            bool? senderLastClient = null,
            int? threadsPerServerPort = null,
            int? connectionsPerThread = null,
            string devInterruptsDifferentiator = null,
            string messagesPerSecond = null,
            double? confidenceLevel = null,
            bool profilingEnabled = false,
            string profilingScenario = null,
            string profilingPeriod = null,
            string profilingWarmUpPeriod = null)
        {
            packageName.ThrowIfNull(nameof(packageName));
            scenario.ThrowIfNull(nameof(scenario));
            tool.ThrowIfNull(nameof(tool));
            toolState.ThrowIfNull(nameof(toolState));

            this.Properties[nameof(this.PackageName)] = packageName;
            this.Properties[nameof(this.Scenario)] = scenario;
            this.Properties[nameof(this.Tool)] = tool.ToString();
            this.Properties[nameof(this.ToolState)] = toolState.ToString();
            this.Properties[nameof(this.Protocol)] = protocol;
            this.Properties[nameof(this.ThreadCount)] = threadCount;
            this.Properties[nameof(this.BufferSizeClient)] = bufferSizeClient;
            this.Properties[nameof(this.BufferSizeServer)] = bufferSizeServer;
            this.Properties[nameof(this.Connections)] = connections;
            this.Properties[nameof(this.TestDuration)] = testDuration;
            this.Properties[nameof(this.WarmupTime)] = warmupTime;
            this.Properties[nameof(this.DelayTime)] = delayTime;
            this.Properties[nameof(this.TestMode)] = testMode;
            this.Properties[nameof(this.MessageSize)] = messageSize;
            this.Properties[nameof(this.Port)] = port;
            this.Properties[nameof(this.ReceiverMultiClientMode)] = receiverMultiClientMode;
            this.Properties[nameof(this.SenderLastClient)] = senderLastClient;
            this.Properties[nameof(this.ThreadsPerServerPort)] = threadsPerServerPort;
            this.Properties[nameof(this.ConnectionsPerThread)] = connectionsPerThread;
            this.Properties[nameof(this.DevInterruptsDifferentiator)] = devInterruptsDifferentiator;
            this.Properties[nameof(this.MessagesPerSecond)] = messagesPerSecond;
            this.Properties[nameof(this.ConfidenceLevel)] = confidenceLevel;
            this.Properties[nameof(this.ProfilingEnabled)] = profilingEnabled;
            this.Properties[nameof(this.ProfilingScenario)] = profilingScenario;
            this.Properties[nameof(this.ProfilingPeriod)] = profilingPeriod;
            this.Properties[nameof(this.ProfilingWarmUpPeriod)] = profilingWarmUpPeriod;
        }

        /// <summary>
        /// The workload package name.
        /// </summary>
        public string PackageName
        {
            get
            {
                return this.Properties.GetValue<string>(nameof(this.PackageName));
            }

            set
            {
                this.Properties[nameof(this.PackageName)] = value;
            }
        }

        /// <summary>
        /// Workload/action scenario.
        /// </summary>
        public string Scenario
        {
            get
            {
                return this.Properties.GetValue<string>(nameof(this.Scenario));
            }

            set
            {
                this.Properties[nameof(this.Scenario)] = value;
            }
        }

        /// <summary>
        /// Networking Workload Tool Name.
        /// </summary>
        public NetworkingWorkloadTool Tool
        {
            get
            {
                return this.Properties.GetEnumValue<NetworkingWorkloadTool>(nameof(this.Tool));
            }

            set
            {
                this.Properties[nameof(this.Tool)] = value.ToString();
            }
        }

        /// <summary>
        /// Networking Workload Tool Name. 
        /// </summary>
        public NetworkingWorkloadToolState ToolState
        {
            get
            {
                return this.Properties.GetEnumValue<NetworkingWorkloadToolState>(nameof(this.ToolState));
            }

            set
            {
                this.Properties[nameof(this.ToolState)] = value.ToString();
            }
        }

        /// <summary>
        /// Protocol type (Parameter for Tools: Latte, NTttcp, SockPerf).
        /// </summary>
        public string Protocol
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.Protocol), out IConvertible protocol);
                return protocol?.ToString();
            }

            set
            {
                this.Properties[nameof(this.Protocol)] = value;
            }
        }

        /// <summary>
        /// Concurrent Threads (Parameter for Tools: NTttcp).
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.ThreadCount), 0);
            }
        }

        /// <summary>
        /// Buffer Size Client (Parameter for Tools: NTttcp).
        /// </summary>
        public string BufferSizeClient
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.BufferSizeClient), out IConvertible bufferSizeClient);
                return bufferSizeClient?.ToString();
            }

            set
            {
                this.Properties[nameof(this.BufferSizeClient)] = value;
            }
        }

        /// <summary>
        /// Buffer Size Server (Parameter for Tools: NTttcp).
        /// </summary>
        public string BufferSizeServer
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.BufferSizeServer), out IConvertible bufferSizeServer);
                return bufferSizeServer?.ToString();
            }

            set
            {
                this.Properties[nameof(this.BufferSizeServer)] = value;
            }
        }

        /// <summary>
        /// Number of connections (Parameter for Tools: CPS).
        /// </summary>
        public int Connections
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.Connections), 0);
            }
        }

        /// <summary>
        /// Test run duration (Parameter for Tools: NTttcp, CPS, SockPerf).
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.TestDuration), 0);
            }
        }

        /// <summary>
        /// Warmup Time (Parameter for Tools: CPS).
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.WarmupTime), 0);
            }
        }

        /// <summary>
        /// Delay Time (Parameter for Tools: CPS).
        /// </summary>
        public int DelayTime
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.DelayTime), 0);
            }
        }

        /// <summary>
        /// Test mode (Parameter for Tools: SockPerf).
        /// </summary>
        public string TestMode
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.TestMode), out IConvertible testMode);
                return testMode?.ToString();
            }

            set
            {
                this.Properties[nameof(this.TestMode)] = value;
            }
        }

        /// <summary>
        /// Message Size (Parameter for Tools: SockPerf).
        /// </summary>
        public int MessageSize
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.MessageSize), 0);
            }
        }

        /// <summary>
        /// Port for first thread (Parameter for Tools: NTttcp)
        /// </summary>
        public int Port
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.Port), 0);
            }
        }

        /// <summary>
        /// Server in multi-client mode (Parameter for Tools: NTttcp)
        /// </summary>
        public bool? ReceiverMultiClientMode
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ReceiverMultiClientMode), out IConvertible receiverMultiClientMode);
                return receiverMultiClientMode?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Last client when test is with multi-client mode (Parameter for Tools: NTttcp)
        /// </summary>
        public bool? SenderLastClient
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.SenderLastClient), out IConvertible senderLastClient);
                return senderLastClient?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Number of threads per each server port (Parameter for Tools: NTttcp)
        /// </summary>
        public int? ThreadsPerServerPort
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ThreadsPerServerPort), out IConvertible threadsPerServerPort);
                return threadsPerServerPort?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Number of connections in each sender thread (Parameter for Tools: NTttcp)
        /// </summary>
        public int? ConnectionsPerThread
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ConnectionsPerThread), out IConvertible connectionsPerThread);
                return connectionsPerThread?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Defines true/false whether profiling is enabled.
        /// </summary>
        public bool ProfilingEnabled
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(this.ProfilingEnabled), false);
            }
        }

        /// <summary>
        /// Defines the length of time to run profiling operations.
        /// </summary>
        public TimeSpan ProfilingPeriod
        {
            get
            {
                return this.Properties.GetTimeSpanValue(nameof(this.ProfilingPeriod), new TimeSpan(0, 0, 30));
            }
        }

        /// <summary>
        /// Defines the length of time to wait allowing the system to warm-up before running
        /// profiling operations.
        /// </summary>
        public TimeSpan ProfilingWarmUpPeriod
        {
            get
            {
                return this.Properties.GetTimeSpanValue(nameof(this.ProfilingWarmUpPeriod), TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Defines the scenario associated with the profiling request/operations.
        /// </summary>
        public string ProfilingScenario
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ProfilingScenario), out IConvertible profilingScenario);
                return profilingScenario?.ToString();
            }

            protected set
            {
                this.Properties[nameof(this.ProfilingScenario)] = value;
            }
        }

        /// <summary>
        /// Differentiator for which to convey the number of interrupts (Parameter for Tools: NTttcp)
        /// </summary>
        public string DevInterruptsDifferentiator
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.DevInterruptsDifferentiator), out IConvertible devInterruptsDifferentiator);
                return devInterruptsDifferentiator?.ToString();
            }
        }

        /// <summary>
        /// Number of Messages-Per-Second (Parameter for Tools: SockPerf).
        /// </summary>
        public string MessagesPerSecond
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.MessagesPerSecond), out IConvertible messagesPerSecond);
                return messagesPerSecond?.ToString();
            }

            set
            {
                this.Properties[nameof(this.MessagesPerSecond)] = value;
            }
        }

        /// <summary>
        /// Confidence level used for calculating the confidence intervals.
        /// </summary>
        public double? ConfidenceLevel
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ConfidenceLevel), out IConvertible confidenceLevel);
                return confidenceLevel?.ToDouble(CultureInfo.InvariantCulture);
            }

            set
            {
                this.Properties[nameof(this.ConfidenceLevel)] = value;
            }
        }
    }
}
