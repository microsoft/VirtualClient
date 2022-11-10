// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;

    /// <summary>
    /// Attribute is used to mark an assembly as one that contains <see cref="VirtualClientComponent"/>
    /// implementations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class VirtualClientComponentAssemblyAttribute : Attribute
    {
    }
}
