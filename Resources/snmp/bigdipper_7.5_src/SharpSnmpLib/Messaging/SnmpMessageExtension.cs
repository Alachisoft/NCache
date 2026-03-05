// SNMP message extension class.
// Copyright (C) 2008-2010 Malcolm Crowe, Lex Li, and other contributors.
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Extension methods for <see cref="ISnmpMessage"/>.
    /// </summary>
    public static class SnmpMessageExtension
    {
        /// <summary>
        /// Gets the <see cref="SnmpType"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <returns></returns>
        public static SnmpType TypeCode(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            
            return message.Pdu().TypeCode;
        }
        
        /// <summary>
        /// Variables.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        public static IList<Variable> Variables(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            var code = message.TypeCode();
            return code == SnmpType.Unknown ? new List<Variable>(0) : message.Scope.Pdu.Variables;
        }

        /// <summary>
        /// Request ID.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        public static int RequestId(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return message.Scope.Pdu.RequestId.ToInt32();
        }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        /// <value>The message ID.</value>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <remarks>For v3, message ID is different from request ID. For v1 and v2c, they are the same.</remarks>
        public static int MessageId(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return message.Header == Header.Empty ? message.RequestId() : message.Header.MessageId;
        }

        /// <summary>
        /// PDU.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
        public static ISnmpPdu Pdu(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return message.Scope.Pdu;
        }

        /// <summary>
        /// Community name.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        public static OctetString Community(this ISnmpMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return message.Parameters.UserName;
        }

        /// <summary>
        /// Sends an <see cref="ISnmpMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="manager">Manager</param>
        public static void Send(this ISnmpMessage message, EndPoint manager)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            var code = message.TypeCode();
            if ((code != SnmpType.TrapV1Pdu && code != SnmpType.TrapV2Pdu) && code != SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "not a trap message: {0}",
                    code));
            }

            using (var socket = manager.GetSocket())
            {
                message.Send(manager, socket);
            }
        }

        /// <summary>
        /// Sends an <see cref="ISnmpMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="manager">Manager</param>
        /// <param name="socket">The socket.</param>
        public static void Send(this ISnmpMessage message, EndPoint manager, Socket socket)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            var code = message.TypeCode();
            if ((code != SnmpType.TrapV1Pdu && code != SnmpType.TrapV2Pdu) && code != SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "not a trap message: {0}",
                    code));
            }

            var bytes = message.ToBytes();
            socket.SendTo(bytes, 0, bytes.Length, SocketFlags.None, manager);
        }

        /// <summary>
        /// Sends an <see cref="ISnmpMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="manager">Manager</param>
        public static void SendAsync(this ISnmpMessage message, EndPoint manager)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            var code = message.TypeCode();
            if ((code != SnmpType.TrapV1Pdu && code != SnmpType.TrapV2Pdu) && code != SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "not a trap message: {0}",
                    code));
            }

            using (var socket = manager.GetSocket())
            {
                message.SendAsync(manager, socket);
            }
        }
        
        /// <summary>
        /// Sends an <see cref="ISnmpMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="manager">Manager</param>
        /// <param name="socket">The socket.</param>
        public static void SendAsync(this ISnmpMessage message, EndPoint manager, Socket socket)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }

            if (manager == null)
            {
                throw new ArgumentNullException("manager");
            }

            var code = message.TypeCode();
            if ((code != SnmpType.TrapV1Pdu && code != SnmpType.TrapV2Pdu) && code != SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "not a trap message: {0}",
                    code));
            }

            var bytes = message.ToBytes();
            socket.BeginSendTo(bytes, 0, bytes.Length, SocketFlags.None, manager, ar => socket.EndSendTo(ar), null);
        }
        
        /// <summary>
        /// Sends this <see cref="ISnmpMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">Port number.</param>
        /// <param name="registry">User registry.</param>
        /// <returns></returns>
        public static ISnmpMessage GetResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, UserRegistry registry)
        {
            // TODO: make more usage of UserRegistry.
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }

            var code = request.TypeCode();
            if (code == SnmpType.TrapV1Pdu || code == SnmpType.TrapV2Pdu || code == SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "not a request message: {0}", code));
            }
            
            using (var socket = receiver.GetSocket())
            {
                return request.GetResponse(timeout, receiver, registry, socket);
            }
        }

        /// <summary>
        /// Sends this <see cref="ISnmpMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">Port number.</param>
        /// <returns></returns>
        public static ISnmpMessage GetResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }

            var code = request.TypeCode();
            if (code == SnmpType.TrapV1Pdu || code == SnmpType.TrapV2Pdu || code == SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "not a request message: {0}", code));
            }
            
            using (var socket = receiver.GetSocket())
            {
                return request.GetResponse(timeout, receiver, socket);
            }
        }

        /// <summary>
        /// Sends this <see cref="ISnmpMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">Agent.</param>
        /// <param name="udpSocket">The UDP <see cref="Socket"/> to use to send/receive.</param>
        /// <returns></returns>
        public static ISnmpMessage GetResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, Socket udpSocket)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            
            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            
            if (udpSocket == null)
            {
                throw new ArgumentNullException("udpSocket");
            }
            
            var registry = new UserRegistry();
            if (request.Version == VersionCode.V3)
            {
                registry.Add(request.Parameters.UserName, request.Privacy);
            }

            return request.GetResponse(timeout, receiver, registry, udpSocket);
        }

        /// <summary>
        /// Sends an  <see cref="ISnmpMessage"/> and handles the response from agent.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="receiver">Agent.</param>
        /// <param name="udpSocket">The UDP <see cref="Socket"/> to use to send/receive.</param>
        /// <param name="registry">The user registry.</param>
        /// <returns></returns>
        public static ISnmpMessage GetResponse(this ISnmpMessage request, int timeout, IPEndPoint receiver, UserRegistry registry, Socket udpSocket)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (udpSocket == null)
            {
                throw new ArgumentNullException("udpSocket");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            
            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }

            var requestCode = request.TypeCode();
            if (requestCode == SnmpType.TrapV1Pdu || requestCode == SnmpType.TrapV2Pdu || requestCode == SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "not a request message: {0}", requestCode));
            }

            var bytes = request.ToBytes();
            #if CF
            int bufSize = 8192;
            #else
            var bufSize = udpSocket.ReceiveBufferSize;
            #endif
            var reply = new byte[bufSize];

            // Whatever you change, try to keep the Send and the Receive close to each other.
            udpSocket.SendTo(bytes, receiver);
            #if !(CF)
            udpSocket.ReceiveTimeout = timeout;
            #endif
            int count;
            try
            {
                count = udpSocket.Receive(reply, 0, bufSize, SocketFlags.None);
            }
            catch (SocketException ex)
            {
                // FIXME: If you use a Mono build without the fix for this issue (https://bugzilla.novell.com/show_bug.cgi?id=599488), please uncomment this code.
                /*
                if (SnmpMessageExtension.IsRunningOnMono && ex.ErrorCode == 10035)
                {
                    throw TimeoutException.Create(receiver.Address, timeout);
                }
                // */

                if (ex.ErrorCode == WSAETIMEDOUT)
                {
                    throw TimeoutException.Create(receiver.Address, timeout);
                }

                throw;
            }

            // Passing 'count' is not necessary because ParseMessages should ignore it, but it offer extra safety (and would avoid an issue if parsing >1 response).
            var response = MessageFactory.ParseMessages(reply, 0, count, registry)[0];
            var responseCode = response.TypeCode();
            if (responseCode == SnmpType.ResponsePdu || responseCode == SnmpType.ReportPdu)
            {
                var requestId = request.MessageId();
                var responseId = response.MessageId();
                if (responseId != requestId)
                {
                    throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response sequence: expected {0}, received {1}", requestId, responseId), receiver.Address);
                }

                return response;
            }

            throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response type: {0}", responseCode), receiver.Address);
        }
        
        /// <summary>
        /// Ends a pending asynchronous read.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="asyncResult">An <see cref="IAsyncResult"/> that stores state information and any user defined data for this asynchronous operation.</param>
        /// <returns></returns>
        public static ISnmpMessage EndGetResponse(this ISnmpMessage request, IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }
            
            var ar = (SnmpMessageAsyncResult)asyncResult;
            var s = ar.WorkSocket;
            var count = s.EndReceive(ar.Inner);
            
            // Passing 'count' is not necessary because ParseMessages should ignore it, but it offer extra safety (and would avoid an issue if parsing >1 response).
            var response = MessageFactory.ParseMessages(ar.GetBuffer(), 0, count, ar.Users)[0];
            var responseCode = response.TypeCode();
            if (responseCode == SnmpType.ResponsePdu || responseCode == SnmpType.ReportPdu)
            {
                var requestId = request.MessageId();
                var responseId = response.MessageId();
                if (responseId != requestId)
                {
                    throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response sequence: expected {0}, received {1}", requestId, responseId), ar.Receiver.Address);
                }

                return response;
            }

            throw OperationException.Create(string.Format(CultureInfo.InvariantCulture, "wrong response type: {0}", responseCode), ar.Receiver.Address);
        }

        /// <summary>
        /// Begins to asynchronously send an <see cref="ISnmpMessage"/> to an <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="request">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="receiver">Agent.</param>
        /// <param name="registry">The user registry.</param>
        /// <param name="udpSocket">The UDP <see cref="Socket"/> to use to send/receive.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state object.</param>
        /// <returns></returns>
        public static IAsyncResult BeginGetResponse(this ISnmpMessage request, IPEndPoint receiver, UserRegistry registry, Socket udpSocket, AsyncCallback callback, object state)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (udpSocket == null)
            {
                throw new ArgumentNullException("udpSocket");
            }

            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }

            if (registry == null)
            {
                throw new ArgumentNullException("registry");
            }

            var requestCode = request.TypeCode();
            if (requestCode == SnmpType.TrapV1Pdu || requestCode == SnmpType.TrapV2Pdu || requestCode == SnmpType.ReportPdu)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "not a request message: {0}", requestCode));
            }

            // Whatever you change, try to keep the Send and the Receive close to each other.
            udpSocket.SendTo(request.ToBytes(), receiver);
            #if CF
            var bufferSize = 8192;
            #else
            var bufferSize = udpSocket.ReceiveBufferSize;
            #endif
            var buffer = new byte[bufferSize];

            // http://sharpsnmplib.codeplex.com/workitem/7234
            if (callback != null)
            {
                AsyncCallback wrapped = callback;
                callback = asyncResult =>
                {
                    var result = new SnmpMessageAsyncResult(asyncResult, udpSocket, registry, receiver, buffer);
                    wrapped(result);
                };
            }

            var ar = udpSocket.BeginReceive(buffer, 0, bufferSize, SocketFlags.None, callback, state);
            return new SnmpMessageAsyncResult(ar, udpSocket, registry, receiver, buffer);
        }

        /// <summary>
        /// Tests if runnning on Mono.
        /// </summary>
        /// <returns></returns>
        public static bool IsRunningOnMono
        {
            get { return Type.GetType("Mono.Runtime") != null; }
        }

        /// <summary>
        /// Packs up the <see cref="ISnmpMessage"/>.
        /// </summary>
        /// <param name="message">The <see cref="ISnmpMessage"/>.</param>
        /// <param name="length">The length bytes.</param>
        /// <returns></returns>
        internal static Sequence PackMessage(this ISnmpMessage message, byte[] length)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return ByteTool.PackMessage(
                length,
                message.Version,
                message.Header,
                message.Parameters,
                message.Privacy.GetScopeData(message.Header, message.Parameters, message.Scope.GetData(message.Version)));
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/ms740668(VS.85).aspx
        /// </summary>
        private const int WSAETIMEDOUT = 10060;

        private sealed class SnmpMessageAsyncResult : IAsyncResult
        {
            private readonly byte[] _buffer;
            
            public SnmpMessageAsyncResult(IAsyncResult inner, Socket socket, UserRegistry users, IPEndPoint receiver, byte[] buffer)
            {
                _buffer = buffer;
                WorkSocket = socket;
                Users = users;
                Receiver = receiver;
                Inner = inner;
            }
            
            public IAsyncResult Inner { get; private set; }
            
            public Socket WorkSocket { get; private set; }
            
            public UserRegistry Users { get; private set; }

            public byte[] GetBuffer()
            {
                return _buffer;
            }
            
            public IPEndPoint Receiver { get; private set; }
            
            public bool IsCompleted
            {
                get { return Inner.IsCompleted; }
            }
            
            public System.Threading.WaitHandle AsyncWaitHandle
            {
                get { return Inner.AsyncWaitHandle; }
            }
            
            public object AsyncState
            {
                get { return Inner.AsyncState; }
            }
            
            public bool CompletedSynchronously
            {
                get { return Inner.CompletedSynchronously; }
            }
        }
    }
}
