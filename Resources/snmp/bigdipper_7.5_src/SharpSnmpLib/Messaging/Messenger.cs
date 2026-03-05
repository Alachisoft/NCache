// Messenger class.
// Copyright (C) 2009-2010 Lex Li
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2009/3/29
 * Time: 17:52
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Lextm.SharpSnmpLib.Security;
using System.Net.Sockets;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Messenger class contains all static helper methods you need to send out SNMP messages.
    /// Static methods in Manager or Agent class will be removed in the future.
    /// </summary>
    /// <remarks>
    /// SNMP v3 is not supported by this class. Please use ISnmpMessage derived classes directly
    /// if you want to do v3 operations.
    /// </remarks>
    public class Messenger
    {
        /// <summary>
        /// RFC 3416 (3.)
        /// </summary>
        private static readonly NumberGenerator RequestCounter = new NumberGenerator(int.MinValue, int.MaxValue); 
        
        /// <summary>
        /// RFC 3412 (6.)
        /// </summary>
        private static readonly NumberGenerator MessageCounter = new NumberGenerator(0, int.MaxValue); 
        private static int _maxMessageSize = Header.MaxMessageSize;
        private VersionCode _version;
        private IPEndPoint _endpoint;
        private int _timeout;
        private Socket _socket;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        public Messenger(VersionCode version, IPEndPoint endpoint, int timeout)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }

            if (version == VersionCode.V3)
            {
                throw new NotSupportedException("SNMP v3 is not supported");
            }

            _version = version;
            _endpoint = endpoint;
            _timeout = timeout;

            _socket = _endpoint.GetSocket();
        }

        /// <summary>
        /// Gets a list of oid variable binds.
        /// </summary>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <returns></returns>
        public IList<Variable> GetValuesAgaintOIDs(OctetString community, IList<Variable> variables)
        {
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }

            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            var message = new GetRequestMessage(RequestCounter.NextId, _version, community, variables);
            var response = message.GetResponse(_timeout, _endpoint, _socket);
            var pdu = response.Pdu();
            if (pdu.ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    _endpoint.Address,
                    response);
            }

            return pdu.Variables;
        }

        /// <summary>
        /// Gets a list of variable binds.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <returns></returns>
        public static IList<Variable> Get(VersionCode version, IPEndPoint endpoint, OctetString community, IList<Variable> variables, int timeout)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }
            
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }
            
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (version == VersionCode.V3)
            {
                throw new NotSupportedException("SNMP v3 is not supported");
            }

            var message = new GetRequestMessage(RequestCounter.NextId, version, community, variables);
            var response = message.GetResponse(timeout, endpoint);
            var pdu = response.Pdu();
            if (pdu.ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    endpoint.Address,
                    response);
            }

            return pdu.Variables;
        }
        
        /// <summary>
        /// Sets a list of variable binds.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="variables">Variable binds.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <returns></returns>
        public static IList<Variable> Set(VersionCode version, IPEndPoint endpoint, OctetString community, IList<Variable> variables, int timeout)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }
            
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }
            
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (version == VersionCode.V3)
            {
                throw new NotSupportedException("SNMP v3 is not supported");
            }

            var message = new SetRequestMessage(RequestCounter.NextId, version, community, variables);
            var response = message.GetResponse(timeout, endpoint);
            var pdu = response.Pdu();
            if (pdu.ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    endpoint.Address,
                    response);
            }

            return pdu.Variables;
        }
        
        /// <summary>
        /// Walks.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">OID.</param>
        /// <param name="list">A list to hold the results.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="mode">Walk mode.</param>
        /// <returns>
        /// Returns row count if the OID is a table. Otherwise this value is meaningless.
        /// </returns>
        public static int Walk(VersionCode version, IPEndPoint endpoint, OctetString community, ObjectIdentifier table, IList<Variable> list, int timeout, WalkMode mode)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            var result = 0;
            var tableV = new Variable(table);
            Variable seed;
            var next = tableV;
            var rowMask = string.Format(CultureInfo.InvariantCulture, "{0}.1.1.", table);
            var subTreeMask = string.Format(CultureInfo.InvariantCulture, "{0}.", table);
            do
            {
                seed = next;
                if (seed == tableV)
                {
                    continue;
                }

                if (mode == WalkMode.WithinSubtree && !seed.Id.ToString().StartsWith(subTreeMask, StringComparison.Ordinal))
                {
                    // not in sub tree
                    break;
                }

                list.Add(seed);
                if (seed.Id.ToString().StartsWith(rowMask, StringComparison.Ordinal))
                {
                    result++;
                }
            }
            while (HasNext(version, endpoint, community, seed, timeout, out next));
            return result;
        }
        
        /// <summary>
        /// Determines whether the specified seed has next item.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="community">The community.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="next">The next.</param>
        /// <returns>
        ///     <c>true</c> if the specified seed has next item; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "5#")]
        private static bool HasNext(VersionCode version, IPEndPoint endpoint, OctetString community, Variable seed, int timeout, out Variable next)
        {
            if (seed == null)
            {
                throw new ArgumentNullException("seed");
            }

            var variables = new List<Variable> { new Variable(seed.Id) };
            var message = new GetNextRequestMessage(
                RequestCounter.NextId,
                version,
                community,
                variables);

            var response = message.GetResponse(timeout, endpoint);
            var pdu = response.Pdu();
            var errorFound = pdu.ErrorStatus.ToErrorCode() == ErrorCode.NoSuchName;
            next = errorFound ? null : pdu.Variables[0];
            return !errorFound;
        }

        /// <summary>
        /// Walks.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">OID.</param>
        /// <param name="list">A list to hold the results.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="maxRepetitions">The max repetitions.</param>
        /// <param name="mode">Walk mode.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="report">The report.</param>
        /// <returns></returns>
        public static int BulkWalk(VersionCode version, IPEndPoint endpoint, OctetString community, ObjectIdentifier table, IList<Variable> list, int timeout, int maxRepetitions, WalkMode mode, IPrivacyProvider privacy, ISnmpMessage report)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }

            var tableV = new Variable(table);
            var seed = tableV;
            IList<Variable> next;
            var result = 0;
            var message = report;
            while (BulkHasNext(version, endpoint, community, seed, timeout, maxRepetitions, out next, privacy, ref message))
            {
                var subTreeMask = string.Format(CultureInfo.InvariantCulture, "{0}.", table);
                var rowMask = string.Format(CultureInfo.InvariantCulture, "{0}.1.1.", table);
                foreach (var v in next)
                {
                    var id = v.Id.ToString();
                    if (v.Data.TypeCode == SnmpType.EndOfMibView)
                    {
                        goto end;
                    }

                    if (mode == WalkMode.WithinSubtree && !id.StartsWith(subTreeMask, StringComparison.Ordinal))
                    {
                        // not in sub tree
                        goto end;
                    }

                    list.Add(v);
                    if (id.StartsWith(rowMask, StringComparison.Ordinal))
                    {
                        result++;
                    }
                }

                seed = next[next.Count - 1];
            }
            
        end:
            return result;
        }

        /// <summary>
        /// Sends a TRAP v1 message.
        /// </summary>
        /// <param name="receiver">Receiver.</param>
        /// <param name="agent">Agent.</param>
        /// <param name="community">Community name.</param>
        /// <param name="enterprise">Enterprise OID.</param>
        /// <param name="generic">Generic code.</param>
        /// <param name="specific">Specific code.</param>
        /// <param name="timestamp">Timestamp.</param>
        /// <param name="variables">Variable bindings.</param>
        [CLSCompliant(false)]
        public static void SendTrapV1(EndPoint receiver, IPAddress agent, OctetString community, ObjectIdentifier enterprise, GenericCode generic, int specific, uint timestamp, IList<Variable> variables)
        {
            var message = new TrapV1Message(VersionCode.V1, agent, community, enterprise, generic, specific, timestamp, variables);
            message.Send(receiver);
        }

        /// <summary>
        /// Sends TRAP v2 message.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="receiver">Receiver.</param>
        /// <param name="community">Community name.</param>
        /// <param name="enterprise">Enterprise OID.</param>
        /// <param name="timestamp">Timestamp.</param>
        /// <param name="variables">Variable bindings.</param>
        /// <param name="requestId">Request ID.</param>
        [CLSCompliant(false)]
        public static void SendTrapV2(int requestId, VersionCode version, EndPoint receiver, OctetString community, ObjectIdentifier enterprise, uint timestamp, IList<Variable> variables)
        {
            if (version != VersionCode.V2)
            {
                throw new ArgumentException("Only SNMP v2c is supported", "version");
            }

            var message = new TrapV2Message(requestId, version, community, enterprise, timestamp, variables);
            message.Send(receiver);
        }

        /// <summary>
        /// Sends INFORM message.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="version">Protocol version.</param>
        /// <param name="receiver">Receiver.</param>
        /// <param name="community">Community name.</param>
        /// <param name="enterprise">Enterprise OID.</param>
        /// <param name="timestamp">Timestamp.</param>
        /// <param name="variables">Variable bindings.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="report">The report.</param>
        [CLSCompliant(false)]
        public static void SendInform(int requestId, VersionCode version, IPEndPoint receiver, OctetString community, ObjectIdentifier enterprise, uint timestamp, IList<Variable> variables, int timeout, IPrivacyProvider privacy, ISnmpMessage report)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException("receiver");
            }
            
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }
            
            if (enterprise == null)
            {
                throw new ArgumentNullException("enterprise");
            }
            
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (version == VersionCode.V3 && privacy == null)
            {
                throw new ArgumentNullException("privacy");
            }
            
            if (version == VersionCode.V3 && report == null)
            {
                throw new ArgumentNullException("report");
            }
            
            var message = version == VersionCode.V3
                                    ? new InformRequestMessage(
                                          version,
                                          MessageCounter.NextId,
                                          requestId,
                                          community,
                                          enterprise,
                                          timestamp,
                                          variables,
                                          privacy,
                                          MaxMessageSize,
                                          report)
                                    : new InformRequestMessage(
                                          requestId,
                                          version,
                                          community,
                                          enterprise,
                                          timestamp,
                                          variables);
            
            var response = message.GetResponse(timeout, receiver);
            if (response.Pdu().ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    receiver.Address,
                    response);
            }
        }

        /// <summary>
        /// Gets a table of variables.
        /// </summary>
        /// <param name="version">Protocol version.</param>
        /// <param name="endpoint">Endpoint.</param>
        /// <param name="community">Community name.</param>
        /// <param name="table">Table OID.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="maxRepetitions">The max repetitions.</param>
        /// <param name="registry">The registry.</param>
        /// <returns></returns>
        /// <remarks><paramref name="registry"/> can be null, which also disables table validation.</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Return", Justification = "ByDesign")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1814:PreferJaggedArraysOverMultidimensional", MessageId = "Body", Justification = "ByDesign")]
        [CLSCompliant(false)]
        [Obsolete("This method only works for a few scenarios. Will provide new methods in the future. If it does not work for you, parse WALK result on your own.")]
        public static Variable[,] GetTable(VersionCode version, IPEndPoint endpoint, OctetString community, ObjectIdentifier table, int timeout, int maxRepetitions, IObjectRegistry registry)
        {
            if (version == VersionCode.V3)
            {
                throw new NotSupportedException("SNMP v3 is not supported");
            }

            var canContinue = registry == null || registry.ValidateTable(table);
            if (!canContinue)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "not a table OID: {0}", table));
            }
            
            IList<Variable> list = new List<Variable>();
            var rows = version == VersionCode.V1 ? Walk(version, endpoint, community, table, list, timeout, WalkMode.WithinSubtree) : BulkWalk(version, endpoint, community, table, list, timeout, maxRepetitions, WalkMode.WithinSubtree, null, null);
            
            if (rows == 0)
            {
                return new Variable[0, 0];
            }

            var cols = list.Count / rows;
            var k = 0;
            var result = new Variable[rows, cols];

            for (var j = 0; j < cols; j++)
            {
                for (var i = 0; i < rows; i++)
                {
                    result[i, j] = list[k];
                    k++;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the request counter.
        /// </summary>
        /// <value>The request counter.</value>
        public static int NextRequestId
        {
            get { return RequestCounter.NextId; }
        }

        /// <summary>
        /// Gets the message counter.
        /// </summary>
        /// <value>The message counter.</value>
        public static int NextMessageId
        {
            get { return MessageCounter.NextId; }
        }

        /// <summary>
        /// Max message size used in #SNMP. 
        /// </summary>
        /// <remarks>
        /// You can use any value for your own application. 
        /// Also this value may be changed in #SNMP in future releases.
        /// </remarks>
        public static int MaxMessageSize
        {
            get { return _maxMessageSize; }
            set { _maxMessageSize = value; }
        }

        /// <summary>
        /// Returns a new discovery request.
        /// </summary>
        /// <returns></returns>
        public static Discovery NextDiscovery
        {
            get { return new Discovery(NextMessageId, NextRequestId, MaxMessageSize); }
        }

        /// <summary>
        /// Determines whether the specified seed has next item.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="community">The community.</param>
        /// <param name="seed">The seed.</param>
        /// <param name="timeout">The time-out value, in milliseconds. The default value is 0, which indicates an infinite time-out period. Specifying -1 also indicates an infinite time-out period.</param>
        /// <param name="maxRepetitions">The max repetitions.</param>
        /// <param name="next">The next.</param>
        /// <param name="privacy">The privacy provider.</param>
        /// <param name="report">The report.</param>
        /// <returns>
        /// <c>true</c> if the specified seed has next item; otherwise, <c>false</c>.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters", MessageId = "5#")]
        private static bool BulkHasNext(VersionCode version, IPEndPoint endpoint, OctetString community, Variable seed, int timeout, int maxRepetitions, out IList<Variable> next, IPrivacyProvider privacy, ref ISnmpMessage report)
        {
            if (version == VersionCode.V1)
            {
                throw new ArgumentException("v1 is not supported", "version");
            }

            var variables = new List<Variable> { new Variable(seed.Id) };
            var requestId = RequestCounter.NextId;
            var message = version == VersionCode.V3
                                                ? new GetBulkRequestMessage(
                                                      version,
                                                      MessageCounter.NextId,
                                                      requestId,
                                                      community,
                                                      0,
                                                      maxRepetitions,
                                                      variables, 
                                                      privacy, 
                                                      MaxMessageSize,
                                                      report)
                                                : new GetBulkRequestMessage(
                                                      requestId,
                                                      version,
                                                      community,
                                                      0,
                                                      maxRepetitions,
                                                      variables);

            var response = message.GetResponse(timeout, endpoint);
            var pdu = response.Pdu();
            if (pdu.ErrorStatus.ToInt32() != 0)
            {
                throw ErrorException.Create(
                    "error in response",
                    endpoint.Address,
                    response);
            }

            next = pdu.Variables;
            report = message;
            return next.Count != 0;
        }

        public void Dispose()
        {
            if (_socket != null)
                _socket.Close(1000);
        }
    }
}
