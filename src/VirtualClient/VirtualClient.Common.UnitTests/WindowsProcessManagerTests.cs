namespace VirtualClient.Common
{
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class WindowsProcessManagerTests
    {
        private WindowsProcessManager processManager;

        [SetUp]
        public void SetupTest()
        {
            this.processManager = new WindowsProcessManager();
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void WindowsProcessManagerCreatesTheExpectedProcess_1()
        {
            string command = @"C:\users\any\temp\command.exe";
            IProcessProxy process = this.processManager.CreateProcess(command);

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
        public void WindowsProcessManagerCreatesTheExpectedProcess_2()
        {
            string command = @"C:\users\any\temp\command.exe";
            string commandArguments = "--argument1=value --argument2=123";

            IProcessProxy process = this.processManager.CreateProcess(command, commandArguments);

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
        public void WindowsProcessManagerCreatesTheExpectedProcess_3()
        {
            string command = "command.exe";
            string commandArguments = "--argument1=value --argument2=123";
            string workingDirectory = "C:\\any\\directory";

            IProcessProxy process = this.processManager.CreateProcess(command, commandArguments, workingDirectory);

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
