// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extension methods used for parameter validation.
    /// </summary>
    public static class ParameterValidationExtensions
    {
        /// <summary>
        /// Returns true/false whether the set of items contains at least 1 object that
        /// is null, empty or white space.
        /// </summary>
        /// <param name="parameter">The object to validate.</param>
        /// <param name="items">A set of one or more string items to compare.</param>
        public static bool ContainsNullEmptyOrWhiteSpace<T>([ValidatedNotNull] this T parameter, params string[] items)
        {
            return items?.Any(i => string.IsNullOrWhiteSpace(i)) == true;
        }

        /// <summary>
        /// Throws an exception if the object is null.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="parameter">The object to validate.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="errorMessage">A custom error message.</param>
        public static void ThrowIfNull<T>([ValidatedNotNull] this T parameter, string parameterName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("A parameter name must be supplied.", parameterName);
            }

            if (parameter == null)
            {
                throw new ArgumentException(errorMessage ?? "Parameter is required.", parameterName);
            }
        }

        /// <summary>
        /// Throws an exception if the target IEnumerable is empty.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="parameter">The object to validate.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="errorMessage">A custom error message.</param>
        public static void ThrowIfEmpty<T>([ValidatedNotNull] this IEnumerable<T> parameter, string parameterName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("A parameter name must be supplied.", nameof(parameterName));
            }

            if (!parameter.Any())
            {
                throw new ArgumentException(errorMessage ?? $"Parameter is required and cannot be an empty set.", parameterName);
            }
        }

        /// <summary>
        /// Throws an exception if the validator returns false for an object.
        /// </summary>
        /// <typeparam name="T">The type of object being valudated.</typeparam>
        /// <param name="toValidate">The object to validate.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="validator">
        /// The validator function. The function should return true when the parameter is valid. If this function
        /// returns false, it means the parameter is invalid and an exception will be thrown.
        /// </param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="exceptionGenerator">An optional exception generator.</param>
        /// <remarks>This extension allows for custom validators outside the scope of null checks. Such as requiring an object to be in a particular state
        /// or to not have a default value. Examples...
        ///     foo.ThrowIfInvalid(nameof(foo), (o) => o.State == State.RequiredState);
        ///     guid.ThrowIfInvalid(nameof(guid), (g) => !g.Equals(Guid.Empty));
        ///     someNumber.ThrowIfInvalid(nameof(foo), (n) => n > 0, null, (p, m) => new ArgumentOutOfRangeException(p, m);
        /// For simple validations this allows us to match our extension based validation style.
        /// </remarks>
        public static void ThrowIfInvalid<T>([ValidatedNotNull] this T toValidate, string parameterName, Func<T, bool> validator, string errorMessage = null, Func<string, string, ArgumentException> exceptionGenerator = null)
        {
            parameterName.ThrowIfNullOrWhiteSpace(nameof(parameterName), "A parameter name must be supplied.");
            validator.ThrowIfNull(nameof(validator), "A validator must be supplied.");

            if (!validator(toValidate))
            {
                throw exceptionGenerator == null ? new ArgumentException(errorMessage, parameterName) : exceptionGenerator(parameterName, errorMessage);
            }
        }

        /// <summary>
        /// Throws an exception if the target IEnumerable is null or empty.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="parameter">The enumerable to validate.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="errorMessage">A custom error message.</param>
        public static void ThrowIfNullOrEmpty<T>([ValidatedNotNull] this IEnumerable<T> parameter, string parameterName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("A parameter name must be supplied.", nameof(parameterName));
            }

            parameter.ThrowIfEmpty(parameterName, errorMessage);
        }

        /// <summary>
        /// Throws if the provided string is null, empty, or only white space.
        /// </summary>
        /// <param name="parameter">The string to verify.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="errorMessage">A custom error message.</param>
        public static void ThrowIfNullOrWhiteSpace([ValidatedNotNull] this string parameter, string parameterName, string errorMessage = null)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentException("A parameter name must be supplied.", nameof(parameterName));
            }

            if (string.IsNullOrWhiteSpace(parameter))
            {
                throw new ArgumentException(errorMessage ?? "Parameter is required and cannot be null, empty or whitespace.", parameterName);
            }
        }
    }
}