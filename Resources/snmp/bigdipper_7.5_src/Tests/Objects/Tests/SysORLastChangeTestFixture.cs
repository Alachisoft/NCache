/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/11
 * Time: 11:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using Lextm.SharpSnmpLib.Pipeline;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysORLastChangeTestFixture
    {
        [Test]
        public void Test()
        {
            var sys = new SysORLastChange();
            Assert.AreEqual(new TimeTicks(0), sys.Data);
            Assert.Throws<AccessFailureException>(() => sys.Data = OctetString.Empty);
        }
    }
}
