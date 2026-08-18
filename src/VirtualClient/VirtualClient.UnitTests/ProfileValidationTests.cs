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
        [TestCaseSource(nameof(GetWorkloadProfileTestSource))]
        public void AllWorkloadProfilesDefineTheRequiredBaseMetadata(string profileName)
        {
            string profileString = File.ReadAllText(profileName);
            ExecutionProfile profileObject = JsonConvert.DeserializeObject<ExecutionProfile>(profileString);

            Assert.IsNotNull(
                profileObject.Metadata,
                $"The profile '{Path.GetFileName(profileName)}' does not define a 'Metadata' section.");

            IEnumerable<string> missing = ProfileMetadata.BaseProperties
                .Where(property => !profileObject.Metadata.TryGetValue(property, out IConvertible value)
                    || string.IsNullOrWhiteSpace(value?.ToString()))
                .ToList();

            Assert.IsEmpty(
                missing,
                $"The profile '{Path.GetFileName(profileName)}' is missing required base metadata: " +
                $"{string.Join(", ", missing)}.");
        }

        [Test]
        [TestCaseSource(nameof(GetWorkloadProfileTestSource))]
        public void AllWorkloadProfilesDefineSupportedOperatingSystemsUsingTheCanonicalNames(string profileName)
        {
            string profileString = File.ReadAllText(profileName);
            ExecutionProfile profileObject = JsonConvert.DeserializeObject<ExecutionProfile>(profileString);

            // Presence of the property is enforced by AllWorkloadProfilesDefineTheRequiredBaseMetadata.
            // Bail out here so a missing value surfaces as that assertion rather than a NullReferenceException.
            if (profileObject.Metadata?.TryGetValue(ProfileMetadata.SupportedOperatingSystems, out IConvertible operatingSystems) != true
                || string.IsNullOrWhiteSpace(operatingSystems?.ToString()))
            {
                return;
            }

            IEnumerable<string> invalid = operatingSystems.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(name => !string.Equals(name, "Windows", StringComparison.Ordinal)
                    && !(Enum.TryParse(name, ignoreCase: false, out LinuxDistribution distribution)
                        && distribution != LinuxDistribution.Unknown
                        && !int.TryParse(name, out int _)));

            Assert.IsEmpty(
                invalid,
                $"The profile '{Path.GetFileName(profileName)}' defines operating systems that do not match the " +
                $"'{nameof(LinuxDistribution)}' names (or 'Windows'): {string.Join(", ", invalid)}.");
        }

        [Test]
        [TestCaseSource(nameof(GetWorkloadProfileTestSource))]
        public void AllWorkloadProfilesDefineAValidRecommendedMinimumExecutionTime(string profileName)
        {
            string profileString = File.ReadAllText(profileName);
            ExecutionProfile profileObject = JsonConvert.DeserializeObject<ExecutionProfile>(profileString);

            // The property is optional. It does not apply to profiles whose runtime is determined externally.
            if (profileObject.Metadata?.TryGetValue(ProfileMetadata.RecommendedMinimumExecutionTime, out IConvertible executionTime) != true)
            {
                return;
            }

            string value = executionTime?.ToString();

            // Either a single timespan, or timespans scaled by core count (e.g. "(4-cores)=02:00:00").
            IEnumerable<string> timespans = Regex.IsMatch(value ?? string.Empty, @"^\(\d+-cores\)=")
                ? Regex.Matches(value, @"\(\d+-cores\)=([^,]+)").Select(match => match.Groups[1].Value)
                : new List<string> { value };

            foreach (string timespan in timespans)
            {
                Assert.IsTrue(
                    TimeSpan.TryParse(timespan?.Trim(), out TimeSpan parsed) && parsed > TimeSpan.Zero,
                    $"The profile '{Path.GetFileName(profileName)}' defines a " +
                    $"'{ProfileMetadata.RecommendedMinimumExecutionTime}' value containing an invalid timespan: " +
                    $"'{timespan}' (in '{value}').");
            }
        }

        [Test]
        [Ignore("This test can be used manually when needed to validate that workload profiles do not have parameter reference inlining mistakes.")]
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
