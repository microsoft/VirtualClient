// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class ComponentTypeCacheTests
    {
        [Test]
        public void ComponentTypeCacheLoadsExpectedTypesFromComponentAssemblies()
        {
            lock (ComponentTypeCache.LockObject)
            {
                try
                {
                    List<Type> expectedTypes = new List<Type>
                    {
                        typeof(VirtualClientComponent)
                    };

                    ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);

                    Assert.IsNotEmpty(ComponentTypeCache.Instance);
                    Assert.IsTrue(ComponentTypeCache.Instance.All(t => expectedTypes.Any(et => t.Type.IsAssignableTo(et))));
                }
                finally
                {
                    ComponentTypeCache.Instance.Clear();
                }
            }
        }
    }
}
