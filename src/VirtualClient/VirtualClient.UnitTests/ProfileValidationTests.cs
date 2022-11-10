// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Validation;

    [TestFixture]
    [Category("Unit")]
    public class ProfileValidationTests
    {
        private ExecutionProfileValidation validator = ExecutionProfileValidation.Instance;

        public void OneTimeSetup()
        { 
            // All profiles should be valid by default.
            this.validator.AddRange(new List<IValidationRule<ExecutionProfile>>()
            {
                SchemaRules.Instance
            });
        }

        [Test]
        [TestCaseSource(nameof(GetWorkloadProfileTestSource))]
        public void AllWorkloadProfilesMeetTheJsonSchemaRequirements(string profileName)
        {
            try
            {
                string profileString = File.ReadAllText(profileName);
                ExecutionProfile profileObject = JsonConvert.DeserializeObject<ExecutionProfile>(profileString);
                ValidationResult result = this.validator.Validate(profileObject);

                if (profileName.EndsWith("MONITORS-NONE.json", System.StringComparison.OrdinalIgnoreCase))
                {
                    Assert.IsTrue(!profileObject.Actions.Any() && !profileObject.Monitors.Any() && !profileObject.Dependencies.Any());
                    Assert.IsTrue(result.IsValid, $"The profile: \'{profileName}\' failed validation. With errors: \'{string.Join(", ", result.ValidationErrors)}\'.");
                }
                else
                {
                    Assert.IsTrue(profileObject.Actions.Any() || profileObject.Monitors.Any() || profileObject.Dependencies.Any());
                    Assert.IsTrue(result.IsValid, $"The profile: \'{profileName}\' failed validation. With errors: \'{string.Join(", ", result.ValidationErrors)}\'.");
                }
            }
            catch
            {
                Assert.Fail($"Profile '{profileName}' does not meet the schema requirements.");
            }
        }

        private static IEnumerable<string> GetWorkloadProfileTestSource()
        {
            DirectoryInfo currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
            bool repoRootFound = false;
            while (currentDirectory != null)
            {
                if (currentDirectory.GetDirectories(".git")?.Any() == true)
                {
                    repoRootFound = true;
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            if (!repoRootFound)
            {
                throw new FileNotFoundException("Could not locate profiles.");
            }

            string pathToProfiles = Path.Combine(currentDirectory.FullName, @"src\VirtualClient\VirtualClient.Main\profiles");
            IEnumerable<string> files = Directory.GetFiles(pathToProfiles, "*.json");

            foreach (string file in Directory.GetFiles(pathToProfiles, "*.json"))
            {
                yield return file;
            }
        }
    }
}
