// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for Virtual Client API client operations.
    /// </summary>
    public static class VirtualClientApiExtensions
    {
        /// <summary>
        /// Extension throws an exception of the type specified if the response status code is unsuccessful.
        /// Note that the contents of the response is expected to be 
        /// </summary>
        public static void ThrowOnError<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TException>(this HttpResponseMessage response, ErrorReason? reason = null)
            where TException : VirtualClientException
        {
            response.ThrowIfNull(nameof(response));

            if (!response.IsSuccessStatusCode)
            {
                TException exception = null;
                ErrorReason errorReason = reason ?? ErrorReason.HttpNonSuccessResponse;
                if (reason == null)
                {
                    switch (response.StatusCode)
                    {
                        case System.Net.HttpStatusCode.BadRequest:
                            errorReason = ErrorReason.Http400BadRequestResponse;
                            break;

                        case System.Net.HttpStatusCode.NotFound:
                            errorReason = ErrorReason.Http404NotFoundResponse;
                            break;

                        case System.Net.HttpStatusCode.Conflict:
                            errorReason = ErrorReason.Http409ConflictResponse;
                            break;

                        case System.Net.HttpStatusCode.Unauthorized:
                            errorReason = ErrorReason.Unauthorized;
                            break;
                    }
                }

                try
                {
                    string errorMessage = response.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                    exception = (TException)Activator.CreateInstance(
                        typeof(TException),
                        $"API Request Error (status code = {response.StatusCode}): {errorMessage}",
                        errorReason);
                }
                catch (Exception exc)
                {
                    exception = new ApiException($"API Request Error (status code = {response.StatusCode})", exc, errorReason) as TException;
                }

                throw exception;
            }
        }
    }
}
