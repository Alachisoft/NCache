/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/30
 * Time: 19:58
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using NUnit.Framework;

#pragma warning disable 1591,0618
namespace Lextm.SharpSnmpLib.Tests
{
    [TestFixture]
    public class DataFactoryTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => DataFactory.CreateSnmpData((Stream)null));
            Assert.Throws<ArgumentNullException>(() => DataFactory.CreateSnmpData(null, 0, 0));
            Assert.Throws<ArgumentNullException>(() => DataFactory.CreateSnmpData((byte[])null));
            Assert.Throws<ArgumentNullException>(() => DataFactory.CreateSnmpData(0, null));
        }
        
        [Test]
        public void TestCreateObjectIdentifier()
        {
            byte[] expected = new byte[] {0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x68, 0x02};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.ObjectIdentifier, data.TypeCode);
            ObjectIdentifier o = (ObjectIdentifier)data;
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 2162, 1000, 2 }, o.ToNumerical());
        }
        
        [Test]
        public void TestCreateObjectIdentifier2()
        {
            byte[] expected = new Byte[] {0x06, 0x01, 0x00};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.ObjectIdentifier, data.TypeCode);
            ObjectIdentifier o = (ObjectIdentifier)data;
            Assert.AreEqual(new uint[] {0, 0}, o.ToNumerical());
        }
        
        [Test]
        public void TestCreateNull()
        {
            byte[] expected = new byte[] {0x05, 0x00};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.Null, data.TypeCode);
            Null n = (Null)data;
            Assert.AreEqual(expected, n.ToBytes());
        }
        
        [Test]
        public void TestCreateInteger()
        {
            byte[] expected = new byte[] {0x02, 0x01, 0x00};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.Integer32, data.TypeCode);
            Integer32 i = (Integer32)data;
            Assert.AreEqual(0, i.ToInt32());
        }
        
        [Test]
        public void TestCreateOctetString()
        {
            byte[] expected = new byte[] {0x04, 0x06, 0x70, 0x75, 0x62, 0x6C, 0x69, 0x63};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.OctetString, data.TypeCode);
            Assert.AreEqual("public", data.ToString());
        }
        
        [Test]
        public void TestCreateIP()
        {
            byte[] expected = new byte[] { 0x40, 0x04, 0x7F, 0x00, 0x00, 0x01};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.IPAddress, data.TypeCode);
            IP a = (IP)data;
            Assert.AreEqual("127.0.0.1", a.ToString());
        }
        
        [Test]
        public void TestTimeticks()
        {
            byte[] expected = new byte[] { 0x43, 0x02, 0x3F, 0xE0 };
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.TimeTicks, data.TypeCode);
            TimeTicks t = (TimeTicks)data;
            Assert.AreEqual(16352, t.ToUInt32());
        }
        
        [Test]
        public void TestVarbind()
        {
            byte[] expected = new byte[] {0x30, 0x17,
                0x06, 0x0B, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x69, 0x15, 0x00,
                0x04, 0x08, 0x54, 0x72, 0x61, 0x70, 0x54, 0x65, 0x73, 0x74};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.Sequence, data.TypeCode);
            Sequence a = (Sequence)data;
            Assert.AreEqual(2, a.Length);
            
            ISnmpData oid = a[0];
            ISnmpData name = a[1];
            Assert.AreEqual(SnmpType.ObjectIdentifier, oid.TypeCode);
            Assert.AreEqual(SnmpType.OctetString, name.TypeCode);
            ObjectIdentifier o = (ObjectIdentifier)oid;
            Assert.AreEqual(new uint[] {1,3,6,1,4,1,2162,1001,21,0}, o.ToNumerical());
            OctetString s = (OctetString)name;
            Assert.AreEqual("TrapTest", s.ToString());
        }
        
        [Test]
        public void TestVarbindSection()
        {
            byte[] expected = new byte[] {0x30, 0x19,
                0x30, 0x17,
                0x06, 0x0B, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x69, 0x15, 0x00,
                0x04, 0x08, 0x54, 0x72, 0x61, 0x70, 0x54, 0x65, 0x73, 0x74};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.Sequence, data.TypeCode);
            
            Sequence a = (Sequence)data;
            Assert.AreEqual(1, a.Length);
            ISnmpData varbind = a[0];
            Assert.AreEqual(SnmpType.Sequence, varbind.TypeCode);
        }
        
        [Test]
        public void TestTrapv1Pdu()
        {
            byte[] expected = new byte[] {0xA4, 0x37,
                0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x68, 0x02,
                0x40, 0x04, 0x7F, 0x00, 0x00, 0x01,
                0x02, 0x01, 0x06,
                0x02, 0x01, 0x0C,
                0x43, 0x02, 0x3F, 0xE0,
                0x30, 0x19,
                0x30, 0x17,
                0x06, 0x0B, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x69, 0x15, 0x00,
                0x04, 0x08, 0x54, 0x72, 0x61, 0x70, 0x54, 0x65, 0x73, 0x74};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.TrapV1Pdu, data.TypeCode);
            
            TrapV1Pdu t = (TrapV1Pdu)data;
            Assert.AreEqual(new uint[] {1,3,6,1,4,1,2162,1000,2}, t.Enterprise.ToNumerical());
            Assert.AreEqual("127.0.0.1", t.AgentAddress.ToIPAddress().ToString());
            Assert.AreEqual(GenericCode.EnterpriseSpecific, t.Generic);
            Assert.AreEqual(12, t.Specific);
            Assert.AreEqual(16352, t.TimeStamp.ToUInt32());
            Assert.AreEqual(1, t.Variables.Count);
        }
        
        [Test]
        public void TestTrapPacket()
        {
            byte[] expected = new byte[] {
                0x30, 0x44,
                0x02, 0x01, 0x00,
                0x04, 0x06, 0x70, 0x75, 0x62, 0x6C, 0x69, 0x63,
                0xA4, 0x37,
                0x06, 0x0A, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x68, 0x02,
                0x40, 0x04, 0x7F, 0x00, 0x00, 0x01,
                0x02, 0x01, 0x06,
                0x02, 0x01, 0x0C,
                0x43, 0x02, 0x3F, 0xE0,
                0x30, 0x19,
                0x30, 0x17,
                0x06, 0x0B, 0x2B, 0x06, 0x01, 0x04, 0x01, 0x90, 0x72, 0x87, 0x69, 0x15, 0x00,
                0x04, 0x08, 0x54, 0x72, 0x61, 0x70, 0x54, 0x65, 0x73, 0x74};
            ISnmpData data = DataFactory.CreateSnmpData(expected);
            Assert.AreEqual(SnmpType.Sequence, data.TypeCode);
            
            Sequence t = (Sequence)data;
            Assert.AreEqual(3, t.Length);
            ISnmpData version = t[0];
            Assert.AreEqual(SnmpType.Integer32, version.TypeCode);
            Assert.AreEqual(1, 1 + ((Integer32)version).ToInt32());
            ISnmpData community = t[1];
            Assert.AreEqual(SnmpType.OctetString, community.TypeCode);
            Assert.AreEqual("public", ((OctetString)community).ToString());
            ISnmpData pdu = t[2];
            Assert.AreEqual(SnmpType.TrapV1Pdu, pdu.TypeCode);
        }
    }
}
#pragma warning restore 1591,0618

