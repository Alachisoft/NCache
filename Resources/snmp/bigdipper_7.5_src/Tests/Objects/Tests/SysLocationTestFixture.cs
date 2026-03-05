using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysLocationTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysLocation();
            Assert.Throws<ArgumentNullException>(() => sys.Data = null);
            Assert.Throws<ArgumentException>(() => sys.Data = new TimeTicks(0));
            sys.Data = OctetString.Empty;
            Assert.AreEqual(OctetString.Empty, sys.Data);
        }
    }
}
