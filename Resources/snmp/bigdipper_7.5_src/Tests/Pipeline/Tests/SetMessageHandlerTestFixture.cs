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
    public class SetMessageHandlerTestFixture
    {
        [Test]
        public void WrongType()
        {
            var handler = new SetMessageHandler();
            var mock = new Mock<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            mock.Setup(foo => foo.Data).Throws<Exception>();
            mock.Setup(foo => foo.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"))).Returns(mock.Object);
            mock.SetupSet(foo => foo.Data = new Integer32(400)).Throws<ArgumentException>();
            var store = new ObjectStore();
            store.Add(mock.Object);
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new Integer32(400))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var wrongType = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.WrongType, wrongType.ErrorStatus);
        }

        [Test]
        public void NoAccess()
        {
            var handler = new SetMessageHandler();
            var mock = new Mock<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            mock.Setup(foo => foo.Data).Throws<Exception>();
            mock.Setup(foo => foo.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"))).Returns(mock.Object);
            mock.SetupSet(foo => foo.Data = new OctetString("test")).Throws<AccessFailureException>();
            var store = new ObjectStore();
            store.Add(mock.Object);
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            handler.Handle(context, store);
            var noAccess = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.NoAccess, noAccess.ErrorStatus);
        }

        [Test]
        public void GenError()
        {
            var handler = new SetMessageHandler();
            var mock = new Mock<ScalarObject>(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"));
            mock.Setup(foo => foo.Data).Throws<Exception>();
            mock.Setup(foo => foo.MatchGet(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"))).Returns(mock.Object);
            mock.SetupSet(foo => foo.Data = new OctetString("test")).Throws<Exception>();
            var store = new ObjectStore();
            store.Add(mock.Object);
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
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
        public void NoError()
        {
            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            store.Add(new SysContact());
            Assert.Throws<ArgumentNullException>(() => handler.Handle(null, null));
            Assert.Throws<ArgumentNullException>(() => handler.Handle(context, null));
            handler.Handle(context, store);
            var noerror = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.NoError, noerror.ErrorStatus);
            Assert.AreEqual(new OctetString("test"), noerror.Variables()[0].Data);
        }

        [Test]
        public void NotWritable()
        {
            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    new List<Variable>
                        {
                            new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test"))
                        }
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            handler.Handle(context, store);
            var notWritable = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.NotWritable, notWritable.ErrorStatus);
        }
        
        [Test]
        public void TooBig()
        {
            var list = new List<Variable>();
            for (int i = 0; i < 5000; i++)
            {
                list.Add(new Variable(new ObjectIdentifier("1.3.6.1.2.1.1.4.0"), new OctetString("test")));
            }

            var handler = new SetMessageHandler();
            var context = SnmpContextFactory.Create(
                new SetRequestMessage(
                    300,
                    VersionCode.V1,
                    new OctetString("lextm"),
                    list
                    ),
                new IPEndPoint(IPAddress.Loopback, 100),
                new UserRegistry(),
                null,
                null);
            var store = new ObjectStore();
            handler.Handle(context, store);
            var notWritable = (ResponseMessage)context.Response;
            Assert.AreEqual(ErrorCode.TooBig, notWritable.ErrorStatus);
        }
    }
}
