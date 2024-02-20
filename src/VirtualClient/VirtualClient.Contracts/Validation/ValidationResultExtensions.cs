// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ValidationResult"/>
    /// </summary>
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// Throws an exception if the result is not valid.
        /// </summary>
        /// <param name="result">The result to evaluate.</param>
        public static void ThrowIfInvalid(this ValidationResult result)
        {
            result.ThrowIfNull(nameof(result));

            if (!result.IsValid || result.ValidationErrors.Any())
            {
                throw new SchemaException($"Validation result is not valid: {string.Join(Environment.NewLine, result.ValidationErrors)}");  
            }
        }

        /// <summary>
        /// Collapse the enumeration of results into one.
        /// </summary>
        /// <param name="results">The enumeration of results</param>
        /// <returns>The single result.</returns>
        public static ValidationResult Collapse(this IEnumerable<ValidationResult> results)
        {
            results.ThrowIfNullOrEmpty(nameof(results));

            bool isValid = results.All(r => r.IsValid);
            IEnumerable<string> validationErrors = results.SelectMany(r => r.ValidationErrors);

            return new ValidationResult(isValid, validationErrors);
        }
    }
}
