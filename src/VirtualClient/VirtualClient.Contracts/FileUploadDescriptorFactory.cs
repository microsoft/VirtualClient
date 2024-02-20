// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.CodeAnalysis;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Default. Provides methods for creating descriptors associated with file uploads.
    /// </summary>
    public static class FileUploadDescriptorFactory
    {
        private const string DefaultContentPathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";

        private static readonly Regex TemplatePlaceholderExpression = new Regex(@"\{(.*?)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Creates a descriptor that represents the path location and content information for a file 
        /// to upload to a blob store.
        /// <para>
        /// Format:
        /// <br/>{container = experimentId}/{agentId}/{toolname}/{parameters:scenario}/file.txt
        /// <br/>{container = experimentId}/{agentId}/{toolname}/{parameters:scenario}/{file-creationtime-utc}file.txt
        /// <br/>{container = experimentId}/{agentId}/{toolname}/{role}/{parameters:scenario}/file.txt
        /// <br/>{container = experimentId}/{agentId}/{toolname}/{role}/{parameters:scenario}/{fileTimestamp}-file.txt
        /// <br/><br/>
        /// Examples:
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/fio/fio_randwrite_496gb_12k_d32_th16/2022-03-18T10-00-05-12765Z-fio_randwrite_496gb_12k_d32_th16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/fio/fio_randwrite_496gb_12k_d32_th16/2022-03-18T10-00-05-12765Z-fio_randwrite_496gb_12k_d32_th16.manifest.json
        /// <br/><br/>
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/client/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-12765Z-ntttcp_tcp_4k_buffer_t1.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/client/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-12765Z-ntttcp_tcp_4k_buffer_t1.manifest.json
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/server/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-13813Z-ntttcp_tcp_4k_buffer_t1.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/ntttcp/server/ntttcp_tcp_4k_buffer_t1/2022-03-18T10-00-05-13813Z-ntttcp_tcp_4k_buffer_t1.manifest.json
        /// <br/><br/>
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/client/cps_t16/2022-03-18T10-00-05-13813Z-cps_t16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/client/cps_t16/2022-03-18T10-00-05-13813Z-cps_t16.manifest.json
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/server/cps_t16/2022-03-18T10-00-06-13813Z-cps_t16.log
        /// <br/>b9d30758-20a7-4779-826e-137c31a867e1/agent01/cps/server/cps_t16/2022-03-18T10-00-06-13813Z-cps_t16.manifest.json
        /// </para>
        /// </summary>
        /// <param name="fileContext">Provides context about a file to be uploaded.</param>
        /// <param name="parameters">Parameters related to the component that produced the file (e.g. the parameters from the component).</param>
        /// <param name="metadata">Additional metadata related to the blob/file to include in the descriptor with the default manifest information.</param>
        /// <param name="timestamped">
        /// True to to include the file creation time in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log). This is explicit to allow for cases where modification of the 
        /// file name is not desirable. Default = true (timestamped file names).
        /// </param>
        /// <param name="pathTemplate">Content path template to use when uploading content to target storage resources.</param>
        public static FileUploadDescriptor CreateDescriptor(FileContext fileContext, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> metadata = null, bool timestamped = true, string pathTemplate = null)
        {
            fileContext.ThrowIfNull(nameof(fileContext));

            string blobName = Path.GetFileName(fileContext.File.Name);

            if (timestamped)
            {
                blobName = FileUploadDescriptor.GetFileName(blobName, fileContext.File.CreationTimeUtc);
            }

            // The caller of this factory method makes the determination on the runtime parameters that are 
            IDictionary<string, IConvertible> runtimeParameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "experimentId", fileContext.ExperimentId },
                { "agentId", fileContext.AgentId },
                { "toolName", fileContext.ToolName },
                { "role", fileContext.Role },
                { "scenario", fileContext.Scenario }
            };

            string effectivePathTemplate = pathTemplate;

            // Backwards Compatibility
            // We originally supported defining the content path template in the metadata (and even in the parameters). For the sake
            // of consistency, we will support this for a bit longer.
            if (effectivePathTemplate == null)
            {
                IConvertible template;
                if (metadata?.TryGetValue(nameof(VirtualClientComponent.ContentPathTemplate), out template) == true)
                {
                    // e.g.
                    // --metadata="ContentPathTemplate={ExperimentId}/{AgentId}..."
                    effectivePathTemplate = template?.ToString();
                }
                else if (parameters?.TryGetValue(nameof(VirtualClientComponent.ContentPathTemplate), out template) == true)
                {
                    // e.g.
                    // --parameters="ContentPathTemplate={ExperimentId}/{AgentId}..."
                    effectivePathTemplate = template?.ToString();
                }
            }

            effectivePathTemplate = effectivePathTemplate ?? FileUploadDescriptorFactory.DefaultContentPathTemplate;
            string resolvedTemplate = FileUploadDescriptorFactory.ResolveContentPathTemplateParts(effectivePathTemplate, runtimeParameters, parameters, metadata);

            string[] resolvedTemplateParts = resolvedTemplate?.Split("/", StringSplitOptions.RemoveEmptyEntries);
            if (resolvedTemplateParts?.Any() != true)
            {
                // This is not expected ever but we perform the check in case of issues caused by
                // future refactorings.
                throw new SchemaException(
                    $"Invalid content path template. The content path template supplied '{effectivePathTemplate}' is not a valid path template.");
            }

            string blobContainer = resolvedTemplateParts.ElementAt(0)?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(blobContainer))
            {
                throw new SchemaException($"The container name in the content path template '{effectivePathTemplate}' cannot be empty string.");
            }

            string blobPath = null;
            if (resolvedTemplateParts.Count() > 1)
            {
                blobPath = $"{BlobDescriptor.SanitizeBlobPath($"/{string.Join('/', resolvedTemplateParts.Skip(1))}").ToLowerInvariant()}/{blobName}";
            }
            else
            {
                blobPath = $"/{blobName}";
            }

            IDictionary<string, IConvertible> fileManifest = FileUploadDescriptor.CreateManifest(fileContext, blobContainer, blobPath, parameters, metadata);

            FileUploadDescriptor descriptor = new FileUploadDescriptor(
                blobPath,
                blobContainer,
                fileContext.ContentEncoding,
                fileContext.ContentType,
                fileContext.File.FullName,
                fileManifest);

            return descriptor;
        }

        private static string ResolveContentPathTemplateParts(
            string pathTemplate,
            IDictionary<string, IConvertible> runtimeMetadata,
            IDictionary<string, IConvertible> parameters,
            IDictionary<string, IConvertible> metadata)
        {
            string resolvedTemplate = pathTemplate;
            MatchCollection matches = FileUploadDescriptorFactory.TemplatePlaceholderExpression.Matches(pathTemplate);

            if (matches?.Any() == true)
            {
                string resolvedValue;
                foreach (Match match in matches)
                {
                    // Order of placeholder resolution:
                    // 1) Metadata known by the VC runtime is applied first because it is definitive.
                    // 2) Component metadata supplied to the factory.
                    // 3) Component parameters supplied to the factory.
                    if (FileUploadDescriptorFactory.TryResolvePlaceholder(runtimeMetadata, match.Groups[1].Value, out resolvedValue))
                    {
                        resolvedTemplate = resolvedTemplate.Replace(match.Value, resolvedValue);
                    }
                    else if (metadata?.Any() == true && FileUploadDescriptorFactory.TryResolvePlaceholder(metadata, match.Groups[1].Value, out resolvedValue))
                    {
                        resolvedTemplate = resolvedTemplate.Replace(match.Value, resolvedValue);
                    }
                    else if (parameters?.Any() == true && FileUploadDescriptorFactory.TryResolvePlaceholder(parameters, match.Groups[1].Value, out resolvedValue))
                    {
                        resolvedTemplate = resolvedTemplate.Replace(match.Value, resolvedValue);
                    }
                    else
                    {
                        resolvedTemplate = resolvedTemplate.Replace(match.Value, string.Empty);
                    }
                }
            }

            return resolvedTemplate.Replace("//", "/");
        }

        private static bool TryResolvePlaceholder(IDictionary<string, IConvertible> metadata, string propertyName, out string resolvedValue)
        {
            resolvedValue = null;
            if (!string.IsNullOrWhiteSpace(propertyName) && metadata.TryGetValue(propertyName, out IConvertible propertyValue) && propertyValue != null)
            {
                resolvedValue = propertyValue.ToString();
            }

            return resolvedValue != null;
        }
    }
}
