using System;
using System.IO;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class StreamExtensionTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => StreamExtension.AppendBytes(null, SnmpType.Counter32, null, null));
            Assert.Throws<ArgumentNullException>(() => new MemoryStream().AppendBytes(SnmpType.Counter32, null, null));
            Assert.Throws<ArgumentNullException>(() => StreamExtension.IgnoreBytes(null, 0));
            Assert.Throws<ArgumentNullException>(() => StreamExtension.ReadPayloadLength(null));
        }
    }
}
