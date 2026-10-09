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
            Assert.AreEqual(PlatformID.Win32NT, manager.Platform);
        }

        [Test]
        public void ProcessManagerCreatesTheExpectedManagerForUnixPlatforms()
        {
            ProcessManager manager = ProcessManager.Create(PlatformID.Unix);
            Assert.IsNotNull(manager);
            Assert.AreEqual(PlatformID.Unix, manager.Platform);
        }

        [Test]
        public void ProcessManagerThrowsWhenAPlatformIsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() => ProcessManager.Create(PlatformID.Other));
        }

        [Test]
        public void ProcessManagerCreatesTheExpectedProcessOnUnixSystems_1()
        {
            string command = "/home/command";

            ProcessManager manager = ProcessManager.Create(PlatformID.Unix);
            IProcessProxy process = manager.CreateProcess(command);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.IsEmpty(process.StartInfo.Arguments);
            Assert.IsEmpty(process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.RedirectStandardInput);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        public void ProcessManagerCreatesTheExpectedProcessOnUnixSystems_2()
        {
            string command = "/home/command";
            string commandArguments = "--argument1=value --argument2=123";

            ProcessManager manager = ProcessManager.Create(PlatformID.Unix);
            IProcessProxy process = manager.CreateProcess(command, commandArguments);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
            Assert.IsEmpty(process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.RedirectStandardInput);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        public void ProcessManagerCreatesTheExpectedProcessOnUnixSystems_3()
        {
            string command = "/home/command";
            string commandArguments = "--argument1=value --argument2=123";
            string workingDirectory = "/home/any/directory";

            ProcessManager manager = ProcessManager.Create(PlatformID.Unix);
            IProcessProxy process = manager.CreateProcess(command, commandArguments, workingDirectory);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
            Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.RedirectStandardInput);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessManagerCreatesTheExpectedProcessOnWindowsSystems_1()
        {
            string command = @"C:\users\any\temp\command.exe";

            ProcessManager manager = ProcessManager.Create(PlatformID.Win32NT);
            IProcessProxy process = manager.CreateProcess(command);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.IsEmpty(process.StartInfo.Arguments);
            Assert.IsEmpty(process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessManagerCreatesTheExpectedProcessOnWindowsSystems_2()
        {
            string command = @"C:\users\any\temp\command.exe";
            string commandArguments = "--argument1=value --argument2=123";

            ProcessManager manager = ProcessManager.Create(PlatformID.Win32NT);
            IProcessProxy process = manager.CreateProcess(command, commandArguments);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
            Assert.IsEmpty(process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        public void WindowsProcessManagerCreatesTheExpectedProcessOnWindowsSystems_3()
        {
            string command = "command.exe";
            string commandArguments = "--argument1=value --argument2=123";
            string workingDirectory = "C:\\any\\directory";

            ProcessManager manager = ProcessManager.Create(PlatformID.Win32NT);
            IProcessProxy process = manager.CreateProcess(command, commandArguments, workingDirectory);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
            Assert.AreEqual(workingDirectory, process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }
    }
}
