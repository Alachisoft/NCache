/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/5/1
 * Time: 20:29
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System.Collections.Generic;
using Lextm.SharpSnmpLib.Security;
using NUnit.Framework;

#pragma warning disable 1591
namespace Lextm.SharpSnmpLib.Messaging.Tests
{
    [TestFixture]
    public class ResponseMessageTestFixture
    {
        [Test]
        public void TestConstructor()
        {
            var response = new ResponseMessage(
                VersionCode.V3,
                new Header(
                    new Integer32(500),
                    new Integer32(4000),
                    Levels.Reportable),
                new SecurityParameters(
                    OctetString.Empty,
                    Integer32.Zero,
                    Integer32.Zero,
                    new OctetString("lextm"),
                    OctetString.Empty,
                    OctetString.Empty),
                new Scope(
                    OctetString.Empty,
                    OctetString.Empty,
                    new ResponsePdu(0x2C6B, ErrorCode.NoError, 0, new List<Variable>{ new Variable(new ObjectIdentifier("1.3.6.1.1.2.5.0"), new Integer32(400))})),
                DefaultPrivacyProvider.DefaultPair,
                true,
                null);
            var registry = new UserRegistry();
            registry.Add(new OctetString("lextm"), DefaultPrivacyProvider.DefaultPair);
            var messages = MessageFactory.ParseMessages(response.ToBytes(), registry);
            Assert.AreEqual(1, messages.Count);
        }
    }
}
#pragma warning restore 1591

