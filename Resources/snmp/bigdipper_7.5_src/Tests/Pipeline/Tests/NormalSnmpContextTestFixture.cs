using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Moq;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Pipeline.Tests
{
    [TestFixture]
    public class NormalSnmpContextTestFixture
    {
        [Test]
        public void Test()
        {
            var message = new GetRequestMessage(0, VersionCode.V1, new OctetString("public"), new List<Variable>());
            var bindingMock = new Mock<IListenerBinding>();
            bindingMock.Setup(foo => foo.SendResponse(It.IsAny<ISnmpMessage>(), It.IsAny<EndPoint>())).AtMostOnce();
            var context = new NormalSnmpContext(message, new IPEndPoint(IPAddress.Loopback, 0),
                                                new UserRegistry(), bindingMock.Object);
            context.GenerateResponse(new List<Variable>());
            Assert.IsNotNull(context.Response);
            context.SendResponse();

            context.HandleAuthenticationFailure();
            Assert.IsNull(context.Response);

            Assert.IsFalse(context.HandleMembership());

            var list = new List<Variable>();
            for (int i = 0; i < 5000; i++)
            {
                list.Add(new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test")));
            }

            context.GenerateResponse(list);
            Assert.AreEqual(ErrorCode.TooBig, context.Response.Pdu().ErrorStatus.ToErrorCode());
        }
    }
}
