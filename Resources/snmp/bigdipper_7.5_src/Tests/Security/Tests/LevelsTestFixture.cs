using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Security.Tests
{
    [TestFixture]
    public class LevelsTestFixture
    {
        [Test]
        public void TestToSecurityLevel()
        {
            Assert.AreEqual((Levels)0, DefaultPrivacyProvider.DefaultPair.ToSecurityLevel());
        }

        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentException>(delegate { new DESPrivacyProvider(new OctetString(""), DefaultAuthenticationProvider.Instance); });
        }

        [Test]
        public void TestAuthenticationOnly()
        {
            Assert.AreEqual(Levels.Authentication, new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("test"))).ToSecurityLevel());
        }
    }
}
