/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/8/3
 * Time: 12:25
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

#pragma warning disable 1591,0618
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class OpaqueTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<NullReferenceException>(()=>new Opaque(null));
        }

        [Test]
        public void TestConstructor()
        {
            Gauge32 e = new Gauge32(3);
            Opaque test = new Opaque(e.ToBytes());
            Assert.AreEqual(new byte[] {0x44, 0x03, 0x42, 0x01, 0x03}, test.ToBytes());
            Assert.AreEqual("42 01 03", test.ToString());
            Assert.Throws<ArgumentNullException>(() => test.AppendBytesTo(null));
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new Opaque(new byte[] { 0x80, 0x77 });
            var right = new Opaque(new byte[] { 0x80, 0x77 });
            Assert.AreEqual(left, right);
// ReSharper disable RedundantCast
            Assert.AreEqual((Opaque)null, (Opaque)null);
// ReSharper restore RedundantCast
            Assert.AreNotEqual(null, right);
            Assert.AreNotEqual(left, null);
            Assert.IsTrue(left != null);
            Assert.IsTrue(null != right);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue((Opaque)null == (Opaque)null);
// ReSharper restore EqualExpressionComparison
            Assert.IsTrue(left.Equals(right));
        }
    }
}
#pragma warning restore 1591,0618