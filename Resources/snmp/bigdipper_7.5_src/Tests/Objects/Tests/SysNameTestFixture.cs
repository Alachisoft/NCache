using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysNameTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysName();
            Assert.Throws<ArgumentNullException>(() => sys.Data = null);
            Assert.Throws<ArgumentException>(() => sys.Data = new TimeTicks(0));
            sys.Data = OctetString.Empty;
            Assert.AreEqual(OctetString.Empty, sys.Data);
        }
    }
}
