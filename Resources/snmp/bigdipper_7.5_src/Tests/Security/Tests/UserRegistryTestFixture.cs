/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/5
 * Time: 14:45
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Security.Tests
{
    [TestFixture]
    public class UserRegistryTestFixture
    {
        [Test]
        public void Test()
        {
            var users = new UserRegistry(new User[] {new User(new OctetString("test"), DefaultPrivacyProvider.DefaultPair)});
            Assert.AreEqual(1, users.Count);
            Assert.IsNotNull(users.Find(new OctetString("test")));
            Assert.AreEqual(1, users.Add(null).Count);
            Assert.AreEqual(2, users.Add(new User(new OctetString("test2"), DefaultPrivacyProvider.DefaultPair)).Count);
            Assert.AreEqual(2, users.Add(new User(new OctetString("test2"), DefaultPrivacyProvider.DefaultPair)).Count);
            Assert.Throws<ArgumentNullException>(() => users.Find(null));
            Assert.AreEqual("User registry: count: 2", users.ToString());
        }
    }
}
