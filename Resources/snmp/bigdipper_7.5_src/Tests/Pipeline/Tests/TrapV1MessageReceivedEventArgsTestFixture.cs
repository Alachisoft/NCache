using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Pipeline.Tests
{
    [TestFixture]
    public class TrapV1MessageReceivedEventArgsTestFixture
    {
        [Test]
        public void TestException()
        {
            Assert.Throws<ArgumentNullException>(() => new TrapV1MessageReceivedEventArgs(null, null, null));
            Assert.Throws<ArgumentNullException>(
                () => new TrapV1MessageReceivedEventArgs(new IPEndPoint(IPAddress.Any, 0), null, null));
            IList<Variable> v = new List<Variable>();
            Assert.Throws<ArgumentNullException>(
                () =>
                new TrapV1MessageReceivedEventArgs(new IPEndPoint(IPAddress.Any, 0),
                                                   new TrapV1Message(VersionCode.V1, IPAddress.Any,
                                                                     new OctetString("community"),
                                                                     new ObjectIdentifier("1.3.6"),
                                                                     GenericCode.ColdStart, 0, 0, v),
                                                   null));
        }
    }
}
