// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="EventContext"/> object instances.
    /// </summary>
    public static class EventContextExtensions
    {
        internal const string ErrorProperty = "error";
        internal const string ErrorCallstackProperty = "errorCallstack";
        private const int MaxCallStackLength = 2000;

        /// <summary>
        /// Adds the exception/error to the event context properties.
        /// </summary>
        /// <param name="context">The event context definition.</param>
        /// <param name="error">The exception/error to add to the context properties.</param>
        /// <param name="withCallStack">True to include the callstack, false otherwise.</param>
        /// <param name="maxCallStackLength">The maximum length (# of characters) of callstack text that will be added to the context properties.</param>
        /// <returns>
        /// An <see cref="EventContext"/> object having the error added to the context properties.
        /// </returns>
        public static EventContext AddError(this EventContext context, Exception error, bool withCallStack = false, int maxCallStackLength = EventContextExtensions.MaxCallStackLength)
        {
            context.ThrowIfNull(nameof(context));

            if (error != null)
            {
                List<object> errors = new List<object>();
                Exception currentError = error;
                while (currentError != null)
                {
                    errors.Add(new
                    {
                        errorType = currentError.GetType().FullName,
                        errorMessage = SensitiveData.ObscureSecrets(currentError.Message)
                    });

                    currentError = currentError.InnerException;
                }

                if (context.Properties.ContainsKey(EventContextExtensions.ErrorProperty))
                {
                    // Retain any errors that already exist.
                    List<object> existingErrors = context.Properties[EventContextExtensions.ErrorProperty] as List<object>;
                    if (existingErrors != null)
                    {
                        existingErrors.AddRange(errors);
                        errors = existingErrors;
                    }
                }

                context.Properties[EventContextExtensions.ErrorProperty] = errors;

                if (withCallStack && error.StackTrace != null)
                {
                    if (error.StackTrace.Length > maxCallStackLength)
                    {
                        context.Properties[EventContextExtensions.ErrorCallstackProperty] = SensitiveData.ObscureSecrets(error.StackTrace.Substring(0, maxCallStackLength));
                    }
                    else
                    {
                        context.Properties[EventContextExtensions.ErrorCallstackProperty] = SensitiveData.ObscureSecrets(error.StackTrace);
                    }
                }
            }

            return context;
        }

        /// <summary>
        /// Returns a clone of the original <see cref="EventContext"/> replacing the context properties
        /// with the new properties.
        /// </summary>
        /// <param name="context">The <see cref="EventContext"/> object whose context properties should be replaced.</param>
        /// <param name="newContextProperties">The new context properties to add to the <see cref="EventContext"/> object.</param>
        /// <returns>
        /// An <see cref="EventContext"/> object having the context properties replaced.
        /// </returns>
        public static EventContext ReplaceProperties(this EventContext context, IEnumerable<KeyValuePair<string, object>> newContextProperties)
        {
            context.ThrowIfNull(nameof(context));
            newContextProperties.ThrowIfNull(nameof(newContextProperties));

            EventContext clonedContext = context.Clone(withProperties: false);
            clonedContext.Properties.AddRange(newContextProperties);
            return clonedContext;
        }

        /// <summary>
        /// Add new context properties to the original context.
        /// </summary>
        /// <param name="context">The <see cref="EventContext"/> object whose context properties should be added.</param>
        /// <param name="properties">The new context properties to add to the <see cref="EventContext"/> object.</param>
        /// <returns>
        /// An <see cref="EventContext"/> object having the context properties added.
        /// </returns>
        public static EventContext AddContext(this EventContext context, IEnumerable<KeyValuePair<string, object>> properties)
        {
            context.ThrowIfNull(nameof(context));
            properties.ThrowIfNull(nameof(properties));

            foreach (var property in properties)
            {
                context.AddContext(property.Key, property.Value);
            }

            return context;
        }

        /// <summary>
        /// Add new context properties to the original context.
        /// </summary>
        /// <param name="context">The <see cref="EventContext"/> object whose context properties should be added.</param>
        /// <param name="key">Key of the new property.</param>
        /// <param name="value">Value of the new property.</param>
        /// <returns>
        /// An <see cref="EventContext"/> object having the context properties added.
        /// </returns>
        public static EventContext AddContext(this EventContext context, string key, object value)
        {
            context.ThrowIfNull(nameof(context));
            key.ThrowIfNull(nameof(key));

            context.Properties[key] = value;
            return context;
        }

        /// <summary>
        /// Add the client request ID to the original context.
        /// </summary>
        /// <param name="context">The <see cref="EventContext"/> object whose context properties should be added.</param>
        /// <param name="requestId">The client request ID to add.</param>
        /// <returns>
        /// An <see cref="EventContext"/> object having the context properties added.
        /// </returns>
        public static EventContext AddClientRequestId(this EventContext context, Guid? requestId)
        {
            context.ThrowIfNull(nameof(context));
            if (requestId != null)
            {
                context.Properties["clientRequestId"] = requestId.ToString();
            }

            return context;
        }
    }
}