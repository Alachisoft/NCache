// TRAP message type (SNMP version 1 only).
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

/*
 * Created by SharpDevelop.
 * User: lextm
 * Date: 2008/4/25
 * Time: 20:30
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using Lextm.SharpSnmpLib.Security;

namespace Lextm.SharpSnmpLib.Messaging
{
    /// <summary>
    /// Trap message.
    /// </summary>
    public sealed class TrapV1Message : ISnmpMessage
    {
        private Scope _scope;
        private readonly ISnmpPdu _pdu;
        private readonly Sequence _container;

        /// <summary>
        /// Creates a <see cref="TrapV1Message"/> with all content.
        /// </summary>
        /// <param name="version">Protocol version</param>
        /// <param name="agent">Agent address</param>
        /// <param name="community">Community name</param>
        /// <param name="enterprise">Enterprise</param>
        /// <param name="generic">Generic</param>
        /// <param name="specific">Specific</param>
        /// <param name="time">Time</param>
        /// <param name="variables">Variables</param>
        [CLSCompliant(false)]
        public TrapV1Message(VersionCode version, IPAddress agent, OctetString community, ObjectIdentifier enterprise, GenericCode generic, int specific, uint time, IList<Variable> variables)
        {
            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }
            
            if (enterprise == null)
            {
                throw new ArgumentNullException("enterprise");
            }
            
            if (community == null)
            {
                throw new ArgumentNullException("community");
            }
            
            if (agent == null)
            {
                throw new ArgumentNullException("agent");
            }
            
            if (version != VersionCode.V1)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "TRAP v1 is not supported in this SNMP version: {0}", version), "version");
            }
            
            Version = version;
            AgentAddress = agent;
            Community = community;
            Enterprise = enterprise;
            Generic = generic;
            Specific = specific;
            TimeStamp = time;
            var pdu = new TrapV1Pdu(
                Enterprise,
                new IP(AgentAddress),
                new Integer32((int)Generic),
                new Integer32(Specific),
                new TimeTicks(TimeStamp),
                variables);
            _pdu = pdu;
            Parameters = SecurityParameters.Create(Community);
        }
        
        /// <summary>
        /// Creates a <see cref="TrapV1Message"/> instance with a message body.
        /// </summary>
        /// <param name="body">Message body</param>
        public TrapV1Message(Sequence body)
        {
            if (body == null)
            {
                throw new ArgumentNullException("body");
            }
            
            if (body.Length != 3)
            {
                throw new ArgumentException("wrong message body");
            }
            
            Community = (OctetString)body[1];
            Version = (VersionCode)((Integer32)body[0]).ToInt32();
            
            // IMPORTANT: comment this check out if you need to support 
            if (Version != VersionCode.V1)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "TRAP v1 is not supported in this SNMP version: {0}", Version), "body");
            }
                        
            _pdu = (ISnmpPdu)body[2];
            if (_pdu.TypeCode != SnmpType.TrapV1Pdu)
            {
                throw new ArgumentException("wrong message type");
            }

            var trapPdu = (TrapV1Pdu)_pdu;
            Enterprise = trapPdu.Enterprise;
            AgentAddress = trapPdu.AgentAddress.ToIPAddress();
            Generic = trapPdu.Generic;
            Specific = trapPdu.Specific;
            TimeStamp = trapPdu.TimeStamp.ToUInt32();
            Parameters = SecurityParameters.Create(Community);
            _container = body;
        }

        /// <summary>
        /// Time stamp.
        /// </summary>
        [CLSCompliant(false), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TimeStamp")]
        public uint TimeStamp { get; private set; }

        /// <summary>
        /// Community name.
        /// </summary>
        public OctetString Community { get; private set; }

        /// <summary>
        /// Enterprise.
        /// </summary>
        public ObjectIdentifier Enterprise { get; private set; }

        /// <summary>
        /// Agent address.
        /// </summary>
        public IPAddress AgentAddress { get; private set; }

        /// <summary>
        /// Generic type.
        /// </summary>
        public GenericCode Generic { get; private set; }

        /// <summary>
        /// Specific type.
        /// </summary>
        public int Specific { get; private set; }

        /// <summary>
        /// Protocol version.
        /// </summary>
        public VersionCode Version { get; private set; }

        /// <summary>
        /// Gets the header.
        /// </summary>
        public Header Header 
        { 
            get { throw new NotSupportedException(); }
        }
        
        /// <summary>
        /// Gets the privacy provider.
        /// </summary>
        public IPrivacyProvider Privacy
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// To byte format.
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            return _container == null ? PackMessage(Version, Community, _pdu).ToBytes() : _container.ToBytes();
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>The parameters.</value>
        /// <remarks><see cref="TrapV1Message"/> returns null here.</remarks>
        public SecurityParameters Parameters { get; private set; }

        /// <summary>
        /// Gets the scope.
        /// </summary>
        /// <value>The scope.</value>
        /// <remarks><see cref="TrapV1Message"/> returns null here.</remarks>
        public Scope Scope
        {
            get { return _scope ?? (_scope = new Scope(_pdu)); }
        }
        
        /// <summary>
        /// Returns a <see cref="string"/> that represents the current <see cref="TrapV1Message"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <param name="objects">The objects.</param>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        [CLSCompliant(false)]
        public string ToString(IObjectRegistry objects)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "SNMPv1 trap: {0}",
                ((TrapV1Pdu)_pdu).ToString(objects));
        }

        /// <summary>
        /// Packs the message.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        internal static Sequence PackMessage(VersionCode version, params ISnmpData[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            
            var collection = new List<ISnmpData>(1 + data.Length) { new Integer32((int)version) };
            collection.AddRange(data);
            return new Sequence(collection);
        }
    }
}
