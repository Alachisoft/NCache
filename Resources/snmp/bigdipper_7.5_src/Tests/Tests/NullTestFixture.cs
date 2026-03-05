/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/3
 * Time: 20:26
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

#pragma warning disable 1591, 0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class NullTestFixture
    {
        [Test]
        public void TestMethod()
        {
            Assert.AreEqual(false, new Null().Equals(null));
        }
        
        [Test]
        public void TestToBytes()
        {
            Assert.AreEqual(new byte[] { 0x05, 0x00 }, new Null().ToBytes());
            Assert.AreEqual(0, new Null().GetHashCode());
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new Null();
            var right = new Null();
            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left != null);
            // ReSharper disable EqualExpressionComparison
            Assert.IsTrue(left == left);
            // ReSharper restore EqualExpressionComparison
            Assert.Throws<ArgumentNullException>(() => left.AppendBytesTo(null));
            Assert.AreEqual("Null", left.ToString());
        }
    }
}
#pragma warning restore 1591, 0618,1718