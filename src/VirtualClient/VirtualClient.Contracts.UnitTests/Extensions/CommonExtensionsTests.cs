// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensions
{
    using System.Collections.Generic;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    internal class CommonExtensionsTests
    {
        [Test]
        public void AddOrReplaceSectionContentExtensionAppendsNewContentToExistingContentWhenThereAreNoContentSectionsExistingThatAreMarked()
        {
            List<string> originalContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
            };

            List<string> newContent = new List<string>
            {
                "1 a host  all  all  0.0.0.0/0  md5"
            };

            List<string> expectedContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
                "# Virtual Client Section Begin",
                "1 a host  all  all  0.0.0.0/0  md5",
                "# Virtual Client Section End"
            };

            IEnumerable<string> updatedContent = originalContent.AddOrReplaceSectionContentAsync(newContent, "# Virtual Client Section Begin", "# Virtual Client Section End");

            Assert.IsNotNull(updatedContent);
            CollectionAssert.AreEqual(expectedContent, updatedContent);
        }

        [Test]
        public void AddOrReplaceSectionContentExtensionReplacesExistingContentWhenThereAreContentSectionsExistingThatAreMarked_1()
        {
            List<string> originalContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
                "# Virtual Client Section Begin",
                "1 a host  all  all  0.0.0.0/0  md5",
                "# Virtual Client Section End"
            };

            List<string> newContent = new List<string>
            {
                "2 a host  all  all  0.0.0.0/0  md5"
            };

            List<string> expectedContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
                "# Virtual Client Section Begin",
                "2 a host  all  all  0.0.0.0/0  md5",
                "# Virtual Client Section End"
            };

            IEnumerable<string> updatedContent = originalContent.AddOrReplaceSectionContentAsync(newContent, "# Virtual Client Section Begin", "# Virtual Client Section End");

            Assert.IsNotNull(updatedContent);
            CollectionAssert.AreEqual(expectedContent, updatedContent);
        }

        [Test]
        public void AddOrReplaceSectionContentExtensionReplacesExistingContentWhenThereAreContentSectionsExistingThatAreMarked_2()
        {
            List<string> originalContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# Virtual Client Section Begin",
                "1 a host  all  all  0.0.0.0/0  md5",
                "# Virtual Client Section End",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
            };

            List<string> newContent = new List<string>
            {
                "2 a host  all  all  0.0.0.0/0  md5"
            };

            List<string> expectedContent = new List<string>
            {
                "# DO NOT DISABLE!",
                "# If you change this first entry you will need to make sure that the",
                "# database superuser can access the database using some other method.",
                "# Noninteractive access to all databases is required during automatic",
                "# maintenance (custom daily cronjobs, replication, and similar tasks).",
                "#",
                "# Database administrative login by Unix domain socket",
                "local   all             postgres                                peer",
                "",
                "# TYPE  DATABASE        USER            ADDRESS                 METHOD",
                "",
                "# \"local\" is for Unix domain socket connections only",
                "local   all             all                                     peer",
                "# IPv4 local connections:",
                "host    all             all             127.0.0.1/32            md5",
                "# IPv6 local connections:",
                "host    all             all             ::1/128                 md5",
                "# Allow replication connections from localhost, by a user with the",
                "# replication privilege.",
                "local   replication     all                                     peer",
                "host    replication     all             127.0.0.1/32            md5",
                "host    replication     all             ::1/128                 md5",
                "# Virtual Client Section Begin",
                "2 a host  all  all  0.0.0.0/0  md5",
                "# Virtual Client Section End"
            };

            IEnumerable<string> updatedContent = originalContent.AddOrReplaceSectionContentAsync(newContent, "# Virtual Client Section Begin", "# Virtual Client Section End");

            Assert.IsNotNull(updatedContent);
            CollectionAssert.AreEqual(expectedContent, updatedContent);
        }
    }
}
