// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture;
    using VirtualClient.Common;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ProxyTelemetryMessageTests
    {
        private ProxyTelemetryMessage message;

        [SetUp]
        public void SetupTest()
        {
            this.message = new ProxyTelemetryMessage
            {
                Source = "VirtualClient",
                EventType = "Traces",
                AppHost = "AnyHost",
                AppName = "VirtualClient",
                ItemType = "trace",
                OperationId = Guid.NewGuid().ToString(),
                OperationParentId = Guid.NewGuid().ToString(),
                Message = "AnyMessage",
                SdkVersion = "1.2.3.4",
                SeverityLevel = LogLevel.Information,
                CustomDimensions = new Dictionary<string, object>
                {
                    ["stringProperty"] = "stringValue",
                    ["intProperty"] = 1234,
                    ["guidProperty"] = Guid.NewGuid(),
                    ["datetimeProperty"] = DateTime.Parse("2022-09-13T00:00:00Z"),
                    ["dictionaryProperty"] = new Dictionary<string, object>
                    {
                        ["string"] = "any value",
                        ["int"] = 9876,
                        ["float"] = 3.45,
                        ["datetime"] = DateTime.Parse("2022-09-13T00:00:00Z"),
                        ["guid"] = Guid.NewGuid().ToString()
                    },
                    ["listProperty"] = new List<object>
                    {
                        "string",
                        12345,
                        1.25,
                        Guid.NewGuid(),
                        DateTime.Parse("2022-09-13T00:00:00Z")
                    }
                }
            };
        }

        [Test]
        public void ProxyTelemetryMessageObjectsAreJsonSerializable()
        {
            SerializationAssert.IsJsonSerializable<ProxyTelemetryMessage>(this.message);
        }
    }
}
