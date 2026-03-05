/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/10
 * Time: 16:14
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;
#pragma warning disable 1591,0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class Gauge32TestFixture
    {
        [Test]
        public void TestEqual()
        {
            var left = new Gauge32(200);
            var right = new Gauge32(200);
            Assert.AreEqual(left, right);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(left == left);
// ReSharper restore EqualExpressionComparison
            Assert.IsFalse(left == null);
            Assert.IsFalse(null == right);
            Assert.IsTrue(left != null);
            Assert.IsTrue(left.Equals(right));
            Assert.AreEqual(((uint)200).GetHashCode(), left.GetHashCode());
            Assert.AreEqual("200", left.ToString());

            Assert.Throws<ArgumentNullException>(() => left.AppendBytesTo(null));
            Assert.Throws<ArgumentNullException>(() => new Gauge32(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
        }
        
        [Test]
        public void TestConstructor()
        {
            byte[] buffer2 = new byte[] {01, 44};
            Gauge32 c2 = new Gauge32(buffer2);
            Assert.AreEqual(300, c2.ToUInt32());

            
            byte[] buffer1 = new byte[] {13};
            Gauge32 c1 = new Gauge32(buffer1);
            Assert.AreEqual(13, c1.ToUInt32());
            
            byte[] buffer3 = new byte[] {1, 17, 112};
            Gauge32 c3 = new Gauge32(buffer3);
            Assert.AreEqual(70000, c3.ToUInt32());
            
            byte[] buffer4 = new byte[] {1, 201, 195, 128};
            Gauge32 c4 = new Gauge32(buffer4);
            Assert.AreEqual(30000000, c4.ToUInt32());
            
            byte[] buffer5 = new byte[] {0, 255, 255, 255, 255};
            Gauge32 c5 = new Gauge32(buffer5);
            Assert.AreEqual(uint.MaxValue, c5.ToUInt32());
            
            byte[] buffer0 = new byte[] {0};
            Gauge32 c0 = new Gauge32(buffer0);
            Assert.AreEqual(uint.MinValue, c0.ToUInt32());
        }
        
        [Test]
        public void TestToBytes()
        {
            Gauge32 c0 = new Gauge32(0);
            Gauge32 r0 = (Gauge32)DataFactory.CreateSnmpData(c0.ToBytes());
            Assert.AreEqual(r0, c0);
            
            Gauge32 c5 = new Gauge32(uint.MaxValue);
            Gauge32 r5 = (Gauge32)DataFactory.CreateSnmpData(c5.ToBytes());
            Assert.AreEqual(r5, c5);
            
            Gauge32 c4 = new Gauge32(30000000);
            Gauge32 r4 = (Gauge32)DataFactory.CreateSnmpData(c4.ToBytes());
            Assert.AreEqual(r4, c4);
            
            Gauge32 c3 = new Gauge32(70000);
            Gauge32 r3 = (Gauge32)DataFactory.CreateSnmpData(c3.ToBytes());
            Assert.AreEqual(r3, c3);
            
            Gauge32 c1 = new Gauge32(13);
            Gauge32 r1 = (Gauge32)DataFactory.CreateSnmpData(c1.ToBytes());
            Assert.AreEqual(r1, c1);
            
            Gauge32 c2 = new Gauge32(300);
            Gauge32 r2 = (Gauge32)DataFactory.CreateSnmpData(c2.ToBytes());
            Assert.AreEqual(r2, c2);
            
            Assert.AreEqual(new byte[] {0x42, 0x01, 0x03}, new Gauge32(3).ToBytes());
            Assert.AreEqual(new byte[] {0x42, 0x05, 0x00, 0x80, 0x00, 0x00, 0x00}, new Gauge32(2147483648).ToBytes());
        }
    }
}
#pragma warning restore 1591,0618,1718