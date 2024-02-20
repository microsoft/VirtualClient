// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.Packaging
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides functionality for downloading and installing dependency packages from
    /// a cloud blob store onto the system. Note that this class is for backwards compatibility.
    /// Use the <see cref="DependencyPackageInstallation"/> component instead for new development.
    /// </summary>
    public class BlobPackageInstallation : DependencyPackageInstallation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public BlobPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }
    }
}
