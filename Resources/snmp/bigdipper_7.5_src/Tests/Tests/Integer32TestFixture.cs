/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/8/3
 * Time: 10:01
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using NUnit.Framework;
#pragma warning disable 1591,0618
namespace Lextm.SharpSnmpLib.Tests
{
    /// <summary>
    /// Description of TestInteger32.
    /// </summary>
    [TestFixture]
    public class Integer32TestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new Integer32(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentException>(() => new Integer32(new Tuple<int, byte[]>(0, new byte[] { 0 }), new MemoryStream()));
            Assert.Throws<ArgumentException>(() => new Integer32(new Tuple<int, byte[]>(-1, new[] { (byte)255 }), new MemoryStream()));
            Assert.Throws<ArgumentException>(() => new Integer32(new Tuple<int, byte[]>(6, new byte[] { 6 }), new MemoryStream()));
            Assert.Throws<ArgumentNullException>(() => new Integer32(6).AppendBytesTo(null));
        }

        [Test]
        public void TestEqual()
        {
            var left = new Integer32(599);
            var right = new Integer32(599);
            Assert.AreEqual(left, right);
// ReSharper disable RedundantCast
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue((Integer32)null == (Integer32)null);
// ReSharper restore EqualExpressionComparison
// ReSharper restore RedundantCast
            Assert.AreNotEqual(null, right);
            Assert.AreNotEqual(left, null);
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left != null);
        }
        
        [Test]
        public void TestNegative()
        {
            const int i = -2147418240;
            Integer32 data = new Integer32(i);
            byte[] bytes = data.ToBytes();
            Integer32 other = (Integer32)DataFactory.CreateSnmpData(bytes);
            Assert.AreEqual(i, other.ToInt32());
        }
        
        [Test]
        public void TestConstructor()
        {
            Integer32 test = new Integer32(100);
            Assert.AreEqual(100, test.ToInt32());
            Assert.Throws<InvalidCastException>(() => test.ToErrorCode());
            Assert.Throws<InvalidCastException>(() => new Integer32(-1).ToErrorCode());
            
            Integer32 test2 = new Integer32(new byte[] {0x00});
            Assert.AreEqual(0, test2.ToInt32());
            
            Integer32 test3 = new Integer32(new byte[] {0xFF});
            Assert.AreEqual(-1, test3.ToInt32());
        }
        
        [Test]
        public void TestToInt32()
        {
            const int result = -26955;
            byte[] expected = new byte[] {0x96, 0xB5};
            Integer32 test = new Integer32(expected);
            Assert.AreEqual(result, test.ToInt32());
            
            Assert.AreEqual(255, new Integer32(new byte[] {0x00, 0xFF}).ToInt32());
        }
        
        [Test]
        public void TestToBytes()
        {
            byte[] bytes = new byte[] {0x02, 0x02, 0x96, 0xB5};
            Integer32 test = new Integer32(-26955);
            Assert.AreEqual(bytes, test.ToBytes());
            
            Assert.AreEqual(new byte[] {0x02, 0x02, 0x00, 0xFF}, new Integer32(255).ToBytes());
            
            Assert.AreEqual(6, new Integer32(2147483647).ToBytes().Length);
        }
        
        [Test]
        public void TestToBytes2()
        {
            Integer32 i = new Integer32(-1);
            Assert.AreEqual(new byte[] {0x02, 0x01, 0xFF}, i.ToBytes());
        }
        
        [Test]
        public void TestNegative2()
        {
            // bug 7217 http://sharpsnmplib.codeplex.com/workitem/7217
            Integer32 i = new Integer32(-250);
            var result = DataFactory.CreateSnmpData(i.ToBytes());
            Assert.AreEqual(SnmpType.Integer32, result.TypeCode);
            Assert.AreEqual(-250, ((Integer32)result).ToInt32());
        }
    }
}
#pragma warning restore 1591,0618