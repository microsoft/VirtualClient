// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Azure.Core;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines a reference to a profile whether on the local system or remote.
    /// </summary>
    public class DependencyProfileReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyProfileReference"/>.
        /// </summary>
        /// <param name="profileName">The name of path to the profile on the local system.</param>
        public DependencyProfileReference(string profileName)
        {
            profileName.ThrowIfNullOrWhiteSpace(nameof(profileName));
            this.ProfileName = profileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyProfileReference"/>.
        /// </summary>
        /// <param name="profileUri">A URI from which the profile can be downloaded.</param>
        public DependencyProfileReference(Uri profileUri)
        {
            profileUri.ThrowIfNull(nameof(profileUri));
            this.ProfileUri = profileUri;
            this.ProfileName = profileUri.Segments.Last();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyProfileReference"/>.
        /// </summary>
        /// <param name="profileUri">A URI from which the profile can be downloaded.</param>
        /// <param name="credentials">An identity token credential to use for authentication against the target storage.</param>
        public DependencyProfileReference(Uri profileUri, TokenCredential credentials)
            : this(profileUri)
        {
            credentials.ThrowIfNull(nameof(credentials));
            this.Credentials = credentials;
        }

        /// <summary>
        /// The name of the profile (e.g. PERF-CPU-OPENSSL-EXT.json).
        /// </summary>
        public string ProfileName { get; }

        /// <summary>
        /// A URI to the profile that can be used to download it from a remote location.
        /// </summary>
        public Uri ProfileUri { get; }

        /// <summary>
        /// True/false whether the profile name provided is a full path location on the
        /// local file system.
        /// </summary>
        public bool IsFullPath
        {
            get
            {
                return PlatformSpecifics.IsFullyQualifiedPath(this.ProfileName);
            }
        }

        /// <summary>
        /// An identity token credential to use for authentication against the Storage Account. 
        /// </summary>
        public TokenCredential Credentials { get; }
    }
}
