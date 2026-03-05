/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/8/3
 * Time: 10:46
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
#pragma warning disable 1591, 0618
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class ByteToolTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => ByteTool.GetRawBytes(null, true));
            Assert.Throws<ArgumentNullException>(() => ByteTool.ConvertDecimal(null));
            Assert.Throws<ArgumentNullException>(() => ByteTool.Convert((byte[])null));
            Assert.Throws<ArgumentNullException>(() => ByteTool.ParseItems(null));
            Assert.Throws<ArgumentException>(() => ByteTool.ParseItems((ISnmpData)null));
            Assert.Throws<ArgumentNullException>(() => ByteTool.ParseItems((IEnumerable<ISnmpData>)null));
            Assert.Throws<ArgumentNullException>(() => ByteTool.Convert((string)null));
            Assert.Throws<ArgumentException>(() => ByteTool.Convert("**"));
            Assert.Throws<ArgumentException>(() => ByteTool.Convert("8AB"));
        }

        [Test]
        public void TestConvertDecimal()
        {
            byte[] b = ByteTool.ConvertDecimal(" 16 18 ");
            Assert.AreEqual(new byte[] { 0x10, 0x12 }, b);
        }

        [Test]
        public void TestReadShortLength()
        {
            MemoryStream m = new MemoryStream();
            m.WriteByte(0x66);
            m.Flush();
            m.Position = 0;
            Assert.AreEqual(new Tuple<int, byte[]>(102, new byte[] {0x66}), m.ReadPayloadLength());
        }
        
        [Test]
        public void TestWriteShortLength()
        {
            const int length = 102;
            const byte expect = 0x66;
            var array = length.WritePayloadLength();
            Assert.AreEqual(1, array.Length);
            Assert.AreEqual(expect, array[0]);
        }
        
        [Test]
        public void TestReadLongLength()
        {
            byte[] expected = new byte[] {0x83, 0x73, 0x59, 0xB5};
            MemoryStream m = new MemoryStream();
            m.Write(expected, 0, 4);
            m.Flush();
            m.Position = 0;
            Assert.AreEqual(7559605, m.ReadPayloadLength().Item1);
        }
        
        [Test]
        public void TestWriteLongLength()
        {
            const int length = 7559605;
            byte[] expected = new byte[] {0x83, 0x73, 0x59, 0xB5};
            MemoryStream m = new MemoryStream();
            var array = length.WritePayloadLength();
            Assert.AreEqual(expected, array);
        }
    }
}
#pragma warning restore 1591, 0618