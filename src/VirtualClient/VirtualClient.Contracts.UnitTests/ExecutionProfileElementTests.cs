// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using AutoFixture;
    using VirtualClient.Common;
    using Newtonsoft.Json.Linq;
    using System.IO;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ExecutionProfileElementElementTests
    {
        private IFixture fixture;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new Fixture().SetupMocks(true);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase(" ")]
        public void ExecutionProfileElementValidatesRequiredParameters(string invalidParameter)
        {
            ExecutionProfileElement validComponent = this.fixture.Create<ExecutionProfileElement>();
            Assert.Throws<ArgumentException>(() => new ExecutionProfileElement(invalidParameter, validComponent.Parameters));
        }

        [Test]
        public void ExecutionProfileElementIsJsonSerializableByDefault()
        {
            ExecutionProfileElement profile = this.fixture.Create<ExecutionProfileElement>();
            SerializationAssert.IsJsonSerializable<ExecutionProfileElement>(profile);
        }

        [Test]
        public void ExecutionProfileElementIsJsonSerializableWithChildComponentDefinitions()
        {
            // Add 2 child/subcomponents to the parent elements.
            ExecutionProfileElement element = new ExecutionProfileElement("AnyType", null, null, new List<ExecutionProfileElement>
            {
                this.fixture.Create<ExecutionProfileElement>(),
                this.fixture.Create<ExecutionProfileElement>()
            });

            SerializationAssert.IsJsonSerializable<ExecutionProfileElement>(element);
        }

        [Test]
        public void ExecutionProfileElementIsJsonSerializableWithParallelExecutionDefinitions()
        {
            // Add 2 child/subcomponents to the parent elements.
            ExecutionProfileElement element = new ExecutionProfileElement(typeof(ParallelExecution).Name, null, null, new List<ExecutionProfileElement>
            {
                this.fixture.Create<ExecutionProfileElement>(),
                this.fixture.Create<ExecutionProfileElement>()
            });

            SerializationAssert.IsJsonSerializable<ExecutionProfileElement>(element);
        }

        [Test]
        public void ExecutionProfileElementIsJsonSerializableWithParallelLoopExecutionDefinitions()
        {
            // Add 2 child/subcomponents to the parent elements.
            ExecutionProfileElement element = new ExecutionProfileElement(typeof(ParallelLoopExecution).Name, null, null, new List<ExecutionProfileElement>
            {
                this.fixture.Create<ExecutionProfileElement>(),
                this.fixture.Create<ExecutionProfileElement>()
            });

            SerializationAssert.IsJsonSerializable<ExecutionProfileElement>(element);
        }

        [Test]
        public void ExecutionProfileElementIsJsonSerializableWithSequentialExecutionDefinitions()
        {
            // Add 2 child/subcomponents to the parent elements.
            ExecutionProfileElement element = new ExecutionProfileElement(typeof(SequentialExecution).Name, null, null, new List<ExecutionProfileElement>
            {
                this.fixture.Create<ExecutionProfileElement>(),
                this.fixture.Create<ExecutionProfileElement>()
            });

            SerializationAssert.IsJsonSerializable<ExecutionProfileElement>(element);
        }

        [Test]
        public void ExecutionProfileElementRepresentsStringConversionSemanticsCorrectly()
        {
            ExecutionProfileElement element = new ExecutionProfileElement(
                "AnyType1",
                new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "ValueA" },
                    { "Parameter2", 123.45 },
                    { "Parameter3", true },
                    { "Parameter4", "00:01:00"  }
                },
                new Dictionary<string, IConvertible>
                {
                    { "Metadata1", "ValueB" },
                    { "Metadata2", 6789.123 },
                    { "Metadata3", false },
                    { "Metadata4", "00:02:30"  }
                });

            element.ComponentType = ComponentType.Monitor;

            string expectedValue = "Type:AnyType1,,,ComponentType:Monitor,,,Metadata:[Metadata1=ValueB;Metadata2=6789.123;Metadata3=False;Metadata4=00:02:30],,,Parameters:[Parameter1=ValueA;Parameter2=123.45;Parameter3=True;Parameter4=00:01:00]";
            string actualValue = element.ToString();

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void ExecutionProfileElementRepresentsStringConversionSemanticsCorrectly_2()
        {
            ExecutionProfileElement element = new ExecutionProfileElement(
                   "AnyType1",
                   new Dictionary<string, IConvertible>
                   {
                    { "Parameter1", "ValueA" }
                   },
                   new Dictionary<string, IConvertible>
                   {
                    { "Metadata1", "ValueB" }
                   },
                   new List<ExecutionProfileElement>
                   {
                        new ExecutionProfileElement(
                            "AnyType2",
                            new Dictionary<string, IConvertible>
                            {
                                { "Parameter1", "ValueC" }
                            },
                            new Dictionary<string, IConvertible>
                            {
                                { "Metadata1", "ValueD" }
                            })
                   });

            element.ComponentType = ComponentType.Action;
            element.Components.First().ComponentType = ComponentType.Action;
            element.Components.First().Extensions.Add("Extension1", JToken.FromObject("{ 'key1': 'value1', 'key2': [ 'one', 'two'], 'key3': { 'any': 'value', 'in': 'the', 'dictionary': 'true' } }"));

            string expectedValue =
                "Type:AnyType1,,,ComponentType:Action,,,Metadata:[Metadata1=ValueB],,,Parameters:[Parameter1=ValueA],,," +
                "Components:[(Type:AnyType2,,,ComponentType:Action,,,Metadata:[Metadata1=ValueD],,,Parameters:[Parameter1=ValueC],,,Extensions:[(Extension1={'key1':'value1','key2':['one','two'],'key3':{'any':'value','in':'the','dictionary':'true'}})])]";

            string actualValue = element.ToString();

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void ExecutionProfileElementRepresentsStringConversionSemanticsCorrectly_3()
        {
            ExecutionProfileElement element = new ExecutionProfileElement(
                   "AnyType1",
                   new Dictionary<string, IConvertible>
                   {
                    { "Parameter1", "ValueA" }
                   },
                   new Dictionary<string, IConvertible>
                   {
                    { "Metadata1", "ValueB" }
                   },
                   new List<ExecutionProfileElement>
                   {
                        new ExecutionProfileElement(
                            "AnyType2",
                            new Dictionary<string, IConvertible>
                            {
                                { "Parameter1", "ValueC" }
                            },
                            new Dictionary<string, IConvertible>
                            {
                                { "Metadata1", "ValueD" }
                            },
                            new List<ExecutionProfileElement>
                            {
                                new ExecutionProfileElement(
                                    "AnyType3",
                                    new Dictionary<string, IConvertible>
                                    {
                                        { "Parameter1", "ValueE" }
                                    },
                                    new Dictionary<string, IConvertible>
                                    {
                                        { "Metadata1", "ValueF" }
                                    })
                            })
                   });

            element.ComponentType = ComponentType.Action;
            string expectedValue =
                "Type:AnyType1,,,ComponentType:Action,,,Metadata:[Metadata1=ValueB],,,Parameters:[Parameter1=ValueA],,," +
                "Components:[(Type:AnyType2,,,ComponentType:Undefined,,,Metadata:[Metadata1=ValueD],,,Parameters:[Parameter1=ValueC],,,Components:[(Type:AnyType3,,,ComponentType:Undefined,,,Metadata:[Metadata1=ValueF],,,Parameters:[Parameter1=ValueE])])]";

            string actualValue = element.ToString();

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void ExecutionProfileElementRepresentsStringConversionSemanticsCorrectly_4()
        {
            ExecutionProfileElement element = new ExecutionProfileElement(
                   "AnyType1",
                   new Dictionary<string, IConvertible>
                   {
                    { "Parameter1", "ValueA" }
                   },
                   new Dictionary<string, IConvertible>
                   {
                    { "Metadata1", "ValueB" }
                   },
                   new List<ExecutionProfileElement>
                   {
                    new ExecutionProfileElement(
                        "AnyType2",
                        new Dictionary<string, IConvertible>
                        {
                            { "Parameter1", "ValueC" }
                        },
                        new Dictionary<string, IConvertible>
                        {
                            { "Metadata1", "ValueD" }
                        },
                        new List<ExecutionProfileElement>
                        {
                            new ExecutionProfileElement(
                                "AnyType3",
                                new Dictionary<string, IConvertible>
                                {
                                    { "Parameter1", "ValueE" }
                                },
                                new Dictionary<string, IConvertible>
                                {
                                    { "Metadata1", "ValueF" }
                                })
                        })
                   });

            element.ComponentType = ComponentType.Dependency;
            element.Extensions.Add("Extension1", JToken.FromObject("{ 'key1': 'value1', 'key2': [ 'one', 'two'], 'key3': { 'any': 'value', 'in': 'the', 'dictionary': 'true' } }"));

            string expectedValue =
                "Type:AnyType1,,,ComponentType:Dependency,,,Metadata:[Metadata1=ValueB],,,Parameters:[Parameter1=ValueA],,," +
                "Components:[(Type:AnyType2,,,ComponentType:Undefined,,,Metadata:[Metadata1=ValueD],,,Parameters:[Parameter1=ValueC],,," +
                "Components:[(Type:AnyType3,,,ComponentType:Undefined,,,Metadata:[Metadata1=ValueF],,,Parameters:[Parameter1=ValueE])])],,," +
                "Extensions:[(Extension1={'key1':'value1','key2':['one','two'],'key3':{'any':'value','in':'the','dictionary':'true'}})]";

            string actualValue = element.ToString();

            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        public void ExecutionProfileElementImplementsHashCodeSemanticsCorrectly()
        {
            ExecutionProfileElement element = this.fixture.Create<ExecutionProfileElement>();
            ExecutionProfileElement element2 = this.fixture.Create<ExecutionProfileElement>();
            EqualityAssert.CorrectlyImplementsHashcodeSemantics<ExecutionProfileElement>(() => element, () => element2);
        }

        [Test]
        public void ExecutionProfileElementImplementsHashCodeSemanticsCorrectly_2()
        {
            ExecutionProfileElement element1 = new ExecutionProfileElement(
                "AnyType1",
                new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "ValueA" },
                    { "Parameter2", 123.45 },
                    { "Parameter3", true },
                    { "Parameter4", "00:01:00"  }
                },
                new Dictionary<string, IConvertible>
                {
                    { "Metadata1", "ValueB" },
                    { "Metadata2", 6789.123 },
                    { "Metadata3", false },
                    { "Metadata4", "00:02:30"  }
                });

            element1.ComponentType = ComponentType.Action;

            ExecutionProfileElement element2 = new ExecutionProfileElement(
                "AnyType2",
                new Dictionary<string, IConvertible>
                {
                    { "Parameter5", "ValueC" },
                    { "Parameter6", 987.65 },
                    { "Parameter7", true },
                    { "Parameter8", "00:03:00"  }
                },
                new Dictionary<string, IConvertible>
                {
                    { "Metadata1", "ValueD" },
                    { "Metadata2", 777.88 },
                    { "Metadata3", false },
                    { "Metadata4", "00:04:15"  }
                });

            element2.ComponentType = ComponentType.Action;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics<ExecutionProfileElement>(() => element1, () => element2);
        }

        [Test]
        public void ExecutionProfileElementImplementsHashCodeSemanticsCorrectly_3()
        {
            ExecutionProfileElement element1 = new ExecutionProfileElement(
                "AnyType1",
                new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "ValueA" }
                },
                new Dictionary<string, IConvertible>
                {
                    { "Metadata1", "ValueB" }
                },
                new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement(
                        "AnyType2",
                        new Dictionary<string, IConvertible>
                        {
                            { "Parameter1", "ValueC" }
                        },
                        new Dictionary<string, IConvertible>
                        {
                            { "Metadata1", "ValueD" }
                        },
                        new List<ExecutionProfileElement>
                        {
                            new ExecutionProfileElement(
                                "AnyType3",
                                new Dictionary<string, IConvertible>
                                {
                                    { "Parameter1", "ValueE" }
                                },
                                new Dictionary<string, IConvertible>
                                {
                                    { "Metadata1", "ValueF" }
                                })
                        })
                });

            element1.ComponentType = ComponentType.Action;
            string here = element1.ToString();

            ExecutionProfileElement element2 = new ExecutionProfileElement(
                "AnyType4",
                new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "ValueG" }
                },
                new Dictionary<string, IConvertible>
                {
                    { "Metadata1", "ValueH" }
                },
                new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement(
                        "AnyType5",
                        new Dictionary<string, IConvertible>
                        {
                            { "Parameter1", "ValueI" }
                        },
                        new Dictionary<string, IConvertible>
                        {
                            { "Metadata1", "ValueJ" }
                        },
                        new List<ExecutionProfileElement>
                        {
                            new ExecutionProfileElement(
                                "AnyType6",
                                new Dictionary<string, IConvertible>
                                {
                                    { "Parameter1", "ValueK" }
                                },
                                new Dictionary<string, IConvertible>
                                {
                                    { "Metadata1", "ValueL" }
                                })
                        })
                });

            element2.ComponentType = ComponentType.Action;

            EqualityAssert.CorrectlyImplementsHashcodeSemantics<ExecutionProfileElement>(() => element1, () => element2);
        }

        [Test]
        public void ExecutionProfileElementImplementsEqualitySemanticsCorrectly()
        {
            ExecutionProfileElement element = this.fixture.Create<ExecutionProfileElement>();
            ExecutionProfileElement element2 = this.fixture.Create<ExecutionProfileElement>();
            EqualityAssert.CorrectlyImplementsEqualitySemantics<ExecutionProfileElement>(() => element, () => element2);
        }
    }
}
