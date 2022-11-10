using VirtualClient.Common.Extensions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    /// <summary>
    /// Media type for Rest API calls.
    /// </summary>
    public class MediaType
    {
        private MediaType(string mediaTypeString)
        {
            this.FieldName = mediaTypeString;
        }

        /// <summary>
        /// text/html
        /// </summary>
        public static MediaType Html
        {
            get
            {
                return new MediaType("text/html");
            }
        }

        /// <summary>
        /// application/json
        /// </summary>
        public static MediaType Json
        {
            get
            {
                return new MediaType("application/json");
            }
        }

        /// <summary>
        /// application/xml
        /// </summary>
        public static MediaType Xml
        {
            get
            {
                return new MediaType("application/xml");
            }
        }

        /// <summary>
        /// text/plain
        /// </summary>
        public static MediaType PlainText
        {
            get
            {
                return new MediaType("text/plain");
            }
        }

        /// <summary>
        /// The media type string
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Implicit operator conversion changes the <see cref="MediaType"/>
        /// to a string representation.
        /// </summary>
        public static implicit operator string(MediaType mediaType)
        {
            mediaType.ThrowIfNull(nameof(mediaType));
            return mediaType.ToString();
        }

        /// <summary>
        /// Returns a string representation of the <see cref="MediaType"/>.
        /// </summary>
        /// <returns>String representation of MediaType.</returns>
        public override string ToString()
        {
            return this.FieldName;
        }
    }
}