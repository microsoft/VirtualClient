// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    /// <summary>
    /// Provides a method for validating data objects.
    /// </summary>
    public interface IValidationRule<T>
    {
        /// <summary>
        /// Validates the contents and correctness of the data object.
        /// </summary>
        /// <param name="data">The data object to validate.</param>
        ValidationResult Validate(T data);
    }
}
