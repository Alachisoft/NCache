using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class GetRequestPduTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new GetRequestPdu(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentNullException>(() => new GetRequestPdu(0, null));
            Assert.Throws<ArgumentNullException>(() => new GetRequestPdu(0, new List<Variable>()).AppendBytesTo(null));
        }

        [Test]
        public void TestConstructor()
        {
            var pdu = new GetRequestPdu(0, ErrorCode.NoError, 0, new List<Variable>());
            Assert.AreEqual("GET request PDU: seq: 0; status: 0; index: 0; variable count: 0", pdu.ToString());
        }
    }
}
