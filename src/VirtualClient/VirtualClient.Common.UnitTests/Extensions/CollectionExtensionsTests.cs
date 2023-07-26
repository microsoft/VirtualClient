// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Collections.Generic;
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
    }
}
