// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="ExecutionProfile"/>
    /// </summary>
    public static class ExecutionProfileExtensions
    {
        /// <summary>
        /// Resolves parameter references within the profile.
        /// </summary>
        /// <param name="profile">The profile to inline.</param>
        public static void Inline(this ExecutionProfile profile)
        {
            profile.ThrowIfNull(nameof(profile));

            List<ExecutionProfileElement> elements = new List<ExecutionProfileElement>(profile.Dependencies);
            elements.AddRange(profile.Actions);
            elements.AddRange(profile.Monitors);

            ExecutionProfileExtensions.InlineElements(elements, profile.Parameters);
        }

        /// <summary>
        /// Returns true/false whether the current executor scenario matches any defined in the
        /// excluded scenarios supplied.
        /// </summary>
        /// <param name="element">The component to check for specified scenario.</param>
        /// <param name="excludedScenarios">The names of the scenarios to exclude.</param>
        /// <returns>True if current executor targets one of the excluded scenarios. False if not.</returns>
        public static bool IsExcludedScenario(this ExecutionProfileElement element, IEnumerable<string> excludedScenarios)
        {
            bool isExcluded = false;

            if (element.Parameters?.Any() == true
                && element.Parameters.TryGetValue(nameof(VirtualClientComponent.Scenario), out IConvertible scenarioValue))
            {
                string componentScenario = scenarioValue.ToString();
                isExcluded = excludedScenarios.Contains($"-{componentScenario}", StringComparer.OrdinalIgnoreCase);
            }

            return isExcluded;
        }

        /// <summary>
        /// Returns true/false whether the current executor scenario matches any defined in the
        /// targeted scenarios supplied.
        /// </summary>
        /// <param name="element">The component to check for specified scenario.</param>
        /// <param name="targetedScenarios">The names of the scenarios targeted.</param>
        /// <returns>True if current executor targets one of the supplied scenarios. False if not.</returns>
        public static bool IsTargetedScenario(this ExecutionProfileElement element, IEnumerable<string> targetedScenarios)
        {
            bool isTargeted = false;

            if (element.Parameters?.Any() == true
                && element.Parameters.TryGetValue(nameof(VirtualClientComponent.Scenario), out IConvertible scenarioValue))
            {
                string componentScenario = scenarioValue.ToString();
                isTargeted = targetedScenarios.Contains(componentScenario, StringComparer.OrdinalIgnoreCase);
            }

            return isTargeted;
        }

        /// <summary>
        /// Merges the two profiles together into a single profile. This merges profile parameters, metadata,
        /// actions, dependencies and monitors. Note that the profile description remains unchanged.
        /// </summary>
        /// <param name="profile">The original profile into which the other profile will be merged.</param>
        /// <param name="otherProfile">
        /// The other profile shows parameters, metadata, actions, dependencies and monitors will be be merged into the original.
        /// </param>
        /// <returns>
        /// An <see cref="ExecutionProfile"/> having parameters, metadata, actions, dependencies and monitors between
        /// the two profiles being merged.
        /// </returns>
        public static ExecutionProfile MergeWith(this ExecutionProfile profile, ExecutionProfile otherProfile)
        {
            ExecutionProfile mergedProfile = new ExecutionProfile(profile);

            if (otherProfile.Parameters.Any())
            {
                foreach (var entry in otherProfile.Parameters)
                {
                    if (!mergedProfile.Parameters.ContainsKey(entry.Key))
                    {
                        mergedProfile.Parameters.Add(entry);
                    }
                }
            }

            if (otherProfile.Metadata.Any())
            {
                foreach (var entry in otherProfile.Metadata)
                {
                    if (!mergedProfile.Metadata.ContainsKey(entry.Key))
                    {
                        mergedProfile.Metadata.Add(entry);
                    }
                }
            }

            if (otherProfile.Actions.Any())
            {
                mergedProfile.Actions.AddRange(otherProfile.Actions);
            }

            if (otherProfile.Dependencies.Any())
            {
                mergedProfile.Dependencies.AddRange(otherProfile.Dependencies);
            }

            if (otherProfile.Monitors.Any())
            {
                mergedProfile.Monitors.AddRange(otherProfile.Monitors);
            }

            return mergedProfile;
        }

        private static void InlineElements(IEnumerable<ExecutionProfileElement> elements, IDictionary<string, IConvertible> parameters)
        {
            foreach (ExecutionProfileElement element in elements)
            {
                ExecutionProfileExtensions.InlineElement(element, parameters);

                if (element.Components?.Any() == true)
                {
                    // Recurse through any subcomponents of the element to inline parameters.
                    ExecutionProfileExtensions.InlineElements(element.Components, parameters);
                }
            }
        }

        private static void InlineElement(ExecutionProfileElement element, IDictionary<string, IConvertible> parameters)
        {
            foreach (KeyValuePair<string, IConvertible> parameterReference in element?.Parameters)
            {
                if (parameterReference.Value != null)
                {
                    if (!ExecutionProfileExtensions.TryGetParameterReference(parameterReference.Value.ToString(), parameters, out IConvertible value))
                    {
                        throw new SchemaException($"Could not resolve {nameof(parameterReference)}: \'{parameterReference.Key}\':\'{parameterReference.Value}\'" +
                            $"in {nameof(ExecutionProfileElement)}: \'{element.Type}\'");
                    }

                    element.Parameters[parameterReference.Key] = value;
                }
            }
        }

        private static bool TryGetParameterReference(string parameterReference, IDictionary<string, IConvertible> parameters, out IConvertible resolvedValue)
        {
            resolvedValue = parameterReference;
            if (parameterReference.StartsWith(ExecutionProfile.ParameterPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string parameterName = parameterReference.Substring(ExecutionProfile.ParameterPrefix.Length);
                return parameters.TryGetValue(parameterName, out resolvedValue);
            }

            return true;
        }
    }
}
