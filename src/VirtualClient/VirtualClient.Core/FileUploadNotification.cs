// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using Newtonsoft.Json;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides information required to upload content to a target storage system.
    /// </summary>
    public class FileUploadNotification
    {
        /// <summary>
        /// The name of the container in the storage location.
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        /// The name or full virtual path for the file/blob.
        /// </summary>
        [JsonProperty("blobName")]
        public string BlobName { get; set; }

        /// <summary>
        /// The web content encoding for the file/blob (e.g. utf-8).
        /// </summary>
        [JsonProperty("contentEncoding")]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// The web content type for the file/blob (e.g. application/octet-stream).
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// The full path to the file to upload.
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
    }
}
