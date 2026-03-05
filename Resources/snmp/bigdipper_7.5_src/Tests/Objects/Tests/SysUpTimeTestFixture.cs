using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysUpTimeTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysUpTime();
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
