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
        /// <param name="componentSettings">Settings to apply to the behavior of the </param>
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
            ComponentSettings componentSettings = null,
            IEnumerable<string> includedScenarios = null,
            IEnumerable<string> excludedScenarios = null)
        {
            componentDescription.ThrowIfNull(nameof(componentDescription));
            dependencies.ThrowIfNull(nameof(dependencies));

            try
            {
                if (!ComponentTypeCache.Instance.TryGetComponentType(componentDescription.Type, out Type componentType))
                {
                    throw new TypeLoadException($"Type '{componentDescription.Type}' does not exist.");
                }

                ComponentSettings effectiveSettings = componentSettings ?? new ComponentSettings();

                VirtualClientComponent component = ComponentFactory.CreateComponent(
                    componentType,
                    componentDescription,
                    effectiveSettings,
                    dependencies,
                    new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase),
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
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType,
            ExecutionProfileElement componentDescription,
            ComponentSettings componentSettings,
            IServiceCollection dependencies,
            IDictionary<string, IConvertible> metadata,
            IDictionary<string, JToken> extensions,
            IEnumerable<string> includeScenarios = null,
            IEnumerable<string> excludeScenarios = null)
        {
            if (!dependencies.TryGetService<ComponentSettings>(out ComponentSettings settings))
            {
                settings = new ComponentSettings();
            }

            IDictionary<string, IConvertible> effectiveParameters = new OrderedDictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            if (componentDescription.Parameters?.Any() == true)
            {
                effectiveParameters.AddRange(componentDescription.Parameters);
            }

            if (componentSettings.FailFast != null)
            {
                effectiveParameters[nameof(VirtualClientComponent.FailFast)] = componentSettings.FailFast.Value;
            }

            if (componentSettings.LogToFile != null)
            {
                effectiveParameters[nameof(VirtualClientComponent.LogToFile)] = componentSettings.LogToFile.Value;
            }

            if (componentSettings.Seed != null)
            {
                effectiveParameters[nameof(VirtualClientComponent.Seed)] = componentSettings.Seed.Value;
            }

            VirtualClientComponent component = (VirtualClientComponent)Activator.CreateInstance(componentType, dependencies, effectiveParameters);
            component.ComponentType = componentDescription.ComponentType;
            component.ContentPathTemplate = componentSettings.ContentPathTemplate;

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
                        subcomponentType,
                        subComponent,
                        componentSettings,
                        dependencies,
                        new Dictionary<string, IConvertible>(metadata, StringComparer.OrdinalIgnoreCase),
                        component.Extensions); // Extensions for the parent component to include.

                    childComponent.ComponentType = componentDescription.ComponentType;
                    componentCollection.Add(childComponent);
                }

                component = componentCollection;
            }

            return component;
        }
    }
}
