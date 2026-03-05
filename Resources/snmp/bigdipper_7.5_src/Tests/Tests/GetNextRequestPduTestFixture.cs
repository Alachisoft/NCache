/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/5
 * Time: 10:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class GetNextRequestPduTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new GetNextRequestPdu(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentNullException>(() => new GetNextRequestPdu(0, null));
            
            var pdu = new GetNextRequestPdu(0, ErrorCode.NoError, 0, new List<Variable>());
            Assert.Throws<ArgumentNullException>(() => pdu.AppendBytesTo(null));
            Assert.AreEqual("GET NEXT request PDU: seq: 0; status: 0; index: 0; variable count: 0", pdu.ToString());

        }
    }
}
