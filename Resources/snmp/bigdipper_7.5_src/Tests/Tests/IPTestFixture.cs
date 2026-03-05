/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 11:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Net;
using NUnit.Framework;

#pragma warning disable 1591,0618,1718
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class IPTestFixture
    {
        [Test]
        public void TestException()
        {
            var test = new IP(IPAddress.Any);
            Assert.Throws<ArgumentNullException>(() => test.AppendBytesTo(null));
            Assert.Throws<ArgumentNullException>(() => new IP(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentNullException>(() => new IP((IPAddress) null));
            Assert.Throws<ArgumentException>(() => new IP(new Tuple<int, byte[]>(1, new byte[] { 1 }), new MemoryStream()));
            Assert.Throws<ArgumentException>(() => new IP("test"));
        }

        [Test]
        public void TestConstructor()
        {
            const string expected = "127.0.0.1";
            IP ip = new IP(expected);
            Assert.AreEqual(expected, ip.ToString());
            var test = new IP(IPAddress.Any);
            Assert.AreEqual(IPAddress.Any.GetHashCode(), test.GetHashCode());
        }
        
        [Test]
        public void TestToBytes()
        {
            IP ip = new IP("129.213.224.111");
            Assert.AreEqual(new byte[] {0x40, 0x04, 0x81, 0xD5, 0xE0, 0x6F}, ip.ToBytes());
        }
        
        [Test]
        public void TestEquals()
        {
            IP actual = new IP("172.0.0.1");
            IP target = new IP("172.0.0.1");
            IP another = new IP("172.0.0.0");

            Assert.IsTrue(actual.Equals(target));
            Assert.IsTrue(actual == target);
// ReSharper disable EqualExpressionComparison
            Assert.IsTrue(actual == actual);
// ReSharper restore EqualExpressionComparison
            Assert.AreEqual(actual, target);
            Assert.IsFalse(actual == another);
            Assert.IsTrue(actual != another);
            Assert.AreNotEqual(actual, another);
        }
    }
}
#pragma warning restore 1591,0618,1718