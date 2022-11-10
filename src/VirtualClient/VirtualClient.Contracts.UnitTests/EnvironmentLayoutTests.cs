// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class EnvironmentLayoutTests
    {
        private IFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new Fixture().SetupMocks(true);
        }

        [Test]
        public void ExperimentLayoutConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentNullException>(() => new EnvironmentLayout(null));
            Assert.Throws<ArgumentException>(() => new EnvironmentLayout(new List<ClientInstance>()));
        }

        [Test]
        public void ExperimentLayoutConstructorsSetPropertiesToExpectedValues()
        {
            ClientInstance instance1 = this.mockFixture.Create<ClientInstance>();
            ClientInstance instance2 = this.mockFixture.Create<ClientInstance>();

            EnvironmentLayout layout = new EnvironmentLayout(new List<ClientInstance>
            {
                instance1,
                instance2
            });

            Assert.IsTrue(layout.Clients.Count() == 2);
            Assert.IsTrue(object.ReferenceEquals(layout.Clients.ElementAt(0), instance1));
            Assert.IsTrue(object.ReferenceEquals(layout.Clients.ElementAt(1), instance2));
        }

        [Test]
        public void ExperimentLayoutObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<EnvironmentLayout>(this.mockFixture.Create<EnvironmentLayout>());
        }
    }
}
