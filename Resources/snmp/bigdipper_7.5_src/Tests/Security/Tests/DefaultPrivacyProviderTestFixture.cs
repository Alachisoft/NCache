/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/5
 * Time: 15:33
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Security.Tests
{
    [TestFixture]
    public class DefaultPrivacyProviderTestFixture
    {
        [Test]
        public void Test()
        {
            var provider = DefaultPrivacyProvider.DefaultPair;
            Assert.Throws<ArgumentNullException>(() => provider.Encrypt(null, null));
            Assert.Throws<ArgumentNullException>(() => provider.Encrypt(OctetString.Empty, null));
            Assert.Throws<ArgumentException>(() => provider.Encrypt(new Null(), SecurityParameters.Create(OctetString.Empty)));

            var expected = new Sequence((byte[])null);
            Assert.AreEqual(expected, provider.Encrypt(expected, SecurityParameters.Create(OctetString.Empty)));
            
            Assert.Throws<ArgumentNullException>(() => provider.Decrypt(null, null));
            Assert.Throws<ArgumentNullException>(() => provider.Decrypt(OctetString.Empty, null));
            var result = provider.Decrypt(new Sequence((byte[])null), SecurityParameters.Create(OctetString.Empty));
            Assert.IsNotNull(result);
        }
    }
}
