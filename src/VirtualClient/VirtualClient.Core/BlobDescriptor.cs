// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using Microsoft.Extensions.Azure;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Represents the address location of a blob.
    /// </summary>
    /// <remarks>
    /// Semantics:
    /// The address of a blob is the combination of the following properties:
    /// - The container in which it is stored.
    /// - The name of the blob itself.
    /// 
    /// Naming Conventions:
    /// https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata
    /// </remarks>
    public class BlobDescriptor : DependencyDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDescriptor"/> class.
        /// </summary>
        public BlobDescriptor()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobDescriptor"/> class.
        /// </summary>
        public BlobDescriptor(DependencyDescriptor descriptor)
            : base(descriptor)
        {
        }

        /// <summary>
        /// Gets the name of the blob container.
        /// </summary>
        public string ContainerName
        {
            get
            {
                return this.GetValue<string>(nameof(this.ContainerName));
            }

            set
            {
                this[nameof(this.ContainerName)] = value;
            }
        }

        /// <summary>
        /// Gets the content encoding of the blob data (e.g. UTF-8).
        /// </summary>
        public Encoding ContentEncoding
        {
            get
            {
                return Encoding.GetEncoding(this.GetValue<string>(nameof(this.ContentEncoding), Encoding.UTF8.WebName));
            }

            set
            {
                this[nameof(this.ContentEncoding)] = value.WebName;
            }
        }

        /// <summary>
        /// Gets the content type of the blob data (e.g. text/plain).
        /// </summary>
        public string ContentType
        {
            get
            {
                this.TryGetValue(nameof(this.ContentType), out IConvertible contentType);
                return contentType?.ToString();
            }

            set
            {
                this[nameof(this.ContentType)] = value;
            }
        }

        /// <summary>
        /// Gets or sets the eTag to use when updating an existing blob
        /// to preserve optimistic concurrency.
        /// </summary>
        public string ETag
        {
            get
            {
                this.TryGetValue(nameof(this.ETag), out IConvertible eTag);
                return eTag?.ToString();
            }

            set
            {
                this[nameof(this.ETag)] = value;
            }
        }

        /// <summary>
        /// Creates a standardized blob store path/virtual path to use for storing a file.
        /// </summary>
        /// <param name="experimentId">Experiment Id</param>
        /// <param name="agentId">The ID of the agent or instance of the Virtual Client that produced the file.</param>
        /// <param name="componentName">The name of the executor, monitor or other component that generated the file (e.g. azureprofiler).</param>
        /// <param name="fileName">The name of the file as it should be represented in the blob store (e.g. azureprofiler.bin).</param>
        /// <param name="timestamp">An optional timestamp to use in as part of the file name to ensure uniqueness of name within the blob store.</param>
        /// <param name="role">The role of the vc.</param>
        /// <param name="directoryPrefix">An optional value used to define the scenario for which the file is associated (e.g. NTttcp_TCP_124K_Buffer_T1).</param>
        /// <param name="contentType">Content type, default to text/plain</param>
        public static BlobDescriptor ToBlobDescriptor(
            string experimentId, 
            string agentId, 
            string componentName, 
            string fileName, 
            DateTime? timestamp = null, 
            string role = null,
            string directoryPrefix = null,
            string contentType = CommonContentType.PlainText)
        {
            agentId.ThrowIfNullOrWhiteSpace(nameof(agentId));
            componentName.ThrowIfNullOrWhiteSpace(nameof(componentName));
            fileName.ThrowIfNullOrWhiteSpace(nameof(fileName));

            DateTime effectiveTimestamp = timestamp ?? DateTime.UtcNow;
            if (timestamp != null)
            {
                if (timestamp.Value.Kind != DateTimeKind.Utc)
                {
                    effectiveTimestamp = timestamp.Value.ToUniversalTime();
                }
            }

            agentId = string.IsNullOrWhiteSpace(role) ? 
                agentId : 
                $"{agentId}-{role}";

            fileName = BlobDescriptor.SanitizeBlobPath(fileName);

            string blobName = string.IsNullOrWhiteSpace(directoryPrefix) ?
                $"{agentId}/{componentName}/{effectiveTimestamp.ToString("O")}-{fileName}".ToLowerInvariant() :
                $"{agentId}/{componentName}/{directoryPrefix}/{effectiveTimestamp.ToString("O")}-{fileName}".ToLowerInvariant();

            BlobDescriptor resultsBlob = new BlobDescriptor()
            {
                Name = BlobDescriptor.SanitizeBlobPath(blobName),
                ContainerName = experimentId.ToLowerInvariant(),
                ContentType = contentType
            };

            return resultsBlob;
        }

        /// <summary>
        /// Creates a standardized blob store path/virtual path to use for storing a file.
        /// </summary>
        /// <param name="experimentId">Experiment Id</param>
        /// <param name="agentId">The ID of the agent or instance of the Virtual Client that produced the file.</param>
        /// <param name="toolName">The name of the executor, monitor or other component that generated the file (e.g. azureprofiler).</param>
        /// <param name="filePaths">List of paths to the files to be uploaded.</param>
        /// <param name="timestamp">An optional timestamp to use in as part of the file name to ensure uniqueness of name within the blob store.</param>
        /// <param name="role">Role of the VC.</param>
        /// <param name="contentType">Content type, default to text/plain</param>
        /// <param name="startDirectory">
        ///     Default null, and files will be sent in a single directory. 
        ///     If set, will preserve the file directory structure, starting from the startDirectory.
        ///     For example: /dev/a/b/c.txt and /dev/a/b.txt have common path at /dev/a/, if /dev/a/ is passed in:
        ///     The file at storage blob will be /expId/agentId/toolname/time/b/c.txt and /expId/agentId/toolname/time/b.txt
        /// </param>
        public static IEnumerable<KeyValuePair<string, BlobDescriptor>> ToBlobDescriptors(
            string experimentId,
            string agentId,
            string toolName,
            IEnumerable<string> filePaths,
            DateTime? timestamp = null,
            string role = null,
            string contentType = CommonContentType.PlainText,
            string startDirectory = null)
        {
            agentId.ThrowIfNullOrWhiteSpace(nameof(agentId));
            toolName.ThrowIfNullOrWhiteSpace(nameof(toolName));

            IEnumerable<KeyValuePair<string, BlobDescriptor>> result = new Dictionary<string, BlobDescriptor>();
            DateTime effectiveTimestamp = timestamp ?? DateTime.UtcNow;

            foreach (string file in filePaths)
            {
                string fileName = BlobDescriptor.SanitizeBlobPath(file);
                string directoryPrefix = string.Empty;
                if (string.IsNullOrEmpty(startDirectory))
                {
                    fileName = Path.GetFileName(file);
                }
                else
                {
                    // Remove the start directory from example: /packages/a/b/c.txt to /a/b/c.txt
                    fileName = fileName.Replace(BlobDescriptor.SanitizeBlobPath(startDirectory), string.Empty);
                    // Directory prefix is /a/b/
                    directoryPrefix = fileName.Replace(Path.GetFileName(fileName), string.Empty);
                    fileName = Path.GetFileName(fileName);
                }

                BlobDescriptor blobDescriptor = BlobDescriptor.ToBlobDescriptor(
                    experimentId,
                    agentId,
                    toolName,
                    fileName,
                    effectiveTimestamp,
                    role: role,
                    contentType: contentType,
                    directoryPrefix: directoryPrefix);

                result = result.Append(new KeyValuePair<string, BlobDescriptor>(file, blobDescriptor));
            }

            return result;
        }

        /// <summary>
        /// Remove characters that are not allowed in windows/linux file and Azure blob path.
        /// </summary>
        /// <param name="name">Name of file path.</param>
        /// <returns>Sanitized file path.</returns>
        public static string SanitizeBlobPath(string name)
        {
            // The following characters are not allowed: " \ / : | < > * ? in blob name
            // The path could contain /
            char[] forbiddenChars = new char[] { '"',  ':', '|', '<', '>', '*', '?' };
            foreach (char c in forbiddenChars)
            {
                name = name.Replace(c.ToString(), "_");
            }

            name = name.Replace('\\', '/').Replace(@"//", "/").ToLowerInvariant();

            return name;
        }

        /// <summary>
        /// Const class for common content types
        /// </summary>
        public static class CommonContentType
        {
            /// <summary>
            /// text/plain
            /// </summary>
            public const string PlainText = "text/plain";

            /// <summary>
            /// text/plain
            /// </summary>
            public const string Binary = "application/octet-stream";
        }
    }
}
