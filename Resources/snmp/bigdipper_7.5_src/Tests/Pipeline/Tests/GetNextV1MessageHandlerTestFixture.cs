using System;
using System.Collections.Generic;
using System.Net;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Objects;
using Lextm.SharpSnmpLib.Security;
using Moq;
using NUnit.Framework;

namespace Lextm.SharpSnmpLib.Pipeline.Tests
{
    [TestFixture]
    public class GetNextV1MessageHandlerTestFixture
    {
        [Test]
        public void NoError()
        {
            var handler = new GetNextV1MessageHandler();
            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(new SysObjectId());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.AreEqual(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"), noerror.Variables()[0].Id);
        }

        [Test]
        public void GenError()
        {
            var handler = new GetNextV1MessageHandler();
            var mock = new Mock<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"));
            mock.Setup(foo => foo.Data).Throws<Exception>();
            mock.Setup(foo => foo.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"))).Returns(mock.Object);
            mock.Setup(foo => foo.MatchGetNext(new ObjectIdentifier("1.3.6.1.2.1.1.1.0"))).Returns(mock.Object);
            var store = new ObjectStore();
            store.Add(new SysDescr());
            store.Add(mock.Object);
            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.1.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var genError = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.GenError, genError.ErrorStatus);
        }

        [Test]
        public void NoSuchName()
        {
            var handler = new GetNextV1MessageHandler();
            var store = new ObjectStore();
            store.Add(new SysDescr());
            var context = SnmpContextFactory.Create(
                new GetNextRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.2.0"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var noSuchName = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.NoSuchName, noSuchName.ErrorStatus);
        }
    }
}
