using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Management.Infrastructure;
using Microsoft.Management.Infrastructure.Options;
using NUnit.Framework;

namespace VirtualClient
{
    [TestFixture]
    [Category("Integration")]
    internal class CimIntegrationTests
    {
        [Test]
        public void GetMemoryHardwareInformation()
        {
            CimSession session = CimSession.Create("10.7.0.15", new DComSessionOptions());

            // https://learn.microsoft.com/en-us/previous-versions/windows/desktop/virtual/cim-logicaldevice
            // https://wutils.com/wmi/namespaces.html
            // var storageInstances = session.QueryInstances(@"Root\Microsoft\Windows\Storage", "WQL", "SELECT * FROM MSFT_Partition");

            var instances = session.QueryInstances(@"Root\CIMV2", "WQL", "SELECT * FROM CIM_PhysicalMemory");

            StringBuilder properties = new StringBuilder();
            foreach (var instance in instances)
            {
                foreach (var property in instance.CimInstanceProperties)
                {
                    properties.AppendLine($"{property.Name}: {property.Value}");
                }
            }
        }
    }
}
