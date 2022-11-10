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
        private static readonly Type ComponentBaseType1 = typeof(VirtualClientComponent);

        private ComponentTypeCache()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the <see cref="ComponentTypeCache"/>
        /// </summary>
        public static ComponentTypeCache Instance { get; } = new ComponentTypeCache();

        /// <summary>
        /// We are mid-stream between 2 different models for how to provide dependencies to 
        /// Virtual Client components. The new model towards which we are moving uses an IServiceCollection
        /// to provide dependencies following .NET recommended dependency injection practices. This method returns
        /// true if the type supports the new model.
        /// </summary>
        public static bool IsComponentType2(Type type)
        {
            return type.IsSubclassOf(ComponentTypeCache.ComponentBaseType1);
        }

        /// <summary>
        /// Loads provider types from assemblies in the path provided.
        /// </summary>
        public void LoadComponentTypes(string assemblyDirectory)
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
            IEnumerable<Type> componentTypes = componentAssembly.GetTypes()
                .Where(type => type.IsSubclassOf(ComponentTypeCache.ComponentBaseType1));

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
