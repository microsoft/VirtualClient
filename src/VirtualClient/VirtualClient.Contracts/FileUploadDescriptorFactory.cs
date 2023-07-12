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
        /// <param name="contentPathTemplate">Content path template to use when uploading content to target storage resources.</param>
        /// <param name="parameters">Parameters related to the component that produced the file (e.g. the parameters from the component).</param>
        /// <param name="manifest">Additional information and metadata related to the blob/file to include in the descriptor alongside the default manifest information.</param>
        /// <param name="timestamped">
        /// True to to include the file creation time in the file name (e.g. 2023-05-21t09-23-30-23813z-file.log). This is explicit to allow for cases where modification of the 
        /// file name is not desirable. Default = true (timestamped file names).
        /// </param>
        public FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentPathTemplate, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true)
        {
            fileContext.ThrowIfNull(nameof(fileContext));
            string blobName = Path.GetFileName(fileContext.File.Name);

            if (timestamped)
            {
                blobName = FileUploadDescriptor.GetFileName(blobName, fileContext.File.CreationTimeUtc);
            }

            string blobContainer = GetContentArgumentValue(fileContext, contentPathTemplate.Split('/')[0], parameters);
            if (string.IsNullOrWhiteSpace(blobContainer))
            {
                throw new ArgumentException("The containerName in blob cannot be empty string.", contentPathTemplate);
            }

            string blobPath = FileUploadDescriptorFactory.CreateBlobPath(fileContext, contentPathTemplate, parameters, blobName);
            
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

        private static string CreateBlobPath(FileContext fileContext, string contentPathTemplate, IDictionary<string, IConvertible> parameters, string blobName)
        {
            string blobPath = null;
            List<string> pathSegments = new List<string>();

            int i = 0;
            foreach (string element in contentPathTemplate.Split('/'))
            {
                if (i == 0)
                {
                    i++;
                    continue;
                }

                string segment = GetContentArgumentValue(fileContext, element, parameters);

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

        private static string GetContentArgumentValue(FileContext fileContext, string contentArgumentName, IDictionary<string, IConvertible> parameters)
        {
            string blobPathComponent = string.Empty;

            Match match = Regex.Match(contentArgumentName, @"\{([^}]+)\}");

            if (match.Success)
            {
                string elementName = match.Groups[1].Value;
                // Each Argument is checked for in FileContext (for some standard fields) and then in Parameters.
                if (!string.IsNullOrWhiteSpace(GetContentNameFromFileContext(fileContext, elementName)))
                {
                    blobPathComponent = GetContentNameFromFileContext(fileContext, elementName);
                }
                else
                {
                    blobPathComponent = GetContentNameFromParameters(parameters, elementName);
                }
            }
            else
            {
                blobPathComponent = contentArgumentName;
            }

            return blobPathComponent;
        }

        private static string GetContentNameFromFileContext(FileContext filecontext, string fieldName)
        {
            if (fieldName.Equals("agentId", StringComparison.OrdinalIgnoreCase))
            {
                return filecontext.AgentId;
            }
            else if (fieldName.Equals("experimentId", StringComparison.OrdinalIgnoreCase))
            {
                return filecontext.ExperimentId;
            }
            else if (fieldName.Equals("toolName", StringComparison.OrdinalIgnoreCase))
            {
                return filecontext.ToolName;
            }
            else if (fieldName.Equals("scenario", StringComparison.OrdinalIgnoreCase))
            {
                return filecontext.Scenario;
            }
            else if (fieldName.Equals("role", StringComparison.OrdinalIgnoreCase))
            {
                return filecontext.Role;
            }

            return string.Empty;
        }

        private static string GetContentNameFromParameters(IDictionary<string, IConvertible> parameters, string fieldName)
        {
            if (parameters == null)
            {
                return string.Empty;
            }

            return parameters.GetValue<string>(fieldName, string.Empty);
        }
    }
}
