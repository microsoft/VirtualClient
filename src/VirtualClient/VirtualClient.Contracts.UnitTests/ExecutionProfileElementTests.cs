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
        public void ExecutionProfileElementImplementsHashCodeSemanticsCorrectly()
        {
            ExecutionProfileElement element = this.fixture.Create<ExecutionProfileElement>();
            ExecutionProfileElement element2 = this.fixture.Create<ExecutionProfileElement>();
            EqualityAssert.CorrectlyImplementsHashcodeSemantics<ExecutionProfileElement>(() => element, () => element2);
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
