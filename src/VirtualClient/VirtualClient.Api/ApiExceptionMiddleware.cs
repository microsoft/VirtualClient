// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Api
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides generic exception response handling functionality for VC API
    /// services.
    /// </summary>
    public class ApiExceptionMiddleware
    {
        private static IDictionary<Type, int> statusCodeMappings = new Dictionary<Type, int>
        {
            [typeof(ArgumentException)] = StatusCodes.Status400BadRequest,
            [typeof(SchemaException)] = StatusCodes.Status400BadRequest
        };

        private RequestDelegate nextMiddlewareComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiExceptionMiddleware"/> class.
        /// </summary>
        public ApiExceptionMiddleware(RequestDelegate next, ILogger logger = null)
        {
            this.nextMiddlewareComponent = next;
            this.Logger = logger ?? NullLogger.Instance;
        }

        /// <summary>
        /// A logger to use for capturing telemetry on unexpected error events.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Middleware pipeline invocation entry point.
        /// </summary>
        /// <param name="context">Provides the context of the HTTP request and response.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (this.nextMiddlewareComponent != null)
                {
                    await this.nextMiddlewareComponent.Invoke(context);
                }
            }
            catch (Exception exc)
            {
                await ApiExceptionMiddleware.HandleResponse(context, exc);
            }
        }

        private static Task HandleResponse(HttpContext context, Exception exception)
        {
            Type exceptionType = exception.GetType();

            int statusCode;
            if (!ApiExceptionMiddleware.statusCodeMappings.TryGetValue(exceptionType, out statusCode))
            {
                statusCode = StatusCodes.Status500InternalServerError;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            return context.Response.WriteAsync(new ProblemDetails
            {
                Detail = exception.ToString(withCallStack: false, withErrorTypes: false),
                Instance = $"{context.Request?.Method} {context.Request?.Path.Value}",
                Status = statusCode,
                Title = exceptionType.Name,
                Type = exceptionType.FullName
            }.ToJson());
        }
    }
}
