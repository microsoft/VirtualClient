// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Collection of HttpCotent methods.
    /// </summary>
    public static class RequestResponseExtensions
    {
        private const string ContentTypeHeader = "content-type";
        private static Encoding defaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Default encoding for this rest client.
        /// </summary>
        public static Encoding DefaultEncoding
        {
            get { return RequestResponseExtensions.defaultEncoding; }
            set { RequestResponseExtensions.defaultEncoding = value; }
        }

        /// <summary>
        /// Extension enables a custom action/delegate to be supplied to evaluate the status and contents
        /// of the HTTP response message.
        /// </summary>
        /// <param name="response">The HTTP response message.</param>
        /// <param name="action">Action(s) to invoke against the HTTP response message.</param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/> for the upon the action completion.
        /// </returns>
        public static HttpResponseMessage Handle(this HttpResponseMessage response, Action<HttpResponseMessage> action)
        {
            response.ThrowIfNull(nameof(response));
            action.ThrowIfNull(nameof(action));

            action.Invoke(response);

            return response;
        }

        /// <summary>
        /// Extension returns true/false whether the response contains
        /// HTML content (i.e. content-type = text/html).
        /// </summary>
        /// <param name="response">The HTTP response message with content.</param>
        public static bool IsHtmlContent(this HttpResponseMessage response)
        {
            response.ThrowIfNull(nameof(response));

            bool isHtml = false;
            IEnumerable<string> headers;
            if (response.Content != null && response.Content.Headers.TryGetValues(RequestResponseExtensions.ContentTypeHeader, out headers))
            {
                isHtml = headers.Any(h => h.Contains(MediaType.Html.FieldName, StringComparison.OrdinalIgnoreCase));
            }

            return isHtml;
        }

        /// <summary>
        /// Extension returns true/false whether the response contains
        /// JSON content (i.e. content-type = application/json).
        /// </summary>
        /// <param name="response">The HTTP response message with content.</param>
        public static bool IsJsonContent(this HttpResponseMessage response)
        {
            response.ThrowIfNull(nameof(response));

            bool isJson = false;
            IEnumerable<string> headers;
            if (response.Content != null && response.Content.Headers.TryGetValues(RequestResponseExtensions.ContentTypeHeader, out headers))
            {
                isJson = headers.Any(h => h.Contains(MediaType.Json.FieldName, StringComparison.OrdinalIgnoreCase));
            }

            return isJson;
        }

        /// <summary>
        /// Reads the contents as JSON-formatted text and converts the text into a
        /// runtime object.
        /// </summary>
        /// <typeparam name="TData">The data type of the runtime object to which the JSON will be converted.</typeparam>
        /// <param name="content">The HTTP content.</param>
        /// <returns>
        /// An object of the type specified deserialized from JSON text in the content.
        /// </returns>
        public static async Task<TData> ReadAsJsonAsync<TData>(this HttpContent content)
        {
            content.ThrowIfNull(nameof(content));

            try
            {
                string contentJson = await content.ReadAsStringAsync().ConfigureAwait(false);
                return contentJson.FromJson<TData>();
            }
            catch (JsonException exc)
            {
                throw new JsonReaderException(
                    $"Invalid HTTP content format.  The contents of the HTTP response cannot be JSON-deserialized into an object of type '{typeof(TData).FullName}'.",
                    exc);
            }
        }

        /// <summary>
        /// An object of the type specified deserialized from JSON text in the content.
        /// </summary>
        public static async Task<object> ReadAsJsonAsync(this HttpContent content, Type objectType)
        {
            content.ThrowIfNull(nameof(content));
            objectType.ThrowIfNull(nameof(objectType));

            try
            {
                string contentJson = await content.ReadAsStringAsync().ConfigureAwait(false);
                return contentJson.FromJson(objectType);
            }
            catch (JsonException exc)
            {
                throw new JsonReaderException(
                    $"Invalid HTTP content format.  The contents of the HTTP response cannot be JSON-deserialized into an object of type '{objectType.FullName}'.",
                    exc);
            }
        }

        /// <summary>
        /// Reads the contents as JSON-formatted text and converts the text into a
        /// <see cref="JToken"/>
        /// </summary>
        /// <param name="content">The HTTP content.</param>
        /// <returns>The JSON text in the content as a JToken</returns>
        public static async Task<JToken> ReadAsJTokenAsync(this HttpContent content)
        {
            content.ThrowIfNull(nameof(content));

            JToken responseObject = null;
            if (content != null)
            {
                string result = await content.ReadAsStringAsync().ConfigureAwait();
                responseObject = JToken.Parse(result);
            }

            return responseObject;
        }

        /// <summary>
        /// Convert string to HttpContent.
        /// </summary>
        public static HttpContent ToJsonContent(this string value)
        {
            return new StringContent(value, RequestResponseExtensions.DefaultEncoding, MediaType.Json.FieldName);
        }

        /// <summary>
        /// Convert string to HttpContent.
        /// </summary>
        public static HttpContent ToXmlContent(this string value)
        {
            return new StringContent(value, RequestResponseExtensions.DefaultEncoding, MediaType.Xml.FieldName);
        }
    }
}