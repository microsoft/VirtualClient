// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
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
        /// Loads provider types from assemblies in the path provided.
        /// </summary>
        public void LoadComponentTypes(string assemblyDirectory)
        {
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
        public bool TryGetComponentType(string componentType, out Type type)
        {
            componentType.ThrowIfNullOrWhiteSpace(nameof(componentType));
            type = this.FirstOrDefault(type => type.FullName == componentType || type.Name == componentType)?.Type;

            return type != null;
        }

        private static IEnumerable<string> GetAssemblies(string directoryPath)
        {
            List<string> assemblies = new List<string>();
            IEnumerable<string> allAssemblies = Directory.GetFiles(directoryPath, "*.dll");

            foreach (string assemblyPath in allAssemblies)
            {
                string fileName = Path.GetFileName(assemblyPath);
                if (!ComponentTypeCache.IsExcluded(fileName))
                {
                    assemblies.Add(assemblyPath);
                }
            }

            return assemblies;
        }

        private static bool IsExcluded(string fileName)
        {
            // Only Virtual Client assemblies at the moment.
            return !fileName.Contains("VirtualClient", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsComponentAssembly(Assembly assembly)
        {
            return assembly.GetCustomAttribute<VirtualClientComponentAssemblyAttribute>() != null;
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
        public ComponentType(Type type)
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
        public Type Type { get; }
    }
}
