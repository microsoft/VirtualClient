// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;

    /// <summary>
    /// Provides assert-style methods for validating expected exception
    /// types, behaviors and messages.
    /// </summary>
    public static class ExceptionAssert
    {
        /// <summary>
        /// Asserts that the exception is implemented in correctly including validation of the constructors
        /// and that the exception class is serializable.
        /// </summary>
        /// <typeparam name="TException">The type of exception</typeparam>
        public static void IsImplementedCorrectly<TException>()
            where TException : Exception
        {
            string expectedMessage = "Any message that makes you happy.";
            NotImplementedException expectedInnerException = new NotImplementedException("Any inner exception message that gets the job done.");

            try
            {
                // Note:
                // Activator.CreateInstance throws if no constructor exists that takes in the parameters provided.

                // Constructor 1:  empty constructor
                TException exception = (TException)Activator.CreateInstance(typeof(TException));

                // Constructor 2:  expects a message
                exception = (TException)Activator.CreateInstance(typeof(TException), expectedMessage);
                if (expectedMessage != exception.Message)
                {
                    throw new ExceptionAssertFailedException("(Constructor 2) Message property does not match expected.");
                }

                // Constructor 3:  expects a message and an inner exception
                exception = (TException)Activator.CreateInstance(typeof(TException), expectedMessage, expectedInnerException);

                if (expectedMessage != exception.Message)
                {
                    throw new ExceptionAssertFailedException("(Constructor 3) Message property does not match expected.");
                }

                if (exception.InnerException == null)
                {
                    throw new ExceptionAssertFailedException("(Constructor 3) InnerException property not set.");
                }

                if (typeof(NotImplementedException) != exception.InnerException.GetType())
                {
                    throw new ExceptionAssertFailedException(
                        "(Constructor 3) InnerException property exception type does not match expected.");
                }

                if (expectedInnerException.Message != exception.InnerException.Message)
                {
                    throw new ExceptionAssertFailedException(
                        "(Constructor 3) InnerException property Message does not match expected.");
                }

                SerializationAssert.IsJsonSerializable<TException>(exception);
            }
            catch (Exception exc)
            {
                throw new ExceptionAssertFailedException(exc.Message);
            }
        }
    }
}
