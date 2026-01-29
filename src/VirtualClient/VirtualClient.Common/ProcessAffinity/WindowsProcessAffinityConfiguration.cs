// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.ProcessAffinity
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Windows-specific CPU affinity configuration using ProcessorAffinity bitmask.
    /// </summary>
    public class WindowsProcessAffinityConfiguration : ProcessAffinityConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsProcessAffinityConfiguration"/> class.
        /// </summary>
        /// <param name="cores">The list of core indices to bind to.</param>
        public WindowsProcessAffinityConfiguration(IEnumerable<int> cores)
            : base(cores)
        {
            this.ValidateCores();
            this.AffinityMask = (IntPtr)this.CalculateAffinityMask();
        }

        /// <summary>
        /// Gets the processor affinity bitmask for the specified cores.
        /// </summary>
        public IntPtr AffinityMask { get; }

        /// <summary>
        /// Applies the CPU affinity to the specified process.
        /// </summary>
        /// <param name="process">The process to apply affinity to.</param>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        public void ApplyAffinity(IProcessProxy process)
        {
            process.ThrowIfNull(nameof(process));

            if (process.HasExited)
            {
                throw new InvalidOperationException("Cannot set affinity on a process that has already exited.");
            }

            try
            {
                // Check if this is a test/mock process with OnApplyAffinity delegate
                // We use reflection to avoid a hard dependency on VirtualClient.TestFramework
                System.Reflection.PropertyInfo affinityDelegateProperty = process.GetType().GetProperty(
                    "OnApplyAffinity",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                if (affinityDelegateProperty != null)
                {
                    // This is a test/mock process - invoke the delegate instead of accessing real process
                    object delegateValue = affinityDelegateProperty.GetValue(process);
                    if (delegateValue != null)
                    {
                        (delegateValue as Action<IntPtr>)?.Invoke(this.AffinityMask);
                        return;
                    }
                }

                // Access the underlying Process through ProcessProxy
                ProcessProxy processProxy = process as ProcessProxy;
                if (processProxy == null)
                {
                    throw new NotSupportedException(
                        $"Cannot apply CPU affinity. The process proxy type '{process.GetType().Name}' does not support affinity configuration.");
                }

                // Use reflection to access the protected UnderlyingProcess property
                System.Reflection.PropertyInfo propertyInfo = typeof(ProcessProxy).GetProperty(
                    "UnderlyingProcess",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    
                if (propertyInfo == null)
                {
                    throw new NotSupportedException("Unable to access the underlying process for affinity configuration.");
                }

                System.Diagnostics.Process underlyingProcess = propertyInfo.GetValue(processProxy) as System.Diagnostics.Process;
                if (underlyingProcess != null)
                {
                    underlyingProcess.ProcessorAffinity = this.AffinityMask;
                }
            }
            catch (Exception ex) when (!(ex is InvalidOperationException || ex is NotSupportedException))
            {
                throw new InvalidOperationException(
                    $"Failed to set processor affinity to cores [{this}] for process '{process.Name}' (PID: {process.Id}). " +
                    $"Affinity mask: 0x{this.AffinityMask.ToInt64():X}",
                    ex);
            }
        }

        /// <summary>
        /// Gets a string representation including the affinity mask.
        /// </summary>
        public override string ToString()
        {
            return $"{base.ToString()} (Mask: 0x{this.AffinityMask.ToInt64():X})";
        }

        private long CalculateAffinityMask()
        {
            long mask = 0;
            foreach (int core in this.Cores)
            {
                mask |= (1L << core);
            }

            return mask;
        }

        private void ValidateCores()
        {
            // Windows supports up to 64 cores per processor group (using 64-bit affinity mask)
            const int MaxCoresPerGroup = 64;

            int maxCore = this.Cores.Max();
            if (maxCore >= MaxCoresPerGroup)
            {
                throw new NotSupportedException(
                    $"Core index {maxCore} exceeds the maximum supported core index ({MaxCoresPerGroup - 1}) for processor affinity on Windows. " +
                    $"Windows supports up to {MaxCoresPerGroup} cores per processor group. For systems with more than {MaxCoresPerGroup} cores, " +
                    $"consider using processor groups or contact the Virtual Client team for extended support.");
            }
        }
    }
}
