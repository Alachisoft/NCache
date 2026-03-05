/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 11/7/2009
 * Time: 8:29 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using Antlr.Runtime;
using Lextm.SharpSnmpLib.Properties;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Mib.Tests
{
    /// <summary>
    /// Description of TestAst.
    /// </summary>
    [TestFixture]
    public class MibDocumentTestFixture
    {
        [Test]
        public void TestSNMPv2_SMI()
        {
            var stream = new ANTLRInputStream(new MemoryStream(Resources.SNMPv2_SMI));
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
            Assert.AreEqual(1, document.Modules.Count);
            Assert.AreEqual("SNMPv2-SMI", document.Modules[0].Name);
            Assert.AreEqual(34, document.Modules[0].Constructs.Count);
            Assert.AreEqual(16, document.Modules[0].Entities.Count);
            var node = document.Modules[0].Entities[15];
            Assert.AreEqual("zeroDotZero", node.Name);
            Assert.AreEqual(0, node.Value);
            Assert.AreEqual("ccitt", node.Parent);
        }

        [Test]
        public void TestSYMMIB_MIB_MIB()
        {
            var stream = new ANTLRInputStream(new MemoryStream(Resources.SYMMIB_MIB_MIB));
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
            Assert.AreEqual(1, document.Modules.Count);
            Assert.AreEqual("SYMMIB_MIB-MIB", document.Modules[0].Name);
            Assert.AreEqual(117, document.Modules[0].Constructs.Count);
            Assert.AreEqual(98, document.Modules[0].Entities.Count);
            var node = document.Modules[0].Entities[97];
            Assert.AreEqual("disableTrapAtt3", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("iNTELMIFTOMIBEntry", node.Parent);
        }

        [Test]
        public void TestComment()
        {
            string test = "ADSL-LINE-MIB --c-- DEFINITIONS ::= BEGIN --c " + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
        }

        [Test]
        public void TestComment2()
        {
            string test = "ADSL-LINE-MIB --c-c-- DEFINITIONS ::= BEGIN --c " + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
        }

        [Test]
        public void TestComment3()
        {
            string test = "ADSL-LINE-MIB --c-- DEFINITIONS ::= BEGIN --c- " + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
        }
        
        
        [Test]
        public void TestComment4()
        {
            string test = "ADSL-LINE-MIB DEFINITIONS ::= BEGIN" + Environment.NewLine +
                "--------------------" + Environment.NewLine +
                "-- adaptergroup Group" + Environment.NewLine +
                "--------------------" + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
        }

        [Test]
        public void TestLexerOK()
        {
            string test = "ADSL-LINE-MIB DEFINITIONS ::= BEGIN" + Environment.NewLine +
                "IMPORTS" + Environment.NewLine +
                "MODULE-IDENTITY, OBJECT-TYPE," + Environment.NewLine +
                "Counter32, Gauge32, Integer32," + Environment.NewLine +
                "NOTIFICATION-TYPE," + Environment.NewLine +
                "transmission           FROM SNMPv2-SMI;" + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
            Assert.AreEqual(1, document.Modules.Count);
            const string expected = "ADSL-LINE-MIB";
            Assert.AreEqual(expected, document.Modules[0].Name);
            var module = document.Modules[0];
            Assert.AreEqual(false, module.Exports.AllExported);
            Assert.AreEqual(0, module.Exports.Symbols.Count);
            Assert.AreEqual(1, module.Imports.Clauses.Count);
            var import = module.Imports.Clauses[0];
            Assert.AreEqual("SNMPv2-SMI", import.Module);
            Assert.AreEqual(7, import.Symbols.Count);
            Assert.AreEqual("MODULE-IDENTITY", import.Symbols[0]);
            Assert.AreEqual("OBJECT-TYPE", import.Symbols[1]);
            Assert.AreEqual("Counter32", import.Symbols[2]);
            Assert.AreEqual("Gauge32", import.Symbols[3]);
            Assert.AreEqual("Integer32", import.Symbols[4]);
            Assert.AreEqual("NOTIFICATION-TYPE", import.Symbols[5]);
            Assert.AreEqual("transmission", import.Symbols[6]);
        }

        [Test]
        public void TestException()
        {
            var stream = new ANTLRInputStream(new MemoryStream(Resources.fivevarbinds));
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var doc = parser.GetDocument();
            Assert.AreEqual(1, parser.Errors.Count);
            var exception = parser.Errors[0];
            var token = exception.Token;
            Assert.AreEqual(1, token.Line);
            Assert.AreEqual("0", token.Text);
        }

        [Test]
        public void TestException2()
        {
            string test = "ADSL-LINE-MIB DEFINITIONS ::= BEGIN" + Environment.NewLine +
                "IMPORTS" + Environment.NewLine +
                "transmission           SNMPv2-SMI" + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var doc = parser.GetDocument();
            Assert.AreEqual(2, parser.Errors.Count);
            var exception = parser.Errors[0];
            var token = exception.Token;
            Assert.AreEqual(3, token.Line);
            Assert.AreEqual("SNMPv2-SMI", token.Text);

            var exception2 = parser.Errors[1];
            var token2 = exception2.Token;
            Assert.AreEqual(4, token2.Line);
            Assert.AreEqual("END", token2.Text);
        }

        [Test]
        public void TestException3()
        {
            string test = "ADSL-LINE-MIB DEFINITIONS ::= BEGIN" + Environment.NewLine +
                "TEST" + Environment.NewLine +
                "END";
            var lex = new SmiLexer(new ANTLRStringStream(test));
            var tokens = new CommonTokenStream(lex);
            var parser = new SmiParser(tokens);
            var exception = Assert.Throws<NoViableAltException>(() => parser.GetDocument());
            var token = exception.Token;
            Assert.AreEqual(3, token.Line);
            Assert.AreEqual("END", token.Text);
        }

        [Test]
        public void TestEmpty()
        {
            // issue 4978
            var stream = new ANTLRInputStream(new MemoryStream(Resources.empty));
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var document = parser.GetDocument();
            Assert.AreEqual("SNMPv2-CONF", document.Modules[0].Name);
            Assert.AreEqual(0, document.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(0, document.Modules[0].Entities.Count);
        }

        [Test]
        public void TestSNMPv2_PDU()
        {
            var m = new MemoryStream(Resources.SNMPV2_PDU);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("SNMPv2-PDU", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void TestRFC1157_SNMP_MIB()
        {
            var m = new MemoryStream(Resources.RFC1157_SNMP);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("RFC1157-SNMP", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void TestJVM_MANAGEMENT_MIB()
        {
            var m = new MemoryStream(Resources.JVM_MANAGEMENT_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("JVM-MANAGEMENT-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(160, file.Modules[0].Entities.Count);
            var node = file.Modules[0].Entities[159];
            Assert.AreEqual("jvmLowMemoryCollectNotifGroup", node.Name);
            Assert.AreEqual(8, node.Value);
            Assert.AreEqual("jvmMgtMIBGroups", node.Parent);
        }

        [Test]
        public void TestIEEE8021_PAE_MIB()
        {
            var m = new MemoryStream(Resources.IEEE8021_PAE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("IEEE8021-PAE-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(106, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[105];
            Assert.AreEqual("dot1xPaeCompliance", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("dot1xPaeCompliances", node.Parent);
        }

        [Test]
        public void TestIEEE802dot11_MIB()
        {
            var m = new MemoryStream(Resources.IEEE802DOT11_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("IEEE802dot11-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(182, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[181];
            Assert.AreEqual("dot11SMTbase2", node.Name);
            Assert.AreEqual(18, node.Value);
            Assert.AreEqual("dot11Groups", node.Parent);

            IEntity node2 = file.Modules[0].Entities[58];
            Assert.AreEqual("dot11Disassociate", node2.Name);
            Assert.AreEqual(1, node2.Value);
            Assert.AreEqual("dot11SMTnotification.0", node2.Parent);
        }

        [Test]
        public void TestCISCO_CSG_MIB()
        {
            var m = new MemoryStream(Resources.CISCO_CSG_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("CISCO-CSG-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(82, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[81];
            Assert.AreEqual("ciscoCsgNotifGroup", node.Name);
            Assert.AreEqual(6, node.Value);
            Assert.AreEqual("ciscoCsgMIBGroups", node.Parent);
        }

        [Test]
        public void TestCISCO_CONFIG_COPY_CAPABILITY()
        {
            var m = new MemoryStream(Resources.CISCO_CONFIG_COPY_CAPABILITY);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("CISCO-CONFIG-COPY-CAPABILITY", file.Modules[0].Name);
        }

        [Test]
        public void TestCISCO_BULK_FILE_MIB()
        {
            var m = new MemoryStream(Resources.CISCO_BULK_FILE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("CISCO-BULK-FILE-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(51, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[50];
            Assert.AreEqual("ciscoBulkFileStatusGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("ciscoBulkFileMIBGroups", node.Parent);
        }

        [Test]
        public void TestCISCO_AAA_SERVER_MIB()
        {
            var m = new MemoryStream(Resources.CISCO_AAA_SERVER_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("CISCO-AAA-SERVER-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(55, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[54];
            Assert.AreEqual("casConfigGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("casMIBGroups", node.Parent);
        }

        [Test]
        public void TestCHECKPOINT_MIB()
        {
            var m = new MemoryStream(Resources.CHECKPOINT_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();
            Assert.AreEqual(5, parser.Warnings.Count);
            Assert.AreEqual("CHECKPOINT-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(539, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[538];
            Assert.AreEqual("lsApplicationType", node.Name);
            Assert.AreEqual(5, node.Value);
            Assert.AreEqual("lsConnectedClientsEntry", node.Parent);
        }

        [Test]
        public void TestCASTELLEMIB()
        {
            var m = new MemoryStream(Resources.CASTELLEMIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();
            Assert.AreEqual(10, parser.Warnings.Count);
            Assert.AreEqual("CASTELLEMIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(128, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[127];
            Assert.AreEqual("fPReboot", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("fPResetInfo", node.Parent);
        }

        [Test]
        public void TestBridge()
        {
            var m = new MemoryStream(Resources.BRIDGE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("BRIDGE-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(62, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[61];
            Assert.AreEqual("dot1dStaticStatus", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("dot1dStaticEntry", node.Parent);
        }

        [Test]
        public void TestBRCM_BSAPTRAP_MIB()
        {
            var m = new MemoryStream(Resources.BRCM_BSAPTRAP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("Brcm-BSAPTrap-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(10, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[9];
            Assert.AreEqual("trapAdapterActivityCause", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("baspTrap", node.Parent);
        }

        [Test]
        public void TestBASEBRDD_MIB_MIB()
        {
            var m = new MemoryStream(Resources.BASEBRDD_MIB_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();
            Assert.AreEqual(94, parser.Warnings.Count);
            Assert.AreEqual("BASEBRDD_MIB-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(607, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[606];
            Assert.AreEqual("dMTFVoltageProbeEvSub", node.Name);
            Assert.AreEqual(7, node.Value);
            Assert.AreEqual("dMTFVoltageProbeTable", node.Parent);
        }
        
        [Test]
        public void TestALTIGA_CAP()
        {
            var m = new MemoryStream(Resources.ALTIGA_CAP);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ALTIGA-CAP", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(3, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[2];
            Assert.AreEqual("altigaBasicAgentRev1", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("altigaCapModule", node.Parent);
        }

        [Test]
        public void TestATM_TC__MIB()
        {
            var m = new MemoryStream(Resources.ATM_TC_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ATM-TC-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(11, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[10];
            Assert.AreEqual("atmClpTaggingScrCdvt", node.Name);
            Assert.AreEqual(15, node.Value);
            Assert.AreEqual("atmTrafficDescriptorTypes", node.Parent);
        }

        [Test]
        public void TestARROWPOINT_IPV4_OSPF_MIB()
        {
            var m = new MemoryStream(Resources.ARROWPOINT_IPV4_OSPF_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ARROWPOINT-IPV4-OSPF-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(50, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[49];
            Assert.AreEqual("apIpv4OspfCompliance", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("apIpv4OspfCompliances", node.Parent);
        }

        [Test]
        public void TestAPPC_MIB()
        {
            var m = new MemoryStream(Resources.APPC_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("APPC-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(305, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[304];
            Assert.AreEqual("appcConversationConfGroup", node.Name);
            Assert.AreEqual(10, node.Value);
            Assert.AreEqual("appcGroups", node.Parent);
        }

        [Test]
        public void TestALVARION_DOT11_WLAN_MIB()
        {
            var m = new MemoryStream(Resources.ALVARION_DOT11_WLAN_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ALVARION-DOT11-WLAN-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(269, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[268];
            Assert.AreEqual("brzaccVLTrapFtpStatus", node.Name);
            Assert.AreEqual(10, node.Value);
            Assert.AreEqual("brzaccVLTraps", node.Parent);
        }

        [Test]
        public void TestARRAYMANAGER_MIB()
        {
            var m = new MemoryStream(Resources.ARRAYMANAGER_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();
            Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ArrayManager-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(380, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[379];
            Assert.AreEqual("rebuildRateEv", node.Name);
            Assert.AreEqual(222, node.Value);
            Assert.AreEqual("aryMgrEvts", node.Parent);
        }

        [Test]
        public void TestAIRPORT_BASESTATION_3_MIB()
        {
            var m = new MemoryStream(Resources.AIRPORT_BASESTATION_3_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("AIRPORT-BASESTATION-3-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(47, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[46];
            Assert.AreEqual("physicalInterfaceNumRXError", node.Name);
            Assert.AreEqual(10, node.Value);
            Assert.AreEqual("physicalInterface", node.Parent);
        }

        [Test]
        public void TestALLIEDTELESYN_MIB()
        {
            var m = new MemoryStream(Resources.ALLIEDTELESYN_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ALLIEDTELESYN-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(606, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[605];
            Assert.AreEqual("ds3TrapInterval", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("ds3TrapEntry", node.Parent);
        }

        [Test]
        public void TestADMIN_AUTH_STATS_MIB()
        {
            var m = new MemoryStream(Resources.ADMIN_AUTH_STATS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ADMIN-AUTH-STATS-MIB", file.Modules[0].Name);
            Assert.AreEqual(4, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(23, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[22];
            Assert.AreEqual("alAdminAuthClientMIBGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("alAdminAuthGroup", node.Parent);
        }

        [Test]
        public void TestADSL_TC_MIB()
        {
            var m = new MemoryStream(Resources.ADSL_TC_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ADSL-TC-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("adsltcmib", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("transmission.94", node.Parent);
        }

        [Test]
        public void TestADSL_LINE_MIB()
        {
            var m = new MemoryStream(Resources.ADSL_LINE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ADSL-LINE-MIB", file.Modules[0].Name);
            Assert.AreEqual(6, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(275, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[274];
            Assert.AreEqual("adslAturLineProfileControlGroup", node.Name);
            Assert.AreEqual(25, node.Value);
            Assert.AreEqual("adslGroups", node.Parent);
        }

        [Test]
        public void TestACTONA_ACTASTOR_MIB()
        {
            var m = new MemoryStream(Resources.ACTONA_ACTASTOR_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("ACTONA-ACTASTOR-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(100, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[99];
            Assert.AreEqual("acNotificationInfoGroup", node.Name);
            Assert.AreEqual(7, node.Value);
            Assert.AreEqual("actastorGroups", node.Parent);
        }

        [Test]
        public void TestRFC1155_SMI()
        {
            var m = new MemoryStream(Resources.RFC1155_SMI);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("RFC1155-SMI", file.Modules[0].Name);
            Assert.AreEqual(6, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("internet", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("iso.org(3).dod(6)", node.Parent);
        }

        [Test]
        public void TestRFC1271_MIB()
        {
            var m = new MemoryStream(Resources.RFC1271_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("RFC1271-MIB", file.Modules[0].Name);
            Assert.AreEqual(213, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[212];
            Assert.AreEqual("logDescription", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("logEntry", node.Parent);
        }

        [Test]
        public void TestRFC1213_MIB()
        {
            var m = new MemoryStream(Resources.RFC1213_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("RFC1213-MIB", file.Modules[0].Name);
            Assert.AreEqual(201, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[200];
            Assert.AreEqual("snmpEnableAuthenTraps", node.Name);
            Assert.AreEqual(30, node.Value);
            Assert.AreEqual("snmp", node.Parent);
        }

        [Test]
        public void TestRFC1213_MIB2()
        {
            var m = new MemoryStream(Resources.RFC1213_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var exception = Assert.Throws<MismatchedTokenException>(() => parser.GetDocument());
            var token = exception.Token;
            Assert.AreEqual(@"DESCRIPTION", token.Text);
            Assert.AreEqual(1, token.Line);
            Assert.AreEqual(1, parser.Errors.Count);
            var error = parser.Errors[0];
            Assert.AreEqual(@"""Test Agent Simulator""", error.Token.Text);
        }

        [Test]
        public void TestRFC_1215()
        {
            var m = new MemoryStream(Resources.RFC_1215);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("RFC-1215", file.Modules[0].Name);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void TestRMON_MIB()
        {
            var m = new MemoryStream(Resources.RMON_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("RMON-MIB", file.Modules[0].Name);
            Assert.AreEqual(232, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[231];
            Assert.AreEqual("rmonNotificationGroup", node.Name);
            Assert.AreEqual(11, node.Value);
            Assert.AreEqual("rmonGroups", node.Parent);
        }

        [Test]
        public void TestSMUX_MIB()
        {
            var m = new MemoryStream(Resources.SMUX_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SMUX-MIB", file.Modules[0].Name);
            Assert.AreEqual(14, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[13];
            Assert.AreEqual("smuxTstatus", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("smuxTreeEntry", node.Parent);
        }

        [Test]
        public void TestSNMP_VIEW_BASED_ACM_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_VIEW_BASED_ACM_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-VIEW-BASED-ACM-MIB", file.Modules[0].Name);
            Assert.AreEqual(38, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[37];
            Assert.AreEqual("vacmBasicGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("vacmMIBGroups", node.Parent);
        }

        [Test]
        public void TestTCP_MIB()
        {
            var m = new MemoryStream(Resources.TCP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("TCP-MIB", file.Modules[0].Name);
            Assert.AreEqual(51, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[50];
            Assert.AreEqual("tcpHCGroup", node.Name);
            Assert.AreEqual(5, node.Value);
            Assert.AreEqual("tcpMIBGroups", node.Parent);
        }

        [Test]
        public void TestTransport_Address_MIB()
        {
            var m = new MemoryStream(Resources.TRANSPORT_ADDRESS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("TRANSPORT-ADDRESS-MIB", file.Modules[0].Name);
            Assert.AreEqual(18, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[17];
            Assert.AreEqual("transportDomainSctpDns", node.Name);
            Assert.AreEqual(16, node.Value);
            Assert.AreEqual("transportDomains", node.Parent);
        }

        [Test]
        public void TestTunnel_MIB()
        {
            var m = new MemoryStream(Resources.TUNNEL_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("TUNNEL-MIB", file.Modules[0].Name);
            Assert.AreEqual(42, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[41];
            Assert.AreEqual("tunnelMIBInetGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("tunnelMIBGroups", node.Parent);
        }

        [Test]
        public void TestUCD_DEMO_MIB()
        {
            var m = new MemoryStream(Resources.UCD_DEMO_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-DEMO-MIB", file.Modules[0].Name);
            Assert.AreEqual(7, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[6];
            Assert.AreEqual("ucdDemoPassphrase", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("ucdDemoPublic", node.Parent);
        }

        [Test]
        public void TestUCD_DISKIO_MIB()
        {
            var m = new MemoryStream(Resources.UCD_DISKIO_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-DISKIO-MIB", file.Modules[0].Name);
            Assert.AreEqual(14, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[13];
            Assert.AreEqual("diskIONWrittenX", node.Name);
            Assert.AreEqual(13, node.Value);
            Assert.AreEqual("diskIOEntry", node.Parent);
        }

        [Test]
        public void TestUCD_DLMOD_MIB()
        {
            var m = new MemoryStream(Resources.UCD_DLMOD_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-DLMOD-MIB", file.Modules[0].Name);
            Assert.AreEqual(9, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[8];
            Assert.AreEqual("dlmodStatus", node.Name);
            Assert.AreEqual(5, node.Value);
            Assert.AreEqual("dlmodEntry", node.Parent);
        }

        [Test]
        public void TestUCD_IPFILTER_MIB()
        {
            var m = new MemoryStream(Resources.UCD_IPFILTER_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-IPFILTER-MIB", file.Modules[0].Name);
            Assert.AreEqual(23, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[22];
            Assert.AreEqual("ipfAccOutBytes", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("ipfAccOutEntry", node.Parent);
        }

        [Test]
        public void TestUCD_IPFWACC_MIB()
        {
            var m = new MemoryStream(Resources.UCD_IPFWACC_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-IPFWACC-MIB", file.Modules[0].Name);
            Assert.AreEqual(29, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[28];
            Assert.AreEqual("ipFwAccPort10", node.Name);
            Assert.AreEqual(26, node.Value);
            Assert.AreEqual("ipFwAccEntry", node.Parent);
        }

        [Test]
        public void TestUCD_SNMP_MIB()
        {
            var m = new MemoryStream(Resources.UCD_SNMP_MIB1);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-SNMP-MIB", file.Modules[0].Name);
            Assert.AreEqual(158, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[157];
            Assert.AreEqual("logMatchRegExCompilation", node.Name);
            Assert.AreEqual(101, node.Value);
            Assert.AreEqual("logMatchEntry", node.Parent);
        }

        [Test]
        public void TestUCD_SNMP_MIB_OLD()
        {
            var m = new MemoryStream(Resources.UCD_SNMP_MIB_OLD);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UCD-SNMP-MIB-OLD", file.Modules[0].Name);
            Assert.AreEqual(35, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[34];
            Assert.AreEqual("loadaveErrMessage", node.Name);
            Assert.AreEqual(101, node.Value);
            Assert.AreEqual("loadaves", node.Parent);
        }

        [Test]
        public void TestUDP_MIB()
        {
            var m = new MemoryStream(Resources.UDP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("UDP-MIB", file.Modules[0].Name);
            Assert.AreEqual(31, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[30];
            Assert.AreEqual("udpEndpointGroup", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("udpMIBGroups", node.Parent);
        }

        [Test]
        public void TestMTA_MIB()
        {
            var m = new MemoryStream(Resources.MTA_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("MTA-MIB", file.Modules[0].Name);
            Assert.AreEqual(81, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[80];
            Assert.AreEqual("mtaRFC2789ErrorGroup", node.Name);
            Assert.AreEqual(9, node.Value);
            Assert.AreEqual("mtaGroups", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_Agent_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_AGENT_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-AGENT-MIB", file.Modules[0].Name);
            Assert.AreEqual(54, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[53];
            Assert.AreEqual("nsAgentNotifyGroup", node.Name);
            Assert.AreEqual(9, node.Value);
            Assert.AreEqual("netSnmpGroups", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_Examples_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_EXAMPLES_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-EXAMPLES-MIB", file.Modules[0].Name);
            Assert.AreEqual(25, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[24];
            Assert.AreEqual("netSnmpExampleNotification", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("netSnmpExampleNotifications", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_Extend_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_EXTEND_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-EXTEND-MIB", file.Modules[0].Name);
            Assert.AreEqual(27, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[26];
            Assert.AreEqual("nsExtendOutputGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("nsExtendGroups", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-MIB", file.Modules[0].Name);
            Assert.AreEqual(14, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[13];
            Assert.AreEqual("netSnmpGroups", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("netSnmpConformance", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_Monitor_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_MONITOR_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-MONITOR-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[4];
            Assert.AreEqual("nsLog", node.Name);
            Assert.AreEqual(24, node.Value);
            Assert.AreEqual("netSnmpObjects", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_System_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_SYSTEM_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-SYSTEM-MIB", file.Modules[0].Name);
            Assert.AreEqual(6, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[5];
            Assert.AreEqual("nsDiskIO", node.Name);
            Assert.AreEqual(35, node.Value);
            Assert.AreEqual("netSnmpObjects", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_TC()
        {
            var m = new MemoryStream(Resources.NET_SNMP_TC);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-TC", file.Modules[0].Name);
            Assert.AreEqual(24, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[23];
            Assert.AreEqual("netSnmpCallbackDomain", node.Name);
            Assert.AreEqual(6, node.Value);
            Assert.AreEqual("netSnmpDomains", node.Parent);
        }

        [Test]
        public void TestNet_Snmp_VACM_MIB()
        {
            var m = new MemoryStream(Resources.NET_SNMP_VACM_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NET-SNMP-VACM-MIB", file.Modules[0].Name);
            Assert.AreEqual(8, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[7];
            Assert.AreEqual("nsVacmStatus", node.Name);
            Assert.AreEqual(5, node.Value);
            Assert.AreEqual("nsVacmAccessEntry", node.Parent);
        }

        [Test]
        public void TestNetwork_Service_MIB()
        {
            var m = new MemoryStream(Resources.NETWORK_SERVICES_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NETWORK-SERVICES-MIB", file.Modules[0].Name);
            Assert.AreEqual(44, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[43];
            Assert.AreEqual("applUDPProtoID", node.Name);
            Assert.AreEqual(5, node.Value);
            Assert.AreEqual("application", node.Parent);
        }

        [Test]
        public void TestNotification_Log_MIB()
        {
            var m = new MemoryStream(Resources.NOTIFICATION_LOG_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("NOTIFICATION-LOG-MIB", file.Modules[0].Name);
            Assert.AreEqual(55, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[54];
            Assert.AreEqual("notificationLogDateGroup", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("notificationLogMIBGroups", node.Parent);
        }

        [Test]
        public void TestIPv6_Flow_Label_MIB()
        {
            var m = new MemoryStream(Resources.IPV6_FLOW_LABEL_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-FLOW-LABEL-MIB", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("ipv6FlowLabelMIB", node.Name);
            Assert.AreEqual(103, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestIPv6_ICMP_MIB()
        {
            var m = new MemoryStream(Resources.IPV6_ICMP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-ICMP-MIB", file.Modules[0].Name);
            Assert.AreEqual(43, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[42];
            Assert.AreEqual("ipv6IcmpGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("ipv6IcmpGroups", node.Parent);
        }

        [Test]
        public void TestIPv6_MIB()
        {
            var m = new MemoryStream(Resources.IPV6_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-MIB", file.Modules[0].Name);
            Assert.AreEqual(91, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[90];
            Assert.AreEqual("ipv6NotificationGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("ipv6Groups", node.Parent);
        }

        [Test]
        public void TestIPv6_TC()
        {
            var m = new MemoryStream(Resources.IPV6_TC);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-TC", file.Modules[0].Name);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void TestIPv6_Tcp_MIB()
        {
            var m = new MemoryStream(Resources.IPV6_TCP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-TCP-MIB", file.Modules[0].Name);
            Assert.AreEqual(15, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[14];
            Assert.AreEqual("ipv6TcpGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("ipv6TcpGroups", node.Parent);
        }

        [Test]
        public void TestIPv6_Udp_MIB()
        {
            var m = new MemoryStream(Resources.IPV6_UDP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IPV6-UDP-MIB", file.Modules[0].Name);
            Assert.AreEqual(12, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[11];
            Assert.AreEqual("ipv6UdpGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("ipv6UdpGroups", node.Parent);
        }

        [Test]
        public void TestLM_Sensors_MIB()
        {
            var m = new MemoryStream(Resources.LM_SENSORS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("LM-SENSORS-MIB", file.Modules[0].Name);
            Assert.AreEqual(22, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[21];
            Assert.AreEqual("lmMiscSensorsValue", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("lmMiscSensorsEntry", node.Parent);
        }

        [Test]
        public void TestIP_Forward_MIB()
        {
            var m = new MemoryStream(Resources.IP_FORWARD_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IP-FORWARD-MIB", file.Modules[0].Name);
            Assert.AreEqual(69, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[68];
            Assert.AreEqual("ipForwardMultiPathGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("ipForwardGroups", node.Parent);
        }

        [Test]
        public void TestIF_Inverted_Stack_MIB()
        {
            var m = new MemoryStream(Resources.IF_INVERTED_STACK_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IF-INVERTED-STACK-MIB", file.Modules[0].Name);
            Assert.AreEqual(10, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[9];
            Assert.AreEqual("ifInvStackGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("ifInvGroups", node.Parent);
        }

        [Test]
        public void TestIANA_RTPROTO_MIB()
        {
            var m = new MemoryStream(Resources.IANA_RTPROTO_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IANA-RTPROTO-MIB", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("ianaRtProtoMIB", node.Name);
            Assert.AreEqual(84, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestIANA_Language_MIB()
        {
            var m = new MemoryStream(Resources.IANA_LANGUAGE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IANA-LANGUAGE-MIB", file.Modules[0].Name);
            Assert.AreEqual(8, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[7];
            Assert.AreEqual("ianaLangSMSL", node.Name);
            Assert.AreEqual(7, node.Value);
            Assert.AreEqual("ianaLanguages", node.Parent);
        }

        [Test]
        public void TestIANA_ADDRESS_FAMILY_NUMBERS_MIB()
        {
            var m = new MemoryStream(Resources.IANA_ADDRESS_FAMILY_NUMBERS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("IANA-ADDRESS-FAMILY-NUMBERS-MIB", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("ianaAddressFamilyNumbers", node.Name);
            Assert.AreEqual(72, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestHOST_RESOURCES_TYPE()
        {
            var m = new MemoryStream(Resources.HOST_RESOURCES_TYPES);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("HOST-RESOURCES-TYPES", file.Modules[0].Name);
            Assert.AreEqual(55, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[54];
            Assert.AreEqual("hrFSLinuxExt2", node.Name);
            Assert.AreEqual(23, node.Value);
            Assert.AreEqual("hrFSTypes", node.Parent);
        }

        [Test]
        public void TestHOST_RESOURCES_MIB()
        {
            var m = new MemoryStream(Resources.HOST_RESOURCES_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("HOST-RESOURCES-MIB", file.Modules[0].Name);
            Assert.AreEqual(104, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[103];
            Assert.AreEqual("hrSWInstalledGroup", node.Name);
            Assert.AreEqual(6, node.Value);
            Assert.AreEqual("hrMIBGroups", node.Parent);
        }

        [Test]
        public void TestHCNUM_TC()
        {
            var m = new MemoryStream(Resources.HCNUM_TC);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("HCNUM-TC", file.Modules[0].Name);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("hcnumTC", node.Name);
            Assert.AreEqual(78, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestEtherLike_MIB()
        {
            var m = new MemoryStream(Resources.EtherLike_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("EtherLike-MIB", file.Modules[0].Name);
            Assert.AreEqual(76, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[75];
            Assert.AreEqual("etherRateControlGroup", node.Name);
            Assert.AreEqual(15, node.Value);
            Assert.AreEqual("etherGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_Event_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_EVENT_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-EVENT-MIB", file.Modules[0].Name);
            Assert.AreEqual(121, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[120];
            Assert.AreEqual("dismanEventNotificationGroup", node.Name);
            Assert.AreEqual(6, node.Value);
            Assert.AreEqual("dismanEventMIBGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_Expression_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_EXPRESSION_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-EXPRESSION-MIB", file.Modules[0].Name);
            Assert.AreEqual(58, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[57];
            Assert.AreEqual("dismanExpressionValueGroup", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("dismanExpressionMIBGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_NSLookUp_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_NSLOOKUP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-NSLOOKUP-MIB", file.Modules[0].Name);
            Assert.AreEqual(25, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[24];
            Assert.AreEqual("lookupGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("lookupGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_Ping_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_PING_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-PING-MIB", file.Modules[0].Name);
            Assert.AreEqual(68, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[67];
            Assert.AreEqual("pingTimeStampGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("pingGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_Schedule_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_SCHEDULE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-SCHEDULE-MIB", file.Modules[0].Name);
            Assert.AreEqual(38, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[37];
            Assert.AreEqual("schedGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("schedGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_Script_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_SCRIPT_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-SCRIPT-MIB", file.Modules[0].Name);
            Assert.AreEqual(94, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[93];
            Assert.AreEqual("smNotificationsGroup", node.Name);
            Assert.AreEqual(6, node.Value);
            Assert.AreEqual("smGroups", node.Parent);
        }

        [Test]
        public void TestDISKMAN_TRACEROUT_MIB()
        {
            var m = new MemoryStream(Resources.DISMAN_TRACEROUTE_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("DISMAN-TRACEROUTE-MIB", file.Modules[0].Name);
            Assert.AreEqual(84, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[83];
            Assert.AreEqual("traceRouteTimeStampGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("traceRouteGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_USM_DH_OBJECTS_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_USM_DH_OBJECTS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-USM-DH-OBJECTS-MIB", file.Modules[0].Name);
            Assert.AreEqual(24, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[23];
            Assert.AreEqual("usmDHKeyKickstartGroup", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("usmDHKeyMIBGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_USM_AES_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_USM_AES_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-USM-AES-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[1];
            Assert.AreEqual("usmAesCfb128Protocol", node.Name);
            Assert.AreEqual(4, node.Value);
            Assert.AreEqual("snmpPrivProtocols", node.Parent);
        }

        [Test]
        public void TestSNMP_USER_BASED_SM_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_USER_BASED_SM_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-USER-BASED-SM-MIB", file.Modules[0].Name);
            Assert.AreEqual(36, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[35];
            Assert.AreEqual("usmMIBBasicGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("usmMIBGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_MPD_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_MPD_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-MPD-MIB", file.Modules[0].Name);
            Assert.AreEqual(12, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[11];
            Assert.AreEqual("snmpMPDGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("snmpMPDMIBGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_Notification_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_NOTIFICATION_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-NOTIFICATION-MIB", file.Modules[0].Name);
            Assert.AreEqual(29, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[28];
            Assert.AreEqual("snmpNotifyFilterGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("snmpNotifyGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_Proxy_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_PROXY_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-PROXY-MIB", file.Modules[0].Name);
            Assert.AreEqual(18, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[17];
            Assert.AreEqual("snmpProxyGroup", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("snmpProxyGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_Target_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_TARGET_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-TARGET-MIB", file.Modules[0].Name);
            Assert.AreEqual(32, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[31];
            Assert.AreEqual("snmpTargetCommandResponderGroup", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("snmpTargetGroups", node.Parent);
        }

        [Test]
        public void TestAgentX_MIB()
        {
            var m = new MemoryStream(Resources.AGENTX_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("AGENTX-MIB", file.Modules[0].Name);
            Assert.AreEqual(41, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[40];
            Assert.AreEqual("agentxMIBGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("agentxMIBGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_Community_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_COMMUNITY_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-COMMUNITY-MIB", file.Modules[0].Name);
            Assert.AreEqual(25, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[24];
            Assert.AreEqual("snmpProxyTrapForwardGroup", node.Name);
            Assert.AreEqual(3, node.Value);
            Assert.AreEqual("snmpCommunityMIBGroups", node.Parent);
        }

        [Test]
        public void TestSNMP_Framework_MIB()
        {
            var m = new MemoryStream(Resources.SNMP_FRAMEWORK_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMP-FRAMEWORK-MIB", file.Modules[0].Name);
            Assert.AreEqual(15, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[14];
            Assert.AreEqual("snmpEngineGroup", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("snmpFrameworkMIBGroups", node.Parent);
        }

        [Test]
        public void Testv2CONF()
        {
            var m = new MemoryStream(Resources.SNMPv2_CONF);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMPv2-CONF", file.Modules[0].Name);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void Testv2_TC()
        {
            var m = new MemoryStream(Resources.SNMPv2_TC);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMPv2-TC", file.Modules[0].Name);
            Assert.AreEqual(0, file.Modules[0].Entities.Count);
        }

        [Test]
        public void Testv2_SMI()
        {
            var m = new MemoryStream(Resources.SNMPv2_SMI);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual(1, file.Modules.Count);
            Assert.AreEqual("SNMPv2-SMI", file.Modules[0].Name);
            Assert.AreEqual(16, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[15];
            Assert.AreEqual("zeroDotZero", node.Name);
            Assert.AreEqual(0, node.Value);
            Assert.AreEqual("ccitt", node.Parent);
        }

        [Test]
        public void Testv2_MIB()
        {
            var m = new MemoryStream(Resources.SNMPv2_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("SNMPv2-MIB", file.Modules[0].Name);
            Assert.AreEqual(3, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(70, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[69];
            Assert.AreEqual("snmpObsoleteGroup", node.Name);
            Assert.AreEqual(10, node.Value);
            Assert.AreEqual("snmpMIBGroups", node.Parent);
            Assert.AreEqual(47, file.Modules[0].Objects.Count);
        }

        [Test]
        public void TestIANAifType_MIB()
        {
            var m = new MemoryStream(Resources.IANAifType_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("IANAifType-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("ianaifType", node.Name);
            Assert.AreEqual(30, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestIF_MIB()
        {
            var m = new MemoryStream(Resources.IF_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("IF-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(91, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[90];
            Assert.AreEqual("ifCompliance2", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("ifCompliances", node.Parent);
        }

        [Test]
        public void TestINET_ADDRESS_MIB()
        {
            var m = new MemoryStream(Resources.INET_ADDRESS_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("INET-ADDRESS-MIB", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(1, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[0];
            Assert.AreEqual("inetAddressMIB", node.Name);
            Assert.AreEqual(76, node.Value);
            Assert.AreEqual("mib-2", node.Parent);
        }

        [Test]
        public void TestIP_MIB()
        {
            var m = new MemoryStream(Resources.IP_MIB);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("IP-MIB", file.Modules[0].Name);
            Assert.AreEqual(5, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(293, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[292];
            Assert.AreEqual("icmpGroup", node.Name);
            Assert.AreEqual(2, node.Value);
            Assert.AreEqual("ipMIBGroups", node.Parent);
        }

        [Test]
        public void Testv2TM()
        {
            var m = new MemoryStream(Resources.SNMPv2_TM);
            var stream = new ANTLRInputStream(m);
            var lexer = new SmiLexer(stream);
            var tokens = new CommonTokenStream(lexer);
            var parser = new SmiParser(tokens);
            var file = parser.GetDocument();Assert.AreEqual(0, parser.Warnings.Count);
            Assert.AreEqual("SNMPv2-TM", file.Modules[0].Name);
            Assert.AreEqual(2, file.Modules[0].Imports.Clauses.Count);
            Assert.AreEqual(8, file.Modules[0].Entities.Count);
            IEntity node = file.Modules[0].Entities[7];
            Assert.AreEqual("rfc1157Domain", node.Name);
            Assert.AreEqual(1, node.Value);
            Assert.AreEqual("rfc1157Proxy", node.Parent);
        }
    }
}
