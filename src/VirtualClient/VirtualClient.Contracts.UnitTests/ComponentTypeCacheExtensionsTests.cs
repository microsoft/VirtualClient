// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class ComponentTypeCacheExtensionsTests
    {
        [Test]
        [Order(0)]
        public void GetFileUploadDescriptorFactoryExtensionInMemoryCachingWorksAsExpected()
        {
            lock (ComponentTypeCache.LockObject)
            {
                lock (ComponentTypeCache.Instance.DescriptorFactoryCache)
                {
                    try
                    {
                        // Clear the cache to ensure we do not inadvertently load or reference
                        // the default descriptor factory.

                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_1)));

                        IFileUploadDescriptorFactory descriptorFactory1 = ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_1));
                        IFileUploadDescriptorFactory descriptorFactory2 = ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_1));

                        Assert.IsNotNull(descriptorFactory1);
                        Assert.IsNotNull(descriptorFactory2);
                        Assert.IsTrue(object.ReferenceEquals(descriptorFactory1, descriptorFactory2));
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [Test]
        [Order(1)]
        public void GetFileUploadDescriptorFactoryExtensionReturnsTheExpectedDefaultFactory()
        {
            lock (ComponentTypeCache.LockObject)
            {
                // Clear the cache to ensure we do not inadvertently load or reference
                // the default descriptor factory.
                ComponentTypeCache.Instance.Clear();
                IFileUploadDescriptorFactory descriptorFactory = ComponentTypeCache.Instance.GetFileUploadDescriptorFactory();

                Assert.IsNotNull(descriptorFactory);
                Assert.IsInstanceOf<FileUploadDescriptorFactory>(descriptorFactory);
            }
        }

        [Test]
        [Order(2)]
        public void GetFileUploadDescriptorFactoryExtensionReturnsTheExpectedFactoryWhenAnIdentifierIsProvided()
        {
            lock (ComponentTypeCache.LockObject)
            {
                lock (ComponentTypeCache.Instance.DescriptorFactoryCache)
                {
                    try
                    {
                        // Clear the cache to ensure we do not inadvertently load or reference
                        // the default descriptor factory.
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_1)));
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_2)));

                        IFileUploadDescriptorFactory descriptorFactory = ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_1));

                        Assert.IsNotNull(descriptorFactory);
                        Assert.IsInstanceOf<TestFileUploadDescriptorFactory_1>(descriptorFactory);

                        descriptorFactory = ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_2));

                        Assert.IsNotNull(descriptorFactory);
                        Assert.IsInstanceOf<TestFileUploadDescriptorFactory_2>(descriptorFactory);
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [Test]
        [Order(3)]
        public void GetFileUploadDescriptorFactoryExtensionThrowsWhenAMatchingFactoryTypeDoesNotExist()
        {
            lock (ComponentTypeCache.LockObject)
            {
                lock (ComponentTypeCache.Instance.DescriptorFactoryCache)
                {
                    try
                    {
                        // Clear the cache to ensure we do not inadvertently load or reference
                        // the default descriptor factory.
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                        Assert.Throws<DependencyException>(() => ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_1)));
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [Test]
        [Order(4)]
        public void GetFileUploadDescriptorFactoryExtensionThrowsWhenFactoryTypesExistThatHaveDuplicateIdentifiers()
        {
            lock (ComponentTypeCache.LockObject)
            {
                lock (ComponentTypeCache.Instance.DescriptorFactoryCache)
                {
                    try
                    {
                        // Clear the cache to ensure we do not inadvertently load or reference
                        // the default descriptor factory.
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_1)));
                        ComponentTypeCache.Instance.Add(new ComponentType(typeof(TestFileUploadDescriptorFactory_1)));

                        Assert.Throws<DependencyException>(() => ComponentTypeCache.Instance.GetFileUploadDescriptorFactory(nameof(TestFileUploadDescriptorFactory_1)));
                    }
                    finally
                    {
                        ComponentTypeCache.Instance.Clear();
                        ComponentTypeCache.Instance.DescriptorFactoryCache.Clear();
                    }
                }
            }
        }

        [ComponentDescription(Id = nameof(TestFileUploadDescriptorFactory_1))]
        private class TestFileUploadDescriptorFactory_1 : IFileUploadDescriptorFactory
        {
            public FileUploadDescriptor CreateDescriptor(VirtualClientComponent component, IFileInfo file, string contentType, string contentEncoding, string toolname = null, DateTime? fileTimestamp = null, IDictionary<string, IConvertible> manifest = null)
            {
                return null;
            }

            public FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentStorePathTemplate, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true)
            {
                throw new NotImplementedException();
            }
        }

        [ComponentDescription(Id = nameof(TestFileUploadDescriptorFactory_2))]
        private class TestFileUploadDescriptorFactory_2 : IFileUploadDescriptorFactory
        {
            public FileUploadDescriptor CreateDescriptor(VirtualClientComponent component, IFileInfo file, string contentType, string contentEncoding, string toolname = null, DateTime? fileTimestamp = null, IDictionary<string, IConvertible> manifest = null)
            {
                return null;
            }

            public FileUploadDescriptor CreateDescriptor(FileContext fileContext, string contentStorePathTemplate, IDictionary<string, IConvertible> parameters = null, IDictionary<string, IConvertible> manifest = null, bool timestamped = true)
            {
                throw new NotImplementedException();
            }
        }
    }
}
