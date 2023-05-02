// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using Newtonsoft.Json;

    /// <summary>
    /// JSON Structure for Marker File to Upload Logs to Blob Storage
    /// </summary>
    public class ContentUploadMarker
    {
        /// <summary>
        /// ContainerName where logs should be uploaded
        /// </summary>
        [JsonProperty("containerName")]
        public string ContainerName { get; set; }

        /// <summary>
        /// Virtual Folder Path in Blob Storage
        /// </summary>
        [JsonProperty("blobName")]
        public string BlobName { get; set; }

        /// <summary>
        /// Content encoding for upload to blob
        /// </summary>
        [JsonProperty("contentEncoding")]
        public string ContentEncoding { get; set; }
        // NEED TO CHECK ON CONVERSION FROM STRING TO ENCODING

        /// <summary>
        /// Content Type for upload to blob
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Local Path of File to uploaded to blob storage
        /// </summary>
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
    }
}
