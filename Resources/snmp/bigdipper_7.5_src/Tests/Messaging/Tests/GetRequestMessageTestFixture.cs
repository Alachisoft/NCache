/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/28
 * Time: 18:35
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Security;
using NUnit.Framework;
using System.Net.Sockets;
using System;

#pragma warning disable 1591

namespace Lextm.SharpSnmpLib.Messaging.Tests
{
    /// <summary>
    /// Description of TestGetMessage.
    /// </summary>
    [TestFixture]
    public class GetRequestMessageTestFixture
    {
        [Test]
        public void Test()
        {
            byte[] expected = Properties.Resources.get;
            ISnmpMessage message = MessageFactory.ParseMessages(expected, new UserRegistry())[0];
            Assert.AreEqual(SnmpType.GetRequestPdu, message.TypeCode());
            GetRequestPdu pdu = (GetRequestPdu)message.Pdu();
            Assert.AreEqual(1, pdu.Variables.Count);
            Variable v = pdu.Variables[0];
            Assert.AreEqual(new uint[] { 1, 3, 6, 1, 2, 1, 1, 6, 0 }, v.Id.ToNumerical());
            Assert.AreEqual(typeof(Null), v.Data.GetType());
            Assert.GreaterOrEqual(expected.Length, message.ToBytes().Length);
        }

        [Test]
        public void TestConstructor()
        {
            List<Variable> list = new List<Variable>(1)
                                      {
                                          new Variable(new ObjectIdentifier(new uint[] {1, 3, 6, 1, 2, 1, 1, 6, 0}),
                                                       new Null())
                                      };
            GetRequestMessage message = new GetRequestMessage(0, VersionCode.V2, new OctetString("public"), list);
            Assert.GreaterOrEqual(Properties.Resources.get.Length, message.ToBytes().Length);
        }

        [Test]
        public void TestConstructorV3Auth1()
        {
            const string bytes = "30 73" +
                                 "02 01  03 " +
                                 "30 0F " +
                                 "02  02 35 41 " +
                                 "02  03 00 FF E3" +
                                 "04 01 05" +
                                 "02  01 03" +
                                 "04 2E  " +
                                 "30 2C" +
                                 "04 0D  80 00 1F 88 80 E9 63 00  00 D6 1F F4  49 " +
                                 "02 01 0D  " +
                                 "02 01 57 " +
                                 "04 05 6C 65 78  6C 69 " +
                                 "04 0C  1C 6D 67 BF  B2 38 ED 63 DF 0A 05 24  " +
                                 "04 00 " +
                                 "30 2D  " +
                                 "04 0D 80 00  1F 88 80 E9 63 00 00 D6  1F F4 49 " +
                                 "04  00 " +
                                 "A0 1A 02  02 01 AF 02 01 00 02 01  00 30 0E 30  0C 06 08 2B  06 01 02 01 01 03 00 05  00";
            ReportMessage report = new ReportMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(13633),
                    new Integer32(0xFFE3),
                    0),
                new SecurityParameters(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    new Integer32(0x0d),
                    new Integer32(0x57),
                    new OctetString("lexli"),
                    new OctetString(new byte[12]),
                    OctetString.Empty),
                new Scope(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    OctetString.Empty,
                    new ReportPdu(
                        0x01AF,
                        ErrorCode.NoError,
                        0,
                        new List<Variable>(1) { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0")) })),
                DefaultPrivacyProvider.DefaultPair,
                null);
            
            IPrivacyProvider privacy = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("testpass")));
            GetRequestMessage request = new GetRequestMessage(
                VersionCode.V3,
                13633,
                0x01AF,
                new OctetString("lexli"),
                new List<Variable>(1) { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0")) },
                privacy,
                Messenger.MaxMessageSize,
                report);
            
            Assert.AreEqual(Levels.Authentication | Levels.Reportable, request.Header.SecurityLevel);
            Assert.AreEqual(ByteTool.Convert(bytes), request.ToBytes());
        }

        [Test]
        public void TestConstructorV2AuthMd5PrivDes()
        {
            const string bytes = "30 81 80 02  01 03 30 0F  02 02 6C 99  02 03 00 FF" +
                                 "E3 04 01 07  02 01 03 04  38 30 36 04  0D 80 00 1F" +
                                 "88 80 E9 63  00 00 D6 1F  F4 49 02 01  14 02 01 35" +
                                 "04 07 6C 65  78 6D 61 72  6B 04 0C 80  50 D9 A1 E7" +
                                 "81 B6 19 80  4F 06 C0 04  08 00 00 00  01 44 2C A3" +
                                 "B5 04 30 4B  4F 10 3B 73  E1 E4 BD 91  32 1B CB 41" +
                                 "1B A1 C1 D1  1D 2D B7 84  16 CA 41 BF  B3 62 83 C4" +
                                 "29 C5 A4 BC  32 DA 2E C7  65 A5 3D 71  06 3C 5B 56" +
                                 "FB 04 A4";
            MD5AuthenticationProvider auth = new MD5AuthenticationProvider(new OctetString("testpass"));
            IPrivacyProvider privacy = new DESPrivacyProvider(new OctetString("passtest"), auth);
            GetRequestMessage request = new GetRequestMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(0x6C99),
                    new Integer32(0xFFE3),
                    Levels.Authentication | Levels.Privacy | Levels.Reportable),
                new SecurityParameters(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    new Integer32(0x14),
                    new Integer32(0x35),
                    new OctetString("lexmark"),
                    new OctetString(ByteTool.Convert("80  50 D9 A1 E7 81 B6 19 80  4F 06 C0")),
                    new OctetString(ByteTool.Convert("00 00 00  01 44 2C A3 B5"))),
                new Scope(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    OctetString.Empty,
                    new GetRequestPdu(
                        0x3A25,
                        new List<Variable>(1) { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0")) })),
                privacy,
                null);
            Assert.AreEqual(Levels.Authentication | Levels.Privacy | Levels.Reportable, request.Header.SecurityLevel);
            Assert.AreEqual(ByteTool.Convert(bytes), request.ToBytes());
        }

        [Test]
        public void TestConstructorV3AuthMd5()
        {
            const string bytes = "30 73" +
                                 "02 01  03 " +
                                 "30 0F " +
                                 "02  02 35 41 " +
                                 "02  03 00 FF E3" +
                                 "04 01 05" +
                                 "02  01 03" +
                                 "04 2E  " +
                                 "30 2C" +
                                 "04 0D  80 00 1F 88 80 E9 63 00  00 D6 1F F4  49 " +
                                 "02 01 0D  " +
                                 "02 01 57 " +
                                 "04 05 6C 65 78  6C 69 " +
                                 "04 0C  1C 6D 67 BF  B2 38 ED 63 DF 0A 05 24  " +
                                 "04 00 " +
                                 "30 2D  " +
                                 "04 0D 80 00  1F 88 80 E9 63 00 00 D6  1F F4 49 " +
                                 "04  00 " +
                                 "A0 1A 02  02 01 AF 02 01 00 02 01  00 30 0E 30  0C 06 08 2B  06 01 02 01 01 03 00 05  00";
            IPrivacyProvider pair = new DefaultPrivacyProvider(new MD5AuthenticationProvider(new OctetString("testpass")));
            GetRequestMessage request = new GetRequestMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(13633),
                    new Integer32(0xFFE3),
                    Levels.Authentication | Levels.Reportable),
                new SecurityParameters(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    new Integer32(0x0d),
                    new Integer32(0x57),
                    new OctetString("lexli"),
                    new OctetString(ByteTool.Convert("1C 6D 67 BF  B2 38 ED 63 DF 0A 05 24")),
                    OctetString.Empty),
                new Scope(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    OctetString.Empty,
                    new GetRequestPdu(
                        0x01AF,
                        new List<Variable>(1) { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"), new Null()) })),
                pair,
                null);
            Assert.AreEqual(Levels.Authentication | Levels.Reportable, request.Header.SecurityLevel);
            Assert.AreEqual(ByteTool.Convert(bytes), request.ToBytes());
        }

        [Test]
        public void TestConstructorV3AuthSha()
        {
            const string bytes = "30 77 02 01  03 30 0F 02  02 47 21 02  03 00 FF E3" +
                                 "04 01 05 02  01 03 04 32  30 30 04 0D  80 00 1F 88" +
                                 "80 E9 63 00  00 D6 1F F4  49 02 01 15  02 02 01 5B" +
                                 "04 08 6C 65  78 74 75 64  69 6F 04 0C  7B 62 65 AE" +
                                 "D3 8F E3 7D  58 45 5C 6C  04 00 30 2D  04 0D 80 00" +
                                 "1F 88 80 E9  63 00 00 D6  1F F4 49 04  00 A0 1A 02" +
                                 "02 56 FF 02  01 00 02 01  00 30 0E 30  0C 06 08 2B" +
                                 "06 01 02 01  01 03 00 05  00";
            IPrivacyProvider pair = new DefaultPrivacyProvider(new SHA1AuthenticationProvider(new OctetString("password")));
            GetRequestMessage request = new GetRequestMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(0x4721),
                    new Integer32(0xFFE3),
                    Levels.Authentication | Levels.Reportable),
                new SecurityParameters(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    new Integer32(0x15),
                    new Integer32(0x015B),
                    new OctetString("lextudio"),
                    new OctetString(ByteTool.Convert("7B 62 65 AE D3 8F E3 7D  58 45 5C 6C")),
                    OctetString.Empty),
                new Scope(
                    new OctetString(ByteTool.Convert("80 00 1F 88 80 E9 63 00  00 D6 1F F4  49")),
                    OctetString.Empty,
                    new GetRequestPdu(
                        0x56FF,
                        new List<Variable>(1) { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.3.0"), new Null()) })),
                pair,
                null);
            Assert.AreEqual(Levels.Authentication | Levels.Reportable, request.Header.SecurityLevel);
            Assert.AreEqual(ByteTool.Convert(bytes), request.ToBytes());
        }
  
        [Test]
        public void TestDiscoveryV3()
        {
            const string bytes = "30 3A 02 01 03 30 0F 02 02 6A 09 02 03 00 FF E3" +
                                 " 04 01 04 02 01 03 04 10 30 0E 04 00 02 01 00 02" +
                                 " 01 00 04 00 04 00 04 00 30 12 04 00 04 00 A0 0C" +
                                 " 02 02 2C 6B 02 01 00 02 01 00 30 00";
            GetRequestMessage request = new GetRequestMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(0x6A09),
                    new Integer32(0xFFE3),
                    Levels.Reportable),
                new SecurityParameters(
                    OctetString.Empty,
                    Integer32.Zero,
                    Integer32.Zero,
                    OctetString.Empty,
                    OctetString.Empty,
                    OctetString.Empty),
                new Scope(
                    OctetString.Empty,
                    OctetString.Empty,
                    new GetRequestPdu(0x2C6B, new List<Variable>())),
                DefaultPrivacyProvider.DefaultPair,
                null
               );
            string test = ByteTool.Convert(request.ToBytes());
            Assert.AreEqual(bytes, test);
        }

        [Test]
        public void TestToBytes()
        {
            const string s = "30 27 02 01  01 04 06 70  75 62 6C 69  63 A0 1A 02" +
                             "02 4B ED 02  01 00 02 01  00 30 0E 30  0C 06 08 2B" +
                             "06 01 02 01  01 01 00 05  00                      ";
            byte[] expected = ByteTool.Convert(s);
            GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString("public"), new List<Variable> { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")) });
            Assert.AreEqual(expected, message.ToBytes());
        }
        
        [Test]
        [Category("External")]
        public void TestTimeOutAsync()
        {
            // IMPORTANT: this test case requires a local SNMP agent such as 
            //   #SNMP Agent (snmpd), 
            //   Windows SNMP agent service, 
            //   Net-SNMP agent, or 
            //   snmp4j agent.
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString("public"), new List<Variable> { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")) });
            
            var users = new UserRegistry();
            var ar2 = message.BeginGetResponse(new IPEndPoint(IPAddress.Loopback, 161), users, socket, null, null);
            var response = message.EndGetResponse(ar2);
            Assert.AreEqual(SnmpType.ResponsePdu, response.TypeCode());
        }
        
        [Test]
        [Category("External")]
        public void TestTimeOut()
        {
            // IMPORTANT: this test case requires a local SNMP agent such as 
            //   #SNMP Agent (snmpd), 
            //   Windows SNMP agent service, 
            //   Net-SNMP agent, or 
            //   snmp4j agent.
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            GetRequestMessage message = new GetRequestMessage(0x4bed, VersionCode.V2, new OctetString("public"), new List<Variable> { new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0")) });
            
            const int time = 1500;
            bool hasException = false;
            try
            {
                message.GetResponse(time, new IPEndPoint(IPAddress.Loopback, 161), socket);
            }
            catch (TimeoutException)
            {
                hasException = true;
            }

            Assert.IsFalse(hasException);

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            
            try
            {
                timer.Start();
                //IMPORTANT: test against an agent that doesn't exist.
// ReSharper disable AssignNullToNotNullAttribute
                message.GetResponse(time, new IPEndPoint(IPAddress.Parse("192.168.0.233"), 161), socket);
// ReSharper restore AssignNullToNotNullAttribute
            }
            catch (TimeoutException)
            {
                hasException = true;
            }

            timer.Stop();            
            
            long elapsedMilliseconds = timer.ElapsedMilliseconds;
            Console.WriteLine(@"elapsed: " + elapsedMilliseconds);
            Console.WriteLine(@"timeout: " + time);
            Assert.LessOrEqual(time, elapsedMilliseconds);
            Assert.IsTrue(hasException);

            // FIXME: these values are valid on my machine openSUSE 11.2. (lex)
            // This test case usually fails on Windows, as strangely WinSock API call adds an extra 500-ms.
            if (SnmpMessageExtension.IsRunningOnMono)
            {
                Assert.LessOrEqual(elapsedMilliseconds, time + 100);
            }
        }
    }
}
#pragma warning restore 1591