using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysORIDTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysORID(3, new ObjectIdentifier(".1.3.6.1.2.1.1.9.1.2.3"));
            Assert.AreEqual(".1.3.6.1.2.1.1.9.1.2.3", sys.Variable.Id.ToString());
            Assert.AreEqual(".1.3.6.1.2.1.1.9.1.2.3", sys.Data.ToString());
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
