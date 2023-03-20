namespace VirtualClient.Common
{
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class UnixProcessManagerTests
    {
        private UnixProcessManager processManager;

        [SetUp]
        public void SetupTest()
        {
            this.processManager = new UnixProcessManager();
        }

        [Test]
        public void UnixProcessManagerCreatesTheExpectedProcess_1()
        {
            string command = "/home/command";
            IProcessProxy process = this.processManager.CreateProcess(command);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.IsEmpty(process.StartInfo.Arguments);
            Assert.AreEqual("/home", process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.RedirectStandardInput);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        public void UnixProcessManagerCreatesTheExpectedProcess_2()
        {
            string command = "/home/command";
            string commandArguments = "--argument1=value --argument2=123";

            IProcessProxy process = this.processManager.CreateProcess(command, commandArguments);

            Assert.IsNotNull(process);
            Assert.IsNotNull(process.StartInfo);
            Assert.AreEqual(command, process.StartInfo.FileName);
            Assert.AreEqual(commandArguments, process.StartInfo.Arguments);
            Assert.AreEqual("/home", process.StartInfo.WorkingDirectory);
            Assert.IsTrue(process.StartInfo.RedirectStandardOutput);
            Assert.IsTrue(process.StartInfo.RedirectStandardError);
            Assert.IsFalse(process.StartInfo.RedirectStandardInput);
            Assert.IsFalse(process.StartInfo.UseShellExecute);
        }

        [Test]
        public void UnixProcessManagerCreatesTheExpectedProcess_3()
        {
            string command = "/home/command";
            string commandArguments = "--argument1=value --argument2=123";
            string workingDirectory = "/home/any/directory";

            IProcessProxy process = this.processManager.CreateProcess(command, commandArguments, workingDirectory);

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
    }
}
