/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/2/15
 * Time: 20:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using NUnit.Framework;
#pragma warning disable 1591, 0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class NoSuchObjectTestFixture
    {
        [Test]
        public void TestToBytes()
        {
            NoSuchObject obj = new NoSuchObject();
            Assert.AreEqual(new byte[] { 0x80, 0x00 }, obj.ToBytes());
            Assert.AreEqual(0, obj.GetHashCode());
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new NoSuchObject();
            var right = new NoSuchObject();
            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(left == left);
// ReSharper restore EqualExpressionComparison
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left != null);

            Assert.Throws<ArgumentNullException>(() => left.AppendBytesTo(null));
            Assert.AreEqual("NoSuchObject", left.ToString());
        }
    }
}
#pragma warning restore 1591, 0618,1718