﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Represents a description of a file blob to upload to a content store.
    /// </summary>
    public class FileBlobDescriptor : BlobDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDescriptor"/> class.
        /// </summary>
        public FileBlobDescriptor(IFileInfo file)
            : base()
        {
            file.ThrowIfNull(nameof(file));
            this.File = file;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDescriptor"/> class.
        /// </summary>
        public FileBlobDescriptor(IFileInfo file, DependencyDescriptor descriptor)
            : base(descriptor)
        {
            file.ThrowIfNull(nameof(file));
            this.File = file;
        }

        /// <summary>
        /// The file associated with the descriptor.
        /// </summary>
        public IFileInfo File { get; }

        /// <summary>
        /// Creates a standardized blob store path/virtual path to use for storing a file.
        /// </summary>
        /// <param name="file">Describes the file associated with the blob descriptor.</param>
        /// <param name="contentType">The web content type (e.g. text/plain, application/octet-stream).</param>
        /// <param name="experimentId">The ID of the experiment. This ID is typically used as the container for content uploads.</param>
        /// <param name="agentId">Optional parameter defines ID of the agent or instance of the Virtual Client that produced the content.</param>
        /// <param name="componentName">Optional parameter defines the name of the executor, monitor or other component that generated the content (e.g. azureprofiler).</param>
        /// <param name="componentScenario">
        /// Optional parameter defines the scenario for the particular component. This typically represents a subdirectory of the component content directory.
        /// (e.g. /b9d30758-20a7-4779-826e-137c31a867e1/agent01/component/component_scenario/2022-03-18T10:00:05.1276589Z-toolset.log
        /// vs. /b9d30758-20a7-4779-826e-137c31a867e1/agent01/component/2022-03-18T10:00:05.1276589Z-toolset.log)
        /// </param>
        /// <param name="role">A specific role for which the instance of Virtual Client is playing (e.g. Client, Server).</param>
        /// <param name="subPath">
        /// Optional parameter defines a subpath of a file directory to include within the final blob name/path
        /// (e.g. given a file /dev/a/b/c.txt and a subpath of /any/path, the final blob path will be 
        /// {experimentId}/{agentId}/.../any/path/c.txt).
        /// </param>
        /// <param name="subPathAfter">
        /// Optional parameter defines a path within the file directory after which to preserve the subpath within the final blob name/path
        /// (e.g. given 2 files /dev/a/b/c.txt and /dev/a/b.txt and instruction to preserve subpath after '/dev/a', the final blob paths will be 
        /// {experimentId}/{agentId}/.../b/c.txt and {experimentId}/{agentId}/.../b.txt respectively).
        /// </param>
        /// <param name="fileNamePrefix">Prefix to be added to the file name</param>
        /// <param name="fileNameSuffix">Suffix to be added to the file name</param>
        public static FileBlobDescriptor ToBlobDescriptor(
            IFileInfo file,
            string contentType,
            string experimentId,
            string agentId = null,
            string componentName = null,
            string componentScenario = null,
            string role = null,
            string subPath = null,
            string subPathAfter = null,
            string fileNamePrefix = null,
            string fileNameSuffix = null)
        {
            file.ThrowIfNull(nameof(file));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            experimentId.ThrowIfNullOrWhiteSpace(nameof(experimentId));

            if (!string.IsNullOrWhiteSpace(subPath) && !string.IsNullOrWhiteSpace(subPathAfter))
            {
                throw new ArgumentException($"Invalid usage. The '{nameof(subPath)}' and '{nameof(subPathAfter)}' parameters cannot be used at the same time.");
            }

            List<string> pathSegments = new List<string>();

            // Examples:
            // /b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/2022-03-18T10:00:05.1276589Z-PrefixToolsetSuffix.log
            // /b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/ntttcp_tcp_4k_buffer_t1/2022-03-18T10:00:05.1276589Z-PrefixToolsetSuffix.log

            string filePath = file.FullName;
            string normalizedFileName = Path.GetFileName(filePath);
            string blobName = $"{file.CreationTimeUtc.ToString("O")}-{fileNamePrefix}{normalizedFileName}{fileNameSuffix}";

            string effectiveAgentId = agentId;
            if (!string.IsNullOrWhiteSpace(agentId) && !string.IsNullOrWhiteSpace(role))
            {
                effectiveAgentId = $"{effectiveAgentId}-{role}";
            }
            else if (!string.IsNullOrWhiteSpace(role))
            {
                effectiveAgentId = role;
            }

            if (!string.IsNullOrWhiteSpace(effectiveAgentId))
            {
                pathSegments.Add(effectiveAgentId);
            }

            if (!string.IsNullOrWhiteSpace(componentName))
            {
                pathSegments.Add(componentName);
            }

            if (!string.IsNullOrWhiteSpace(componentScenario))
            {
                pathSegments.Add(componentScenario);
            }

            if (!string.IsNullOrWhiteSpace(subPath))
            {
                pathSegments.Add(subPath.Trim('/').Trim('\\'));
            }

            if (!string.IsNullOrWhiteSpace(subPathAfter))
            {
                int indexOfSubPath = filePath.IndexOf(subPathAfter);
                if (indexOfSubPath >= 0)
                {
                    pathSegments.Add(
                        Path.GetDirectoryName(filePath.Replace(subPathAfter, string.Empty, StringComparison.OrdinalIgnoreCase)).Trim('/').Trim('\\'));
                }
            }

            pathSegments.Add(blobName);

            string blobPath = string.Join('/', pathSegments);

            FileBlobDescriptor resultsBlob = new FileBlobDescriptor(file)
            {
                Name = BlobDescriptor.SanitizeBlobPath(blobPath),
                ContainerName = experimentId.ToLowerInvariant(),
                ContentType = contentType
            };

            return resultsBlob;
        }

        /// <summary>
        /// Creates a standardized blob store path/virtual path to use for storing a file.
        /// </summary>
        /// <param name="files">Describes a set of one or more files associated with the blob descriptors.</param>
        /// <param name="contentType">The web content type (e.g. text/plain, application/octet-stream).</param>
        /// <param name="experimentId">The ID of the experiment. This ID is typically used as the container for content uploads.</param>
        /// <param name="agentId">Optional parameter defines ID of the agent or instance of the Virtual Client that produced the content.</param>
        /// <param name="componentName">Optional parameter defines the name of the executor, monitor or other component that generated the content (e.g. azureprofiler).</param>
        /// <param name="componentScenario">
        /// Optional parameter defines the scenario for the particular component. This typically represents a subdirectory of the component content directory.
        /// (e.g. /b9d30758-20a7-4779-826e-137c31a867e1/agent01/component/component_scenario/2022-03-18T10:00:05.1276589Z-toolset.log
        /// vs. /b9d30758-20a7-4779-826e-137c31a867e1/agent01/component/2022-03-18T10:00:05.1276589Z-toolset.log)
        /// </param>
        /// <param name="role">A specific role for which the instance of Virtual Client is playing (e.g. Client, Server).</param>
        /// <param name="subPath">
        /// Optional parameter defines a subpath of a file directory to include within the final blob name/path
        /// (e.g. given a file /dev/a/b/c.txt and a subpath of /any/path, the final blob path will be 
        /// {experimentId}/{agentId}/.../any/path/c.txt).
        /// </param>
        /// <param name="subPathAfter">
        /// Optional parameter defines a path within the file directory after which to preserve the subpath within the final blob name/path
        /// (e.g. given 2 files /dev/a/b/c.txt and /dev/a/b.txt and instruction to preserve subpath after '/dev/a', the final blob paths will be 
        /// {experimentId}/{agentId}/.../b/c.txt and {experimentId}/{agentId}/.../b.txt respectively).
        /// </param>
        /// <param name="fileNamePrefix">Prefix to be added to the file name</param>
        /// <param name="fileNameSuffix">Suffix to be added to the file name</param>
        public static IEnumerable<FileBlobDescriptor> ToBlobDescriptors(
            IEnumerable<IFileInfo> files,
            string contentType,
            string experimentId,
            string agentId = null,
            string componentName = null,
            string componentScenario = null,
            string role = null,
            string subPath = null,
            string subPathAfter = null,
            string fileNamePrefix = null,
            string fileNameSuffix = null)
        {
            files.ThrowIfNullOrEmpty(nameof(files));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            experimentId.ThrowIfNullOrWhiteSpace(nameof(experimentId));

            List<FileBlobDescriptor> result = new List<FileBlobDescriptor>();

            foreach (IFileInfo file in files)
            {
                result.Add(FileBlobDescriptor.ToBlobDescriptor(file, contentType, experimentId, agentId, componentName, componentScenario, role, subPath, subPathAfter, fileNamePrefix, fileNameSuffix));
            }

            return result;
        }
    }
}
