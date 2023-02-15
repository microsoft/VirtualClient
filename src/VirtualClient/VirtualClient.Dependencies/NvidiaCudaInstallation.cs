// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides functionality for installing specific version of CUDA and supported Nvidia GPU driver on linux.
    /// </summary>
    public class NvidiaCudaInstallation : CudaAndNvidiaGPUDriverInstallation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CudaAndNvidiaGPUDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public NvidiaCudaInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }
    }
}
