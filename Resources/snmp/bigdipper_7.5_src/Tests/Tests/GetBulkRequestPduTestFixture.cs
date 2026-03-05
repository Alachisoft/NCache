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
    public class GetBulkRequestPduTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new GetBulkRequestPdu(new Tuple<int, byte[]>(0, new byte[] { 0 }), null));
            Assert.Throws<ArgumentNullException>(() => new GetBulkRequestPdu(0, 0, 0, null));
            
            var pdu = new GetBulkRequestPdu(0, 0, 0, new List<Variable>());
            Assert.Throws<ArgumentNullException>(() => pdu.AppendBytesTo(null));
            Assert.AreEqual("GET BULK request PDU: seq: 0; non-repeaters: 0; max-repetitions: 0; variable count: 0", pdu.ToString());

        }
    }
}
