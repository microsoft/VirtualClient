// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Class that allows for validation on the <see cref="ExecutionProfile"/>
    /// </summary>
    public class ExecutionProfileValidation : List<IValidationRule<ExecutionProfile>>, IValidationRule<ExecutionProfile>
    {
        private ExecutionProfileValidation()
        {
        }

        /// <summary>
        /// Singleton instance of <see cref="ExecutionProfileValidation"/>
        /// </summary>
        public static ExecutionProfileValidation Instance { get; } = new ExecutionProfileValidation();

        /// <summary>
        /// Validates the <see cref="ExecutionProfile"/>
        /// </summary>
        /// <param name="data">The profile to validate.</param>
        /// <returns>A ValidationResult that denotes if the profile is valid or not.</returns>
        public ValidationResult Validate(ExecutionProfile data)
        {
            data.ThrowIfNull(nameof(data));

            ValidationResult result = new ValidationResult(true);
            foreach (IValidationRule<ExecutionProfile> rule in this)
            {
                result = result.Merge(rule.Validate(data));
            }

            return result;
        }
    }
}
