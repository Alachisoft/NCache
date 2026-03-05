/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/5
 * Time: 14:55
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Security.Tests
{
    [TestFixture]
    public class SaltGeneratorTestFixture
    {
        [Test]
        public void Test()
        {
            var gen = new SaltGenerator();
            var first = gen.GetSaltBytes();
            var second = gen.GetSaltBytes();
            Assert.AreNotEqual(first, second);
            Assert.AreEqual("Salt generator", gen.ToString());
            
            gen.SetSalt(long.MaxValue);
            Assert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }, gen.GetSaltBytes());
        }
    }
}
