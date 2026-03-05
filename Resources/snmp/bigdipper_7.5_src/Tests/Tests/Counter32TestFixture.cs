using System;
using System.IO;
using NUnit.Framework;

#pragma warning disable 1591,0618, 1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class Counter32TestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(()=> new Counter32(0).AppendBytesTo(null));
        }
        
        [Test]
        public void TestEqual()
        {
            var left = new Counter32(100);
            var right = new Counter32(100);
            Assert.AreEqual(left, right);
        }
        
        [Test]
        public void TestConstructor()
        {
            byte[] buffer2 = new byte[] {01, 44};
            Counter32 c2 = new Counter32(buffer2);
            Assert.AreEqual(300, c2.ToUInt32());
            Assert.AreEqual("300", c2.ToString());
            Counter32 sec = new Counter32(300);
            Counter32 thr = new Counter32(301);
            Assert.IsTrue(c2.Equals(sec));
            Assert.AreEqual(300.GetHashCode(), c2.GetHashCode());
            Assert.IsTrue(c2 == sec);
            Assert.IsTrue(c2 != thr);
            Assert.IsFalse(c2 == null);
            Assert.IsFalse(null == sec);
            Assert.IsTrue(c2 == c2);
            
            byte[] buffer1 = new byte[] {13};
            Counter32 c1 = new Counter32(buffer1);
            Assert.AreEqual(13, c1.ToUInt32());
            
            byte[] buffer3 = new byte[] {1, 17, 112};
            Counter32 c3 = new Counter32(buffer3);
            Assert.AreEqual(70000, c3.ToUInt32());
            
            byte[] buffer4 = new byte[] {1, 201, 195, 128};
            Counter32 c4 = new Counter32(buffer4);
            Assert.AreEqual(30000000, c4.ToUInt32());
            
            byte[] buffer5 = new byte[] {0, 255, 255, 255, 255};
            Counter32 c5 = new Counter32(buffer5);
            Assert.AreEqual(uint.MaxValue, c5.ToUInt32());
            
            byte[] buffer0 = new byte[] {0};
            Counter32 c0 = new Counter32(buffer0);
            Assert.AreEqual(uint.MinValue, c0.ToUInt32());
        }
        
        [Test]
        public void TestConstructor2()
        {
            Counter32 test = new Counter32(300);
            Assert.AreEqual(300, test.ToUInt32());
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestContructor3()
        {
            new Counter32(new Tuple<int, byte[]>(1, new byte[] { 1 }), null);
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestConstructor4()
        {
            new Counter32(new Tuple<int, byte[]>(0, new byte[] { 0 }), new MemoryStream());
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestConstructor5()
        {
            new Counter32(new Tuple<int, byte[]>(6, new byte[] { 6 }), new MemoryStream());
        }
        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TestConstructor6()
        {
            byte[] buffer5 = new byte[] {3, 255, 255, 255, 255};
            new Counter32(buffer5);
        }
        
        [Test]
        public void TestToBytes()
        {
            Counter32 c0 = new Counter32(0);
            Counter32 r0 = (Counter32)DataFactory.CreateSnmpData(c0.ToBytes());
            Assert.AreEqual(r0, c0);
            
            Counter32 c5 = new Counter32(uint.MaxValue);
            Counter32 r5 = (Counter32)DataFactory.CreateSnmpData(c5.ToBytes());
            Assert.AreEqual(r5, c5);
            
            Counter32 c4 = new Counter32(30000000);
            Counter32 r4 = (Counter32)DataFactory.CreateSnmpData(c4.ToBytes());
            Assert.AreEqual(r4, c4);
            
            Counter32 c3 = new Counter32(70000);
            Counter32 r3 = (Counter32)DataFactory.CreateSnmpData(c3.ToBytes());
            Assert.AreEqual(r3, c3);
            
            Counter32 c1 = new Counter32(13);
            Counter32 r1 = (Counter32)DataFactory.CreateSnmpData(c1.ToBytes());
            Assert.AreEqual(r1, c1);
            
            Counter32 c2 = new Counter32(300);
            Counter32 r2 = (Counter32)DataFactory.CreateSnmpData(c2.ToBytes());
            Assert.AreEqual(r2, c2);
            
            Counter32 c255 = new Counter32(255);
            Assert.AreEqual(new byte[] {0x41, 0x02, 0x00, 0xff}, c255.ToBytes());
            
            Assert.AreEqual(new byte[] {0x41, 0x01, 0x04}, new Counter32(4).ToBytes());
        }
    }
}
#pragma warning restore 1591,0618, 1718