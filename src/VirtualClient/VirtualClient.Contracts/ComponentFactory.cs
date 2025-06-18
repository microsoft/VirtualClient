// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A factory for creating Virtual Client actions and monitoring components.
    /// </summary>
    public static class ComponentFactory
    {
        /// <summary>
        /// Creates the expected component from the assemblies/.dlls loaded.
        /// </summary>
        /// <param name="componentDescription">The component type description.</param>
        /// <param name="dependencies">A collection of dependencies that can be used for dependency injection.</param>
        /// <param name="randomizationSeed">A randomization seed to use to ensure consistency across workloads running on different systems.</param>
        /// <param name="failFast">
        /// True if the application should fail/crash immediately upon experiencing any errors in actions, monitors or dependencies. It is the default
        /// behavior for the application to attempt to "stay alive" when experiencing certain types of errors because they could be transient in nature
        /// (vs. terminal).
        /// </param>
        /// <param name="logToFile">True to instruct the application to log output to files on the file system.</param>
        /// <param name="includedScenarios">
        /// When evaluating child components evaluate whether or not the child component should be 
        /// included dictated by the scenarios provided on the command line. These scenarios must be included.
        /// </param>
        /// <param name="excludedScenarios">
        /// When evaluating child components evaluate whether or not the child component should be 
        /// included dictated by the scenarios provided on the command line. These scenarios must be excluded.
        /// </param>
        public static VirtualClientComponent CreateComponent(
            ExecutionProfileElement componentDescription, 
            IServiceCollection dependencies,
            int? randomizationSeed = null,
            bool? failFast = null,
            bool? logToFile = null,
            IEnumerable<string> includedScenarios = null,
            IEnumerable<string> excludedScenarios = null)
        {
            componentDescription.ThrowIfNull(nameof(componentDescription));
            dependencies.ThrowIfNull(nameof(dependencies));

            try
            {
                if (!ComponentTypeCache.Instance.TryGetComponentType(componentDescription.Type, out Type type))
                {
                    throw new TypeLoadException($"Type '{componentDescription.Type}' does not exist.");
                }

                VirtualClientComponent component = ComponentFactory.CreateComponent(
                    componentDescription,
                    type,
                    dependencies,
                    new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase),
                    randomizationSeed,
                    failFast,
                    logToFile,
                    includedScenarios,
                    excludedScenarios);

                return component;
            }
            catch (TypeLoadException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. A component of type '{componentDescription.Type}' does not exist in the application domain.",
                    exc);
            }
            catch (InvalidCastException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. The type '{componentDescription.Type}' is not a valid instance of the required " +
                    $"base type '{typeof(VirtualClientComponent).FullName}'.",
                    exc);
            }
            catch (MissingMethodException exc)
            {
                throw new StartupException(
                   $"Virtual Client component initialization failed. The class for this component '{componentDescription.Type}' must have a constructor " +
                   $"that takes in exactly 2 parameters: a '{typeof(IServiceCollection)}' parameter first followed by a '{typeof(IDictionary<string, IConvertible>)}' parameter.",
                   exc);
            }
            catch (JsonException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. The component of type '{componentDescription.Type}' contains extensions that are NOT valid JSON-formatted content.",
                    exc);
            }
            catch (Exception exc)
            {
                throw new StartupException("Virtual Client component initialization failed.", exc);
            }
        }

        private static VirtualClientComponent CreateComponent(
            ExecutionProfileElement componentDescription, 
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
            IServiceCollection dependencies,
            IDictionary<string, IConvertible> metadata,
            IDictionary<string, JToken> extensions,
            int? randomizationSeed = null,
            bool? failFast = null,
            bool? logToFile = null,
            IEnumerable<string> includeScenarios = null,
            IEnumerable<string> excludeScenarios = null)
        {
            VirtualClientComponent component = component = (VirtualClientComponent)Activator.CreateInstance(type, dependencies, componentDescription.Parameters);
            component.ComponentType = componentDescription.ComponentType;
            component.ExecutionSeed = randomizationSeed;

            // Metadata is merged at each level down the hierarchy. Metadata at higher levels
            // takes priority overriding metadata at lower levels (i.e. withReplace: false).
            // This allows metadata to be set at higher levels that is then in turn applied to
            // components throughout the hierarchy.
            if (componentDescription.Metadata?.Any() == true)
            {
                metadata.AddRange(componentDescription.Metadata, withReplace: false);
            }

            // Extensions are merged at each level down the hierarchy. Parent component
            // extensions are merged with child/subcomponent extensions.
            if (componentDescription.Extensions?.Any() == true)
            {
                component.Extensions.AddRange(componentDescription.Extensions, withReplace: false);
            }

            if (failFast != null)
            {
                component.FailFast = failFast.Value;
            }

            if (logToFile != null)
            {
                component.LogToFile = logToFile.Value;
            }

            if (metadata?.Any() == true)
            {
                component.Metadata.AddRange(metadata, withReplace: true);
            }

            // Extensions at lower levels takes priority overriding extensions at higher levels.
            // This allows extensions to be set at higher levels that is then in turn applied to
            // components throughout the hierarchy while also supporting overriding individual parts
            // of the extensions as needed in the child subcomponents.
            if (extensions?.Any() == true)
            {
                component.Extensions.AddRange(extensions, withReplace: true);
            }

            // Recursive to handle subcomponents.
            if (componentDescription.Components?.Any() == true)
            {
                VirtualClientComponentCollection componentCollection = component as VirtualClientComponentCollection;

                foreach (ExecutionProfileElement subComponent in componentDescription.Components)
                {
                    if (!ComponentTypeCache.Instance.TryGetComponentType(subComponent.Type, out Type subcomponentType))
                    {
                        throw new TypeLoadException($"Type '{subComponent.Type}' does not exist.");
                    }

                    bool scenarioIncluded = includeScenarios?.Any() == true;
                    if (includeScenarios?.Any() == true)
                    {
                        scenarioIncluded = subComponent.IsTargetedScenario(includeScenarios);
                        if (!scenarioIncluded)
                        {
                            continue;
                        }
                    }

                    // Included scenarios take precedence over excluded (e.g. Scenario1,-Scenario1 -> Scenario1 will be included).
                    if (!scenarioIncluded && excludeScenarios?.Any() == true && subComponent.IsExcludedScenario(excludeScenarios))
                    {
                        continue;
                    }

                    VirtualClientComponent childComponent = ComponentFactory.CreateComponent(
                        subComponent, 
                        subcomponentType, 
                        dependencies, 
                        new Dictionary<string, IConvertible>(metadata, StringComparer.OrdinalIgnoreCase),
                        component.Extensions, // Extensions for the parent component to include.
                        randomizationSeed,
                        failFast,
                        logToFile);

                    childComponent.ComponentType = componentDescription.ComponentType;
                    componentCollection.Add(childComponent);
                }

                component = componentCollection;
            }

            return component;
        }
    }
}
