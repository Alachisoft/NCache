using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Moq;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Pipeline.Tests
{
    [TestFixture]
    public class SnmpContextFactoryTestFixture
    {
        [Test]
        public void Test()
        {
            var messageMock = new Mock<ISnmpMessage>();
            messageMock.Setup(foo => foo.Version).Returns(VersionCode.V3);
            var bindingMock = new Mock<IListenerBinding>();
            bindingMock.Setup(foo => foo.SendResponse(It.IsAny<ISnmpMessage>(), It.IsAny<EndPoint>())).AtMostOnce();
            var context = SnmpContextFactory.Create(messageMock.Object, new IPEndPoint(IPAddress.Loopback, 0), new UserRegistry(),
                                      new EngineGroup(),
                                      bindingMock.Object);
            context.SendResponse();
        }
    }
}
