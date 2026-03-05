using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysORDescrTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysORDescr(3, new OctetString("test"));
            Assert.AreEqual(".1.3.6.1.2.1.1.9.1.3.3", sys.Variable.Id.ToString());
            Assert.AreEqual("test", sys.Data.ToString());
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
