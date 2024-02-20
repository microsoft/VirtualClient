// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization.Formatters.Binary;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Contains assertions related to object equality.
    /// </summary>
    public static class EqualityAssert
    {
        /// <summary>
        /// Verifies that a type correctly implements equality semantics following
        /// .NET best practices.
        /// </summary>
        /// <typeparam name="T">The type to be checked.</typeparam>
        /// <param name="createA">A function to create a version of the object.</param>
        /// <param name="createB">A function to create a different version of the object.</param>
        /// <param name="createOthers">Functions to create other, different versions of the object.</param>
        public static void CorrectlyImplementsEqualitySemantics<T>(Func<T> createA, Func<T> createB, params Func<T>[] createOthers)
            where T : class, IEquatable<T>
        {
            if (createA == null)
            {
                throw new ArgumentNullException(nameof(createA));
            }

            if (createB == null)
            {
                throw new ArgumentNullException(nameof(createB));
            }

            List<Func<T>> itemCreators = new List<Func<T>>();
            itemCreators.Add(createA);
            itemCreators.Add(createB);
            itemCreators.AddRange(createOthers);

            // Validate that scenario coverage will be high
            IEnumerable<T> items = itemCreators.Select(creator => creator())
                .Where(item => !object.ReferenceEquals(item, null));
            if (items.Count() < 2)
            {
                throw new ArgumentException("There should be at least 2 concrete objects to test, to avoid short circuiting the test on ReferenceEquals(null) code paths.");
            }

            EqualityAssert.AssertEqualitySemanticsImplementedCorrectly(itemCreators);
        }

        /// <summary>
        /// Verifies that a type correctly implements hashcode generation semantics following
        /// .NET best practices.
        /// </summary>
        /// <typeparam name="T">The type to be checked.</typeparam>
        /// <param name="createA">A function to create a version of the object.</param>
        /// <param name="createB">A function to create a different version of the object.</param>
        public static void CorrectlyImplementsHashcodeSemantics<T>(Func<T> createA, Func<T> createB)
        {
            if (createA == null)
            {
                throw new ArgumentNullException(nameof(createA));
            }

            if (createB == null)
            {
                throw new ArgumentNullException(nameof(createB));
            }

            T item1 = createA.Invoke();
            T item2 = createA.Invoke();
            T item3 = createB.Invoke();

            // Hashcodes should be immutable
            if (item1.GetHashCode() != item1.GetHashCode())
            {
                throw new EqualityAssertFailedException("Hashcodes should be immutable given no changes to the object.");
            }

            // Hashcodes of equal objects should be equal.
            if (item1.GetHashCode() != item2.GetHashCode())
            {
                throw new EqualityAssertFailedException("Hashcodes of equal objects should be equal.");
            }

            // Hashcodes of equal objects should NOT be equal.
            if (item1.GetHashCode() == item3.GetHashCode())
            {
                throw new EqualityAssertFailedException("Hashcodes of unequal objects should not be equal.");
            }
        }

        /// <summary>
        /// Asserts that the property is set to any value.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyName">The name of the property.</param>
        public static void PropertySet(object instance, string propertyName)
        {
            instance.ThrowIfNull(nameof(instance));
            propertyName.ThrowIfNullOrWhiteSpace(nameof(propertyName));

            bool isSet = false;
            PropertyInfo property = instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(prop => prop.Name == propertyName);

            if (property != null)
            {
                isSet = property.GetValue(instance) != null;
            }

            if (!isSet)
            {
                throw new EqualityAssertFailedException($"The property '{propertyName}' is not set to a value.");
            }
        }

        /// <summary>
        /// Asserts that the property is set to the value defined on the object instance. This is
        /// used to validate any properties regardless of access (e.g. protected, private).
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="propertyValue">The value of the property to validate.</param>
        /// <param name="comparison">Optional parameter defines the type of comparison that will be applied to properties with <see cref="string"/> values.</param>
        public static void PropertySet(object instance, string propertyName, object propertyValue, StringComparison comparison = StringComparison.Ordinal)
        {
            instance.ThrowIfNull(nameof(instance));
            propertyName.ThrowIfNullOrWhiteSpace(nameof(propertyName));

            bool isSet = false;
            PropertyInfo property = instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(prop => prop.Name == propertyName);

            if (property != null)
            {
                if (propertyValue == null)
                {
                    isSet = property.GetValue(instance) == null;
                }
                else if (propertyValue is string)
                {
                    isSet = string.Equals(property.GetValue(instance)?.ToString(), propertyValue.ToString(), comparison);
                }
                else if (property.GetValue(instance).GetType().IsValueType)
                {
                    isSet = object.Equals(property.GetValue(instance), propertyValue);
                }
                else
                {
                    isSet = object.ReferenceEquals(property.GetValue(instance), propertyValue);
                }
            }

            if (!isSet)
            {
                throw new EqualityAssertFailedException($"The property '{propertyName}' is not set to the value expected.");
            }
        }

        private static void AssertEqualitySemanticsImplementedCorrectly<T>(IList<Func<T>> itemCreatorsAllUnequal)
            where T : class, IEquatable<T>
        {
            foreach (Func<T> itemCreator in itemCreatorsAllUnequal)
            {
                T item = itemCreator();
                EqualityAssert.AssertEqualitySemanticsImplementedCorrectlyForObjectsThatAreEqual(item, item);

                T itemPrime = itemCreator();
                EqualityAssert.AssertEqualitySemanticsImplementedCorrectlyForObjectsThatAreEqual(item, itemPrime);

                foreach (Func<T> otherItemCreator in itemCreatorsAllUnequal.Except(new[] { itemCreator }))
                {
                    T otherItem = otherItemCreator();
                    EqualityAssert.AssertEqualitySemanticsImplementedCorrectlyForObjectsThatAreNotEqual(item, otherItem);
                }

                EqualityAssert.AssertEqualitySemanticsImplementedCorrectlyForObjectsOfDifferentDataTypes(item);
            }
        }

        /// <summary>
        /// Verifies that the .Equals, ==, and != works for two objects that should be equal.
        /// </summary>
        /// <typeparam name="T">The object type being tested.</typeparam>
        /// <param name="item">The item to test.</param>
        /// <param name="equalItem">An item equal to the first item to test.</param>
        private static void AssertEqualitySemanticsImplementedCorrectlyForObjectsThatAreEqual<T>(T item, T equalItem)
            where T : class, IEquatable<T>
        {
            // Following calls are only valid on non-null items
            if (!object.ReferenceEquals(item, null))
            {
                if (!item.Equals(equalItem))
                {
                    throw new EqualityAssertFailedException(
                        "Generic Equals<T>(T) method not implemented correctly for case where items are equal.");
                }

                if (!item.Equals((object)equalItem))
                {
                    throw new EqualityAssertFailedException(
                        "Equals(object) method not implemented correctly for case where items are equal.");
                }

                if (item.GetHashCode() != equalItem.GetHashCode())
                {
                    throw new EqualityAssertFailedException(
                        "GetHashCode() method not implemented correctly.  The hash code of equal objects should be equal.");
                }
            }

            // Have to cast as dynamic to call to operator==(T a, T b). Otherwise, 
            // since these are generic types, it binds to operator==(object a, object b)
            if ((dynamic)item == (dynamic)equalItem != true)
            {
                throw new EqualityAssertFailedException(
                    "Generic operator overload ==(T item1, T item2) must be implemented/implemented correctly.");
            }

            if ((dynamic)item != (dynamic)equalItem != false)
            {
                throw new EqualityAssertFailedException(
                    "Generic operator overload !=(T item1, T item2) must be implemented/implemented correctly.");
            }

            // Object equality should not be implemented, as then == operations with (object) types are not commutative
            // [Skip check when reference equal since object.Equals() does a ReferenceEquals]
            if (!object.ReferenceEquals(item, equalItem))
            {
                if (((dynamic)item == (object)equalItem) != false)
                {
                    throw new EqualityAssertFailedException("(T)item1 == (object)item2 should be false.");
                }

                if (((dynamic)item != (object)equalItem) != true)
                {
                    throw new EqualityAssertFailedException("(T)item1 != (object)item2 should be true.");
                }
            }
        }

        private static void AssertEqualitySemanticsImplementedCorrectlyForObjectsThatAreNotEqual<T>(T item, T differentItem)
            where T : class, IEquatable<T>
        {
            // Following calls are only valid on non-null items
            if (!object.ReferenceEquals(item, null))
            {
                // According to MSDN, two unequal objects do not need different hash codes:
                // https://msdn.microsoft.com/en-us/library/system.object.gethashcode(v=vs.110).aspx
                if (item.Equals(differentItem))
                {
                    throw new EqualityAssertFailedException(
                        "Generic Equals<T>(T) method not implemented correctly for the case where items are not equal.");
                }

                if (item.Equals((object)differentItem))
                {
                    throw new EqualityAssertFailedException(
                        "Equals(object) method not implemented correctly for the case where items are not equal.");
                }
            }

            // Have to cast as dynamic to call to operator==(T a, T b). Otherwise, 
            // since these are generic types, it binds to operator==(object a, object b)
            if (((dynamic)item == (dynamic)differentItem) != false)
            {
                throw new EqualityAssertFailedException(
                    "Generic operator overload ==(T item1, T item2) must be implemented/implemented correctly.");
            }

            if (((dynamic)item != (dynamic)differentItem) != true)
            {
                throw new EqualityAssertFailedException(
                    "Generic operator overload !=(T item1, T item2) must be implemented/implemented correctly.");
            }

            if (((dynamic)item == (object)differentItem) != false)
            {
                throw new EqualityAssertFailedException("(T)item1 == (object)item2 should be false.");
            }

            if (((dynamic)item != (object)differentItem) != true)
            {
                throw new EqualityAssertFailedException("(T)item1 != (object)item2 should be true.");
            }
        }

        private static void AssertEqualitySemanticsImplementedCorrectlyForObjectsOfDifferentDataTypes<T>(T item)
            where T : class, IEquatable<T>
        {
            PrivateClass notA = new PrivateClass();

            // Following calls are only valid on non-null items
            if (!object.ReferenceEquals(item, null))
            {
                if (item.Equals(notA))
                {
                    throw new EqualityAssertFailedException("Equals(object) method not implemented correctly.");
                }
            }

            if (notA.Equals(item))
            {
                throw new EqualityAssertFailedException("Equals(object) method not implemented correctly.");
            }
        }

        private class PrivateClass
        {
        }
    }
}
