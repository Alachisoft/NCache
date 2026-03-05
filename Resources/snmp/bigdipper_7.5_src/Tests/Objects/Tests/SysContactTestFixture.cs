using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysContactTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysContact();
            Assert.Throws<ArgumentNullException>(() => sys.Data = null);
            Assert.Throws<ArgumentException>(() => sys.Data = new TimeTicks(0));
        }
    }
}
