// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Validates the <see cref="ExecutionProfile"/> schema.
    /// </summary>
    public class SchemaRules : IValidationRule<ExecutionProfile>
    { 
        private SchemaRules()
        { 
        }

        /// <summary>
        /// Singleton instance of <see cref="SchemaRules"/>
        /// </summary>
        public static SchemaRules Instance { get; } = new SchemaRules();

        /// <summary>
        /// Validates the schema of the <see cref="ExecutionProfile"/>
        /// </summary>
        /// <param name="data">The data to validate</param>
        /// <returns>A <see cref="ValidationResult"/> that denotes if the profile is valid or not.</returns>
        public ValidationResult Validate(ExecutionProfile data)
        {
            data.ThrowIfNull(nameof(data));

            List<ExecutionProfileElement> elements = new List<ExecutionProfileElement>();
            if (data.Actions?.Any() == true)
            {
                elements.AddRange(data.Actions);
            }

            if (data.Monitors?.Any() == true)
            {
                elements.AddRange(data.Monitors);
            }

            if (data.Dependencies?.Any() == true)
            {
                elements.AddRange(data.Dependencies);
            }

            ValidationResult result = null;
            if (elements.Any())
            {
                result = SchemaRules.ValidateElements(elements, data.Parameters);
            }

            return result ?? new ValidationResult(true);
        }

        private static ValidationResult ValidateElements(IEnumerable<ExecutionProfileElement> elements, IDictionary<string, IConvertible> parameters)
        {
            IEnumerable<ValidationResult> result = elements.Select(e => SchemaRules.ValidateElement(e, parameters));
            return result.Collapse();
        }

        private static ValidationResult ValidateElement(ExecutionProfileElement element, IDictionary<string, IConvertible> parameters)
        {
            IEnumerable<string> parameterReferences = element.Parameters
                .Where(p => p.Value != null && p.Value.ToString().StartsWith(ExecutionProfile.ParameterPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Value.ToString().Substring(ExecutionProfile.ParameterPrefix.Length));

            if (parameterReferences?.Any() == true)
            {
                IEnumerable<string> errors = parameterReferences
                    .Where(p => !parameters.ContainsKey(p))
                    .Select(p => $"The parameter reference: \'{p}\' could not be found in the profile parameters.");

                return new ValidationResult(errors?.Any() == false, errors);
            }

            return new ValidationResult(true);
        }
    }
}
