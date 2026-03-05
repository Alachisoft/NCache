using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class HeaderTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new Header(null));
            Assert.Throws<ArgumentNullException>(() => new Header(null, null, 0));
            Assert.Throws<ArgumentNullException>(() => new Header(new Integer32(0), null, 0));
            Assert.AreEqual("Header: messageId: 0;maxMessageSize: 0;securityBits: 0x00;securityModel: 3", new Header(new Integer32(0), new Integer32(0), 0).ToString());
        }
    }
}
