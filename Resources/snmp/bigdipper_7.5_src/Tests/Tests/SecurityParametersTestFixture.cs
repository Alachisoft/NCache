using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class SecurityParametersTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new SecurityParameters(null));
            Assert.Throws<ArgumentNullException>(() => new SecurityParameters(null, null, null, null, null, null));
        }
        
        [Test]
        public void TestToString()
        {
            Assert.AreEqual("Security parameters: engineId: ;engineBoots: ;engineTime: ;userName: test; authen hash: ; privacy hash: ", SecurityParameters.Create(new OctetString("test")).ToString());
        }
    }
}
