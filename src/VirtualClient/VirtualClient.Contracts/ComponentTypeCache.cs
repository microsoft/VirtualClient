// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a cache for object types associated with Virtual Client.
    /// </summary>
    public class ComponentTypeCache : List<ComponentType>
    {
        /// <summary>
        /// Lock object used to ensure single-threaded access to the cache when
        /// loading types.
        /// </summary>
        public static readonly object LockObject = new object();

        // The following list are different types of components that can be defined in contracts, core
        // or even extensions libraries. They are dynamically loaded at runtime.
        private static readonly List<Type> ComponentDependencyTypes = new List<Type>
        {
            typeof(VirtualClientComponent)
        };

        private ComponentTypeCache()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="ComponentTypeCache"/>
        /// </summary>
        public static ComponentTypeCache Instance { get; } = new ComponentTypeCache();

        /// <summary>
        /// Returns true if the assembly is attributed as containing Virtual Client components.
        /// </summary>
        /// <param name="assembly">The assembly to validate.</param>
        /// <returns>True if the assembly contains Virtual Client components. False if not.</returns>
        public static bool IsComponentAssembly(Assembly assembly)
        {
            return assembly.GetCustomAttribute<VirtualClientComponentAssemblyAttribute>() != null;
        }

        /// <summary>
        /// Loads the assembly at the path specified into the current runtime.
        /// </summary>
        /// <param name="assemblyPath">The full path to the binary/assembly/.dll.</param>
        public void LoadAssembly(string assemblyPath)
        {
            try
            {
                Assembly.LoadFrom(assemblyPath);
            }
            catch (BadImageFormatException)
            {
                // Expected for certain types of assemblies that are not .NET intermediate
                // language assemblies (IL).
            }
            catch (FileLoadException)
            {
            }
        }

        /// <summary>
        /// Loads component types from assemblies in the path provided.
        /// </summary>
        /// <param name="assemblyDirectory">The full path to the binary/assemblies directory.</param>
        public void LoadComponentTypes(string assemblyDirectory)
        {
            assemblyDirectory.ThrowIfNullOrWhiteSpace(nameof(assemblyDirectory));

            lock (ComponentTypeCache.LockObject)
            {
                foreach (string assemblyPath in ComponentTypeCache.GetAssemblies(assemblyDirectory))
                {
                    try
                    {
                        Assembly assembly = Assembly.LoadFrom(assemblyPath);

                        if (ComponentTypeCache.IsComponentAssembly(assembly))
                        {
                            this.CacheProviderTypes(assembly);
                        }
                    }
                    catch (BadImageFormatException)
                    {
                        // For the case that our exclusions miss assemblies that are NOT intermediate/IL
                        // assemblies, we need to handle this until we have a better model.
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the component exists in the type cache.
        /// </summary>
        /// <param name="componentType">The name or full name of the component.</param>
        /// <param name="type">The component type if found in the type cache.</param>
        /// <returns>
        /// True if a matching component exists in the type cache.
        /// </returns>
        public bool TryGetComponentType(string componentType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] out Type type)
        {
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            type = this.FirstOrDefault(type => type.FullName == componentType || type.Name == componentType)?.Type;

            return type != null;
        }

        private static IEnumerable<string> GetAssemblies(string directoryPath)
        {
            List<string> assemblies = new List<string>();
            string[] allAssemblies = Directory.GetFiles(directoryPath, "*.dll");

            if (allAssemblies?.Length > 0)
            {
                foreach (string assemblyPath in allAssemblies)
                {
                    string fileName = Path.GetFileName(assemblyPath);
                    if (!ComponentTypeCache.IsExcluded(fileName))
                    {
                        assemblies.Add(assemblyPath);
                    }
                }
            }

            return assemblies;
        }

        private static bool IsExcluded(string fileName)
        {
            // Only Virtual Client assemblies at the moment.
            return !fileName.Contains("VirtualClient", StringComparison.OrdinalIgnoreCase);
        }

        private void CacheProviderTypes(Assembly componentAssembly)
        {
            foreach (Type dependencyType in ComponentTypeCache.ComponentDependencyTypes)
            {
                IEnumerable<Type> componentTypes = null;
                if (dependencyType.IsInterface)
                {
                    componentTypes = componentAssembly.GetTypes()?.Where(type => type.IsAssignableFrom(dependencyType));
                }
                else
                {
                    componentTypes = componentAssembly.GetTypes()?.Where(type => type.IsSubclassOf(dependencyType));
                }

                if (componentTypes?.Any() == true)
                {
                    foreach (Type componentType in componentTypes)
                    {
                        if (!this.Any(type => type.Type == componentType))
                        {
                            this.Add(new ComponentType(componentType));
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Represents a cached component type.
    /// </summary>
    public class ComponentType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentType"/> class.
        /// </summary>
        public ComponentType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
        {
            type.ThrowIfNull(nameof(type));
            this.Type = type;
        }

        /// <summary>
        /// The name of the component data type (e.g. TestExecutor).
        /// </summary>
        public string Name
        {
            get
            {
                return this.Type.Name;
            }
        }

        /// <summary>
        /// The fully qualified name of the component data type (e.g. VirtualClient.Actions.TestExecutor).
        /// </summary>
        public string FullName
        {
            get
            {
                return this.Type.FullName;
            }
        }

        /// <summary>
        /// The component type.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type Type { get; }
    }
}
