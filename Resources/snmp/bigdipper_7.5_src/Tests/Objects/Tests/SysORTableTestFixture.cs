/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/11
 * Time: 11:04
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Objects.Tests
{
    [TestFixture]
    public class SysORTableTestFixture
    {
        [Test]
        public void Test()
        {
            var table = new SysORTable();
            Assert.AreEqual(null, table.MatchGet(new ObjectIdentifier("1.3.6")));
            var id = new ObjectIdentifier("1.3.6.1.2.1.1.9.1.1.1");
            Assert.AreEqual(id, table.MatchGet(id).Variable.Id);
            Assert.AreEqual(new ObjectIdentifier("1.3.6.1.2.1.1.9.1.1.2"), table.MatchGetNext(id).Variable.Id);
        }
    }
}
