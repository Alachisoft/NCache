/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/9/30
 * Time: 11:04
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Mib.Tests
{
    [TestFixture]
    public class ObjectTreeTestFixture
    {
        [Test]
        public void TestExtractValue()
        {
            const string test = "org(3)";
            Assert.AreEqual(3, ObjectTree.ExtractValue(test));
        }

        [Test]
        public void TestExtractName()
        {
            const string test = "org(3)";
            Assert.AreEqual("org", ObjectTree.ExtractName(test));
            
            const string test1 = "iso";
            Assert.AreEqual("iso", ObjectTree.ExtractName(test1));
        }
    }
}
