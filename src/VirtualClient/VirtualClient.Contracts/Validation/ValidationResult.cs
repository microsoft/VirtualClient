// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationResult"/> class.
        /// </summary>
        /// <param name="isValid">True/false whether the target is valid.</param>
        /// <param name="validationErrors">
        /// A set of validation errors that occurred for the case that the target is not valid.
        /// </param>
        public ValidationResult(bool isValid, IEnumerable<string> validationErrors = null)
        {
            this.IsValid = isValid;

            List<string> errors = new List<string>();
            if (validationErrors?.Any() == true)
            {
                errors.AddRange(validationErrors);
            }

            this.ValidationErrors = errors;
        }

        /// <summary>
        /// Gets true/false whether the target is valid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the set of validation errors that occurred for the case that the
        /// target is not valid.
        /// </summary>
        public List<string> ValidationErrors { get; }

        /// <summary>
        /// Return new combined instance of the <see cref="ValidationResult"/>
        /// </summary>
        /// <param name="validationResult">Instance of <see cref="ValidationResult"/> to be combined</param>
        /// <returns>Merged valiadation result.</returns>
        public ValidationResult Merge(ValidationResult validationResult)
        {
            validationResult.ThrowIfNull(nameof(validationResult));

            List<string> errors = new List<string>(this.ValidationErrors);
            errors?.AddRange(validationResult.ValidationErrors);

            return new ValidationResult((this.IsValid && validationResult.IsValid), errors);
        }
    }
}
