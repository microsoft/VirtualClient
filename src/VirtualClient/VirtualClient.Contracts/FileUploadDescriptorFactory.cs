// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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
        /// <param name="contentPathPattern">Content path template to use when uploading content to target storage resources.</param>
        /// <param name="parameters">Parameters related to the component that produced the file (e.g. the parameters from the component).</param>
        /// <param name="manifest">Additional information and metadata related to the blob/file to include in the descriptor alongside the default manifest information.</param>
        /// <param name="timestamped">
        /// True to to include the file creation time in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log). This is explicit to allow for cases where modification of the 
        /// file name is not desirable. Default = true (timestamped file names).
        /// </param>
        public FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentPathPattern, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true)
        {
            fileContext.ThrowIfNull(nameof(fileContext));
            contentPathPattern.ThrowIfNullOrWhiteSpace(nameof(contentPathPattern));

            string blobName = Path.GetFileName(fileContext.File.Name);

            if (timestamped)
            {
                blobName = FileUploadDescriptor.GetFileName(blobName, fileContext.File.CreationTimeUtc);
            }

            string blobContainer = GetInlinedContentArgumentValue(fileContext, contentPathPattern.Split('/')[0]);
            if (string.IsNullOrWhiteSpace(blobContainer))
            {
                throw new ArgumentException("The containerName in blob cannot be empty string.", contentPathPattern);
            }

            string blobPath = FileUploadDescriptorFactory.CreateBlobPath(fileContext, contentPathPattern, blobName);
            
            // Create the default manifest information.
            IDictionary<string, IConvertible> fileManifest = FileUploadDescriptor.CreateManifest(fileContext, blobContainer, blobPath, parameters, manifest);

            FileUploadDescriptor descriptor = new FileUploadDescriptor(
                blobPath,
                blobContainer,
                fileContext.ContentEncoding,
                fileContext.ContentType,
                fileContext.File.FullName,
                fileManifest);

            return descriptor;
        }

        private static string CreateBlobPath(FileContext fileContext, string contentPathPattern, string blobName)
        {
            string blobPath = null;
            List<string> pathSegments = new List<string>();

            int i = 0;
            foreach (string element in contentPathPattern.Split('/'))
            {
                if (i == 0)
                {
                    i++;
                    continue;
                }

                string segment = GetInlinedContentArgumentValue(fileContext, element);

                if (!string.IsNullOrWhiteSpace(segment))
                {
                    pathSegments.Add(segment);
                }

                i++;
            }

            if (pathSegments.Any())
            {
                blobPath = $"{BlobDescriptor.SanitizeBlobPath($"/{string.Join('/', pathSegments)}").ToLowerInvariant()}/{blobName}";
            }
            else
            {
                blobPath = $"/{blobName}";
            }

            return blobPath;
        }

        private static string GetInlinedContentArgumentValue(FileContext fileContext, string contentArgumentName)
        {
            IDictionary<string, IConvertible> fileContextDictionary = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "experimentId", fileContext.ExperimentId },
                { "agentId", fileContext.AgentId },
                { "toolName", fileContext.ToolName },
                { "role", fileContext.Role },
                { "scenario", fileContext.Scenario }
            };

            string inlinedArgument = Regex.Replace(contentArgumentName, @"\{(.*?)\}", match =>
            {
                string paramName = match.Groups[1].Value;
                if (fileContextDictionary.ContainsKey(paramName))
                {
                    return fileContextDictionary.GetValue<string>(paramName, string.Empty);
                }

                return string.Empty;
            });

            return inlinedArgument;
        }
    }
}
