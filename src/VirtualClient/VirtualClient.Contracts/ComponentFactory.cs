// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A factory for creating Virtual Client actions and monitoring components.
    /// </summary>
    public static class ComponentFactory
    {
        private static readonly Type ParallelExecutionType = typeof(ParallelExecution);

        /// <summary>
        /// Creates the expected component from the assemblies/.dlls loaded.
        /// </summary>
        /// <param name="componentDescription">The component type description.</param>
        /// <param name="dependencies">A collection of dependencies that can be used for dependency injection.</param>
        /// <param name="randomizationSeed">A randomization seed to use to ensure consistency across workloads running on different systems.</param>
        public static VirtualClientComponent CreateComponent(ExecutionProfileElement componentDescription, IServiceCollection dependencies, int? randomizationSeed = null)
        {
            componentDescription.ThrowIfNull(nameof(componentDescription));
            dependencies.ThrowIfNull(nameof(dependencies));

            try
            {
                if (!ComponentTypeCache.Instance.TryGetComponentType(componentDescription.Type, out Type type))
                {
                    throw new TypeLoadException($"Type '{componentDescription.Type}' does not exist.");
                }

                VirtualClientComponent component = ComponentFactory.CreateComponent(componentDescription, type, dependencies, randomizationSeed);

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

        /// <summary>
        /// Creates the expected component from the assemblies/.dlls loaded.
        /// </summary>
        /// <param name="parameters">Parameters of the component..</param>
        /// <param name="componentType">The component type.</param>
        /// <param name="dependencies">A collection of dependencies that can be used for dependency injection.</param>
        /// <param name="randomizationSeed">A randomization seed to use to ensure consistency across workloads running on different systems.</param>
        public static VirtualClientComponent CreateComponent(IDictionary<string, IConvertible> parameters, string componentType, IServiceCollection dependencies, int? randomizationSeed = null)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            try
            {
                if (!ComponentTypeCache.Instance.TryGetComponentType(componentType, out Type type))
                {
                    throw new TypeLoadException($"Type '{componentType}' does not exist.");
                }

                VirtualClientComponent component = (VirtualClientComponent)Activator.CreateInstance(type, dependencies, parameters);
                component.ExecutionSeed = randomizationSeed;

                return component;
            }
            catch (TypeLoadException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. A component of type '{componentType}' does not exist in the application domain.",
                    exc);
            }
            catch (InvalidCastException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. The type '{componentType}' is not a valid instance of the required " +
                    $"base type '{typeof(VirtualClientComponent).FullName}'.",
                    exc);
            }
            catch (JsonException exc)
            {
                throw new StartupException(
                    $"Virtual Client component initialization failed. The component of type '{componentType}' contains extensions that are NOT valid JSON-formatted content.",
                    exc);
            }
            catch (Exception exc)
            {
                throw new StartupException("Virtual Client component initialization failed.", exc);
            }
        }

        private static VirtualClientComponent CreateComponent(ExecutionProfileElement componentDescription, Type type, IServiceCollection dependencies, int? randomizationSeed = null)
        {
            VirtualClientComponent component = null;
            if (componentDescription.Components?.Any() != true)
            {
                component = (VirtualClientComponent)Activator.CreateInstance(type, dependencies, componentDescription.Parameters);
                component.ExecutionSeed = randomizationSeed;
            }
            else
            {
                if (componentDescription.Components?.Any() == true)
                {
                    VirtualClientComponentCollection componentCollection = (VirtualClientComponentCollection)Activator.CreateInstance(type, dependencies, componentDescription.Parameters);
                    foreach (ExecutionProfileElement subComponent in componentDescription.Components)
                    {
                        if (!ComponentTypeCache.Instance.TryGetComponentType(subComponent.Type, out Type subcomponentType))
                        {
                            throw new TypeLoadException($"Type '{subComponent.Type}' does not exist.");
                        }

                        componentCollection.Add(ComponentFactory.CreateComponent(subComponent, subcomponentType, dependencies, randomizationSeed));
                    }

                    component = componentCollection;
                }
            }

            return component;
        }
    }
}
