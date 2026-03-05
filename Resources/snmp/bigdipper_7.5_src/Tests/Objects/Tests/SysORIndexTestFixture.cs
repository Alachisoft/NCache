using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysORIndexTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysORIndex(3);
            Assert.AreEqual(".1.3.6.1.2.1.1.9.1.1.3", sys.Variable.Id.ToString());
            Assert.AreEqual("3", sys.Data.ToString());
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
