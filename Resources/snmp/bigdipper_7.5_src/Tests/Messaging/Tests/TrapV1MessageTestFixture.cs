using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Security;
using NUnit.Framework;

#pragma warning disable 1591
namespace Lextm.SharpSnmpLib.Messaging.Tests
{
    [TestFixture]
    public class TrapV1MessageTestFixture
    {
        [Test]
        public void TestSendTrap()
        {
            TrapV1Message message = new TrapV1Message(VersionCode.V1, 
                                                      IPAddress.Parse("127.0.0.1"),
                                                      new OctetString("public"),
                                                      new ObjectIdentifier(new uint[] {1,3,6}),
                                                      GenericCode.AuthenticationFailure,
                                                      0, 
                                                      0,
                                                      new List<Variable>());
            byte[] bytes = message.ToBytes();
            ISnmpMessage parsed = MessageFactory.ParseMessages(bytes, new UserRegistry())[0];
            Assert.AreEqual(SnmpType.TrapV1Pdu, parsed.TypeCode());
            TrapV1Message m = (TrapV1Message)parsed;
            Assert.AreEqual(GenericCode.AuthenticationFailure, m.Generic);
            Assert.AreEqual(0, m.Specific);
            Assert.AreEqual("public", m.Community.ToString());
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), m.AgentAddress);
            Assert.AreEqual(new uint[] {1,3,6}, m.Enterprise.ToNumerical());
            Assert.AreEqual(0, m.TimeStamp);
            Assert.AreEqual(0, m.Variables().Count);
        }
        
        [Test]
        public void TestParseNoVarbind()
        {
            byte[] buffer = Properties.Resources.novarbind;
            ISnmpMessage m = MessageFactory.ParseMessages(buffer, new UserRegistry())[0];
            TrapV1Message message = (TrapV1Message)m;
            Assert.AreEqual(GenericCode.EnterpriseSpecific, message.Generic);
            Assert.AreEqual(12, message.Specific);
            Assert.AreEqual("public", message.Community.ToString());
            Assert.AreEqual(IPAddress.Parse("127.0.0.1"), message.AgentAddress);
            Assert.AreEqual(new uint[] { 1,3 }, message.Enterprise.ToNumerical());
            Assert.AreEqual(16352, message.TimeStamp);
            Assert.AreEqual(0, message.Variables().Count);            
        }
        
        [Test]
        public void TestParseOneVarbind()
        {
            byte[] buffer = Properties.Resources.onevarbind;
            TrapV1Message message = (TrapV1Message)MessageFactory.ParseMessages(buffer, new UserRegistry())[0];
            Assert.AreEqual(1, message.Variables().Count);
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 2162, 1000, 2 }, message.Enterprise.ToNumerical());
            Assert.AreEqual("TrapTest", message.Variables()[0].Data.ToString());
            Assert.AreEqual(new uint[] {1,3,6,1,4,1,2162,1001,21,0}, message.Variables()[0].Id.ToNumerical());
        }
        
        [Test]
        public void TestParseTwoVarbinds()
        {
            byte[] buffer = Properties.Resources.twovarbinds;
            TrapV1Message message = (TrapV1Message)MessageFactory.ParseMessages(buffer, new UserRegistry())[0];
            Assert.AreEqual(2, message.Variables().Count);
            Assert.AreEqual("TrapTest", message.Variables()[0].Data.ToString());
            Assert.AreEqual(new uint[] {1,3,6,1,4,1,2162,1001,21,0}, message.Variables()[0].Id.ToNumerical());
            Assert.AreEqual("TrapTest", message.Variables()[1].Data.ToString());
            Assert.AreEqual(new uint[] {1,3,6,1,4,1,2162,1001,22,0}, message.Variables()[1].Id.ToNumerical());
        }
    }
}
#pragma warning restore 1591

