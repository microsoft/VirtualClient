// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.TestExtensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Moq;
    using Moq.Language;
    using Moq.Language.Flow;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions to help setup common mock class instances and behaviors.
    /// </summary>
    public static class TestMockSetupExtensions
    {
        private static Random randomGen = new Random();

        /// <summary>
        /// Setup default property values and behaviors for a mock system/OS process.
        /// </summary>
        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Mock object support.")]
        public static Mock<IProcessProxy> Setup(
            this Mock<IProcessProxy> mockProcess,
            string command = null,
            string commandArguments = null,
            string workingDirectory = null,
            int? processId = null,
            int? exitCode = null,
            bool hasExited = true)
        {
            mockProcess.ThrowIfNull(nameof(mockProcess));

            ProcessStartInfo mockStartInfo = TestMockSetupExtensions.CreateMockProcessStartInfo(command, commandArguments, workingDirectory);

            mockProcess.SetupGet(p => p.ExitCode).Returns(exitCode ?? 0);
            mockProcess.SetupGet(p => p.HasExited).Returns(hasExited);
            mockProcess.SetupGet(p => p.Id).Returns(processId ?? TestMockSetupExtensions.randomGen.Next(100, 10000000));
            mockProcess.SetupGet(p => p.StartInfo).Returns(mockStartInfo);
            mockProcess.SetupGet(p => p.Name).Returns(Path.GetFileNameWithoutExtension(mockStartInfo.FileName));
            mockProcess.SetupGet(p => p.StandardError).Returns(new ConcurrentBuffer());
            mockProcess.SetupGet(p => p.StandardOutput).Returns(new ConcurrentBuffer());
            mockProcess.SetupGet(p => p.StandardInput).Returns(new StreamWriter(new MemoryStream()));
            mockProcess.Setup(p => p.Start()).Returns(true);

            return mockProcess;
        }

        /// <summary>
        /// Quick setup extension: Mock&lt;ProcessManager&gt;.Setup(mgr => mgr.CreateProcess(anyCommand, anyCommandArguments, anyWorkingDir));
        /// </summary>
        public static ISetup<ProcessManager, IProcessProxy> SetupOnCreateProcess(this Mock<ProcessManager> processManager)
        {
            processManager.ThrowIfNull(nameof(processManager));
            return processManager.Setup(mgr => mgr.CreateProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        /// <summary>
        /// Quick setup extension: Mock&lt;ProcessManager&gt;.SetupSequence(mgr => mgr.CreateProcess(anyCommand, anyCommandArguments, anyWorkingDir));
        /// </summary>
        public static ISetupSequentialResult<IProcessProxy> SetupSequenceOnCreateProcess(this Mock<ProcessManager> processManager)
        {
            processManager.ThrowIfNull(nameof(processManager));
            return processManager.SetupSequence(mgr => mgr.CreateProcess(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        }

        /// <summary>
        /// Quick setup extension: Mock&lt;ProcessManager&gt;.Setup(mgr => mgr.GetProcess(anyProcessId));
        /// </summary>
        public static ISetup<ProcessManager, IProcessProxy> SetupOnGetProcess(this Mock<ProcessManager> processManager)
        {
            processManager.ThrowIfNull(nameof(processManager));
            return processManager.Setup(mgr => mgr.GetProcess(It.IsAny<int>()));
        }

        /// <summary>
        /// Quick setup extension: Mock&lt;ProcessManager&gt;.SetupSequence(mgr => mgr.GetProcess(anyProcessId));
        /// </summary>
        public static ISetupSequentialResult<IProcessProxy> SetupSequenceOnGetProcess(this Mock<ProcessManager> processManager)
        {
            processManager.ThrowIfNull(nameof(processManager));
            return processManager.SetupSequence(mgr => mgr.GetProcess(It.IsAny<int>()));
        }

        /// <summary>
        /// Quick setup extension: Mock&lt;ProcessManager&gt;.Setup(mgr => mgr.GetProcesses(anyProcessName));
        /// </summary>
        public static ISetup<ProcessManager, IEnumerable<IProcessProxy>> SetupOnGetProcesses(this Mock<ProcessManager> processManager)
        {
            processManager.ThrowIfNull(nameof(processManager));
            return processManager.Setup(mgr => mgr.GetProcesses(It.IsAny<string>()));
        }

        private static ProcessStartInfo CreateMockProcessStartInfo(string command = null, string commandArguments = null, string workingDirectory = null)
        {
            return new ProcessStartInfo
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = commandArguments ?? "--option1=value --option2=1234",
                FileName = command ?? "SomeCommand.exe",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = workingDirectory ?? "./Any/Working/Directory"
            };
        }
    }
}
