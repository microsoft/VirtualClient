// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class EventContextExtensionsTests
    {
        private const string ErrorProperty = "error";
        private const string ErrorCallstackProperty = "errorCallstack";

        private EventContext context;
        private InvalidOperationException exampleException1;
        private FormatException exampleException2;

        [SetUp]
        public void SetupTest()
        {
            this.context = new EventContext(Guid.NewGuid());
            this.exampleException1 = new InvalidOperationException("Things went south at 2:00 in the morning.");
            this.exampleException2 = new FormatException("Format is neat but not right.");
        }

        [Test]
        public void AddErrorExtensionAddsTheErrorToTheContextPropertiesAsExpected()
        {
            this.context.AddError(this.exampleException1);
            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorProperty));

            List<object> errors = this.context.Properties[ErrorProperty] as List<object>;
            Assert.IsNotNull(errors);
            Assert.IsTrue(errors.Count == 1);

            string expectedError = new List<object>
            {
                new
                {
                    errorType = this.exampleException1.GetType().FullName,
                    errorMessage = this.exampleException1.Message
                }
            }.ToJson();

            string actualError = errors.ToJson();

            SerializationAssert.JsonEquals(expectedError, actualError);
        }

        [Test]
        public void AddErrorExtensionCapturesInnerExceptionsInTheContextProperties()
        {
            InvalidCastException expectedException = new InvalidCastException("Not good", new InvalidOperationException("Even worse.", this.exampleException1));

            this.context.AddError(expectedException);
            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorProperty));

            List<object> errors = this.context.Properties[ErrorProperty] as List<object>;
            Assert.IsNotNull(errors);
            Assert.IsTrue(errors.Count == 3);

            string expectedError = new List<object>
            {
                new
                {
                    errorType = expectedException.GetType().FullName,
                    errorMessage = expectedException.Message
                },
                new
                {
                    errorType = expectedException.InnerException.GetType().FullName,
                    errorMessage = expectedException.InnerException.Message
                },
                new
                {
                    errorType = expectedException.InnerException.InnerException.GetType().FullName,
                    errorMessage = expectedException.InnerException.InnerException.Message
                }
            }.ToJson();

            string actualError = errors.ToJson();

            SerializationAssert.JsonEquals(expectedError, actualError);
        }

        [Test]
        public void AddErrorExtensionAddsAdditionalExceptionsToTheExistingList()
        {
            this.context.AddError(this.exampleException1);
            this.context.AddError(this.exampleException2);

            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorProperty));

            List<object> errors = this.context.Properties[ErrorProperty] as List<object>;
            Assert.IsNotNull(errors);
            Assert.IsTrue(errors.Count == 2);

            string expectedError = new List<object>
            {
                new
                {
                    errorType = this.exampleException1.GetType().FullName,
                    errorMessage = this.exampleException1.Message
                },
                new
                {
                    errorType = this.exampleException2.GetType().FullName,
                    errorMessage = this.exampleException2.Message
                }
            }.ToJson();

            string actualError = errors.ToJson();

            SerializationAssert.JsonEquals(expectedError, actualError);
        }

        [Test]
        public void AddErrorExtensionAddsAdditionalExceptionsThatHaveInnerExceptionsToTheExistingList()
        {
            InvalidCastException expectedException1 = new InvalidCastException("Not good", new InvalidOperationException("Deeper reason for the issue."));
            InvalidCastException expectedException2 = new InvalidCastException("Even Worse", new InvalidOperationException("Real answer hiding."));

            this.context.AddError(expectedException1);
            this.context.AddError(expectedException2);

            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorProperty));

            List<object> errors = this.context.Properties[ErrorProperty] as List<object>;
            Assert.IsNotNull(errors);
            Assert.IsTrue(errors.Count == 4);

            string expectedError = new List<object>
            {
                new
                {
                    errorType = expectedException1.GetType().FullName,
                    errorMessage = expectedException1.Message
                },
                new
                {
                    errorType = expectedException1.InnerException.GetType().FullName,
                    errorMessage = expectedException1.InnerException.Message
                },
                new
                {
                    errorType = expectedException2.GetType().FullName,
                    errorMessage = expectedException2.Message
                },
                new
                {
                    errorType = expectedException2.InnerException.GetType().FullName,
                    errorMessage = expectedException2.InnerException.Message
                }
            }.ToJson();

            string actualError = errors.ToJson();

            SerializationAssert.JsonEquals(expectedError, actualError);
        }

        [Test]
        public void AddErrorExtensionAddsTheExceptionCallstackToTheContextPropertiesAsExpected()
        {
            try
            {
                throw this.exampleException1;
            }
            catch (Exception exc)
            {
                this.context.AddError(exc, withCallStack: true);
            }

            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorProperty));
            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorCallstackProperty));

            List<object> errors = this.context.Properties[ErrorProperty] as List<object>;
            string errorCallstack = this.context.Properties[ErrorCallstackProperty] as string;

            Assert.IsNotNull(errors);
            Assert.IsNotNull(errorCallstack);
            Assert.IsTrue(errors.Count == 1);

            Assert.AreEqual(this.exampleException1.StackTrace, errorCallstack);
        }

        [Test]
        public void AddErrorExtensionHandlesCallstacksWhoseLengthExceedsTheMaximum()
        {
            try
            {
                throw this.exampleException1;
            }
            catch (Exception exc)
            {
                this.context.AddError(exc, withCallStack: true, maxCallStackLength: 50);
            }

            Assert.IsTrue(this.context.Properties.ContainsKey(ErrorCallstackProperty));
            string errorCallstack = this.context.Properties[ErrorCallstackProperty] as string;

            Assert.IsNotNull(errorCallstack);
            Assert.AreEqual(this.exampleException1.StackTrace.Substring(0, 50), errorCallstack);
        }

        [Test]
        public void AddPropertyExtensionHandlesConflicts()
        {
            this.context.AddContext("property1", 1);
            this.context.AddContext("property2", "2");
            this.context.AddContext("property1", "3");
            this.context.AddContext("property2", 4);
            this.context.AddContext("property3", "5");
            this.context.AddContext("property4", 6);

            Assert.IsTrue(this.context.Properties.Count == 4);
            Assert.AreEqual(this.context.Properties["property1"], "3");
            Assert.AreEqual(this.context.Properties["property2"], 4);
            Assert.AreEqual(this.context.Properties["property3"], "5");
            Assert.AreEqual(this.context.Properties["property4"], 6);
        }
    }
}
