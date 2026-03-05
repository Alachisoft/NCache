using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysServicesTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysServices();
            Assert.AreEqual(new Integer32(72), sys.Data);
            Assert.Throws<AccessFailureException>(() => sys.Data = new TimeTicks(0));
        }
    }
}
