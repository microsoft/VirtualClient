namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using VirtualClient.Common;

    /// <summary>
    /// Represents a fake ssh command.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
    public class InMemorySshCommand : Dictionary<string, IConvertible>, ISshCommandProxy
    {
        private LogResults logResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySshCommand"/>
        /// </summary>
        public InMemorySshCommand()
        {
            this.logResults = new LogResults();
        }

        public int ExitStatus { get; set; }

        public string Result { get; set; }

        public string Error { get; set; }

        public string CommandText { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LogResults LogResults
        {
            get
            {
                this.logResults.CommandLine = SensitiveData.ObscureSecrets($"{this.CommandText}".Trim());
                this.logResults.ExitCode = this.ExitStatus;
                this.logResults.StandardError = this.Error;
                this.logResults.StandardOutput = this.Result;

                return this.logResults;
            }
        }

        /// <summary>
        /// Delegate allows user/test to define the logic to execute when the 
        /// 'Dispose' method is called.
        /// </summary>
        public Action OnDispose { get; set; }

        public Func<string> OnExecute { get; set; }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This is a test/mock class with no real resources.")]
        public void Dispose()
        {
            this.OnDispose?.Invoke();
        }

        public string Execute()
        {
            string mockOutput = string.Empty;
            if (this.OnExecute != null)
            {
                mockOutput = this.OnExecute?.Invoke();
            }

            return mockOutput;
        }
    }
}
