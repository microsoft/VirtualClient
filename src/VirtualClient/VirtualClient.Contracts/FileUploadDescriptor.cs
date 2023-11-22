// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information required to upload content to a target storage system.
    /// </summary>
    public class FileUploadDescriptor
    {
        /// <summary>
        /// The default template to use for defining blob paths with file uploads.
        /// </summary>
        public const string DefaultContentPathTemplate = "{experimentId}/{agentId}/{toolName}/{role}/{scenario}";

        /// <summary>
        /// The default extension for the file uploads.
        /// </summary>
        public const string UploadDescriptorFileExtension = "upload.json";

        private const string FileTimestampFormat = "yyyy-MM-ddTHH-mm-ss-fffffK";
        private static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);
        private static readonly char[] PathDelimiters = new char[] { '/', '\\' };

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadDescriptor"/> class.
        /// </summary>
        /// <param name="blobPath">The path/virtual path to the blob/file.</param>
        /// <param name="containerName">The container in which the blob/file will be stored.</param>
        /// <param name="contentEncoding">The encoding for the contents of the blob (e.g. utf-8).</param>
        /// <param name="contentType">The web content type for the blob (e.g. text/plain, application/octet-stream).</param>
        /// <param name="filePath">The path to the file containing the content to upload.</param>
        /// <param name="manifest">Properties that define a manifest (e.g. metadata) for the file and its contents.</param>
        /// <param name="deleteOnUpload">True/false whether the file should be deleted upon being successfully uploaded.</param>
        [JsonConstructor]
        public FileUploadDescriptor(string blobPath, string containerName, string contentEncoding, string contentType, string filePath, IDictionary<string, IConvertible> manifest = null, bool deleteOnUpload = false)
        {
            blobPath.ThrowIfNullOrWhiteSpace(nameof(blobPath));
            containerName.ThrowIfNullOrWhiteSpace(nameof(containerName));
            contentEncoding.ThrowIfNullOrWhiteSpace(nameof(contentEncoding));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));

            string path = blobPath.Trim(FileUploadDescriptor.PathDelimiters);
            this.BlobName = Path.GetFileName(path);
            this.BlobPath = $"/{path.Replace('\\', '/')}";
            this.ContainerName = containerName;
            this.ContentEncoding = contentEncoding;
            this.ContentType = contentType;
            this.DeleteOnUpload = deleteOnUpload;
            this.FilePath = filePath;
            this.Manifest = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if (manifest?.Any() == true)
            {
                this.Manifest.AddRange(manifest);
            }
        }

        /// <summary>
        /// The name of the file/blob.
        /// </summary>
        [JsonIgnore]
        public string BlobName { get; }

        /// <summary>
        /// The path/virtual path for the file/blob.
        /// </summary>
        [JsonProperty(PropertyName = "blobPath", Required = Required.Default)]
        public string BlobPath { get; }

        /// <summary>
        /// The name of the container in the storage location.
        /// </summary>
        [JsonProperty(PropertyName = "containerName", Required = Required.Always)]
        public string ContainerName { get; }

        /// <summary>
        /// The web content encoding for the file/blob (e.g. utf-8).
        /// </summary>
        [JsonProperty(PropertyName = "contentEncoding", Required = Required.Always)]
        public string ContentEncoding { get; }

        /// <summary>
        /// The web content type for the file/blob (e.g. application/octet-stream).
        /// </summary>
        [JsonProperty(PropertyName = "contentType", Required = Required.Always)]
        public string ContentType { get; }

        /// <summary>
        /// True/false whether the file should be deleted upon being successfully uploaded.
        /// </summary>
        [JsonProperty(PropertyName = "deleteOnUpload", Required = Required.Always)]
        public bool DeleteOnUpload { get; set;  }

        /// <summary>
        /// The full path to the file to upload.
        /// </summary>
        [JsonProperty(PropertyName = "filePath", Required = Required.Always)]
        public string FilePath { get; }

        /// <summary>
        /// Manifest/metadata information related to the file and. its contents
        /// </summary>
        [JsonProperty(PropertyName = "manifest", Required = Required.Default)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Manifest { get; }

        /// <summary>
        /// Creates a manifest for a given file that can be published alongside the file in a blob store.
        /// </summary>
        /// <param name="fileContext">Provides context about a file to be uploaded.</param> 
        /// <param name="blobContainer">The name of the blob container.</param>
        /// <param name="blobPath">The path/virtual path to the blob itself (e.g. /agent01/fio/fio_randwrite_496gb_12k_d32_th16/2022-03-18T10-00-05-12765Z-fio_randwrite_496gb_12k_d32_th16.log).</param>
        /// <param name="parameters">Parameters related to the component that produced the file (e.g. the parameters from the component).</param>
        /// <param name="metadata">Additional information and metadata related to the blob/file to include in the descriptor alongside the default manifest information.</param>
        public static IDictionary<string, IConvertible> CreateManifest(FileContext fileContext, string blobContainer, string blobPath, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> metadata = null)
        {
            fileContext.ThrowIfNull(nameof(fileContext));

            // Create the default manifest information.
            string platform = PlatformSpecifics.GetPlatformArchitectureName(Environment.OSVersion.Platform, RuntimeInformation.ProcessArchitecture);

            IDictionary<string, IConvertible> fileManifest = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { "experimentId", fileContext.ExperimentId },
                { "agentId", fileContext.AgentId },
                { "appHost", Environment.MachineName },
                { "role", fileContext.Role },
                { "platform", platform },
                { "toolName", fileContext.ToolName },
                { "toolArguments", fileContext.CommandArguments },
                { "scenario", fileContext.Scenario },
                { "blobPath", blobPath },
                { "blobContainer", blobContainer },
                { "contentType", fileContext.ContentType },
                { "contentEncoding", fileContext.ContentEncoding },
                { "fileName", fileContext.File.Name },
                { "fileSizeBytes", fileContext.File.Length },
                { "fileCreationTime", fileContext.File.CreationTime.ToString("o") },
                { "fileCreationTimeUtc", fileContext.File.CreationTimeUtc.ToString("o") }
            };

            // Add in any parameters defined on/supplied to the component.
            if (parameters?.Any() == true)
            {
                foreach (var entry in parameters)
                {
                    if (!fileManifest.ContainsKey(entry.Key))
                    {
                        fileManifest.Add(entry.Key.CamelCased(), entry.Value);
                    }
                }
            }

            // Add in any additional/special metadata provided. Given that the user supplied these
            // they take priority over existing metadata and will override it.
            if (metadata?.Any() == true)
            {
                foreach (var entry in metadata)
                {
                    fileManifest[entry.Key.CamelCased()] = entry.Value;
                }
            }

            return fileManifest.ObscureSecrets();
        }

        /// <summary>
        /// Returns a file name containing a timestamp as part of the name having removed any
        /// characters not allowed in file paths (e.g. 2023-02-01T12-23-30241Z-randomwrite_4k_blocksize.log).
        /// </summary>
        /// <param name="fileName">The name of the file (e.g. randomwrite_4k_blocksize.log)</param>
        /// <param name="timestamp">The timestamp to add to the file name.</param>
        public static string GetFileName(string fileName, DateTime timestamp)
        {
            return PathReservedCharacterExpression.Replace(
                $"{timestamp.ToString(FileTimestampFormat)}-{fileName.RemoveWhitespace()}",
                string.Empty);
        }

        /// <summary>
        /// Returns a <see cref="BlobDescriptor"/> for the current instance.
        /// </summary>
        public BlobDescriptor ToBlobDescriptor()
        {
            return new BlobDescriptor
            {
                Name = this.BlobPath,
                ContainerName = this.ContainerName,
                ContentEncoding = Encoding.GetEncoding(this.ContentEncoding),
                ContentType = this.ContentType
            };
        }

        /// <summary>
        /// Returns a <see cref="BlobDescriptor"/> for the current instance.
        /// </summary>
        /// <param name="manifestStream">A stream that can be used to upload the manifest related to the original file.</param>
        public BlobDescriptor ToBlobManifestDescriptor(out Stream manifestStream)
        {
            manifestStream = new MemoryStream(Encoding.UTF8.GetBytes(this.Manifest.ToJson()));

            string fileExtension = Path.GetExtension(this.BlobPath);
            if (!string.IsNullOrWhiteSpace(fileExtension))
            {
                return new BlobDescriptor
                {
                    Name = this.BlobPath.Replace(fileExtension, ".manifest.json"),
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.UTF8,
                    ContentType = HttpContentType.Json
                };
            }
            else
            {
                return new BlobDescriptor
                {
                    Name = $"{this.BlobPath}.manifest.json",
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.UTF8,
                    ContentType = HttpContentType.Json
                };
            }
        }
    }
}
