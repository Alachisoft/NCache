using System;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class MalformedPduTestFixture
    {
        [Test]
        public void Test()
        {
            var pdu = new MalformedPdu();
            Assert.Throws<NotSupportedException>(() => { var test = pdu.ErrorIndex; });
            Assert.Throws<NotSupportedException>(() => { var test = pdu.ErrorStatus; });
            Assert.Throws<NotSupportedException>(() => pdu.AppendBytesTo(null));
            Assert.AreEqual(0, pdu.Variables.Count);
            Assert.AreEqual(Integer32.Zero, pdu.RequestId);
            Assert.AreEqual(SnmpType.Unknown, pdu.TypeCode);
            Assert.AreEqual("Malformed PDU", pdu.ToString());
        }
    }
}
