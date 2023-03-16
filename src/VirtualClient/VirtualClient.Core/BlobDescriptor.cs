// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Text;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Represents the address location of a blob for upload to a content store/storage account.
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
    }
}
