// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Monitor captures performance counters from Windows systems using WMI
    /// (CimSession querying Win32_PerfFormattedData_* classes). Required on
    /// bare-metal systems with more than 64 logical processors where the legacy
    /// .NET PerformanceCounter API fails.
    /// </summary>
    /// <remarks>
    /// This subclass always uses the WMI provider regardless of the CounterProvider
    /// parameter or logical processor count. It can be referenced directly in profiles
    /// as an alternative to setting CounterProvider=WMI on the base class.
    /// </remarks>
    public class WindowsWmiPerformanceCounterMonitor : WindowsPerformanceCounterMonitor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsWmiPerformanceCounterMonitor"/> class.
        /// </summary>
        public WindowsWmiPerformanceCounterMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override string CounterProviderName => "WMI";

        /// <summary>
        /// Always uses WMI for counter discovery, bypassing the auto-detection logic.
        /// </summary>
        protected override void LoadCounters(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.LoadWmiCounters(telemetryContext, cancellationToken);
        }
    }
}
