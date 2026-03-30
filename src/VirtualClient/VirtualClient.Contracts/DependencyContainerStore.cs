// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using VirtualClient.Contracts;

    /// <summary>
    /// </summary>
    public class DependencyContainerStore
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageName"></param>
        /// <param name="containerName"></param>
        /// <param name="platformSpecifics"></param>
        public DependencyContainerStore(string imageName, string containerName, PlatformSpecifics platformSpecifics)
        {
            this.ImageName = imageName;
            this.ContainerName = containerName;
            this.PlatformSpecifics = platformSpecifics;
        }

        /// <summary>
        /// </summary>
        public string ImageName { get; }

        /// <summary>
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Gets or sets the platform-specific details for the docker Container.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; set; }
    }
}
