namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SensitiveDataTests
    {
        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario1()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario1();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario2()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario2();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario3()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario3();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario4()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario4();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario5()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario5();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario6()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario6();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario7()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario7();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccessTokens_Scenario8_FullObfuscation()
        {
            // The default obfuscation when a percentage is not defined is 100% (e.g. remove the secret
            // entirely).
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccessTokenPairScenario8();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccountKeys_Scenario1()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccountKeyPair();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccountKeys_Scenario2()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccountKeyPairForUnusualScenario();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesAccountKeys_Scenario3_FullObfuscation()
        {
            // The default obfuscation when a percentage is not defined is 100% (e.g. remove the secret
            // entirely).
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetAccountKeyPairScenario2();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSharedAccessKeys_Scenario1()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSharedAccessKeyPair();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSharedAccessKeys_Scenario2()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSharedAccessKeyPairForUnusualScenario();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSharedAccessKeys_Scenario3_FullObfuscation()
        {
            // The default obfuscation when a percentage is not defined is 100% (e.g. remove the secret
            // entirely).
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSharedAccessKeyPairForUnusualScenario2();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSasUriSignatures_Scenario1()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSasUriPairScenario1();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSasUriSignatures_Scenario2()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSasUriPairScenario2();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        [TestCase("Password=AnySecretHereae09g34YT112", "Password=...")]
        [TestCase("Password AnySecretHereae09g34YT112", "Password ...")]
        [TestCase("Password=AnySecretHereae09g34YT112,,,Property1=Value1", "Password=...,,,Property1=Value1")]
        [TestCase("Password=AnySecretHereae09g34YT112,,,Property1=Value1,,,Property2=1234", "Password=...,,,Property1=Value1,,,Property2=1234")]
        [TestCase("Property1=Value1,,,Password=AnySecretHereae09g34YT112,,,Property2=Value2", "Property1=Value1,,,Password=...,,,Property2=Value2")]
        [TestCase("Password=AnySecret,Herea,,e09g34Y;T112,,,Property1=Value1", "Password=...,,,Property1=Value1")]
        [TestCase("Property1=Value1,,,Password=AnySecret,Hereae,,09g34Y;T112,,,Property2=Value2", "Property1=Value1,,,Password=...,,,Property2=Value2")]
        [TestCase("Property1=Value1,,,Password=AnySecret,Hereae,,09g34Y;T112,,,Property2=Value2,,,Password=AnySecret,THereae,,09g34Y;T112", "Property1=Value1,,,Password=...,,,Property2=Value2,,,Password=...")]
        public void ObscureSecretsObfuscatesPasswordSignatures_Scenario1(string originalString, string expectedString)
        {
            string obscuredString = SensitiveData.ObscureSecrets(originalString);
            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        [TestCase("Pwd=AnySecretHereae09g34YT112", "Pwd=...")]
        [TestCase("Pwd AnySecretHereae09g34YT112", "Pwd ...")]
        [TestCase("Pwd=AnySecretHereae09g34YT112,,,Property1=Value1", "Pwd=...,,,Property1=Value1")]
        [TestCase("Pwd=AnySecretHereae09g34YT112,,,Property1=Value1,,,Property2=1234", "Pwd=...,,,Property1=Value1,,,Property2=1234")]
        [TestCase("Property1=Value1,,,Pwd=AnySecretHereae09g34YT112,,,Property2=Value2", "Property1=Value1,,,Pwd=...,,,Property2=Value2")]
        [TestCase("Pwd=AnySecret,Herea,,e09g34Y;T112,,,Property1=Value1", "Pwd=...,,,Property1=Value1")]
        [TestCase("Property1=Value1,,,Pwd=AnySecret,Hereae,,09g34Y;T112,,,Property2=Value2", "Property1=Value1,,,Pwd=...,,,Property2=Value2")]
        [TestCase("Property1=Value1,,,Pwd=AnySecret,Hereae,,09g34Y;T112,,,Property2=Value2,,,Pwd=AnySecret,THereae,,09g34Y;T112", "Property1=Value1,,,Pwd=...,,,Property2=Value2,,,Pwd=...")]
        public void ObscureSecretsObfuscatesPasswordSignatures_Scenario2(string originalString, string expectedString)
        {
            string obscuredString = SensitiveData.ObscureSecrets(originalString);
            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        [TestCase("PaSSwoRD=AnySecretHereae09g34YT112", "PaSSwoRD=...")]
        [TestCase("PaSSwoRD AnySecretHereae09g34YT112", "PaSSwoRD ...")]
        [TestCase("PwD=AnySecretHereae09g34YT112", "PwD=...")]
        [TestCase("PwD AnySecretHereae09g34YT112", "PwD ...")]
        public void ObscureSecretsIsNotCaseSensitiveForPasswordSignatures_Scenario1(string originalString, string expectedString)
        {
            string obscuredString = SensitiveData.ObscureSecrets(originalString);
            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        [TestCase("UserPassword=AnySecretHereae09g34YT112,,,Property1=Value1", "UserPassword=...,,,Property1=Value1")]
        [TestCase("ClientPassword=AnySecretHereae09g34YT112,,,Property1=Value1", "ClientPassword=...,,,Property1=Value1")]
        [TestCase("ServerPassword=AnySecretHereae09g34YT112,,,Property1=Value1", "ServerPassword=...,,,Property1=Value1")]
        [TestCase("UserPwd=AnySecretHereae09g34YT112,,,Property1=Value1", "UserPwd=...,,,Property1=Value1")]
        [TestCase("ClientPwd=AnySecretHereae09g34YT112,,,Property1=Value1", "ClientPwd=...,,,Property1=Value1")]
        [TestCase("ServerPwd=AnySecretHereae09g34YT112,,,Property1=Value1", "ServerPwd=...,,,Property1=Value1")]
        [TestCase("--mode 'unattended' --serverport '1234' --superpassword 'AnySecret,Hereae,,09g34Y;T112'", "--mode 'unattended' --serverport '1234' --superpassword ...")]
        [TestCase("--mode 'unattended' --serverport '1234' --superpwd 'AnySecret,Hereae,,09g34Y;T112'", "--mode 'unattended' --serverport '1234' --superpwd ...")]
        public void ObscureSecretsHandlesCasesWhereThePasswordTermIsASubstringThatIsPartOfAnotherWord(string originalString, string expectedString)
        {
            string obscuredString = SensitiveData.ObscureSecrets(originalString);
            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSecretsThatMatchAGivenRegularExpression_Scenario1()
        {
            string stringContainingSecrets = "Secret=AnySecretHereae09g34YT112;Property1=Value1;Property2=1234";
            string expectedString = "Secret=AnySecretHer...;Property1=Value1;Property2=1234";

            string obscuredString = SensitiveData.ObscureSecrets(
                new Regex("Secret=([a-z0-9]+)", RegexOptions.IgnoreCase),
                stringContainingSecrets,
                50);

            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        public void ObscureSecretsHandlesBugFoundInImplementation_Scenario1()
        {
            Tuple<string, string> dataContainingSecrets = SensitiveDataTests.GetSharedAccessKeyPairForBugScenario1();
            string obscuredString = SensitiveData.ObscureSecrets(dataContainingSecrets.Item1, 50);

            Assert.AreEqual(dataContainingSecrets.Item2, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSecretsThatMatchAGivenRegularExpression_Scenario2()
        {
            string stringContainingSecrets = "Property1=Value1;Property2=1234;Secret=AnySecretHereae09g34YT112";
            string expectedString = "Property1=Value1;Property2=1234;Secret=AnySecretHer...";

            string obscuredString = SensitiveData.ObscureSecrets(
                new Regex("Secret=([a-z0-9]+)", RegexOptions.IgnoreCase),
                stringContainingSecrets,
                50);

            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSecretsThatMatchAGivenRegularExpression_Scenario3()
        {
            string stringContainingSecrets = "Property1=Value1;Secret=AnySecretHereae09g34YT112;Property2=1234";
            string expectedString = "Property1=Value1;Secret=AnySecretHer...;Property2=1234";

            string obscuredString = SensitiveData.ObscureSecrets(
                new Regex("Secret=([a-z0-9]+)", RegexOptions.IgnoreCase),
                stringContainingSecrets,
                50);

            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        public void ObscureSecretsObfuscatesSecretsThatMatchAGivenRegularExpression_Scenario4()
        {
            // Multiple secrets with the same name in the string.
            string stringContainingSecrets = "Property1=Value1;Secret=AnySecretHereae09g34YT112;Property2=1234;Secret=AnyOtherSecretHereae33gS34wT1339";
            string expectedString = "Property1=Value1;Secret=AnySecretHer...;Property2=1234;Secret=AnyOtherSecretHe...";

            string obscuredString = SensitiveData.ObscureSecrets(
                new Regex("Secret=([a-z0-9]+)", RegexOptions.IgnoreCase),
                stringContainingSecrets,
                50);

            Assert.AreEqual(expectedString, obscuredString);
        }

        [Test]
        public void ObscureSecretsSupportsMultipleCaptureGroupsInRegularExpressionsProvided()
        {
            // Multiple different secrets in the string
            string stringContainingSecrets = "Secret=AnySecretHereae09g34YT112;SomeOtherSecret=AnyOtherSecret3eT98xs1FG";
            string expectedString = "Secret=AnySecretHer...;SomeOtherSecret=AnyOtherSecr...";

            string obscuredString = SensitiveData.ObscureSecrets(
                new Regex("Secret=([a-z0-9]+);SomeOtherSecret=([a-z0-9]+)", RegexOptions.IgnoreCase),
                stringContainingSecrets,
                50);

            Assert.AreEqual(expectedString, obscuredString);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario1()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            // 
            // Scenario:
            // AccessToken={token}
            var originalBytes = new List<byte>
            {
                65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 115, 114, 113, 102, 119, 114, 101, 52, 53, 102, 49,
                101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56, 121, 53, 100, 119, 99, 113, 110, 113, 106, 114, 108,
                120, 109, 100, 53, 120, 101, 104, 97, 112, 100, 107, 110, 113, 111, 109, 116, 113, 116, 97
            };

            var obscuredBytes = new List<byte>
            {
                65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 115, 114, 113, 102, 119, 114, 101, 52, 53, 102, 49,
                101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56, 121, 53, 100, 119, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario2()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            // 
            // Scenario:
            // PersonalAccessToken={token}
            var originalBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 115, 114, 113, 102, 119,
                114, 101, 52, 53, 102, 49, 101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56, 121, 53, 100, 119, 99, 113, 110, 113,
                106, 114, 108, 120, 109, 100, 53, 120, 101, 104, 97, 112, 100, 107, 110, 113, 111, 109, 116, 113, 116, 97
            };

            var obscuredBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 115, 114, 113, 102, 119,
                114, 101, 52, 53, 102, 49, 101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56, 121, 53, 100, 119, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario3()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            //
            // Scenario:
            // --profile=ANY-PROFILE-V1.json --platform=Any --timeout=1440 --parameters=PersonalAccessToken={token} --metadata=key1=value1,,,key2=value2
            var originalBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 65, 78, 89, 45, 80, 82, 79, 70, 73, 76, 69, 45, 86, 49, 46, 106, 115,
                111, 110, 32, 45, 45, 112, 108, 97, 116, 102, 111, 114, 109, 61, 65, 110, 121, 32, 45, 45, 116, 105, 109, 101, 111,
                117, 116, 61, 49, 52, 52, 48, 32, 45, 45, 112, 97, 114, 97, 109, 101, 116, 101, 114, 115, 61, 80, 101, 114, 115, 111,
                110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 119, 122, 113, 103, 119, 114, 101, 52, 54, 102,
                49, 101, 99, 112, 116, 104, 54, 100, 103, 115, 114, 54, 122, 49, 100, 115, 114, 113, 110, 113, 115, 114, 108, 122,
                101, 100, 56, 120, 101, 104, 107, 112, 100, 110, 110, 120, 111, 109, 116, 107, 116, 97, 32, 45, 45, 109, 101, 116,
                97, 100, 97, 116, 97, 61, 107, 101, 121, 49, 61, 118, 97, 108, 117, 101, 49, 44, 44, 44, 107, 101, 121, 50, 61, 118,
                97, 108, 117, 101, 50
            };

            var obscuredBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 65, 78, 89, 45, 80, 82, 79, 70, 73, 76, 69, 45, 86, 49, 46, 106, 115,
                111, 110, 32, 45, 45, 112, 108, 97, 116, 102, 111, 114, 109, 61, 65, 110, 121, 32, 45, 45, 116, 105, 109, 101, 111,
                117, 116, 61, 49, 52, 52, 48, 32, 45, 45, 112, 97, 114, 97, 109, 101, 116, 101, 114, 115, 61, 80, 101, 114, 115, 111,
                110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 119, 122, 113, 103, 119, 114, 101, 52, 54, 102,
                49, 101, 99, 112, 116, 104, 54, 100, 103, 115, 114, 54, 122, 49, 100, 115, 46, 46, 46, 32, 45, 45, 109, 101, 116, 97,
                100, 97, 116, 97, 61, 107, 101, 121, 49, 61, 118, 97, 108, 117, 101, 49, 44, 44, 44, 107, 101, 121, 50, 61, 118, 97,
                108, 117, 101, 50
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario4()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            //
            // Scenario:
            // --profile=ANY-PROFILE-V1.json --platform=Any --timeout=1440 --parameters="PersonalAccessToken={token}" --metadata=key1=value1,,,key2=value2
            var originalBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 65, 78, 89, 45, 80, 82, 79, 70, 73, 76, 69, 45, 86, 49, 46, 106, 115,
                111, 110, 32, 45, 45, 112, 108, 97, 116, 102, 111, 114, 109, 61, 65, 110, 121, 32, 45, 45, 116, 105, 109, 101, 111,
                117, 116, 61, 49, 52, 52, 48, 32, 45, 45, 112, 97, 114, 97, 109, 101, 116, 101, 114, 115, 61, 34, 80, 101, 114, 115, 111,
                110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 119, 122, 113, 103, 119, 114, 101, 52, 54, 102,
                49, 101, 99, 112, 116, 104, 54, 100, 103, 115, 114, 54, 122, 49, 100, 115, 114, 113, 110, 113, 115, 114, 108, 122,
                101, 100, 56, 120, 101, 104, 107, 112, 100, 110, 110, 120, 111, 109, 116, 107, 116, 97, 34, 32, 45, 45, 109, 101, 116,
                97, 100, 97, 116, 97, 61, 107, 101, 121, 49, 61, 118, 97, 108, 117, 101, 49, 44, 44, 44, 107, 101, 121, 50, 61, 118,
                97, 108, 117, 101, 50
            };

            var obscuredBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 65, 78, 89, 45, 80, 82, 79, 70, 73, 76, 69, 45, 86, 49, 46, 106, 115, 111,
                110, 32, 45, 45, 112, 108, 97, 116, 102, 111, 114, 109, 61, 65, 110, 121, 32, 45, 45, 116, 105, 109, 101, 111, 117, 116,
                61, 49, 52, 52, 48, 32, 45, 45, 112, 97, 114, 97, 109, 101, 116, 101, 114, 115, 61, 34, 80, 101, 114, 115, 111, 110, 97,
                108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 119, 122, 113, 103, 119, 114, 101, 52, 54, 102, 49, 101, 99,
                112, 116, 104, 54, 100, 103, 115, 114, 54, 122, 49, 100, 115, 46, 46, 46, 34, 32, 45, 45, 109, 101, 116, 97, 100, 97, 116,
                97, 61, 107, 101, 121, 49, 61, 118, 97, 108, 117, 101, 49, 44, 44, 44, 107, 101, 121, 50, 61, 118, 97, 108, 117, 101, 50
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario5()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            //
            // Scenario:
            // PersonalAccessToken="{token}"
            var originalBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 34, 112, 122, 113, 102,
                101, 114, 99, 52, 55, 52, 51, 101, 99, 112, 113, 104, 51, 100, 103, 114, 114, 104, 121, 53, 100, 103, 99, 113, 110,
                113, 103, 114, 108, 122, 55, 109, 100, 50, 120, 101, 48, 97, 112, 100, 107, 102, 113, 121, 109, 116, 119, 116, 49, 34
            };

            var obscuredBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61, 34, 112, 122, 113, 102,
                101, 114, 99, 52, 55, 52, 51, 101, 99, 112, 113, 104, 51, 100, 103, 114, 114, 104, 121, 53, 100, 103, 46, 46, 46, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario6()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            //
            // Scenario:
            // PersonalAccessToken= {token}
            var originalBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 116, 122, 113, 102, 103,
                114, 99, 52, 51, 102, 51, 101, 100, 112, 107, 104, 51, 115, 103, 114, 114, 113, 121, 53, 99, 115, 99, 113, 110, 53,
                103, 114, 54, 122, 109, 100, 100, 120, 101, 104, 108, 112, 100, 101, 110, 113, 111, 53, 116, 113, 102, 99
            };

            var obscuredBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 116, 122, 113, 102, 103,
                114, 99, 52, 51, 102, 51, 101, 100, 112, 107, 104, 51, 115, 103, 114, 114, 113, 121, 53, 99, 115, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario7()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            // Scenario:
            // PersonalAccessToken=  "{token}"
            var originalBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 32, 34, 116, 122,
                113, 102, 103, 114, 99, 52, 51, 102, 51, 101, 100, 112, 107, 104, 51, 115, 103, 114, 114, 113, 121, 53, 99, 115,
                99, 113, 110, 53, 103, 114, 54, 122, 109, 100, 100, 120, 101, 104, 108, 112, 100, 101, 110, 113, 111, 53, 116,
                113, 102, 99, 34
            };

            var obscuredBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 32, 34, 116, 122,
                113, 102, 103, 114, 99, 52, 51, 102, 51, 101, 100, 112, 107, 104, 51, 115, 103, 114, 114, 113, 121, 53, 99, 115,
                46, 46, 46, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccessTokenPairScenario8()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            // Scenario:
            // PersonalAccessToken=  "{token}"
            var originalBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 32, 34, 116, 122,
                113, 102, 103, 114, 99, 52, 51, 102, 51, 101, 100, 112, 107, 104, 51, 115, 103, 114, 114, 113, 121, 53, 99, 115,
                99, 113, 110, 53, 103, 114, 54, 122, 109, 100, 100, 120, 101, 104, 108, 112, 100, 101, 110, 113, 111, 53, 116,
                113, 102, 99, 34
            };

            var obscuredBytes = new List<byte>
            {
                80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 32, 32, 34, 46, 46, 46, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccountKeyPair()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                68, 101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116, 111, 99, 111, 108,
                61, 104, 116, 116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117, 110, 111, 100, 101,
                118, 48, 49, 100, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110, 116, 75, 101, 121,
                61, 97, 101, 114, 119, 75, 53, 121, 76, 68, 85, 97, 85, 97, 97, 107, 121, 100, 49, 119, 103, 51, 101, 87, 74, 117,
                73, 106, 43, 113, 122, 56, 66, 66, 100, 74, 101, 76, 76, 66, 52, 115, 86, 113, 48, 86, 111, 119, 114, 66, 88, 43, 120,
                74, 49, 66, 80, 98, 79, 83, 80, 86, 69, 79, 50, 52, 104, 55, 73, 115, 82, 106, 121, 76, 103, 49, 36, 117, 80, 49, 86,
                57, 48, 56, 113, 98, 74, 61, 61, 59, 69, 110, 100, 112, 111, 105, 110, 116, 83, 117, 102, 102, 105, 120, 61, 99, 111,
                114, 101, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116
            };

            var obscuredBytes = new List<byte>
            {
                68, 101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116, 111, 99, 111, 108, 61,
                104, 116, 116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117, 110, 111, 100, 101, 118,
                48, 49, 100, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110, 116, 75, 101, 121, 61, 97,
                101, 114, 119, 75, 53, 121, 76, 68, 85, 97, 85, 97, 97, 107, 121, 100, 49, 119, 103, 51, 101, 87, 74, 117, 73, 106, 43,
                113, 122, 56, 66, 66, 100, 74, 101, 76, 76, 66, 52, 115, 86, 113, 48, 86, 111, 119, 114, 66, 88, 43, 120, 74, 49, 66,
                80, 98, 79, 83, 80, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccountKeyPairScenario2()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101,
                114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110,
                111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105,
                110, 103, 32, 34, 68, 101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116,
                111, 99, 111, 108, 61, 104, 116, 116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117,
                110, 111, 100, 101, 118, 48, 49, 100, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110,
                116, 75, 101, 121, 61, 121, 90, 68, 86, 97, 85, 99, 97, 100, 121, 100, 57, 119, 103, 48, 100, 87, 73, 117, 72, 106,
                43, 112, 122, 56, 99, 74, 122, 102, 72, 120, 43, 47, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49,
                50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64, 91, 93, 92, 94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115,
                113, 111, 99, 117, 50, 121, 119, 66, 48, 115, 86, 112, 48, 86, 111, 119, 114, 65, 88, 43, 98, 77, 83, 67, 86, 82, 79,
                50, 55, 104, 55, 89, 115, 70, 106, 121, 80, 103, 49, 52, 117, 85, 49, 86, 56, 48, 56, 114, 98, 81, 61, 61, 59, 69, 110,
                100, 112, 111, 105, 110, 116, 83, 117, 102, 102, 105, 120, 61, 99, 111, 114, 101, 46, 119, 105, 110, 100, 111, 119, 115,
                46, 110, 101, 116, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115,
                95, 118, 51, 34
            };

            var obscuredBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114, 46,
                101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45, 100, 101,
                118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105, 110, 103, 32, 34, 68, 101,
                102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116, 111, 99, 111, 108, 61, 104, 116, 116,
                112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117, 110, 111, 100, 101, 118, 48, 49, 100, 105, 97,
                103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110, 116, 75, 101, 121, 61, 46, 46, 46, 34, 32, 45, 45, 118,
                109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccountKeyPairForUnusualScenario()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101,
                114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110,
                111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105,
                110, 103, 32, 34, 68, 101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116,
                111, 99, 111, 108, 61, 104, 116, 116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117,
                110, 111, 100, 101, 118, 48, 49, 100, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110,
                116, 75, 101, 121, 61, 121, 90, 68, 86, 97, 85, 99, 97, 100, 121, 100, 57, 119, 103, 48, 100, 87, 73, 117, 72, 106,
                43, 112, 122, 56, 99, 74, 122, 102, 72, 120, 43, 47, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49,
                50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64, 91, 93, 92, 94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115,
                113, 111, 99, 117, 50, 121, 119, 66, 48, 115, 86, 112, 48, 86, 111, 119, 114, 65, 88, 43, 98, 77, 83, 67, 86, 82, 79,
                50, 55, 104, 55, 89, 115, 70, 106, 121, 80, 103, 49, 52, 117, 85, 49, 86, 56, 48, 56, 114, 98, 81, 61, 61, 59, 69, 110,
                100, 112, 111, 105, 110, 116, 83, 117, 102, 102, 105, 120, 61, 99, 111, 114, 101, 46, 119, 105, 110, 100, 111, 119, 115,
                46, 110, 101, 116, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115,
                95, 118, 51, 34
            };

            var obscuredBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114,
                46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45, 100,
                101, 118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105, 110, 103, 32, 34, 68,
                101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116, 111, 99, 111, 108, 61, 104, 116,
                116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117, 110, 111, 100, 101, 118, 48, 49, 100, 105,
                97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110, 116, 75, 101, 121, 61, 121, 90, 68, 86, 97, 85, 99,
                97, 100, 121, 100, 57, 119, 103, 48, 100, 87, 73, 117, 72, 106, 43, 112, 122, 56, 99, 74, 122, 102, 72, 120, 43, 47, 33, 35,
                36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64, 91, 93, 92,
                94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115, 113, 111, 99, 46, 46, 46, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34,
                83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetAccountKeyPairForUnusualScenario2()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101,
                114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110,
                111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105,
                110, 103, 32, 34, 68, 101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116,
                111, 99, 111, 108, 61, 104, 116, 116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117,
                110, 111, 100, 101, 118, 48, 49, 100, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110,
                116, 75, 101, 121, 61, 121, 90, 68, 86, 97, 85, 99, 97, 100, 121, 100, 57, 119, 103, 48, 100, 87, 73, 117, 72, 106,
                43, 112, 122, 56, 99, 74, 122, 102, 72, 120, 43, 47, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49,
                50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64, 91, 93, 92, 94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115,
                113, 111, 99, 117, 50, 121, 119, 66, 48, 115, 86, 112, 48, 86, 111, 119, 114, 65, 88, 43, 98, 77, 83, 67, 86, 82, 79,
                50, 55, 104, 55, 89, 115, 70, 106, 121, 80, 103, 49, 52, 117, 85, 49, 86, 56, 48, 56, 114, 98, 81, 61, 61, 59, 69, 110,
                100, 112, 111, 105, 110, 116, 83, 117, 102, 102, 105, 120, 61, 99, 111, 114, 101, 46, 119, 105, 110, 100, 111, 119, 115,
                46, 110, 101, 116, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115,
                95, 118, 51, 34
            };

            var obscuredBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114,
                46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45, 100,
                101, 118, 48, 49, 34, 32, 45, 45, 99, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105, 110, 103, 32, 34, 68,
                101, 102, 97, 117, 108, 116, 69, 110, 100, 112, 111, 105, 110, 116, 115, 80, 114, 111, 116, 111, 99, 111, 108, 61, 104, 116,
                116, 112, 115, 59, 65, 99, 99, 111, 117, 110, 116, 78, 97, 109, 101, 61, 106, 117, 110, 111, 100, 101, 118, 48, 49, 100, 105,
                97, 103, 110, 111, 115, 116, 105, 99, 115, 59, 65, 99, 99, 111, 117, 110, 116, 75, 101, 121, 61, 121, 90, 68, 86, 97, 85, 99,
                97, 100, 121, 100, 57, 119, 103, 48, 100, 87, 73, 117, 72, 106, 43, 112, 122, 56, 99, 74, 122, 102, 72, 120, 43, 47, 33, 35,
                36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64, 91, 93, 92,
                94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115, 113, 111, 99, 46, 46, 46, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34,
                83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSharedAccessKeyPair()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 97, 110, 121, 46, 115, 101, 114, 118, 105,
                99, 101, 98, 117, 115, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114,
                101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 65, 110, 121, 65, 99, 99, 101, 115,
                115, 75, 101, 121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 65, 83, 81,
                85, 54, 103, 88, 99, 85, 53, 122, 106, 47, 100, 75, 75, 78, 50, 101, 67, 56, 82, 52, 71, 75, 103, 103, 122,
                109, 80, 111, 103, 82, 88, 115, 57, 98, 43, 107, 106, 89, 82, 69, 61
            };

            var obscuredBytes = new List<byte>
            {
                69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 97, 110, 121, 46, 115, 101, 114, 118, 105, 99,
                101, 98, 117, 115, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100,
                65, 99, 99, 101, 115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 65, 110, 121, 65, 99, 99, 101, 115, 115, 75, 101,
                121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 65, 83, 81, 85, 54, 103, 88,
                99, 85, 53, 122, 106, 47, 100, 75, 75, 78, 50, 101, 67, 56, 82, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSharedAccessKeyPairForUnusualScenario()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101,
                114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110,
                111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 32, 34,
                98, 56, 54, 101, 48, 54, 52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97, 50, 50, 98, 45, 49, 57, 97, 48,
                55, 56, 100, 54, 50, 54, 100, 56, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99,
                116, 105, 111, 110, 83, 116, 114, 105, 110, 103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47,
                47, 106, 117, 110, 111, 100, 101, 118, 48, 49, 101, 118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99,
                101, 98, 117, 115, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65,
                99, 99, 101, 115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 84, 101, 108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101,
                115, 115, 75, 101, 121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 99, 74, 122, 102,
                72, 120, 43, 47, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58,
                60, 61, 62, 63, 64, 91, 93, 92, 94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115, 113, 111, 99, 117, 50, 121, 119, 75,
                102, 85, 77, 43, 111, 51, 66, 71, 89, 78, 51, 68, 112, 101, 121, 67, 80, 119, 115, 76, 98, 111, 80, 61, 34, 32, 45, 45,
                101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116, 101, 108, 101, 109, 101, 116, 114, 121, 45, 97, 103, 101, 110, 116,
                115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            var obscuredBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114,
                46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45,
                100, 101, 118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 32, 34, 98, 56, 54,
                101, 48, 54, 52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97, 50, 50, 98, 45, 49, 57, 97, 48, 55, 56, 100, 54,
                50, 54, 100, 56, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99, 116, 105, 111, 110,
                83, 116, 114, 105, 110, 103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 106, 117, 110, 111,
                100, 101, 118, 48, 49, 101, 118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99, 101, 98, 117, 115, 46,
                119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115,
                75, 101, 121, 78, 97, 109, 101, 61, 84, 101, 108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101, 115, 115, 75, 101,
                121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 99, 74, 122, 102, 72, 120, 43, 47,
                33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 60, 61, 62, 63, 64,
                91, 93, 92, 94, 46, 46, 46, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116, 101, 108, 101, 109, 101,
                116, 114, 121, 45, 97, 103, 101, 110, 116, 115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100,
                97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSharedAccessKeyPairForUnusualScenario2()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101,
                114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110,
                111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 32, 34,
                98, 56, 54, 101, 48, 54, 52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97, 50, 50, 98, 45, 49, 57, 97, 48,
                55, 56, 100, 54, 50, 54, 100, 56, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99,
                116, 105, 111, 110, 83, 116, 114, 105, 110, 103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47,
                47, 106, 117, 110, 111, 100, 101, 118, 48, 49, 101, 118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99,
                101, 98, 117, 115, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65,
                99, 99, 101, 115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 84, 101, 108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101,
                115, 115, 75, 101, 121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 99, 74, 122, 102,
                72, 120, 43, 47, 33, 35, 36, 37, 38, 39, 40, 41, 42, 43, 39, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58,
                60, 61, 62, 63, 64, 91, 93, 92, 94, 95, 96, 123, 124, 125, 126, 85, 49, 80, 115, 113, 111, 99, 117, 50, 121, 119, 75,
                102, 85, 77, 43, 111, 51, 66, 71, 89, 78, 51, 68, 112, 101, 121, 67, 80, 119, 115, 76, 98, 111, 80, 61, 34, 32, 45, 45,
                101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116, 101, 108, 101, 109, 101, 116, 114, 121, 45, 97, 103, 101, 110, 116,
                115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            var obscuredBytes = new List<byte>
            {
               74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114, 46,
               101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45, 100, 101,
               118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 32, 34, 98, 56, 54, 101, 48, 54,
               52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97, 50, 50, 98, 45, 49, 57, 97, 48, 55, 56, 100, 54, 50, 54, 100, 56,
               34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99, 116, 105, 111, 110, 83, 116, 114, 105, 110,
               103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 106, 117, 110, 111, 100, 101, 118, 48, 49, 101,
               118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99, 101, 98, 117, 115, 46, 119, 105, 110, 100, 111, 119, 115,
               46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 84, 101,
               108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101, 115, 115, 75, 101, 121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101,
               115, 115, 75, 101, 121, 61, 46, 46, 46, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116, 101, 108, 101, 109,
               101, 116, 114, 121, 45, 97, 103, 101, 110, 116, 115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100, 97,
               114, 100, 95, 68, 50, 115, 95, 118, 51, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSharedAccessKeyPairForBugScenario1()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108,
                108, 101, 114, 46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32,
                34, 106, 117, 110, 111, 45, 100, 101, 118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101,
                110, 116, 73, 100, 32, 34, 98, 56, 54, 101, 48, 54, 52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97,
                50, 50, 98, 45, 49, 57, 97, 48, 55, 56, 100, 54, 50, 54, 100, 56, 34, 32, 45, 45, 97, 103, 101, 110, 116,
                73, 100, 32, 34, 117, 110, 107, 110, 111, 119, 110, 44, 117, 110, 107, 110, 111, 119, 110, 44, 102, 50, 101,
                52, 51, 54, 98, 98, 100, 102, 101, 45, 48, 34, 32, 45, 45, 112, 97, 99, 107, 97, 103, 101, 86, 101, 114, 115,
                105, 111, 110, 32, 34, 51, 46, 51, 46, 52, 34, 32, 45, 45, 97, 112, 112, 73, 110, 115, 105, 103, 104, 116,
                115, 73, 110, 115, 116, 114, 117, 109, 101, 110, 116, 97, 116, 105, 111, 110, 75, 101, 121, 32, 34, 102, 57,
                102, 51, 99, 98, 55, 50, 45, 54, 101, 57, 50, 45, 52, 101, 98, 55, 45, 56, 100, 98, 99, 45, 100, 51, 100, 57,
                98, 55, 56, 57, 50, 57, 51, 52, 34, 32, 45, 45, 107, 101, 121, 86, 97, 117, 108, 116, 85, 114, 105, 32, 34,
                104, 116, 116, 112, 115, 58, 47, 47, 106, 117, 110, 111, 100, 101, 118, 48, 49, 118, 97, 117, 108, 116, 46,
                118, 97, 117, 108, 116, 46, 97, 122, 117, 114, 101, 46, 110, 101, 116, 47, 34, 32, 45, 45, 99, 101, 114, 116,
                105, 102, 105, 99, 97, 116, 101, 78, 97, 109, 101, 32, 34, 106, 117, 110, 111, 45, 100, 101, 118, 48, 49, 45,
                103, 117, 101, 115, 116, 97, 103, 101, 110, 116, 34, 32, 45, 45, 110, 117, 103, 101, 116, 70, 101, 101, 100, 85,
                114, 105, 32, 34, 104, 116, 116, 112, 115, 58, 47, 47, 109, 115, 97, 122, 117, 114, 101, 46, 112, 107, 103, 115,
                46, 118, 105, 115, 117, 97, 108, 115, 116, 117, 100, 105, 111, 46, 99, 111, 109, 47, 95, 112, 97, 99, 107, 97,
                103, 105, 110, 103, 47, 100, 56, 102, 49, 50, 98, 53, 101, 45, 99, 97, 56, 48, 45, 52, 100, 101, 99, 45, 56, 54,
                55, 98, 45, 55, 50, 56, 57, 99, 51, 102, 99, 54, 49, 52, 54, 47, 110, 117, 103, 101, 116, 47, 118, 50, 47, 34, 32,
                45, 45, 110, 117, 103, 101, 116, 80, 97, 116, 32, 34, 78, 117, 103, 101, 116, 65, 99, 99, 101, 115, 115, 84, 111,
                107, 101, 110, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99, 116, 105, 111, 110,
                83, 116, 114, 105, 110, 103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 106, 117, 110,
                111, 100, 101, 118, 48, 49, 101, 118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99, 101, 98, 117,
                115, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101,
                115, 115, 75, 101, 121, 78, 97, 109, 101, 61, 84, 101, 108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101, 115, 115,
                75, 101, 121, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 99, 74, 122, 102, 72, 120,
                43, 47, 85, 49, 80, 115, 113, 111, 99, 117, 50, 121, 119, 75, 102, 85, 77, 43, 111, 51, 66, 71, 89, 78, 51, 68, 112,
                101, 121, 67, 80, 119, 115, 76, 98, 111, 80, 61, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116,
                101, 108, 101, 109, 101, 116, 114, 121, 45, 97, 103, 101, 110, 116, 115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32,
                34, 83, 116, 97, 110, 100, 97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34, 32, 45, 45, 114, 101, 103, 105, 111, 110,
                32, 34, 69, 97, 115, 116, 85, 83, 34
            };

            var obscuredBytes = new List<byte>
            {
                74, 117, 110, 111, 46, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 46, 73, 110, 115, 116, 97, 108, 108, 101, 114,
                46, 101, 120, 101, 32, 45, 45, 101, 110, 118, 105, 114, 111, 110, 109, 101, 110, 116, 32, 34, 106, 117, 110, 111, 45,
                100, 101, 118, 48, 49, 34, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 32, 34, 98, 56, 54,
                101, 48, 54, 52, 55, 45, 57, 50, 99, 54, 45, 52, 52, 54, 54, 45, 97, 50, 50, 98, 45, 49, 57, 97, 48, 55, 56, 100, 54,
                50, 54, 100, 56, 34, 32, 45, 45, 97, 103, 101, 110, 116, 73, 100, 32, 34, 117, 110, 107, 110, 111, 119, 110, 44, 117,
                110, 107, 110, 111, 119, 110, 44, 102, 50, 101, 52, 51, 54, 98, 98, 100, 102, 101, 45, 48, 34, 32, 45, 45, 112, 97, 99,
                107, 97, 103, 101, 86, 101, 114, 115, 105, 111, 110, 32, 34, 51, 46, 51, 46, 52, 34, 32, 45, 45, 97, 112, 112, 73, 110,
                115, 105, 103, 104, 116, 115, 73, 110, 115, 116, 114, 117, 109, 101, 110, 116, 97, 116, 105, 111, 110, 75, 101, 121, 32,
                34, 102, 57, 102, 51, 99, 98, 55, 50, 45, 54, 101, 57, 50, 45, 52, 101, 98, 55, 45, 56, 100, 98, 99, 45, 100, 51, 100,
                57, 98, 55, 56, 57, 50, 57, 51, 52, 34, 32, 45, 45, 107, 101, 121, 86, 97, 117, 108, 116, 85, 114, 105, 32, 34, 104, 116,
                116, 112, 115, 58, 47, 47, 106, 117, 110, 111, 100, 101, 118, 48, 49, 118, 97, 117, 108, 116, 46, 118, 97, 117, 108, 116,
                46, 97, 122, 117, 114, 101, 46, 110, 101, 116, 47, 34, 32, 45, 45, 99, 101, 114, 116, 105, 102, 105, 99, 97, 116, 101, 78,
                97, 109, 101, 32, 34, 106, 117, 110, 111, 45, 100, 101, 118, 48, 49, 45, 103, 117, 101, 115, 116, 97, 103, 101, 110, 116,
                34, 32, 45, 45, 110, 117, 103, 101, 116, 70, 101, 101, 100, 85, 114, 105, 32, 34, 104, 116, 116, 112, 115, 58, 47, 47, 109,
                115, 97, 122, 117, 114, 101, 46, 112, 107, 103, 115, 46, 118, 105, 115, 117, 97, 108, 115, 116, 117, 100, 105, 111, 46, 99,
                111, 109, 47, 95, 112, 97, 99, 107, 97, 103, 105, 110, 103, 47, 100, 56, 102, 49, 50, 98, 53, 101, 45, 99, 97, 56, 48, 45,
                52, 100, 101, 99, 45, 56, 54, 55, 98, 45, 55, 50, 56, 57, 99, 51, 102, 99, 54, 49, 52, 54, 47, 110, 117, 103, 101, 116, 47,
                118, 50, 47, 34, 32, 45, 45, 110, 117, 103, 101, 116, 80, 97, 116, 32, 34, 78, 117, 103, 101, 116, 65, 99, 99, 101, 115, 115,
                84, 111, 107, 101, 110, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 67, 111, 110, 110, 101, 99, 116, 105, 111, 110,
                83, 116, 114, 105, 110, 103, 32, 34, 69, 110, 100, 112, 111, 105, 110, 116, 61, 115, 98, 58, 47, 47, 106, 117, 110, 111, 100,
                101, 118, 48, 49, 101, 118, 101, 110, 116, 104, 117, 98, 46, 115, 101, 114, 118, 105, 99, 101, 98, 117, 115, 46, 119, 105,
                110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 59, 83, 104, 97, 114, 101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 78,
                97, 109, 101, 61, 84, 101, 108, 101, 109, 101, 116, 114, 121, 65, 99, 99, 101, 115, 115, 75, 101, 121, 59, 83, 104, 97, 114,
                101, 100, 65, 99, 99, 101, 115, 115, 75, 101, 121, 61, 99, 74, 122, 102, 72, 120, 43, 47, 85, 49, 80, 115, 113, 111, 99, 117,
                50, 121, 119, 75, 102, 85, 46, 46, 46, 34, 32, 45, 45, 101, 118, 101, 110, 116, 72, 117, 98, 32, 34, 116, 101, 108, 101, 109,
                101, 116, 114, 121, 45, 97, 103, 101, 110, 116, 115, 34, 32, 45, 45, 118, 109, 83, 107, 117, 32, 34, 83, 116, 97, 110, 100,
                97, 114, 100, 95, 68, 50, 115, 95, 118, 51, 34, 32, 45, 45, 114, 101, 103, 105, 111, 110, 32, 34, 69, 97, 115, 116, 85, 83, 34
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSasUriPairScenario1()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners. This is in the
            // format of a secret but is NOT a real secret.
            var originalBytes = new List<byte>
            {
                104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108, 111, 98, 46, 99, 111, 114, 101, 46, 119, 105, 110,
                100, 111, 119, 115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63, 115, 112, 61, 114, 38, 115, 116,
                61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 53, 84, 50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61, 50, 48, 50, 50,
                45, 48, 53, 45, 50, 54, 84, 48, 54, 58, 48, 53, 58, 48, 51, 90, 38, 115, 112, 114, 61, 104, 116, 116, 112, 115, 38,
                115, 118, 61, 50, 48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38, 115, 105, 103, 61, 119, 114, 78, 98,
                117, 89, 118, 56, 48, 105, 112, 119, 116, 54, 49, 71, 37, 50, 66, 68, 97, 81, 88, 79, 114, 99, 119, 65, 74, 80, 110,
                49, 118, 115, 66, 49, 88, 65, 74, 87, 48, 111, 102, 99, 56, 37, 51, 68
            };

            var obscuredBytes = new List<byte>
            {
                104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108, 111, 98, 46, 99, 111, 114, 101, 46, 119, 105, 110, 100,
                111, 119, 115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63, 115, 112, 61, 114, 38, 115, 116, 61, 50,
                48, 50, 50, 45, 48, 53, 45, 50, 53, 84, 50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61, 50, 48, 50, 50, 45, 48, 53,
                45, 50, 54, 84, 48, 54, 58, 48, 53, 58, 48, 51, 90, 38, 115, 112, 114, 61, 104, 116, 116, 112, 115, 38, 115, 118, 61, 50,
                48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38, 115, 105, 103, 61, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }

        private static Tuple<string, string> GetSasUriPairScenario2()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners. This is in the
            // format of a secret but is NOT a real secret.
            var originalBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 80, 69, 82, 70, 45, 73, 79, 45, 70, 73, 79, 46, 106, 115, 111, 110, 32, 45,
                45, 112, 108, 97, 116, 102, 111, 114, 109, 61, 74, 117, 110, 111, 32, 45, 45, 116, 105, 109, 101, 111, 117, 116, 61, 54, 48,
                32, 45, 45, 109, 101, 116, 97, 100, 97, 116, 97, 61, 97, 103, 101, 110, 116, 73, 100, 61, 97, 109, 122, 48, 55, 112, 114, 100,
                97, 112, 112, 48, 51, 44, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55,
                45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 98, 56, 56, 56, 56, 100, 99, 102, 100, 52, 101, 45, 48, 44, 102, 51,
                57, 48, 49, 102, 98, 97, 45, 99, 49, 51, 55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56, 101, 100, 100, 101, 97, 52, 100,
                54, 55, 52, 100, 44, 44, 44, 97, 103, 101, 110, 116, 84, 121, 112, 101, 61, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 44,
                44, 44, 99, 111, 110, 116, 97, 105, 110, 101, 114, 73, 100, 61, 97, 53, 50, 52, 49, 98, 53, 97, 45, 53, 56, 102, 101, 45, 52, 52,
                54, 50, 45, 98, 100, 48, 100, 45, 51, 51, 99, 49, 97, 98, 101, 98, 97, 50, 99, 52, 44, 44, 44, 116, 105, 112, 83, 101, 115, 115,
                105, 111, 110, 73, 100, 61, 102, 51, 57, 48, 49, 102, 98, 97, 45, 99, 49, 51, 55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56,
                101, 100, 100, 101, 97, 52, 100, 54, 55, 52, 100, 44, 44, 44, 110, 111, 100, 101, 73, 100, 61, 53, 55, 50, 99, 50, 53, 98, 48, 45,
                102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 44, 44, 110,
                111, 100, 101, 78, 97, 109, 101, 61, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48,
                55, 45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100,
                61, 51, 54, 55, 98, 99, 50, 99, 49, 45, 57, 56, 99, 48, 45, 52, 97, 54, 101, 45, 98, 53, 100, 55, 45, 49, 50, 51, 97, 101, 53,
                52, 52, 51, 98, 52, 56, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 83, 116, 101, 112, 73, 100, 61, 100, 51,
                52, 101, 52, 49, 50, 55, 45, 54, 52, 99, 56, 45, 52, 50, 54, 100, 45, 57, 54, 54, 57, 45, 49, 100, 57, 98, 49, 49, 50, 55, 49,
                51, 53, 102, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 71, 114, 111, 117, 112, 61, 71, 114, 111, 117, 112,
                32, 66, 44, 44, 44, 103, 114, 111, 117, 112, 73, 100, 61, 71, 114, 111, 117, 112, 32, 66, 44, 44, 44, 118, 105, 114, 116, 117,
                97, 108, 77, 97, 99, 104, 105, 110, 101, 78, 97, 109, 101, 61, 98, 56, 56, 56, 56, 100, 99, 102, 100, 52, 101, 45, 48, 44, 44,
                44, 99, 108, 117, 115, 116, 101, 114, 78, 97, 109, 101, 61, 97, 109, 122, 48, 55, 112, 114, 100, 97, 112, 112, 48, 51, 44, 44,
                44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 84, 121, 112, 101, 61, 81, 111, 83, 44, 44, 44, 103, 101, 110, 101, 114,
                97, 116, 105, 111, 110, 61, 71, 101, 110, 54, 44, 44, 44, 110, 111, 100, 101, 67, 112, 117, 73, 100, 61, 53, 48, 54, 53, 52, 44,
                44, 44, 112, 97, 121, 108, 111, 97, 100, 61, 81, 111, 83, 44, 44, 44, 112, 97, 121, 108, 111, 97, 100, 80, 70, 86, 101, 114, 115,
                105, 111, 110, 61, 49, 46, 48, 46, 49, 57, 49, 51, 46, 54, 49, 50, 44, 44, 44, 112, 97, 121, 108, 111, 97, 100, 84, 121, 112, 101,
                61, 81, 111, 83, 44, 44, 44, 112, 97, 121, 108, 111, 97, 100, 86, 101, 114, 115, 105, 111, 110, 61, 81, 111, 83, 44, 44, 44, 119,
                111, 114, 107, 108, 111, 97, 100, 61, 80, 69, 82, 70, 45, 73, 79, 45, 70, 73, 79, 44, 44, 44, 119, 111, 114, 107, 108, 111, 97,
                100, 84, 121, 112, 101, 61, 86, 105, 114, 116, 117, 97, 108, 67, 108, 105, 101, 110, 116, 44, 44, 44, 119, 111, 114, 107, 108,
                111, 97, 100, 86, 101, 114, 115, 105, 111, 110, 61, 49, 46, 48, 46, 49, 57, 49, 51, 46, 54, 50, 48, 44, 44, 44, 105, 109, 112,
                97, 99, 116, 84, 121, 112, 101, 61, 78, 111, 110, 101, 44, 44, 44, 114, 101, 118, 105, 115, 105, 111, 110, 61, 50, 48, 48, 54,
                66, 48, 54, 95, 50, 49, 46, 51, 46, 51, 48, 48, 48, 50, 46, 48, 44, 44, 44, 116, 101, 110, 97, 110, 116, 73, 100, 61, 55, 50,
                102, 57, 56, 56, 98, 102, 45, 56, 54, 102, 49, 45, 52, 49, 97, 102, 45, 57, 49, 97, 98, 45, 50, 100, 55, 99, 100, 48, 49, 49,
                100, 98, 52, 55, 44, 44, 44, 101, 110, 97, 98, 108, 101, 68, 105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 61, 84, 114, 117,
                101, 44, 44, 44, 116, 97, 114, 103, 101, 116, 71, 111, 97, 108, 61, 81, 111, 83, 95, 76, 105, 110, 117, 120, 95, 80, 69, 82, 70,
                45, 73, 79, 45, 70, 73, 79, 44, 44, 44, 101, 120, 101, 99, 117, 116, 105, 111, 110, 71, 111, 97, 108, 61, 81, 111, 83, 95, 76,
                105, 110, 117, 100, 50, 50, 57, 99, 52, 48, 99, 45, 49, 51, 53, 53, 45, 52, 56, 57, 99, 45, 97, 56, 48, 101, 45, 53, 52, 100, 97,
                52, 101, 49, 51, 97, 52, 97, 54, 44, 44, 44, 101, 120, 101, 99, 117, 116, 105, 111, 110, 71, 111, 97, 108, 73, 100, 61, 81, 111,
                83, 95, 76, 105, 110, 117, 100, 50, 50, 57, 99, 52, 48, 99, 45, 49, 51, 53, 53, 45, 52, 56, 57, 99, 45, 97, 56, 48, 101, 45, 53,
                52, 100, 97, 52, 101, 49, 51, 97, 52, 97, 54, 44, 44, 44, 111, 119, 110, 101, 114, 61, 118, 97, 115, 97, 108, 64, 109, 105, 99,
                114, 111, 115, 111, 102, 116, 46, 99, 111, 109, 59, 118, 45, 109, 97, 114, 116, 106, 101, 64, 109, 105, 99, 114, 111, 115, 111,
                102, 116, 46, 99, 111, 109, 44, 44, 44, 116, 101, 109, 112, 108, 97, 116, 101, 79, 119, 110, 101, 114, 61, 118, 45, 109, 97, 114,
                116, 106, 101, 64, 109, 105, 99, 114, 111, 115, 111, 102, 116, 46, 99, 111, 109, 44, 44, 44, 118, 101, 114, 115, 105, 111, 110, 61,
                50, 48, 50, 49, 45, 48, 49, 45, 48, 49, 44, 44, 44, 116, 101, 97, 109, 78, 97, 109, 101, 61, 67, 82, 67, 32, 65, 73, 82, 44, 44,
                44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 46, 110, 97, 109, 101, 61, 81, 111, 83, 95, 76, 105, 110, 117, 120, 44, 44,
                44, 100, 97, 116, 101, 99, 114, 101, 97, 116, 101, 100, 61, 48, 54, 47, 48, 56, 47, 50, 48, 50, 49, 44, 44, 44, 105, 110, 116, 101,
                110, 116, 61, 74, 117, 110, 111, 32, 81, 111, 83, 32, 119, 105, 116, 104, 32, 76, 105, 110, 117, 120, 32, 83, 117, 112, 112, 111,
                114, 116, 32, 86, 97, 108, 105, 100, 97, 116, 105, 111, 110, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 67, 97,
                116, 101, 103, 111, 114, 121, 61, 65, 66, 44, 44, 44, 109, 111, 110, 105, 116, 111, 114, 105, 110, 103, 69, 110, 97, 98, 108, 101,
                100, 61, 84, 114, 117, 101, 44, 44, 44, 116, 101, 109, 112, 108, 97, 116, 101, 73, 100, 61, 81, 111, 83, 95, 76, 105, 110, 117, 120,
                46, 84, 101, 109, 112, 108, 97, 116, 101, 46, 118, 50, 46, 106, 115, 111, 110, 32, 45, 45, 108, 97, 121, 111, 117, 116, 80, 97, 116,
                104, 61, 47, 104, 111, 109, 101, 47, 106, 117, 110, 111, 118, 109, 97, 100, 109, 105, 110, 47, 110, 117, 103, 101, 116, 47, 112, 97,
                99, 107, 97, 103, 101, 115, 47, 118, 105, 114, 116, 117, 97, 108, 99, 108, 105, 101, 110, 116, 47, 49, 46, 48, 46, 49, 57, 49, 51,
                46, 54, 50, 48, 47, 99, 111, 110, 116, 101, 110, 116, 47, 108, 105, 110, 117, 120, 45, 120, 54, 52, 47, 108, 97, 121, 111, 117, 116,
                46, 106, 115, 111, 110, 32, 45, 45, 115, 101, 101, 100, 61, 50, 51, 56, 56, 54, 52, 55, 50, 49, 32, 45, 45, 97, 103, 101, 110, 116,
                73, 100, 61, 97, 109, 122, 48, 55, 112, 114, 100, 97, 112, 112, 48, 51, 44, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100, 100,
                45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 98, 56, 56, 56, 56, 100, 99, 102,
                100, 52, 101, 45, 48, 44, 102, 51, 57, 48, 49, 102, 98, 97, 45, 99, 49, 51, 55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56, 101,
                100, 100, 101, 97, 52, 100, 54, 55, 52, 100, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 61, 51, 54, 55,
                98, 99, 50, 99, 49, 45, 57, 56, 99, 48, 45, 52, 97, 54, 101, 45, 98, 53, 100, 55, 45, 49, 50, 51, 97, 101, 53, 52, 52, 51, 98, 52, 56,
                32, 45, 45, 112, 97, 99, 107, 97, 103, 101, 115, 61, 104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108, 111, 98, 46, 99,
                111, 114, 101, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63, 115, 112, 61,
                114, 38, 115, 116, 61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 53, 84, 50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61, 50, 48, 50,
                50, 45, 48, 53, 45, 50, 54, 84, 48, 54, 58, 48, 53, 58, 48, 51, 90, 38, 115, 112, 114, 61, 104, 116, 116, 112, 115, 38, 115, 118, 61,
                50, 48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38, 115, 105, 103, 61, 119, 114, 78, 98, 117, 89, 118, 56, 48, 105, 112,
                119, 116, 54, 49, 71, 37, 50, 66, 68, 97, 81, 88, 79, 114, 99, 119, 65, 74, 80, 110, 49, 118, 115, 66, 49, 88, 65, 74, 87, 48, 111, 102,
                99, 56, 37, 51, 68, 32, 45, 45, 99, 111, 110, 116, 101, 110, 116, 61, 104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108,
                111, 98, 46, 99, 111, 114, 101, 46, 119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63,
                115, 112, 61, 114, 38, 115, 116, 61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 53, 84, 50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61,
                50, 48, 50, 50, 45, 48, 53, 45, 50, 54, 84, 48, 54, 58, 48, 53, 58, 48, 51, 90, 38, 115, 112, 114, 61, 104, 116, 116, 112, 115, 38, 115,
                118, 61, 50, 48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38, 115, 105, 103, 61, 119, 114, 78, 98, 117, 89, 118, 56, 48,
                105, 112, 119, 116, 54, 49, 71, 37, 50, 66, 68, 97, 81, 88, 79, 114, 99, 119, 65, 74, 80, 110, 49, 118, 115, 66, 49, 88, 65, 74, 87,
                48, 111, 102, 99, 56, 37, 51, 68
            };

            var obscuredBytes = new List<byte>
            {
                45, 45, 112, 114, 111, 102, 105, 108, 101, 61, 80, 69, 82, 70, 45, 73, 79, 45, 70, 73, 79, 46, 106, 115, 111, 110, 32, 45, 45, 112,
                108, 97, 116, 102, 111, 114, 109, 61, 74, 117, 110, 111, 32, 45, 45, 116, 105, 109, 101, 111, 117, 116, 61, 54, 48, 32, 45, 45, 109,
                101, 116, 97, 100, 97, 116, 97, 61, 97, 103, 101, 110, 116, 73, 100, 61, 97, 109, 122, 48, 55, 112, 114, 100, 97, 112, 112, 48, 51,
                44, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56, 51, 50, 48,
                49, 100, 49, 49, 49, 44, 98, 56, 56, 56, 56, 100, 99, 102, 100, 52, 101, 45, 48, 44, 102, 51, 57, 48, 49, 102, 98, 97, 45, 99, 49, 51,
                55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56, 101, 100, 100, 101, 97, 52, 100, 54, 55, 52, 100, 44, 44, 44, 97, 103, 101, 110,
                116, 84, 121, 112, 101, 61, 71, 117, 101, 115, 116, 65, 103, 101, 110, 116, 44, 44, 44, 99, 111, 110, 116, 97, 105, 110, 101, 114, 73,
                100, 61, 97, 53, 50, 52, 49, 98, 53, 97, 45, 53, 56, 102, 101, 45, 52, 52, 54, 50, 45, 98, 100, 48, 100, 45, 51, 51, 99, 49, 97, 98, 101,
                98, 97, 50, 99, 52, 44, 44, 44, 116, 105, 112, 83, 101, 115, 115, 105, 111, 110, 73, 100, 61, 102, 51, 57, 48, 49, 102, 98, 97, 45, 99,
                49, 51, 55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56, 101, 100, 100, 101, 97, 52, 100, 54, 55, 52, 100, 44, 44, 44, 110, 111, 100,
                101, 73, 100, 61, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56,
                51, 50, 48, 49, 100, 49, 49, 49, 44, 44, 44, 110, 111, 100, 101, 78, 97, 109, 101, 61, 53, 55, 50, 99, 50, 53, 98, 48, 45, 102, 102, 100,
                100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 44, 44, 101, 120, 112, 101, 114,
                105, 109, 101, 110, 116, 73, 100, 61, 51, 54, 55, 98, 99, 50, 99, 49, 45, 57, 56, 99, 48, 45, 52, 97, 54, 101, 45, 98, 53, 100, 55, 45,
                49, 50, 51, 97, 101, 53, 52, 52, 51, 98, 52, 56, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 83, 116, 101, 112, 73, 100,
                61, 100, 51, 52, 101, 52, 49, 50, 55, 45, 54, 52, 99, 56, 45, 52, 50, 54, 100, 45, 57, 54, 54, 57, 45, 49, 100, 57, 98, 49, 49, 50, 55, 49,
                51, 53, 102, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 71, 114, 111, 117, 112, 61, 71, 114, 111, 117, 112, 32, 66, 44,
                44, 44, 103, 114, 111, 117, 112, 73, 100, 61, 71, 114, 111, 117, 112, 32, 66, 44, 44, 44, 118, 105, 114, 116, 117, 97, 108, 77, 97, 99, 104,
                105, 110, 101, 78, 97, 109, 101, 61, 98, 56, 56, 56, 56, 100, 99, 102, 100, 52, 101, 45, 48, 44, 44, 44, 99, 108, 117, 115, 116, 101, 114,
                78, 97, 109, 101, 61, 97, 109, 122, 48, 55, 112, 114, 100, 97, 112, 112, 48, 51, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110,
                116, 84, 121, 112, 101, 61, 81, 111, 83, 44, 44, 44, 103, 101, 110, 101, 114, 97, 116, 105, 111, 110, 61, 71, 101, 110, 54, 44, 44, 44, 110,
                111, 100, 101, 67, 112, 117, 73, 100, 61, 53, 48, 54, 53, 52, 44, 44, 44, 112, 97, 121, 108, 111, 97, 100, 61, 81, 111, 83, 44, 44, 44, 112,
                97, 121, 108, 111, 97, 100, 80, 70, 86, 101, 114, 115, 105, 111, 110, 61, 49, 46, 48, 46, 49, 57, 49, 51, 46, 54, 49, 50, 44, 44, 44, 112, 97,
                121, 108, 111, 97, 100, 84, 121, 112, 101, 61, 81, 111, 83, 44, 44, 44, 112, 97, 121, 108, 111, 97, 100, 86, 101, 114, 115, 105, 111, 110, 61,
                81, 111, 83, 44, 44, 44, 119, 111, 114, 107, 108, 111, 97, 100, 61, 80, 69, 82, 70, 45, 73, 79, 45, 70, 73, 79, 44, 44, 44, 119, 111, 114, 107,
                108, 111, 97, 100, 84, 121, 112, 101, 61, 86, 105, 114, 116, 117, 97, 108, 67, 108, 105, 101, 110, 116, 44, 44, 44, 119, 111, 114, 107, 108,
                111, 97, 100, 86, 101, 114, 115, 105, 111, 110, 61, 49, 46, 48, 46, 49, 57, 49, 51, 46, 54, 50, 48, 44, 44, 44, 105, 109, 112, 97, 99, 116,
                84, 121, 112, 101, 61, 78, 111, 110, 101, 44, 44, 44, 114, 101, 118, 105, 115, 105, 111, 110, 61, 50, 48, 48, 54, 66, 48, 54, 95, 50, 49, 46,
                51, 46, 51, 48, 48, 48, 50, 46, 48, 44, 44, 44, 116, 101, 110, 97, 110, 116, 73, 100, 61, 55, 50, 102, 57, 56, 56, 98, 102, 45, 56, 54, 102,
                49, 45, 52, 49, 97, 102, 45, 57, 49, 97, 98, 45, 50, 100, 55, 99, 100, 48, 49, 49, 100, 98, 52, 55, 44, 44, 44, 101, 110, 97, 98, 108, 101, 68,
                105, 97, 103, 110, 111, 115, 116, 105, 99, 115, 61, 84, 114, 117, 101, 44, 44, 44, 116, 97, 114, 103, 101, 116, 71, 111, 97, 108, 61, 81, 111,
                83, 95, 76, 105, 110, 117, 120, 95, 80, 69, 82, 70, 45, 73, 79, 45, 70, 73, 79, 44, 44, 44, 101, 120, 101, 99, 117, 116, 105, 111, 110, 71, 111,
                97, 108, 61, 81, 111, 83, 95, 76, 105, 110, 117, 100, 50, 50, 57, 99, 52, 48, 99, 45, 49, 51, 53, 53, 45, 52, 56, 57, 99, 45, 97, 56, 48, 101, 45,
                53, 52, 100, 97, 52, 101, 49, 51, 97, 52, 97, 54, 44, 44, 44, 101, 120, 101, 99, 117, 116, 105, 111, 110, 71, 111, 97, 108, 73, 100, 61, 81, 111,
                83, 95, 76, 105, 110, 117, 100, 50, 50, 57, 99, 52, 48, 99, 45, 49, 51, 53, 53, 45, 52, 56, 57, 99, 45, 97, 56, 48, 101, 45, 53, 52, 100, 97, 52,
                101, 49, 51, 97, 52, 97, 54, 44, 44, 44, 111, 119, 110, 101, 114, 61, 118, 97, 115, 97, 108, 64, 109, 105, 99, 114, 111, 115, 111, 102, 116, 46,
                99, 111, 109, 59, 118, 45, 109, 97, 114, 116, 106, 101, 64, 109, 105, 99, 114, 111, 115, 111, 102, 116, 46, 99, 111, 109, 44, 44, 44, 116, 101,
                109, 112, 108, 97, 116, 101, 79, 119, 110, 101, 114, 61, 118, 45, 109, 97, 114, 116, 106, 101, 64, 109, 105, 99, 114, 111, 115, 111, 102, 116, 46,
                99, 111, 109, 44, 44, 44, 118, 101, 114, 115, 105, 111, 110, 61, 50, 48, 50, 49, 45, 48, 49, 45, 48, 49, 44, 44, 44, 116, 101, 97, 109, 78, 97,
                109, 101, 61, 67, 82, 67, 32, 65, 73, 82, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 46, 110, 97, 109, 101, 61, 81, 111, 83,
                95, 76, 105, 110, 117, 120, 44, 44, 44, 100, 97, 116, 101, 99, 114, 101, 97, 116, 101, 100, 61, 48, 54, 47, 48, 56, 47, 50, 48, 50, 49, 44, 44,
                44, 105, 110, 116, 101, 110, 116, 61, 74, 117, 110, 111, 32, 81, 111, 83, 32, 119, 105, 116, 104, 32, 76, 105, 110, 117, 120, 32, 83, 117, 112,
                112, 111, 114, 116, 32, 86, 97, 108, 105, 100, 97, 116, 105, 111, 110, 44, 44, 44, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 67, 97, 116,
                101, 103, 111, 114, 121, 61, 65, 66, 44, 44, 44, 109, 111, 110, 105, 116, 111, 114, 105, 110, 103, 69, 110, 97, 98, 108, 101, 100, 61, 84, 114,
                117, 101, 44, 44, 44, 116, 101, 109, 112, 108, 97, 116, 101, 73, 100, 61, 81, 111, 83, 95, 76, 105, 110, 117, 120, 46, 84, 101, 109, 112, 108, 97,
                116, 101, 46, 118, 50, 46, 106, 115, 111, 110, 32, 45, 45, 108, 97, 121, 111, 117, 116, 80, 97, 116, 104, 61, 47, 104, 111, 109, 101, 47, 106, 117,
                110, 111, 118, 109, 97, 100, 109, 105, 110, 47, 110, 117, 103, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 47, 118, 105, 114, 116, 117, 97,
                108, 99, 108, 105, 101, 110, 116, 47, 49, 46, 48, 46, 49, 57, 49, 51, 46, 54, 50, 48, 47, 99, 111, 110, 116, 101, 110, 116, 47, 108, 105, 110, 117,
                120, 45, 120, 54, 52, 47, 108, 97, 121, 111, 117, 116, 46, 106, 115, 111, 110, 32, 45, 45, 115, 101, 101, 100, 61, 50, 51, 56, 56, 54, 52, 55, 50,
                49, 32, 45, 45, 97, 103, 101, 110, 116, 73, 100, 61, 97, 109, 122, 48, 55, 112, 114, 100, 97, 112, 112, 48, 51, 44, 53, 55, 50, 99, 50, 53, 98, 48,
                45, 102, 102, 100, 100, 45, 52, 48, 102, 101, 45, 98, 57, 48, 55, 45, 54, 57, 97, 56, 51, 50, 48, 49, 100, 49, 49, 49, 44, 98, 56, 56, 56, 56, 100,
                99, 102, 100, 52, 101, 45, 48, 44, 102, 51, 57, 48, 49, 102, 98, 97, 45, 99, 49, 51, 55, 45, 52, 97, 48, 102, 45, 56, 49, 56, 48, 45, 56, 101, 100,
                100, 101, 97, 52, 100, 54, 55, 52, 100, 32, 45, 45, 101, 120, 112, 101, 114, 105, 109, 101, 110, 116, 73, 100, 61, 51, 54, 55, 98, 99, 50, 99, 49,
                45, 57, 56, 99, 48, 45, 52, 97, 54, 101, 45, 98, 53, 100, 55, 45, 49, 50, 51, 97, 101, 53, 52, 52, 51, 98, 52, 56, 32, 45, 45, 112, 97, 99, 107, 97,
                103, 101, 115, 61, 104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108, 111, 98, 46, 99, 111, 114, 101, 46, 119, 105, 110, 100, 111, 119,
                115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63, 115, 112, 61, 114, 38, 115, 116, 61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 53, 84,
                50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 54, 84, 48, 54, 58, 48, 53, 58, 48, 51, 90, 38, 115, 112,
                114, 61, 104, 116, 116, 112, 115, 38, 115, 118, 61, 50, 48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38, 115, 105, 103, 61, 46, 46, 46,
                32, 45, 45, 99, 111, 110, 116, 101, 110, 116, 61, 104, 116, 116, 112, 115, 58, 47, 47, 97, 110, 121, 46, 98, 108, 111, 98, 46, 99, 111, 114, 101, 46,
                119, 105, 110, 100, 111, 119, 115, 46, 110, 101, 116, 47, 112, 97, 99, 107, 97, 103, 101, 115, 63, 115, 112, 61, 114, 38, 115, 116, 61, 50, 48, 50,
                50, 45, 48, 53, 45, 50, 53, 84, 50, 50, 58, 48, 53, 58, 48, 51, 90, 38, 115, 101, 61, 50, 48, 50, 50, 45, 48, 53, 45, 50, 54, 84, 48, 54, 58, 48, 53,
                58, 48, 51, 90, 38, 115, 112, 114, 61, 104, 116, 116, 112, 115, 38, 115, 118, 61, 50, 48, 50, 48, 45, 48, 56, 45, 48, 52, 38, 115, 114, 61, 99, 38,
                115, 105, 103, 61, 46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }
    }
}
