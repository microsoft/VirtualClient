// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Default. Provides methods for creating descriptors associated with file uploads.
    /// </summary>
    [ComponentDescription(Id = FileUploadDescriptorFactory.Default, Description = "The default file upload descriptor factory implementation.")]
    public class FileUploadDescriptorFactory : IFileUploadDescriptorFactory
    {
        /// <summary>
        /// Default. The name of the default descriptor factory.
        /// </summary>
        public const string Default = nameof(Default);

        /// <summary>
        /// Creates a manifest for the file that can be published alongside the file in a blob store.
        /// </summary>
        /// <param name="component">The component that produced the file.</param>
        /// <param name="file">The file for which to create the manifest.</param>
        /// <param name="blobPath">The path/virtual path to the blob itself.</param>
        /// <param name="blobContainer">The name of the blob container.</param>
        /// <param name="toolname">The name of the toolset that produced the file (e.g. GeekbenchExecutor, Geekbench5)</param>
        /// <param name="manifest">Supplemental information to include in the manifest.</param>
        /// <returns></returns>
        public static IDictionary<string, IConvertible> CreateManifest(VirtualClientComponent component, IFileInfo file, string blobPath, string blobContainer, string toolname = null, IDictionary<string, IConvertible> manifest = null)
        {
            // Create the default manifest information.
            IDictionary<string, IConvertible> fileManifest = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "agentId", component.AgentId },
                { "experimentId", component.ExperimentId },
                { "platform", component.PlatformSpecifics.PlatformArchitectureName },
                { "toolName", toolname ?? component.TypeName },
                { "scenario", component.Scenario },
                { "blobPath", blobPath },
                { "blobContainer", blobContainer },
                { "fileName", file.Name },
                { "fileCreationTime", file.CreationTime.ToString("o") },
                { "fileCreationTimeUtc", file.CreationTimeUtc.ToString("o") }
            };

            if (component.Roles?.Any() == true)
            {
                fileManifest["Role"] = string.Join(',', component.Roles);
            }

            // Add in any additional/special metadata provided. Given that the user supplied these
            // they take priority over existing metadata and will override it.
            if (manifest?.Any() == true)
            {
                foreach (var entry in manifest)
                {
                    fileManifest[entry.Key] = entry.Value;
                }
            }

            // Add in any metadata provided to the Virtual Client on the command line.
            if (component.Metadata?.Any() == true)
            {
                foreach (var entry in component.Metadata)
                {
                    if (!fileManifest.ContainsKey(entry.Key))
                    {
                        fileManifest[entry.Key] = entry.Value;
                    }
                }
            }

            // Add in any parameters defined on/supplied to the component.
            if (component.Parameters?.Any() == true)
            {
                foreach (var entry in component.Parameters)
                {
                    if (!fileManifest.ContainsKey(entry.Key))
                    {
                        fileManifest[entry.Key] = entry.Value;
                    }
                }
            }

            return fileManifest.ObscureSecrets();
        }

        /// <summary>
        /// Creates a descriptor that represents the path location and content information for a file 
        /// to upload to a blob store. The following examples illustrate the format and structure of the paths.
        /// <para>
        /// Format:
        /// <br/>{container = experiment_id}/{agent_id}/{vc_component_or_tool}/{scenario}/file.txt
        /// <br/>{container = experiment_id}/{agent_id}/{vc_component_or_tool}/{role}/{scenario}/file.txt
        /// <br/><br/>
        /// Examples:
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/fio/fio_randwrite_496gb_12k_d32_th16/2022-03-18T10-00-05-12765Z-fio_randwrite_496gb_12k_d32_th16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/fio/fio_randwrite_496gb_12k_d32_th16/2022-03-18T10-00-05-12765Z-fio_randwrite_496gb_12k_d32_th16.manifest
        /// 
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/client/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-12765Z-ntttcp_tcp_4k_buffer_t1.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/client/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-12765Z-ntttcp_tcp_4k_buffer_t1.manifest
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/server/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-13813Z-ntttcp_tcp_4k_buffer_t1.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/server/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-13813Z-ntttcp_tcp_4k_buffer_t1.manifest
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/client/cps_t16/2022-03-18T10-00-05-13813Z-cps_t16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/client/cps_t16/2022-03-18T10-00-05-13813Z-cps_t16.manifest
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/server/cps_t16/2022-03-18T10-00-06-13813Z-cps_t16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/server/cps_t16/2022-03-18T10-00-06-13813Z-cps_t16.manifest
        /// </para>
        /// </summary>
        /// <param name="component">The component that produced the file.</param>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="contentType">The type of content (e.g. application/octet-stream).</param>
        /// <param name="contentEncoding">The web content encoding (e.g. utf-8).</param>
        /// <param name="toolname">The name of the toolset that produced the file (e.g. FIO, Geekbench5).</param>
        /// <param name="fileTimestamp">A timestamp to include in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log).</param>
        /// <param name="manifest">Information and metadata related to the blob/file. This information will be appended to the default manifest information.</param>
        public FileUploadDescriptor CreateDescriptor(VirtualClientComponent component, IFileInfo file, string contentType, string contentEncoding, string toolname = null, DateTime? fileTimestamp = null, IDictionary<string, IConvertible> manifest = null)
        {
            component.ThrowIfNull(nameof(component));
            file.ThrowIfNull(nameof(file));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            contentEncoding.ThrowIfNullOrWhiteSpace(nameof(contentEncoding));

            string blobName = Path.GetFileName(file.Name);
            if (fileTimestamp != null)
            {
                blobName = FileUploadDescriptor.GetFileName(blobName, fileTimestamp.Value);
            }

            string blobPath = FileUploadDescriptorFactory.CreateBlobPath(component, toolname, fileTimestamp);
            string blobContainer = component.ExperimentId.ToLowerInvariant();

            // Create the default manifest information.
            IDictionary<string, IConvertible> fileManifest = FileUploadDescriptorFactory.CreateManifest(component, file, $"{blobPath}/{blobName}", blobContainer, toolname, manifest);

            FileUploadDescriptor descriptor = new FileUploadDescriptor(
                blobName,
                blobPath,
                blobContainer,
                contentEncoding,
                contentType,
                file.FullName,
                fileManifest);

            return descriptor;
        }

        private static string CreateBlobPath(VirtualClientComponent component, string toolname = null, DateTime? fileTimestamp = null)
        {
            List<string> pathSegments = new List<string>();

            string effectiveAgentId = component.AgentId;
            pathSegments.Add(effectiveAgentId);

            if (!string.IsNullOrWhiteSpace(toolname))
            {
                pathSegments.Add(toolname);
            }
            else
            {
                pathSegments.Add(component.TypeName);
            }

            if (component.Roles?.Any() == true)
            {
                pathSegments.Add(string.Join(',', component.Roles));
            }

            if (!string.IsNullOrWhiteSpace(component.Scenario))
            {
                pathSegments.Add(component.Scenario);
            }

            return BlobDescriptor.SanitizeBlobPath($"/{string.Join('/', pathSegments)}".ToLowerInvariant());
        }
    }
}
