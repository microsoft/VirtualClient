// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientComponentExtensionsTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public void ApplyParameterExtensionReplacesPlaceholderReferencesWithMatchingParameterValues_1()
        {
            // Placeholders like: {Parameter1}
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_{Parameter1}_ending{Parameter1}_{Parameter1}beginning_in{Parameter1}between";
                IConvertible parameterValue = "anyvalue";

                string inlinedText = component.ApplyParameter(text, "Parameter1", parameterValue);

                Assert.AreEqual(
                    "any_text_anyvalue_endinganyvalue_anyvaluebeginning_inanyvaluebetween",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParameterExtensionReplacesPlaceholderReferencesWithMatchingParameterValues_2()
        {
            // Placeholders like: [Parameter1]
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[Parameter1]_ending[Parameter1]_[Parameter1]beginning_in[Parameter1]between";
                IConvertible parameterValue = "anyvalue";

                string inlinedText = component.ApplyParameter(text, "Parameter1", parameterValue);

                Assert.AreEqual(
                    "any_text_anyvalue_endinganyvalue_anyvaluebeginning_inanyvaluebetween",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParameterExtensionDoesNotChangeNonMatchingParameterReferences()
        {
            // Multiple placeholders
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[Parameter1]_ending{Parameter2}_[Parameter3]beginning_in[Parameter4]between";
                IConvertible parameterValue = "anyvalue";

                string inlinedText = component.ApplyParameter(text, "Parameter1", parameterValue);

                Assert.AreEqual(
                    "any_text_anyvalue_ending{Parameter2}_[Parameter3]beginning_in[Parameter4]between",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParameterExtensionSupportsMixedFormatParameterReferences()
        {
            // Mixed Placeholders: {Parameter1} and [Parameter1]
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[Parameter1]_ending{Parameter1}_[Parameter1]beginning_in{Parameter1}between";
                IConvertible parameterValue = "anyvalue";

                string inlinedText = component.ApplyParameter(text, "Parameter1", parameterValue);

                Assert.AreEqual(
                    "any_text_anyvalue_endinganyvalue_anyvaluebeginning_inanyvaluebetween",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParameterExtensionIsNotCaseSensitive()
        {
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[parameter1]_{PARAMETER1}_{paRaMeter1}";
                IConvertible parameterValue = "anyvalue";

                string inlinedText = component.ApplyParameter(text, "Parameter1", parameterValue);

                Assert.AreEqual("any_text_anyvalue_anyvalue_anyvalue", inlinedText);
            }
        }

        [Test]
        public void ApplyParametersExtensionReplacesPlaceholderReferencesWithMatchingParameterValues_1()
        {
            // Placeholders like: {Parameter1}
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_{Parameter1}_ending{Parameter2}_{Parameter3}beginning_in{Parameter4}between";

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234,
                    ["Parameter3"] = true,
                    ["Parameter4"] = 23.1
                };

                string inlinedText = component.ApplyParameters(text, parameters).ToLowerInvariant();

                Assert.AreEqual(
                    "any_text_value1_ending1234_truebeginning_in23.1between",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParametersExtensionReplacesPlaceholderReferencesWithMatchingParameterValues_2()
        {
            // Placeholders like: [Parameter1]
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[Parameter1]_ending[Parameter2]_[Parameter3]beginning_in[Parameter4]between";

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234,
                    ["Parameter3"] = true,
                    ["Parameter4"] = 23.1
                };

                string inlinedText = component.ApplyParameters(text, parameters).ToLowerInvariant();

                Assert.AreEqual(
                    "any_text_value1_ending1234_truebeginning_in23.1between",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParametersExtensionReplacesPlaceholderReferencesWithMatchingParameterValues_3()
        {
            // Single parameter referenced in multiple places.
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_{Parameter1}_ending{Parameter2}_{Parameter1}beginning_in{Parameter2}between";

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234
                };

                string inlinedText = component.ApplyParameters(text, parameters).ToLowerInvariant();

                Assert.AreEqual(
                    "any_text_value1_ending1234_value1beginning_in1234between",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParametersExtensionSupportsMixedFormatParameterReferences()
        {
            // Mixed Placeholders: {Parameter1} and [Parameter2]
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[Parameter1]_ending{Parameter2}_[Parameter3]beginning_in{Parameter4}between";

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234,
                    ["Parameter3"] = true,
                    ["Parameter4"] = 23.1
                };

                string inlinedText = component.ApplyParameters(text, parameters).ToLowerInvariant();

                Assert.AreEqual(
                    "any_text_value1_ending1234_truebeginning_in23.1between",
                    inlinedText);
            }
        }

        [Test]
        public void ApplyParametersExtensionIsNotCaseSensitive()
        {
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                string text = "any_text_[parameter1]_ending{parameter2}_[PARAMETER3]beginning_in{ParaMeteR4}between";

                IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Parameter1"] = "Value1",
                    ["Parameter2"] = 1234,
                    ["Parameter3"] = true,
                    ["Parameter4"] = 23.1
                };

                string inlinedText = component.ApplyParameters(text, parameters).ToLowerInvariant();

                Assert.AreEqual(
                    "any_text_value1_ending1234_truebeginning_in23.1between",
                    inlinedText);
            }
        }

        [Test]
        public void CombineExtensionProducesTheExpectedPathOnWindowsSystems()
        {
            this.fixture.Setup(PlatformID.Win32NT);
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(@"C:\any\path\with\sub\directories", component.Combine(@"C:\any\path", "with", "sub", "directories"));
            }
        }

        [Test]
        public void CombineExtensionProducesTheExpectedPathOnUnixSystems()
        {
            this.fixture.Setup(PlatformID.Unix);
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual("/any/path/with/sub/directories", component.Combine("/any/path", "with", "sub", "directories"));
            }
        }

        [Test]
        public void CreateFileUploadDescriptorsExtensionCreatesTheExpectedDescriptorsOnUnixSystems_1()
        {
            this.fixture.Setup(PlatformID.Unix);

            string directory = "/home/user/Logs";
            string[] expectedFiles = new string[]
            {
                $"{directory}/log1.txt",
                $"{directory}/log2.txt"
            };

            this.fixture.FileSystem
                .Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(file => directory);

            this.fixture.FileSystem
                .Setup(fs => fs.Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Returns(expectedFiles);

            using (var component = new TestVirtualClientComponent(this.fixture))
            {
                IEnumerable<FileUploadDescriptor> descriptors = component.CreateFileUploadDescriptors(directory, timestamped: false);

                Assert.IsNotNull(descriptors);
                Assert.IsTrue(descriptors.Count() == 2);

                FileUploadDescriptor descriptor1 = descriptors.ElementAt(0);
                Assert.AreEqual(expectedFiles[0], descriptor1.FilePath);
                Assert.IsTrue(descriptor1.BlobName.EndsWith("log1.txt"));
                Assert.IsTrue(descriptor1.BlobPath.EndsWith("/log1.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor1.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor1.ContentEncoding);
                Assert.IsNotNull(descriptor1.ContentType);

                FileUploadDescriptor descriptor2 = descriptors.ElementAt(1);
                Assert.AreEqual(expectedFiles[1], descriptor2.FilePath);
                Assert.IsTrue(descriptor2.BlobName.EndsWith("log2.txt"));
                Assert.IsTrue(descriptor2.BlobPath.EndsWith("/log2.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor2.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor2.ContentEncoding);
                Assert.IsNotNull(descriptor2.ContentType);
            }
        }

        [Test]
        public void CreateFileUploadDescriptorsExtensionCreatesTheExpectedDescriptorsOnUnixSystems_2()
        {
            this.fixture.Setup(PlatformID.Unix);

            string directory = "/home/user/Logs";
            string[] expectedFiles = new string[]
            {
                $"{directory}/log1.txt",
                $"{directory}/directory2/log2.txt",
                $"{directory}/directory2/directory3/log3.txt"
            };

            this.fixture.FileSystem
                .Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(file => file.Replace(Path.GetFileName(file), string.Empty));

            this.fixture.FileSystem
                .Setup(fs => fs.Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Returns(expectedFiles);

            using (var component = new TestVirtualClientComponent(this.fixture))
            {
                IEnumerable<FileUploadDescriptor> descriptors = component.CreateFileUploadDescriptors(directory, timestamped: false);

                Assert.IsNotNull(descriptors);
                Assert.IsTrue(descriptors.Count() == 3);

                FileUploadDescriptor descriptor1 = descriptors.ElementAt(0);
                Assert.AreEqual(expectedFiles[0], descriptor1.FilePath);
                Assert.IsTrue(descriptor1.BlobName.EndsWith("log1.txt"));
                Assert.IsTrue(descriptor1.BlobPath.EndsWith("/log1.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor1.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor1.ContentEncoding);
                Assert.IsNotNull(descriptor1.ContentType);

                FileUploadDescriptor descriptor2 = descriptors.ElementAt(1);
                Assert.AreEqual(expectedFiles[1], descriptor2.FilePath);
                Assert.IsTrue(descriptor2.BlobName.EndsWith("log2.txt"));
                Assert.IsTrue(descriptor2.BlobPath.EndsWith("/directory2/log2.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor2.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor2.ContentEncoding);
                Assert.IsNotNull(descriptor2.ContentType);

                FileUploadDescriptor descriptor3 = descriptors.ElementAt(2);
                Assert.AreEqual(expectedFiles[2], descriptor3.FilePath);
                Assert.IsTrue(descriptor3.BlobName.EndsWith("log3.txt"));
                Assert.IsTrue(descriptor3.BlobPath.EndsWith("/directory2/directory3/log3.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor3.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor3.ContentEncoding);
                Assert.IsNotNull(descriptor3.ContentType);
            }
        }

        [Test]
        public void CreateFileUploadDescriptorsExtensionCreatesTheExpectedDescriptorsOnWindowsSystems_1()
        {
            string directory = "C:\\Users\\User\\Logs";
            string[] expectedFiles = new string[]
            {
                $"{directory}\\log1.txt",
                $"{directory}\\log2.txt"
            };

            this.fixture.FileSystem
                .Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(file => directory);

            this.fixture.FileSystem
                .Setup(fs => fs.Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Returns(expectedFiles);

            using (var component = new TestVirtualClientComponent(this.fixture))
            {
                IEnumerable<FileUploadDescriptor> descriptors = component.CreateFileUploadDescriptors(directory, timestamped: false);

                Assert.IsNotNull(descriptors);
                Assert.IsTrue(descriptors.Count() == 2);

                FileUploadDescriptor descriptor1 = descriptors.ElementAt(0);
                Assert.AreEqual(expectedFiles[0], descriptor1.FilePath);
                Assert.IsTrue(descriptor1.BlobName.EndsWith("log1.txt"));
                Assert.IsTrue(descriptor1.BlobPath.EndsWith("/log1.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor1.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor1.ContentEncoding);
                Assert.IsNotNull(descriptor1.ContentType);

                FileUploadDescriptor descriptor2 = descriptors.ElementAt(1);
                Assert.AreEqual(expectedFiles[1], descriptor2.FilePath);
                Assert.IsTrue(descriptor2.BlobName.EndsWith("log2.txt"));
                Assert.IsTrue(descriptor2.BlobPath.EndsWith("/log2.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor2.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor2.ContentEncoding);
                Assert.IsNotNull(descriptor2.ContentType);
            }
        }

        [Test]
        public void CreateFileUploadDescriptorsExtensionCreatesTheExpectedDescriptorsOnWindowsSystems_2()
        {
            string directory = "C:\\Users\\User\\Logs";
            string[] expectedFiles = new string[]
            {
                $"{directory}\\log1.txt",
                $"{directory}\\directory2\\log2.txt",
                $"{directory}\\directory2\\directory3\\log3.txt"
            };

            this.fixture.FileSystem
                .Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>()))
                .Returns<string>(file => Path.GetDirectoryName(file));

            this.fixture.FileSystem
                .Setup(fs => fs.Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
                .Returns(expectedFiles);

            using (var component = new TestVirtualClientComponent(this.fixture))
            {
                IEnumerable<FileUploadDescriptor> descriptors = component.CreateFileUploadDescriptors(directory, timestamped: false);

                Assert.IsNotNull(descriptors);
                Assert.IsTrue(descriptors.Count() == 3);

                FileUploadDescriptor descriptor1 = descriptors.ElementAt(0);
                Assert.AreEqual(expectedFiles[0], descriptor1.FilePath);
                Assert.IsTrue(descriptor1.BlobName.EndsWith("log1.txt"));
                Assert.IsTrue(descriptor1.BlobPath.EndsWith("/log1.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor1.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor1.ContentEncoding);
                Assert.IsNotNull(descriptor1.ContentType);

                FileUploadDescriptor descriptor2 = descriptors.ElementAt(1);
                Assert.AreEqual(expectedFiles[1], descriptor2.FilePath);
                Assert.IsTrue(descriptor2.BlobName.EndsWith("log2.txt"));
                Assert.IsTrue(descriptor2.BlobPath.EndsWith("/directory2/log2.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor2.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor2.ContentEncoding);
                Assert.IsNotNull(descriptor2.ContentType);

                FileUploadDescriptor descriptor3 = descriptors.ElementAt(2);
                Assert.AreEqual(expectedFiles[2], descriptor3.FilePath);
                Assert.IsTrue(descriptor3.BlobName.EndsWith("log3.txt"));
                Assert.IsTrue(descriptor3.BlobPath.EndsWith("/directory2/directory3/log3.txt"));
                Assert.AreEqual(component.ExperimentId, descriptor3.ContainerName);
                Assert.AreEqual(Encoding.UTF8.WebName, descriptor3.ContentEncoding);
                Assert.IsNotNull(descriptor3.ContentType);
            }
        }

        [Test]
        public void VerifyLayoutDefinedExtensionThrowsWhenVerifyingTheEnvironmentLayoutIfItDoesNotExist()
        {
            // The environment layout is not provided.
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();

            DependencyException error = Assert.Throws<DependencyException>(() => component.ThrowIfLayoutNotDefined());
            Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstanceExtensionThrowsWhenMatchingClientInstancesDoNotExistInTheEnvironmentLayout()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // Matching client instance does not exist by agent ID
            DependencyException error = Assert.Throws<DependencyException>(() => component.GetLayoutClientInstance("NonExistentAgentID"));
            Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstancesExtensionThrowsWhenMatchingClientInstancesDoNotExistInTheEnvironmentLayoutWithTheRoleSpecified()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // Matching client instance(s) not found by role
            DependencyException error = Assert.Throws<DependencyException>(() => component.GetLayoutClientInstances("NonExistentAgentRole"));
            Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstanceExtensionReturnsTheExpectedClientInstanceFromTheEnvironmentLayoutForAGivenAgentId()
        {
            ClientInstance expectedInstance = this.fixture.Layout.Clients.ElementAt(1);
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            ClientInstance actualInstance = component.GetLayoutClientInstance(expectedInstance.Name);
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstance));
        }

        [Test]
        public void GetLayoutClientInstanceExtensionReturnsTheExpectedClientInstanceFromTheEnvironmentLayoutWhenAnAgentIdIsNotExplicitlyProvided()
        {
            // The mock fixture will set one of the client instances to have the same
            // name/agent ID as the current machine name.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance", "1.2.3.5", "Server")
            });

            ClientInstance expectedInstance = this.fixture.Layout.Clients.First();
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // When the agent ID is not provided, the ID of the current instance of the Virtual Client will
            // be used. In test scenarios, this will be the current machine name.
            ClientInstance actualInstance = component.GetLayoutClientInstance();
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstance));
        }

        [Test]
        public void IsMultiRoleLayoutExtensionCorrectlyDeterminesWhenItIsInAMultiRoleScenarioBasedOnTheEnvironmentLayout()
        {
            // No layout provided, we have to assume we are not in a multi-role scenario.
            this.fixture.Layout = null;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided but there are no roles defined.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4"),
                new ClientInstance("AnyClient2", "1.2.3.5")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided but there is only 1 distinct role.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4", "Client"),
                new ClientInstance("AnyClient2", "1.2.3.5", "Client")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided and there are more than 1 distinct role. This is multi-role.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4", "Client"),
                new ClientInstance("AnyClient2", "1.2.3.5", "Server")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsTrue(component.IsMultiRoleLayout());
        }

        [Test]
        public void ThrowIfParameterNotDefinedDoesNotThrowWhenExpectedParametersExist()
        {
            this.fixture.Parameters["RequiredParameter"] = "Value";
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.DoesNotThrow(() => component.ThrowIfParameterNotDefined("RequiredParameter"));
        }

        [Test]
        public void ThrowIfParameterNotDefinedThrowsIfAnExpectedParameterIsNotDefined()
        {
            this.fixture.Parameters.Clear();
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.Throws<DependencyException>(() => component.ThrowIfParameterNotDefined("AnyRequiredParameter"));
        }

        [Test]
        [TestCase("Value1", "Value2")]
        [TestCase(1, 2)]
        public void ThrowIfParameterNotDefinedThrowsIfAParameterHasAnUnsupportedValue(IConvertible value, IConvertible supportedValues)
        {
            this.fixture.Parameters["RequiredParameter"] = value;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.Throws<DependencyException>(() => component.ThrowIfParameterNotDefined("RequiredParameter", supportedValues));
        }

        [Test]
        [TestCase(1, "1")]
        public void ThrowIfParameterNotDefinedHandlesConvertibleValuesForUnsupportedValue(IConvertible value, IConvertible supportedValues)
        {
            this.fixture.Parameters["RequiredParameter"] = value;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.DoesNotThrow(() => component.ThrowIfParameterNotDefined("RequiredParameter", supportedValues));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionThrowsIfTheRoleSuppliedIsNotSupportedByTheWorkload()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = new List<string> { "AnyRole" };
            Assert.Throws<NotSupportedException>(() => component.ThrowIfRoleNotSupported("AnyOtherRole"));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionDoesNotThrowsIfTheWorkloadHasNoSupportedRolesDefined()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = null;
            Assert.DoesNotThrow(() => component.ThrowIfRoleNotSupported("AnyRole"));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionDoesNotThrowsIfTheRoleIsSupportedByTheWorkload()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = new List<string> { "AnyRole" };
            Assert.DoesNotThrow(() => component.ThrowIfRoleNotSupported("AnyRole"));
        }

        [Test]
        public void ToPlatformSpecificPathExtensionProducesTheExpectedPathOnUnixSystems()
        {
            this.fixture.Setup(PlatformID.Unix);
            DependencyPath dependency = new DependencyPath("AnyDependency", "/any/path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(
                    $"{dependency.Path}/linux",
                    component.ToPlatformSpecificPath(dependency, PlatformID.Unix).Path);

                Assert.AreEqual(
                   $"{dependency.Path}/linux-x64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Unix, Architecture.X64).Path);

                Assert.AreEqual(
                   $"{dependency.Path}/linux-arm64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Unix, Architecture.Arm64).Path);
            }
        }

        [Test]
        public void ToPlatformSpecificPathExtensionProducesTheExpectedPathOnWindowsSystems()
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(
                    $@"{dependency.Path}\win",
                    component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT).Path);

                Assert.AreEqual(
                   $@"{dependency.Path}\win-x64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT, Architecture.X64).Path);

                Assert.AreEqual(
                   $@"{dependency.Path}\win-arm64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT, Architecture.Arm64).Path);
            }
        }

        [Test]
        [TestCase(PlatformID.Other)]
        public void ToPlatformSpecificPathThrowsIfThePlatformDefinedIsNotSupported(PlatformID unsupportedPlatform)
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.Throws<NotSupportedException>(() => component.ToPlatformSpecificPath(dependency, unsupportedPlatform));
            }
        }

        [Test]
        [TestCase(Architecture.Arm)]
        [TestCase(Architecture.Wasm)]
        [TestCase(Architecture.X86)]
        public void ToPlatformSpecificPathThrowsIfTheArchitectureDefinedIsNotSupported(Architecture unsupportedArchitecture)
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.Throws<NotSupportedException>(
                    () => component.ToPlatformSpecificPath(dependency, PlatformID.Unix, unsupportedArchitecture));
            }
        }

        private class TestVirtualClientComponent : VirtualClientComponent
        {
            public TestVirtualClientComponent(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new IEnumerable<string> SupportedRoles
            {
                get
                {
                    return base.SupportedRoles;
                }

                set
                {
                    base.SupportedRoles = value;
                }
            }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
