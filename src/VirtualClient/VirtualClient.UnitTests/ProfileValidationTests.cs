// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
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

        [Test]
        public async Task WorkloadProfileDoNotHaveInlineParameterReferencingMistakes()
        {
            List<string> badApples = new List<string>();

            IEnumerable<string> profiles = GetWorkloadProfileTestSource();
            if (profiles?.Any() == true)
            {
                foreach (string profilePath in profiles)
                {
                    try
                    {
                        string profileString = File.ReadAllText(profilePath);
                        ExecutionProfile profileObject = JsonConvert.DeserializeObject<ExecutionProfile>(profileString);
                        if (profileObject.Parameters.Any())
                        {
                            profileObject.Inline();

                            MockFixture fixture = new MockFixture();
                            fixture.Setup(System.PlatformID.Win32NT);
                            using (TestExecutor executor = new TestExecutor(fixture.Dependencies, profileObject.Parameters))
                            {
                                if (executor.Parameters?.Any() == true)
                                {
                                    await executor.EvaluateParametersAsync(CancellationToken.None);
                                    Assert.IsFalse(executor.Parameters
                                        .Any(p => !string.IsNullOrWhiteSpace(p.Value?.ToString()) && Regex.IsMatch(p.Value?.ToString(), "{[^{}]+}", RegexOptions.IgnoreCase)));
                                }
                            }
                        }
                    }
                    catch
                    {
                        badApples.Add(Path.GetFileName(profilePath));
                    }
                }
            }

            if (badApples.Any())
            {
                Assert.Fail(
                    $"The following profiles have parameter reference inlining issues:{Environment.NewLine}" +
                    $"{string.Join(Environment.NewLine, badApples.Select(a => $"- {a}"))}");
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

            string pathToProfiles = Path.Combine(currentDirectory.FullName, "src", "VirtualClient", "VirtualClient.Main", "profiles");
            IEnumerable<string> files = Directory.GetFiles(pathToProfiles, "*.json");

            foreach (string file in Directory.GetFiles(pathToProfiles, "*.json"))
            {
                yield return file;
            }
        }
    }
}
