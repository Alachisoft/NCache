using Lextm.SharpSnmpLib.Mib;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests.Mib
{
    [TestFixture]
    public class NumberLiteralValueTestFixture
    {
        [Test]
        public void Test()
        {
            var v = new NumberLiteralValue(9223372036854775808);
            Assert.IsNull(v.Value);
            Assert.AreEqual(9223372036854775808, v.UnsignedValue);
        }

        [Test]
        public void Test2()
        {
            var v = new NumberLiteralValue(9223372036854775807);
            Assert.IsNull(v.UnsignedValue);
            Assert.AreEqual(9223372036854775807, v.Value);
        }
    }
}
