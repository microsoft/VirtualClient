namespace VirtualClient.Actions
{
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;

    using NUnit.Framework;

    using VirtualClient.Actions.Wrathmark;

    [TestFixture]
    [Category("Unit")]
    public class WrathmarkTests
    {
        protected static readonly string ProfilesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(WrathmarkResultParserTests)).Location),
            "Examples",
            "Wrathmark");

        protected static string GetExampleFileForTests(string fileName)
        {
            string retVal = Path.Combine(ProfilesDirectory, fileName);

            Debug.Assert(File.Exists(retVal), $"The specified file '{fileName}' does not exist.");

            return retVal;
        }

        protected WrathmarkWorkloadExecutor WorkloadExecutorFactory(MockFixture fixture)
        {
            return new WrathmarkWorkloadExecutor(fixture.Dependencies, fixture.Parameters);
        }
    }
}