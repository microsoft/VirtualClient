// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using NUnit.Framework;
    using Match = System.Text.RegularExpressions.Match;

    [TestFixture]
    [Category("Unit")]
    public class RepoConsistencyTests
    {
        private const string DirectoryPackagesFileName = "Directory.Packages.props";
        private static readonly Assembly TestAssembly = Assembly.GetAssembly(typeof(RepoConsistencyTests));

        [Test]
        [Platform(Include = "Win")]
        public void ValidateThatTheRepoHasNoUnusedPackageReferences()
        {
            if (TryFindRepoRootDirectory(out DirectoryInfo repoRootDirectory))
            {
                string directoryPackagesFile = Path.Combine(repoRootDirectory.FullName, "Directory.Packages.props");
                if (File.Exists(directoryPackagesFile))
                {
                    XmlDocument packageVersionDocument = new XmlDocument();
                    packageVersionDocument.Load(directoryPackagesFile);
                    XmlNodeList packageVersions = packageVersionDocument.SelectNodes("/Project/ItemGroup/PackageVersion");

                    if (packageVersions != null)
                    {
                        HashSet<string> repoReferencedPackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        HashSet<string> projectPackageReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (XmlNode packageVersionElement in packageVersions)
                        {
                            repoReferencedPackageNames.Add(packageVersionElement.Attributes["Include"].Value.Trim());
                        }

                        IEnumerable<string> csprojFiles = GetProjectFiles(repoRootDirectory);
                        IEnumerable<string> propsFiles = GetPropsFiles(repoRootDirectory);
                        IEnumerable<string> targetsFiles = GetTargetsFiles(repoRootDirectory);

                        Regex packageReferenceExpression = new Regex(@"PackageReference\s+Include=""([\x21-\x7E]+)""", RegexOptions.IgnoreCase);
                        foreach (string projectFile in csprojFiles.Union(propsFiles).Union(targetsFiles))
                        {
                            string projectFileContent = File.ReadAllText(projectFile);
                            MatchCollection packageReferences = packageReferenceExpression.Matches(projectFileContent);

                            if (packageReferences?.Any() == true)
                            {
                                foreach (Match packageReferenceMatch in packageReferences)
                                {
                                    projectPackageReferences.Add(packageReferenceMatch.Groups[1].Value.Trim());
                                }
                            }
                        }

                        IEnumerable<string> unreferencedPackages = repoReferencedPackageNames.Except(projectPackageReferences);

                        if (unreferencedPackages?.Any() == true)
                        {
                            Assert.Fail(
                                $"Unreferenced package references found. The following package references are defined in the '{DirectoryPackagesFileName}' file " +
                                $"but are not referenced in any project, props or targets file within the repo. The <PackageVersion /> references can be removed:{Environment.NewLine}- " +
                                $"{string.Join($"{Environment.NewLine}- ", unreferencedPackages)}");
                        }
                    }
                }
            }
        }

        [Test]
        [Platform(Include = "Win")]
        public void ValidateThatTheRepoHasNoPackageReferencesIsolatedInProjects()
        {
            List<string> excludes = new List<string>
            {
                // The refernences below are used in integration with scripts that build the Juno Host Agent
                // pilotfish package. These references must be defined in a .props file specifically for this
                // build process.
                "DRI",
                "VirtualClient"
            };

            if (TryFindRepoRootDirectory(out DirectoryInfo repoRootDirectory))
            {
                string directoryPackagesFile = Path.Combine(repoRootDirectory.FullName, "Directory.Packages.props");
                if (File.Exists(directoryPackagesFile))
                {
                    XmlDocument packageVersionDocument = new XmlDocument();
                    packageVersionDocument.Load(directoryPackagesFile);
                    XmlNodeList packageVersions = packageVersionDocument.SelectNodes("/Project/ItemGroup/PackageVersion");

                    if (packageVersions != null)
                    {
                        HashSet<string> repoReferencedPackageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        HashSet<string> projectPackageReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        List<Tuple<string, string>> isolatedReferences = new List<Tuple<string, string>>();

                        foreach (XmlNode packageVersionElement in packageVersions)
                        {
                            repoReferencedPackageNames.Add(packageVersionElement.Attributes["Include"].Value.Trim());
                        }

                        IEnumerable<string> csprojFiles = GetProjectFiles(repoRootDirectory);
                        IEnumerable<string> propsFiles = GetPropsFiles(repoRootDirectory);
                        IEnumerable<string> targetsFiles = GetTargetsFiles(repoRootDirectory);

                        Regex packageReferenceExpression = new Regex(@"PackageReference\s+Include=""([\x21-\x7E]+)""", RegexOptions.IgnoreCase);
                        foreach (string projectFile in csprojFiles.Union(propsFiles).Union(targetsFiles))
                        {
                            string projectFileContent = File.ReadAllText(projectFile);
                            MatchCollection packageReferences = packageReferenceExpression.Matches(projectFileContent);

                            if (packageReferences?.Any() == true)
                            {
                                List<string> projectLevelReferences = new List<string>();
                                foreach (Match packageReferenceMatch in packageReferences)
                                {
                                    string packageName = packageReferenceMatch.Groups[1].Value.Trim();
                                    if (!repoReferencedPackageNames.Contains(packageName) && !excludes.Contains(packageName))
                                    {
                                        isolatedReferences.Add(new Tuple<string, string>(projectFile, packageName));
                                    }
                                }
                            }
                        }

                        if (isolatedReferences?.Any() == true)
                        {
                            StringBuilder projectSpecificReferences = new StringBuilder();
                            foreach (var projectReference in isolatedReferences.GroupBy(r => r.Item1))
                            {
                                projectSpecificReferences.AppendLine();
                                projectSpecificReferences.AppendLine(projectReference.Key);
                                foreach (var packageReference in projectReference)
                                {
                                    projectSpecificReferences.AppendLine($"- {packageReference.Item2}");
                                }
                            }

                            Assert.Fail(
                                $"Isolated package references found. The following package references are defined in specific project files " +
                                $"versus in the '{DirectoryPackagesFileName}' file. ALL package references should be defined in this single location " +
                                $" as <PackageVersion /> references for maintainability:{projectSpecificReferences}");
                        }
                    }
                }
            }
        }

        private static IEnumerable<string> GetProjectFiles(DirectoryInfo repoRootDirectory)
        {
            return Directory.GetFiles(Path.Combine(repoRootDirectory.FullName, "src"), "*.*proj", SearchOption.AllDirectories);
        }

        private static IEnumerable<string> GetPropsFiles(DirectoryInfo repoRootDirectory)
        {
            return Directory.GetFiles(Path.Combine(repoRootDirectory.FullName, "src"), "*.props", SearchOption.AllDirectories)
                ?.Where(file => !file.EndsWith(DirectoryPackagesFileName));
        }

        private static IEnumerable<string> GetTargetsFiles(DirectoryInfo repoRootDirectory)
        {
            return Directory.GetFiles(Path.Combine(repoRootDirectory.FullName, "src"), "*.targets", SearchOption.AllDirectories);
        }

        private static bool TryFindRepoRootDirectory(out DirectoryInfo repoRootDirectory)
        {
            repoRootDirectory = null;
            DirectoryInfo currentDirectory = new DirectoryInfo(RepoConsistencyTests.TestAssembly.Location);

            while (currentDirectory != null)
            {
                string gitDirectory = Path.Combine(currentDirectory.FullName, ".git");
                if (Directory.Exists(gitDirectory))
                {
                    repoRootDirectory = currentDirectory;
                    break;
                }

                currentDirectory = currentDirectory.Parent;
            }

            return repoRootDirectory != null;
        }
    }
}
