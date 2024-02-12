// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class CollectionExtensionsTests
    {
        [Test]
        public void AddRangeExtensionValidatesParameters()
        {
            IDictionary<string, IConvertible> set = null;
            Assert.Throws<ArgumentException>(() => set.AddRange(new Dictionary<string, IConvertible>()));

            set = new Dictionary<string, IConvertible>();
            Assert.Throws<ArgumentException>(() => set.AddRange(null));
        }

        [Test]
        public void AddRangeExtensionAddsExpectedItems()
        {
            IDictionary<string, IConvertible> originalSet = new Dictionary<string, IConvertible>();
            IDictionary<string, IConvertible> newItems = new Dictionary<string, IConvertible>
            {
                ["Parameter2"] = 5678
            };

            originalSet.AddRange(newItems);
            Assert.IsTrue(originalSet.Count == 1);
            CollectionAssert.AreEqual(originalSet.Keys, newItems.Keys);
            CollectionAssert.AreEqual(originalSet.Values, newItems.Values);

            originalSet = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = 1234
            };

            originalSet.AddRange(newItems);
            Assert.IsTrue(originalSet.Count == 2);
            CollectionAssert.AreEqual(originalSet.Keys, new string[] { "Parameter1", "Parameter2" });
            CollectionAssert.AreEqual(originalSet.Values, new IConvertible[] { 1234, 5678 });
        }

        [Test]
        public void AddRangeExtensionReplacesAnItemThatAlreadyExistsWhenRequested()
        {
            IDictionary<string, IConvertible> originalSet = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = 1234
            };

            IDictionary<string, IConvertible> newItems = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = 5678
            };

            originalSet.AddRange(newItems, withReplace: true);
            Assert.IsTrue(originalSet.Count == 1);
            Assert.AreEqual(originalSet["Parameter1"], 5678);
        }

        [Test]
        public void EqualsExtensionValidatesParameters()
        {
            IDictionary<string, IConvertible> set1 = null;
            IDictionary<string, IConvertible> set2 = new Dictionary<string, IConvertible>();

            Assert.Throws<ArgumentException>(() => set1.Equals<IConvertible>(set2));

            set1 = new Dictionary<string, IConvertible>();
            Assert.Throws<ArgumentException>(() => set1.Equals<IConvertible>(null));
        }

        [Test]
        public void EqualsExtensionDeterminesEmptySetsAreEqual()
        {
            IDictionary<string, IConvertible> set1 = new Dictionary<string, IConvertible>();
            IDictionary<string, IConvertible> set2 = new Dictionary<string, IConvertible>();

            Assert.IsTrue(set1.Equals<IConvertible>(set2));
        }

        [Test]
        public void EqualsExtensionDeterminesSetsHavingMatchingKeysAndValuesAsEqual()
        {
            IDictionary<string, IConvertible> set1 = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = $"AnyValue",
                ["Parameter2"] = 1234,
                ["Parameter3"] = true
            };

            Assert.IsTrue(set1.Equals<IConvertible>(set1));
        }

        [Test]
        public void EqualsExtensionDeterminesSetsNotHavingMatchingKeysAndValuesAsNotEqual()
        {
            IDictionary<string, IConvertible> set1 = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = $"AnyValue1",
                ["Parameter2"] = 1234,
                ["Parameter3"] = true
            };

            IDictionary<string, IConvertible> set2 = new Dictionary<string, IConvertible>
            {
                ["Parameter1"] = $"AnyValue2",
                ["Parameter2"] = 1234,
                ["Parameter3"] = false
            };

            Assert.IsFalse(set1.Equals<IConvertible>(set2));
        }

        [Test]
        public void GetValueExtensionHandlesSettingValuesThatAreConvertibleCorrectly()
        {
            IDictionary<string, IConvertible> convertibleSettings = new Dictionary<string, IConvertible>
            {
                [typeof(string).Name] = "Any string will do",
                [typeof(char).Name] = char.MaxValue,
                [typeof(byte).Name] = byte.MaxValue,
                [typeof(short).Name] = short.MaxValue,
                [typeof(int).Name] = int.MaxValue,
                [typeof(long).Name] = long.MaxValue,
                [typeof(ushort).Name] = ushort.MaxValue,
                [typeof(uint).Name] = uint.MaxValue,
                [typeof(ulong).Name] = ulong.MaxValue,
                [typeof(float).Name] = float.MaxValue,
                [typeof(double).Name] = double.MaxValue,
                [typeof(bool).Name] = true
            };

            Assert.IsTrue(convertibleSettings.GetValue<string>(typeof(string).Name) == "Any string will do", $"{typeof(string).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<char>(typeof(char).Name) == char.MaxValue, $"{typeof(char).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<byte>(typeof(byte).Name) == byte.MaxValue, $"{typeof(byte).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<short>(typeof(short).Name) == short.MaxValue, $"{typeof(short).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<int>(typeof(int).Name) == int.MaxValue, $"{typeof(int).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<long>(typeof(long).Name) == long.MaxValue, $"{typeof(long).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<ushort>(typeof(ushort).Name) == ushort.MaxValue, $"{typeof(ushort).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<uint>(typeof(uint).Name) == uint.MaxValue, $"{typeof(uint).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<ulong>(typeof(ulong).Name) == ulong.MaxValue, $"{typeof(ulong).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<float>(typeof(float).Name) == float.MaxValue, $"{typeof(float).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<double>(typeof(double).Name) == double.MaxValue, $"{typeof(double).Name} data type parsing failed.");
            Assert.IsTrue(convertibleSettings.GetValue<bool>(typeof(bool).Name) == true, $"{typeof(bool).Name} data type parsing failed.");
        }

        [Test]
        public void GetValueExtensionHandlesSettingValuesThatAreStringRepresentationsOfConvertibleDataTypeValues()
        {
            IDictionary<string, IConvertible> convertibleSettings = new Dictionary<string, IConvertible>
            {
                [typeof(char).Name] = char.MaxValue.ToString(),
                [typeof(byte).Name] = byte.MaxValue.ToString(),
                [typeof(short).Name] = short.MaxValue.ToString(),
                [typeof(int).Name] = int.MaxValue.ToString(),
                [typeof(long).Name] = long.MaxValue.ToString(),
                [typeof(ushort).Name] = ushort.MaxValue.ToString(),
                [typeof(uint).Name] = uint.MaxValue.ToString(),
                [typeof(ulong).Name] = ulong.MaxValue.ToString(),
                [typeof(float).Name] = (1.0).ToString(),
                [typeof(double).Name] = (2.0).ToString(),
                [typeof(bool).Name] = bool.TrueString
            };

            Assert.AreEqual(convertibleSettings.GetValue<char>(typeof(char).Name), char.MaxValue, $"{typeof(char).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<byte>(typeof(byte).Name), byte.MaxValue, $"{typeof(byte).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<short>(typeof(short).Name), short.MaxValue, $"{typeof(short).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<int>(typeof(int).Name), int.MaxValue, $"{typeof(int).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<long>(typeof(long).Name), long.MaxValue, $"{typeof(long).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<ushort>(typeof(ushort).Name), ushort.MaxValue, $"{typeof(ushort).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<uint>(typeof(uint).Name), uint.MaxValue, $"{typeof(uint).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<ulong>(typeof(ulong).Name), ulong.MaxValue, $"{typeof(ulong).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<float>(typeof(float).Name), 1.0, $"{typeof(float).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<double>(typeof(double).Name), 2.0, $"{typeof(double).Name} data type parsing failed.");
            Assert.AreEqual(convertibleSettings.GetValue<bool>(typeof(bool).Name), true, $"{typeof(bool).Name} data type parsing failed.");
        }

        [Test]
        public void GetValueExtensionThrowsWhenTheSettingIsNotDefined()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetValue<string>("UndefinedSetting"));
        }

        [Test]
        public void GetValueExtensionThrowsWhenTheSettingCannotBeConvertedToTheTypeSpecified()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                ["Setting"] = "I cannot be converted to an integer"
            };

            Assert.Throws<FormatException>(() => dictionary.GetValue<int>("Setting"));
        }

        [Test]
        public void GetValueExtensionReturnsTheDefaultValueSuppliedWhenASettingDoesNotExist()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            string defaultValue = "DefaultValue";
            string value = dictionary.GetValue<string>("UndefinedSetting", defaultValue);

            Assert.IsTrue(value == defaultValue);
        }

        [Test]
        public void GetEnumValueExtensionHandlesValuesThatCanBeParsedAsValidEnums()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                ["Property1"] = StringComparison.OrdinalIgnoreCase.ToString()
            };

            Assert.IsTrue(dictionary.GetEnumValue<StringComparison>("Property1") == StringComparison.OrdinalIgnoreCase);
        }

        [Test]
        public void GetEnumValueExtensionThrowsWhenTheSettingIsNotDefined()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetEnumValue<StringComparison>("UndefinedSetting"));
        }

        [Test]
        public void GetEnumValueExtensionThrowsWhenTheSettingCannotBeConvertedToTheEnumTypeSpecified()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                ["Setting"] = "NotAValidEnumValue"
            };

            Assert.Throws<FormatException>(() => dictionary.GetEnumValue<StringComparison>("Setting"));
        }

        [Test]
        public void GetEnumValueExtensionReturnsTheDefaultValueSuppliedWhenASettingIsNotDefined()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            StringComparison value = dictionary.GetEnumValue<StringComparison>("UndefinedSetting", StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(value == StringComparison.OrdinalIgnoreCase);
        }

        [Test]
        public void GetTimeSpanValueExtensionHandlesValuesThatAreTimeSpanFormatted()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                [TimeSpan.Zero.ToString()] = TimeSpan.Zero.ToString(),
                [TimeSpan.FromSeconds(1).ToString()] = TimeSpan.FromSeconds(1).ToString(),
                [TimeSpan.FromMinutes(1).ToString()] = TimeSpan.FromMinutes(1).ToString(),
                [TimeSpan.FromHours(1).ToString()] = TimeSpan.FromHours(1).ToString(),
                [TimeSpan.FromDays(1).ToString()] = TimeSpan.FromDays(1).ToString(),
                ["1.12:35:22.123456"] = "1.12:35:22.123456"
            };

            foreach (var entry in dictionary)
            {
                TimeSpan expectedTimeSpan = TimeSpan.Parse(entry.Key);
                TimeSpan actualTimeSpan = dictionary.GetTimeSpanValue(entry.Key);

                Assert.IsTrue(expectedTimeSpan == actualTimeSpan);
            }
        }

        [Test]
        public void GetTimeSpanValueExtensionHandlesValuesThatAreIntegerFormatted()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                [TimeSpan.Zero.ToString()] = 0,
                [TimeSpan.FromSeconds(1).ToString()] = 1,
                [TimeSpan.FromMinutes(1).ToString()] = 60,
                [TimeSpan.FromHours(1).ToString()] = 3600,
                [TimeSpan.FromDays(1).ToString()] = 86400
            };

            foreach (var entry in dictionary)
            {
                TimeSpan expectedTimeSpan = TimeSpan.Parse(entry.Key);
                TimeSpan actualTimeSpan = dictionary.GetTimeSpanValue(entry.Key);

                Assert.IsTrue(expectedTimeSpan == actualTimeSpan);
            }
        }

        [Test]
        public void GetTimeSpanValueExtensionThrowsWhenTheSettingIsNotDefined()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>();
            Assert.Throws<KeyNotFoundException>(() => dictionary.GetTimeSpanValue("UndefinedSetting"));
        }

        [Test]
        public void GetTimeSpanValueExtensionThrowsWhenTheSettingCannotBeConvertedToTheTypeSpecified()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                ["Setting"] = "I cannot be converted to a TimeSpan"
            };

            Assert.Throws<FormatException>(() => dictionary.GetTimeSpanValue("Setting"));
        }

        [Test]
        public void TryGetCollectionExtensionHandlesConvertibleDataTypes()
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                [nameof(Char)] = "A,B,C",
                [nameof(String)] = "String1,String2,String3",
                [nameof(Byte)] = "10,20,30",
                [nameof(Int16)] = "-22,-33,-44",
                [nameof(Int32)] = "-1000000,-2000000,-3000000",
                [nameof(Int64)] = "-100000000000,-200000000000,-300000000000",
                [nameof(UInt16)] = "22,33,44",
                [nameof(UInt32)] = "1000000,2000000,3000000",
                [nameof(UInt64)] = "100000000000,200000000000,300000000000",
                [nameof(Single)] = $"{float.MinValue},{float.MaxValue}",
                [nameof(Double)] = $"{double.MinValue},{double.MaxValue}",
                [nameof(Decimal)] = $"{decimal.MinValue},{decimal.MaxValue}",
                [nameof(Boolean)] = "true,false",
            };

            // Char conversions
            Assert.IsTrue(dictionary.TryGetCollection<char>(nameof(Char), out IEnumerable<char> charValues));
            CollectionAssert.AreEqual(dictionary[nameof(Char)].ToString().Split(",").Select(i => Convert.ToChar(i)), charValues);

            // String conversions
            Assert.IsTrue(dictionary.TryGetCollection<string>(nameof(String), out IEnumerable<string> stringValues));
            CollectionAssert.AreEqual(dictionary[nameof(String)].ToString().Split(","), stringValues);

            // Byte conversions
            Assert.IsTrue(dictionary.TryGetCollection<byte>(nameof(Byte), out IEnumerable<byte> byteValues));
            CollectionAssert.AreEqual(dictionary[nameof(Byte)].ToString().Split(",").Select(i => Convert.ToByte(i)), byteValues);

            // Int16/short conversions
            Assert.IsTrue(dictionary.TryGetCollection<short>(nameof(Int16), out IEnumerable<short> shortValues));
            CollectionAssert.AreEqual(dictionary[nameof(Int16)].ToString().Split(",").Select(i => Convert.ToInt16(i)), shortValues);

            // Int32/int conversions
            Assert.IsTrue(dictionary.TryGetCollection<int>(nameof(Int32), out IEnumerable<int> intValues));
            CollectionAssert.AreEqual(dictionary[nameof(Int32)].ToString().Split(",").Select(i => Convert.ToInt32(i)), intValues);

            // Int64/long conversions
            Assert.IsTrue(dictionary.TryGetCollection<long>(nameof(Int64), out IEnumerable<long> longValues));
            CollectionAssert.AreEqual(dictionary[nameof(Int64)].ToString().Split(",").Select(i => Convert.ToInt64(i)), longValues);

            // UInt16/ushort conversions
            Assert.IsTrue(dictionary.TryGetCollection<ushort>(nameof(UInt16), out IEnumerable<ushort> ushortValues));
            CollectionAssert.AreEqual(dictionary[nameof(UInt16)].ToString().Split(",").Select(i => Convert.ToUInt16(i)), ushortValues);

            // UInt32/uint conversions
            Assert.IsTrue(dictionary.TryGetCollection<uint>(nameof(UInt32), out IEnumerable<uint> uintValues));
            CollectionAssert.AreEqual(dictionary[nameof(UInt32)].ToString().Split(",").Select(i => Convert.ToUInt32(i)), uintValues);

            // UInt64/ulong conversions
            Assert.IsTrue(dictionary.TryGetCollection<ulong>(nameof(UInt64), out IEnumerable<ulong> ulongValues));
            CollectionAssert.AreEqual(dictionary[nameof(UInt64)].ToString().Split(",").Select(i => Convert.ToUInt64(i)), ulongValues);

            // Float conversions
            Assert.IsTrue(dictionary.TryGetCollection<float>(nameof(Single), out IEnumerable<float> floatValues));
            CollectionAssert.AreEqual(dictionary[nameof(Single)].ToString().Split(",").Select(i => Convert.ToSingle(i)), floatValues);

            // Double Float conversions
            Assert.IsTrue(dictionary.TryGetCollection<double>(nameof(Double), out IEnumerable<double> doubleValues));
            CollectionAssert.AreEqual(dictionary[nameof(Double)].ToString().Split(",").Select(i => Convert.ToDouble(i)), doubleValues);

            // Decimal conversions
            Assert.IsTrue(dictionary.TryGetCollection<decimal>(nameof(Decimal), out IEnumerable<decimal> decimalValues));
            CollectionAssert.AreEqual(dictionary[nameof(Decimal)].ToString().Split(",").Select(i => Convert.ToDecimal(i)), decimalValues);

            // Boolean conversions
            Assert.IsTrue(dictionary.TryGetCollection<bool>(nameof(Boolean), out IEnumerable<bool> booleanValues));
            CollectionAssert.AreEqual(dictionary[nameof(Boolean)].ToString().Split(",").Select(i => Convert.ToBoolean(i)), booleanValues);
        }

        [Test]
        [TestCase("|")]
        [TestCase(",,,")]
        [TestCase(";;;")]
        [TestCase("&&")]
        public void TryGetCollectionExtensionSupportsNonStandardDelimitersWhenSpecified(string nonStandardDelimiter)
        {
            IDictionary<string, IConvertible> dictionary = new Dictionary<string, IConvertible>
            {
                ["Collection"] = $"String1{nonStandardDelimiter}String2{nonStandardDelimiter}String3",
            };

            // String conversions
            Assert.IsTrue(dictionary.TryGetCollection<string>("Collection", nonStandardDelimiter.AsArray(), out IEnumerable<string> stringValues));
            CollectionAssert.AreEqual(dictionary["Collection"].ToString().Split(nonStandardDelimiter), stringValues);
        }
    }
}
