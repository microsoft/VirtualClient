// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using AutoFixture;

    [TestFixture]
    [Category("Unit")]
    public class SchemaValidationRuleTests
    {
        private static readonly string errorMessage = "The parameter reference: \'{0}\' could not be found in the profile parameters.";
        private IFixture fixture;
        private ExecutionProfileElement element;
        private ExecutionProfile profile;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new Fixture().SetupMocks();
            this.profile = this.fixture.Create<ExecutionProfile>();
            this.element = this.fixture.Create<ExecutionProfileElement>();
        }

        [Test]
        public void SchemaRuleValidatesArgumentsOnValidate()
        {
            Assert.Throws<ArgumentException>(() => SchemaRules.Instance.Validate(null));
        }

        [Test]
        public void SchemaRuleValidatesActionsOnValidate()
        {
            string reference = "notAvailable";
            this.element.Parameters["parameter"] = $"{ExecutionProfile.ParameterPrefix}{reference}";
            this.profile.Actions.Add(this.element);

            ValidationResult result = SchemaRules.Instance.Validate(this.profile);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.ValidationErrors.Count);
            Assert.AreEqual(string.Format(SchemaValidationRuleTests.errorMessage, reference), result.ValidationErrors[0]);
        }

        [Test]
        public void SchemaRulesValidatesMonitorsOnValidate()
        {
            string reference = "notAvailable";
            this.element.Parameters["parameter"] = $"{ExecutionProfile.ParameterPrefix}{reference}";
            this.profile.Monitors.Add(this.element);

            ValidationResult result = SchemaRules.Instance.Validate(this.profile);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.ValidationErrors.Count);
            Assert.AreEqual(string.Format(SchemaValidationRuleTests.errorMessage, reference), result.ValidationErrors[0]);
        }

        [Test]
        public void SchemaRulesValidatesDependenciesOnValidate()
        {
            string reference = "notAvailable";
            this.element.Parameters["parameter"] = $"{ExecutionProfile.ParameterPrefix}{reference}";
            this.profile.Dependencies.Add(this.element);

            ValidationResult result = SchemaRules.Instance.Validate(this.profile);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.ValidationErrors.Count);
            Assert.AreEqual(string.Format(SchemaValidationRuleTests.errorMessage, reference), result.ValidationErrors[0]);
        }

        [Test]
        public void SchemaRulesAggregatesAllErrorsOnValidate()
        {
            string reference1 = "reference1";
            string reference2 = "reference2";
            ExecutionProfileElement element2 = new ExecutionProfileElement(this.element);
            this.element.Parameters["parameter"] = $"{ExecutionProfile.ParameterPrefix}{reference1}";
            element2.Parameters["parameter"] = $"{ExecutionProfile.ParameterPrefix}{reference2}";

            this.profile.Actions.Add(this.element);
            this.profile.Monitors.Add(element2);

            ValidationResult result = SchemaRules.Instance.Validate(this.profile);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.ValidationErrors.Count);
            Assert.AreNotEqual(result.ValidationErrors[0], result.ValidationErrors[1]);
        }

        [Test]
        public void SchemaRulesReturnsExpectedResultWhenSchemaIsValidOnValidate()
        {
            ValidationResult result = SchemaRules.Instance.Validate(this.profile);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.ValidationErrors);
        }
    }
}
