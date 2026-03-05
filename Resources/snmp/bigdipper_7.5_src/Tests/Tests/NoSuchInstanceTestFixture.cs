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
    public class NoSuchInstanceTestFixture
    {
        [Test]
        public void TestToBytes()
        {
            NoSuchInstance obj = new NoSuchInstance();
            Assert.AreEqual(new byte[] { 0x81, 0x00 }, obj.ToBytes());
            Assert.AreEqual(0, obj.GetHashCode());
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new NoSuchInstance();
            var right = new NoSuchInstance();
            Assert.AreEqual(left, right);
            Assert.IsTrue(left == right);
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left != null);
            // ReSharper disable EqualExpressionComparison
            Assert.IsTrue(left == left);
            // ReSharper restore EqualExpressionComparison

            Assert.Throws<ArgumentNullException>(() => left.AppendBytesTo(null));
            Assert.AreEqual("NoSuchInstance", left.ToString());
        }
    }
}
#pragma warning restore 1591, 0618,1718