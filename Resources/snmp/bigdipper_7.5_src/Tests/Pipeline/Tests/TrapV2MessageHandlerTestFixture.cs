/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2010/12/6
 * Time: 17:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Moq;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Pipeline.Tests
{
    [TestFixture]
    public class TrapV2MessageHandlerTestFixture
    {
        [Test]
        public void Test()
        {
            var mock = new Mock<ISnmpContext>();
            var mock2 = new Mock<IListenerBinding>();
            IList<Variable> v = new List<Variable>();
            var message = new TrapV2Message(0, VersionCode.V2, new OctetString("community"), new ObjectIdentifier("1.3.6"), 0, v);
            mock.Setup(foo => foo.Binding).Returns(mock2.Object);
            mock.Setup(foo => foo.Request).Returns(message);
            mock.Setup(foo => foo.Sender).Returns(new IPEndPoint(IPAddress.Any, 0));
            mock.Setup(foo => foo.CopyRequest(ErrorCode.NoError, 0)).Verifiable("this must be called");
            var handler = new TrapV2MessageHandler();
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(mock.Object, null));
            handler.MessageReceived += delegate(object args, TrapV2MessageReceivedEventArgs e)
            {
                Assert.AreEqual(mock2.Object, e.Binding);
                Assert.AreEqual(message, e.TrapV2Message);
                Assert.IsTrue(new IPEndPoint(IPAddress.Any, 0).Equals(e.Sender));
            };
            handler.Handle(mock.Object, new ObjectStore());
        }
    }
}
