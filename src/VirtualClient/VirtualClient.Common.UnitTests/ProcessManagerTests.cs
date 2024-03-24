namespace VirtualClient.Common
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ProcessManagerTests
    {
        [Test]
        public void ProcessManagerCreatesTheExpectedManagerForWindowsPlatforms()
        {
            ProcessManager manager = ProcessManager.Create(PlatformID.Win32NT);
            Assert.IsNotNull(manager);
            Assert.IsInstanceOf<WindowsProcessManager>(manager);
        }

        [Test]
        public void ProcessManagerCreatesTheExpectedManagerForUnixPlatforms()
        {
            ProcessManager manager = ProcessManager.Create(PlatformID.Unix);
            Assert.IsNotNull(manager);
            Assert.IsInstanceOf<UnixProcessManager>(manager);
        }

        [Test]
        public void ProcessManagerThrowsWhenAPlatformIsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => ProcessManager.Create(PlatformID.Other));
        }
    }
}
