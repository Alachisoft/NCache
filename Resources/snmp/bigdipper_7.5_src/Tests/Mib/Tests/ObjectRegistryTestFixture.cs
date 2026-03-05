/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/16
 * Time: 21:10
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

#pragma warning disable 1591
namespace Lextm.SharpSnmpLib.Mib.Tests
{
    [TestFixture]
    public class ObjectRegistryTestFixture
    {
        // ReSharper disable InconsistentNaming
        [Test]
        public void TestValidateTable()
        {
            var table = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9 });
            var entry = new ObjectIdentifier(new uint[] { 1, 3, 6, 1, 2, 1, 1, 9, 1 });
            var unknown = new ObjectIdentifier(new uint[] { 1, 3, 6, 8, 18579, 111111});
            Assert.IsTrue(DefaultObjectRegistry.Instance.ValidateTable(table));
            Assert.IsFalse(DefaultObjectRegistry.Instance.ValidateTable(entry));
            Assert.IsFalse(DefaultObjectRegistry.Instance.ValidateTable(unknown));
        }

        [Test]
        public void TestGetTextualFrom()
        {
            var oid = new uint[] {1};
            string result = DefaultObjectRegistry.Instance.Translate(oid);
            Assert.AreEqual("SNMPv2-SMI::iso", result);
        }
        [Test]
        public void TestGetTextualForm()
        {
            var oid2 = new uint[] {1,3,6,1,2,1,10};
            string result2 = DefaultObjectRegistry.Instance.Translate(oid2);
            Assert.AreEqual("SNMPv2-SMI::transmission", result2);
        }   
 
        [Test]
        public void TestSNMPv2MIBTextual()
        {
            var oid = new uint[] {1,3,6,1,2,1,1};
            string result = DefaultObjectRegistry.Instance.Translate(oid);
            Assert.AreEqual("SNMPv2-MIB::system", result);            
        }

        [Test]
        public void TestSNMPv2TMTextual()
        {
            uint[] old = DefaultObjectRegistry.Instance.Translate("SNMPv2-SMI::snmpDomains");
            string result = DefaultObjectRegistry.Instance.Translate(ObjectIdentifier.AppendTo(old, 1));
            Assert.AreEqual("SNMPv2-TM::snmpUDPDomain", result);
        }

        [Test]
        public void TestIso()
        {
            var expected = new uint[] {1};
            const string textual = "SNMPv2-SMI::iso";
            uint[] result = DefaultObjectRegistry.Instance.Translate(textual);
            Assert.AreEqual(expected, result);            
        }

        [Test]
        public void TestTransmission()
        {
            var expected = new uint[] {1,3,6,1,2,1,10};
            const string textual = "SNMPv2-SMI::transmission";
            uint[] result = DefaultObjectRegistry.Instance.Translate(textual);
            Assert.AreEqual(expected, result);    
        }
        
        [Test]
        public void TestZeroDotZero()
        {
            Assert.AreEqual(new uint[] {0}, DefaultObjectRegistry.Instance.Translate("SNMPv2-SMI::ccitt"));
            const string textual = "SNMPv2-SMI::zeroDotZero";
            var expected = new uint[] {0, 0};
            Assert.AreEqual(textual, DefaultObjectRegistry.Instance.Translate(expected));
            Assert.AreEqual(expected, DefaultObjectRegistry.Instance.Translate(textual));            
        }

        [Test]
        public void TestSNMPv2MIBNumerical()
        {
            var expected = new uint[] {1,3,6,1,2,1,1};
            const string textual = "SNMPv2-MIB::system";
            uint[] result = DefaultObjectRegistry.Instance.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestSNMPv2TMNumerical()
        {
            var expected = new uint[] {1,3,6,1,6,1,1};
            const string textual = "SNMPv2-TM::snmpUDPDomain";
            uint[] result = DefaultObjectRegistry.Instance.Translate(textual);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestsysORTable()
        {
            const string name = "SNMPv2-MIB::sysORTable";
            uint[] id = DefaultObjectRegistry.Instance.Translate(name);
            Assert.IsTrue(DefaultObjectRegistry.Instance.IsTableId(id));
        }

        [Test]
        public void TestsysORTable0()
        {
            var expected = new uint[] {1,3,6,1,2,1,1,9,0};
            const string name = "SNMPv2-MIB::sysORTable.0";
            uint[] id = DefaultObjectRegistry.Instance.Translate(name);
            Assert.AreEqual(expected, id);
        }

        [Test]
        public void TestsysORTable0Reverse()
        {
            var id = new uint[] {1,3,6,1,2,1,1,9,0};
            const string expected = "SNMPv2-MIB::sysORTable.0";
            string value = DefaultObjectRegistry.Instance.Translate(id);
            Assert.AreEqual(expected, value);
        }   
 
        [Test]
        public void TestsnmpMIB()
        {
            const string name = "SNMPv2-MIB::snmpMIB";
            uint[] id = DefaultObjectRegistry.Instance.Translate(name);
            Assert.IsFalse(DefaultObjectRegistry.Instance.IsTableId(id));
        }
        
        [Test]
        public void TestActona()
        {
            const string name = "ACTONA-ACTASTOR-MIB::actona";
            var modules = Parser.Compile(new MemoryStream(Properties.Resources.ACTONA_ACTASTOR_MIB), new List<CompilerError>(), new List<CompilerWarning>());
            DefaultObjectRegistry.Instance.Import(modules);
            DefaultObjectRegistry.Instance.Refresh();
            uint[] id = DefaultObjectRegistry.Instance.Translate(name);
            
            Assert.AreEqual(new uint[] {1, 3, 6, 1, 4, 1, 17471}, id);
            Assert.AreEqual("ACTONA-ACTASTOR-MIB::actona", DefaultObjectRegistry.Instance.Translate(id));
        }
        
        [Test]
        public void TestSYMMIB_MIB_MIB()
        {
            const string name = "SYMMIB_MIB-MIB::symbios_3_1";
            var registry = new SimpleObjectRegistry();
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC_1212), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC1155_SMI), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC1213_MIB1), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.HOST_RESOURCES_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.IF_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.IANAifType_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_SMI), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_CONF), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_TC), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_TM), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SYMMIB_MIB_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.DMTF_DMI_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Refresh();
            uint[] id = registry.Translate(name);
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 4, 1, 1123, 3, 1 }, id);
            Assert.AreEqual(name, registry.Translate(id));
        }
        
        [Test]
        public void TestIEEE802dot11_MIB()
        {
            var registry = new SimpleObjectRegistry();
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC_1212), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC1155_SMI), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.RFC1213_MIB1), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_SMI), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_CONF), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_TC), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.SNMPv2_TM), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Import(Parser.Compile(new MemoryStream(Properties.Resources.IEEE802DOT11_MIB), new List<CompilerError>(), new List<CompilerWarning>()));
            registry.Refresh();
            
            Assert.AreEqual("IEEE802dot11-MIB::dot11SMTnotification", registry.Translate(new uint[] {1, 2, 840, 10036, 1, 6}));
            uint[] id = registry.Translate("IEEE802dot11-MIB::dot11SMTnotification");
            Assert.AreEqual(new uint[] {1, 2, 840, 10036, 1, 6}, id);

            const string name1 = "IEEE802dot11-MIB::dot11Disassociate";
            var id1 = new uint[] {1, 2, 840, 10036, 1, 6, 0, 1};
            Assert.AreEqual(id1, registry.Translate(name1));
            Assert.AreEqual(name1, registry.Translate(id1));
        }
        // ReSharper restore InconsistentNaming
    }
}
#pragma warning restore 1591