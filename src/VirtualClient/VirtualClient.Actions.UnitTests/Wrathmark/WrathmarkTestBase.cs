namespace VirtualClient.Actions
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using NUnit.Framework;

    using VirtualClient.Actions.Wrathmark;


    /// <summary>
    /// Base class for Wrathmark unit tests.
    /// </summary>
    [TestFixture]
    [Category("Unit")]
    public abstract class WrathmarkTestBase
    {
        protected static readonly string ProfilesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(WrathmarkResultParserTests)).Location),
            "Examples",
            "Wrathmark");

        /// <summary>
        /// Gets the example file path for tests.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The full file path.</returns>
        protected static string GetExampleFileForTests(string fileName)
        {
            string retVal = Path.Combine(ProfilesDirectory, fileName);

            Debug.Assert(File.Exists(retVal), $"The specified file '{fileName}' does not exist.");

            return retVal;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="WrathmarkWorkloadExecutor" /> class.
        /// </summary>
        /// <param name="fixture">The mock fixture.</param>
        /// <returns>An instance of the <see cref="WrathmarkWorkloadExecutor" /> class based on the <paramref name="fixture"/>.</returns>
        protected WrathmarkWorkloadExecutor WorkloadExecutorFactory(MockFixture fixture)
        {
            return new WrathmarkWorkloadExecutor(fixture.Dependencies, fixture.Parameters);
        }
    }
}