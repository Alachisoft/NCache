/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/8/3
 * Time: 12:57
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using NUnit.Framework;

#pragma warning disable 1591,0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class Counter64TestFixture
    {
        [Test]
        public void TestContructor()
        {
            var counter64 = new Counter64(new byte[] {0x00, 0xC9, 0xAC, 0xC1, 0x87, 0x4B, 0xB1, 0xE1, 0xC9});
            Assert.AreEqual(14532202884452442569, counter64.ToUInt64());
            Assert.AreEqual(14532202884452442569.GetHashCode(), counter64.GetHashCode());
            Assert.AreEqual("14532202884452442569", counter64.ToString());

            Assert.Throws<ArgumentNullException>(() => new Counter64(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentException>(() => new Counter64(new Tuple<int, byte[]>(-1, new[] { (byte)255 }), new MemoryStream()));
            Assert.Throws<ArgumentException>(() => new Counter64(new Tuple<int, byte[]>(10, new byte[]{10}), new MemoryStream()));
            Assert.Throws<ArgumentException>(
                () => new Counter64(new byte[] {0x05, 0xC9, 0xAC, 0xC1, 0x87, 0x4B, 0xB1, 0xE1, 0xC9}));

            var small = new Counter64(new byte[] { 0x00, 0xC9, 0xAC, 0xC1, 0x87 });
            Assert.AreEqual(3383542151, small.ToUInt64());

            Assert.Throws<ArgumentNullException>(() => new Counter64(0).AppendBytesTo(null));
        }
        
        [Test]
        public void TestToBytes()
        {
            Assert.AreEqual(new byte[] {0x46, 0x09, 0x00, 0xC9, 0xAC, 0xC1, 0x87, 0x4B, 0xB1, 0xE1, 0xC9}, new Counter64(14532202884452442569).ToBytes());
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new Counter64(673737665);
            var right = new Counter64(673737665);
            Assert.AreEqual(left, right);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(left == left);
// ReSharper restore EqualExpressionComparison
            Assert.IsTrue(left != null);
            Assert.IsTrue(null != right);
            Assert.IsTrue(left.Equals(right));
        }
    }
}
#pragma warning restore 1591,0618,1718