// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
        /// The default extension for the file uploads.
        /// </summary>
        public const string UploadDescriptorFileExtension = "upload.json";

        private const string FileTimestampFormat = "yyyy-MM-ddTHH-mm-ss-fffffK";
        private static readonly Regex PathReservedCharacterExpression = new Regex(@"[""<>:|?*\\/]+", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadDescriptor"/> class.
        /// </summary>
        /// <param name="blobName">The name (or name + virtual path) of the blob/file.</param>
        /// <param name="containerName">The container in which the blob/file will be stored.</param>
        /// <param name="contentEncoding">The encoding for the contents of the blob (e.g. utf-8).</param>
        /// <param name="contentType">The web content type for the blob (e.g. text/plain, application/octet-stream).</param>
        /// <param name="filePath">The path to the file containing the content to upload.</param>
        /// <param name="manifest">Properties that define a manifest (e.g. metadata) for the file and its contents.</param>
        public FileUploadDescriptor(string blobName, string containerName, string contentEncoding, string contentType, string filePath, IDictionary<string, IConvertible> manifest = null)  
        {
            blobName.ThrowIfNullOrWhiteSpace(nameof(blobName));
            containerName.ThrowIfNullOrWhiteSpace(nameof(containerName));
            contentEncoding.ThrowIfNullOrWhiteSpace(nameof(contentEncoding));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));

            this.BlobName = blobName.Trim('/').Trim('\\');
            this.ContainerName = containerName;
            this.ContentEncoding = contentEncoding;
            this.ContentType = contentType;
            this.FilePath = filePath;
            this.Manifest = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if (manifest?.Any() == true)
            {
                this.Manifest.AddRange(manifest);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadDescriptor"/> class.
        /// </summary>
        /// <param name="blobName">The name of the blob/file.</param>
        /// <param name="blobPath">The path/virtual path to the blob/file.</param>
        /// <param name="containerName">The container in which the blob/file will be stored.</param>
        /// <param name="contentEncoding">The encoding for the contents of the blob (e.g. utf-8).</param>
        /// <param name="contentType">The web content type for the blob (e.g. text/plain, application/octet-stream).</param>
        /// <param name="filePath">The path to the file containing the content to upload.</param>
        /// <param name="manifest">Properties that define a manifest (e.g. metadata) for the file and its contents.</param>
        [JsonConstructor]
        public FileUploadDescriptor(string blobName, string blobPath, string containerName, string contentEncoding, string contentType, string filePath, IDictionary<string, IConvertible> manifest = null)
            : this(blobName, containerName, contentEncoding, contentType, filePath, manifest)
        {
            if (!string.IsNullOrWhiteSpace(blobPath))
            {
                this.BlobPath = $"/{blobPath.Trim('/').Replace('\\', '/')}";
            }
        }

        /// <summary>
        /// The name of the file/blob.
        /// </summary>
        [JsonProperty(PropertyName = "blobName", Required = Required.Always)]
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
        /// Returns a file name containing a timestamp as part of the name having removed any
        /// characters not allowed in file paths (e.g. 2023-02-01T12-23-30241Z-randomwrite_4k_blocksize.log).
        /// </summary>
        /// <param name="fileName">The name of the file (e.g. randomwrite_4k_blocksize.log)</param>
        /// <param name="timestamp">The timestamp to add to the file name.</param>
        public static string GetFileName(string fileName, DateTime timestamp)
        {
            return PathReservedCharacterExpression.Replace(
                $"{timestamp.ToString(FileTimestampFormat)}-{fileName.ToLowerInvariant().RemoveWhitespace()}",
                string.Empty);
        }

        /// <summary>
        /// Returns a <see cref="BlobDescriptor"/> for the current instance.
        /// </summary>
        public BlobDescriptor ToBlobDescriptor()
        {
            if (string.IsNullOrWhiteSpace(this.BlobPath))
            {
                return new BlobDescriptor
                {
                    Name = this.BlobName,
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.GetEncoding(this.ContentEncoding),
                    ContentType = this.ContentType
                };
            }
            else
            { 
                return new BlobDescriptor
                {
                    Name = $"{this.BlobPath}/{this.BlobName}",
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.GetEncoding(this.ContentEncoding),
                    ContentType = this.ContentType
                };
            }
        }

        /// <summary>
        /// Returns a <see cref="BlobDescriptor"/> for the current instance.
        /// </summary>
        /// <param name="manifestStream">A stream that can be used to upload the manifest related to the original file.</param>
        public BlobDescriptor ToBlobManifestDescriptor(out Stream manifestStream)
        {
            manifestStream = new MemoryStream(Encoding.UTF8.GetBytes(this.Manifest.ToJson()));

            if (string.IsNullOrWhiteSpace(this.BlobPath))
            {
                return new BlobDescriptor
                {
                    Name = $"{Path.GetFileNameWithoutExtension(this.BlobName)}.manifest",
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.UTF8,
                    ContentType = HttpContentType.Json
                };
            }
            else
            {
                return new BlobDescriptor
                {
                    Name = $"{this.BlobPath}/{Path.GetFileNameWithoutExtension(this.BlobName)}.manifest",
                    ContainerName = this.ContainerName,
                    ContentEncoding = Encoding.UTF8,
                    ContentType = HttpContentType.Json
                };
            }
        }
    }
}
