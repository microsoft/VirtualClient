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
    public class ExecutionProfileValidationTests
    {
        private IFixture fixture;
        private ExecutionProfile profile;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new Fixture().SetupMocks();
            this.profile = this.fixture.Create<ExecutionProfile>();
        }

        [Test]
        public void ExecutionProfileValidatorValidatesArgumentsOnValidate()
        { 
            Assert.Throws<ArgumentException>(() => ExecutionProfileValidation.Instance.Validate(null));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ExecutionPofileValidatorReturnsExpectedResultOnValidate(bool isValid)
        {
            TestRule rule = new TestRule();
            rule.OnValidate = (p) => new ValidationResult(isValid);
            ExecutionProfileValidation.Instance.Add(rule);

            ValidationResult result = ExecutionProfileValidation.Instance.Validate(this.profile);
            Assert.AreEqual(isValid, result.IsValid);
        }

        [Test]
        public void ExecutionProfileValidatorSurfacesRuleErrorMessages()
        {
            string message = "my descriptive error message";
            TestRule rule = new TestRule();
            rule.OnValidate = (p) => new ValidationResult(false, new List<string>() { message });
            ExecutionProfileValidation.Instance.Add(rule);

            ValidationResult result = ExecutionProfileValidation.Instance.Validate(this.profile);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.ValidationErrors.Count);
            Assert.AreEqual(message, result.ValidationErrors[0]);
        }

        private class TestRule : IValidationRule<ExecutionProfile>
        {
            public Func<ExecutionProfile, ValidationResult> OnValidate { get; set; }

            public ValidationResult Validate(ExecutionProfile profile) => 
                this.OnValidate == null ? new ValidationResult(true) : this.OnValidate.Invoke(profile);
        }
    }
}
