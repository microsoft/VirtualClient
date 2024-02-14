// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using YamlDotNet.Core;
    using YamlDotNet.Core.Events;
    using YamlDotNet.Serialization;

    /// <summary>
    /// A type converter that can be used to handle the serialization/deserialization
    /// of <see cref="ExecutionProfileYamlShim"/> objects.
    /// </summary>
    public class YamlParameterDictionaryTypeConverter : IYamlTypeConverter
    {
        private static readonly Type SupportedType = typeof(IDictionary<string, IConvertible>);

        /// <summary>
        /// Returns true if the type is a supported type for serialization/deserialization
        /// (e.g. <see cref="ExecutionProfileYamlShim"/>).
        /// </summary>
        /// <param name="type">The type to validate as serializable.</param>
        /// <returns>True if the type is YAML serializable, false if not.</returns>
        public bool Accepts(Type type)
        {
            return type == YamlParameterDictionaryTypeConverter.SupportedType;
        }

        /// <summary>
        /// Parses a parameter dictionary from the current parser context (e.g. metadata, parameters).
        /// </summary>
        public object ReadYaml(IParser parser, Type type)
        {
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            string currentKey = null;
            IConvertible currentValue = null;

            while (parser.MoveNext())
            {
                ParsingEvent current = parser.Current;

                if (current is Scalar)
                {
                    Scalar currentScalar = current as Scalar;
                    if (currentScalar.IsKey)
                    {
                        currentKey = currentScalar.Value;
                    }
                    else
                    {
                        if (currentScalar.IsQuotedImplicit)
                        {
                            currentValue = currentScalar.Value;
                        }
                        else
                        {
                            currentValue = YamlParameterDictionaryTypeConverter.ConvertType(currentScalar.Value);
                        }

                        parameters[currentKey] = currentValue;
                    }
                }
                else if (current is MappingEnd)
                {
                    parser.MoveNext();
                    break;
                }
            }

            return parameters;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            throw new NotImplementedException();
        }

        private static IConvertible ConvertType(string value)
        {
            // We support limited data type conversions. The final conversion is the responsibility
            // of the logic accessing the parameters.
            IConvertible convertedValue = value;
            if (int.TryParse(value, out int intValue))
            {
                convertedValue = intValue;
            }
            else if (long.TryParse(value, out long longValue))
            {
                convertedValue = longValue;
            }
            else if (double.TryParse(value, out double doubleValue))
            {
                convertedValue = doubleValue;
            }
            else if (decimal.TryParse(value, out decimal decimalValue))
            {
                convertedValue = decimalValue;
            }
            else if (bool.TryParse(value, out bool boolValue))
            {
                convertedValue = boolValue;
            }

            return convertedValue;
        }
    }
}
