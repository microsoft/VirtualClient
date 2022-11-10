// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;

    /// <summary>
    /// An attribute used to assert that a parameter is validated.
    /// </summary>
    /// <remarks>
    /// This attribute is used to provide instructions to code analyzers that
    /// a specific parameter "has been" or "is expected to be" null. For example, this is used
    /// in extension methods within the <see cref="ParameterValidationExtensions"/> class to
    /// avoid code analysis CA1062 errors about parameter validation when using the extensions.
    /// Effectively, the code analyzer must associate the attributed parameter (i.e. the one being
    /// validated) meets the requirement of a null reference check as far as the code analyzer is
    /// concerned.
    ///
    /// https://stackoverflow.com/questions/44005383/using-a-custom-argument-validation-helper-breaks-code-analysis
    /// </remarks>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public sealed class ValidatedNotNullAttribute : Attribute
    {
    }
}