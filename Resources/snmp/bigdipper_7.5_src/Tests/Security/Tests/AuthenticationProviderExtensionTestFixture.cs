/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/5
 * Time: 15:04
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Security.Tests
{
    [TestFixture]
    public class AuthenticationProviderExtensionTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => AuthenticationProviderExtension.ComputeHash(null, VersionCode.V1, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => AuthenticationProviderExtension.ComputeHash(DefaultAuthenticationProvider.Instance, VersionCode.V1, null, null, null, null));
            Assert.Throws<ArgumentNullException>(() => AuthenticationProviderExtension.ComputeHash(DefaultAuthenticationProvider.Instance, VersionCode.V1, Header.Empty, null, null, null));
            Assert.Throws<ArgumentNullException>(() => AuthenticationProviderExtension.ComputeHash(DefaultAuthenticationProvider.Instance, VersionCode.V1, Header.Empty, SecurityParameters.Create(new OctetString("test")), null, null));
            Assert.Throws<ArgumentNullException>(() => DefaultAuthenticationProvider.Instance.ComputeHash(VersionCode.V1, Header.Empty, SecurityParameters.Create(new OctetString("test")), new Scope(OctetString.Empty, OctetString.Empty, new MalformedPdu()), null));
            
            var parameters = SecurityParameters.Create(new OctetString("test"));
            DefaultAuthenticationProvider.Instance.ComputeHash(VersionCode.V1, Header.Empty, parameters, new Scope(OctetString.Empty, OctetString.Empty, new MalformedPdu()), DefaultPrivacyProvider.DefaultPair);
            Assert.AreEqual(null, parameters.AuthenticationParameters);
            
            //Assert.Throws<ArgumentNullException>(() => AuthenticationProviderExtension.VerifyHash(null, VersionCode.V1, null, null, null, null));
            //Assert.Throws<ArgumentNullException>(() => DefaultAuthenticationProvider.Instance.VerifyHash(VersionCode.V1, null, null, null, null));
            //Assert.Throws<ArgumentNullException>(() => DefaultAuthenticationProvider.Instance.VerifyHash(VersionCode.V1, Header.Empty, null, null, null));
            //Assert.Throws<ArgumentNullException>(() => DefaultAuthenticationProvider.Instance.VerifyHash(VersionCode.V1, Header.Empty, SecurityParameters.Create(new OctetString("test")), null, null));
            //Assert.Throws<ArgumentNullException>(() => DefaultAuthenticationProvider.Instance.VerifyHash(VersionCode.V1, Header.Empty, SecurityParameters.Create(new OctetString("test")), new MalformedPdu(), null));
            //Assert.IsTrue(DefaultAuthenticationProvider.Instance.VerifyHash(VersionCode.V1, Header.Empty, SecurityParameters.Create(new OctetString("test")), new MalformedPdu(), DefaultPrivacyProvider.DefaultPair));
        }
    }
}
