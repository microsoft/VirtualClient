// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides functionality for cloning a git repo on the system.
    /// </summary>
    public class CloneRepo : GitRepoClone
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GitRepoClone"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        /// <remarks>
        /// We are simplifying the name a bit. The term "clone" is synonymous with Git. For backwards compatibility, we are supporting 
        /// the original name in addition to the new name.
        /// </remarks>
        public CloneRepo(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }
    }
}
