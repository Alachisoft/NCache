// TRAP message PDU (SNMP version 1 only).
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
 * Date: 2008/4/30
 * Time: 21:22
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Trap v1 PDU.
    /// </summary>
    /// <remarks>represents the PDU of trap v1 message.</remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
    public sealed class TrapV1Pdu : ISnmpPdu
    {
        private byte[] _raw;
        private readonly Integer32 _generic;
        private readonly Integer32 _specific;
        private readonly Sequence _varbindSection;
        private readonly byte[] _length;

        /// <summary>
        /// Creates a <see cref="TrapV1Pdu"/> instance with PDU elements.
        /// </summary>
        /// <param name="enterprise">Enterprise</param>
        /// <param name="agent">Agent address</param>
        /// <param name="generic">Generic trap type</param>
        /// <param name="specific">Specific trap type</param>
        /// <param name="timestamp">Time stamp</param>
        /// <param name="variables">Variable binds</param>
        [CLSCompliant(false)]
        public TrapV1Pdu(uint[] enterprise, IP agent, Integer32 generic, Integer32 specific, TimeTicks timestamp, IList<Variable> variables)
            : this(new ObjectIdentifier(enterprise), agent, generic, specific, timestamp, variables) 
        {
        }
        
        /// <summary>
        /// Creates a <see cref="TrapV1Pdu"/> instance with PDU elements.
        /// </summary>
        /// <param name="enterprise">Enterprise</param>
        /// <param name="agent">Agent address</param>
        /// <param name="generic">Generic trap type</param>
        /// <param name="specific">Specific trap type</param>
        /// <param name="timestamp">Time stamp</param>
        /// <param name="variables">Variable binds</param>
        public TrapV1Pdu(ObjectIdentifier enterprise, IP agent, Integer32 generic, Integer32 specific, TimeTicks timestamp, IList<Variable> variables)
        {
            if (enterprise == null)
            {
                throw new ArgumentNullException("enterprise");
            }

            if (agent == null)
            {
                throw new ArgumentNullException("agent");
            }

            if (generic == null)
            {
                throw new ArgumentNullException("generic");
            }

            if (specific == null)
            {
                throw new ArgumentNullException("specific");
            }

            if (timestamp == null)
            {
                throw new ArgumentNullException("timestamp");
            }

            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            Enterprise = enterprise;
            AgentAddress = agent;
            _generic = generic;
            _specific = specific;
            TimeStamp = timestamp;
            _varbindSection = Variable.Transform(variables);
            Variables = variables;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrapV1Pdu"/> class.
        /// </summary>
        /// <param name="length">The length data.</param>
        /// <param name="stream">The stream.</param>
        public TrapV1Pdu(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null) 
            {
                throw new ArgumentNullException("stream");
            }
            
            Enterprise = (ObjectIdentifier)DataFactory.CreateSnmpData(stream);
            AgentAddress = (IP)DataFactory.CreateSnmpData(stream);
            _generic = (Integer32)DataFactory.CreateSnmpData(stream);
            _specific = (Integer32)DataFactory.CreateSnmpData(stream);
            TimeStamp = (TimeTicks)DataFactory.CreateSnmpData(stream);
            _varbindSection = (Sequence)DataFactory.CreateSnmpData(stream);
            Variables = Variable.Transform(_varbindSection);
            _length = length.Item2;
        }

        /// <summary>
        /// Gets the request ID.
        /// </summary>
        /// <value>The request ID.</value>
        public Integer32 RequestId
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the index of the error.
        /// </summary>
        /// <value>The index of the error.</value>
        public Integer32 ErrorIndex
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Gets the error status.
        /// </summary>
        /// <value>The error status.</value>
        public Integer32 ErrorStatus
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode 
        {
            get 
            {
                return SnmpType.TrapV1Pdu;
            }
        }

        /// <summary>
        /// Appends the bytes to <see cref="Stream"/>.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public void AppendBytesTo(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (_raw == null)
            {
                _raw = ByteTool.ParseItems(Enterprise, AgentAddress, _generic, _specific, TimeStamp, _varbindSection);
            }

            stream.AppendBytes(TypeCode, _length, _raw);
        }

        /// <summary>
        /// Enterprise.
        /// </summary>
        public ObjectIdentifier Enterprise { get; private set; }

        /// <summary>
        /// Agent address.
        /// </summary>
        public IP AgentAddress { get; private set; }

        /// <summary>
        /// Generic trap type.
        /// </summary>
        public GenericCode Generic
        {
            get { return (GenericCode)_generic.ToInt32(); }
        }
        
        /// <summary>
        /// Specific trap type.
        /// </summary>
        public int Specific 
        {
            get { return _specific.ToInt32(); }
        }

        /// <summary>
        /// Time stamp.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TimeStamp")]
        public TimeTicks TimeStamp { get; private set; }

        /// <summary>
        /// Variable binds.
        /// </summary>
        public IList<Variable> Variables { get; private set; }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="TrapV1Pdu"/>.
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
                "SNMPv1 TRAP PDU: agent address: {0}; time stamp: {1}; enterprise: {2}; generic: {3}; specific: {4}; varbind count: {5}",
                AgentAddress,
                TimeStamp,
                Enterprise.ToString(objects),
                Generic,
                Specific.ToString(CultureInfo.InvariantCulture),
                Variables.Count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
