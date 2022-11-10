// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Extensions used to create common patterns with API method telemetry
    /// capture mechanics.
    /// </summary>
    public static class ApiTelemetryExtensions
    {
        /// <summary>
        /// Extension adds HTTP action response information to the telemetry context.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context object.</param>
        /// <param name="response">The HTTP action response.</param>
        public static void AddResponseContext(this EventContext telemetryContext, IActionResult response)
        {
            response.ThrowIfNull(nameof(response));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            ObjectResult objectResult = response as ObjectResult;
            if (objectResult != null)
            {
                telemetryContext.AddContext(nameof(response), new
                {
                    statusCode = objectResult.StatusCode,
                    contentType = objectResult.ContentTypes?.Select(ct => ct),
                    declaredType = objectResult.DeclaredType?.FullName,
                    result = objectResult.Value
                });
            }
            else
            {
                StatusCodeResult statusCodeResult = response as StatusCodeResult;
                if (statusCodeResult != null)
                {
                    telemetryContext.AddContext(nameof(response), new
                    {
                        statusCode = statusCodeResult.StatusCode
                    });
                }
            }
        }

        /// <summary>
        /// Creates an error response with a common schema to include in an HTTP response.
        /// </summary>
        /// <param name="controller">The API controller.</param>
        /// <param name="statusCode">The HTTP status code of the error response.</param>
        /// <param name="title">The title of the error message.</param>
        /// <param name="details">A description of the error message.</param>
        public static ProblemDetails CreateErrorDetails(this ControllerBase controller, int statusCode, string title, string details)
        {
            return new ProblemDetails
            {
                Detail = details,
                Instance = $"{controller.Request?.Method} {controller.Request?.Path.Value}",
                Status = statusCode,
                Title = title
            };
        }
    }
}
