// INFORM request message PDU.
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
 * Date: 2008/8/3
 * Time: 15:36
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
    /// INFORM request PDU.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu")]
    public sealed class InformRequestPdu : ISnmpPdu
    {
        private readonly uint[] _timeId = new uint[] { 1, 3, 6, 1, 2, 1, 1, 3, 0 };
        private readonly uint[] _enterpriseId = new uint[] { 1, 3, 6, 1, 6, 3, 1, 1, 4, 1, 0 };
        private byte[] _raw;
        private readonly Sequence _varbindSection;
        private readonly TimeTicks _time;
        private readonly byte[] _length;

        /// <summary>
        /// Creates a <see cref="InformRequestPdu"/> instance with all content.
        /// </summary>
        /// <param name="requestId">The request id.</param>
        /// <param name="enterprise">Enterprise</param>
        /// <param name="time">Time stamp</param>
        /// <param name="variables">Variables</param>
        [CLSCompliant(false)]
        public InformRequestPdu(int requestId, ObjectIdentifier enterprise, uint time, IList<Variable> variables)
        {
            if (enterprise == null)
            {
                throw new ArgumentNullException("enterprise");
            }

            if (variables == null)
            {
                throw new ArgumentNullException("variables");
            }

            Enterprise = enterprise;
            RequestId = new Integer32(requestId);
            _time = new TimeTicks(time);
            Variables = variables;
            IList<Variable> full = new List<Variable>(variables);
            full.Insert(0, new Variable(_timeId, _time));
            full.Insert(1, new Variable(_enterpriseId, Enterprise));
            _varbindSection = Variable.Transform(full);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InformRequestPdu"/> class.
        /// </summary>
        /// <param name="length">The length data.</param>
        /// <param name="stream">The stream.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "temp1")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "temp2")]
        public InformRequestPdu(Tuple<int, byte[]> length, Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            RequestId = (Integer32)DataFactory.CreateSnmpData(stream);
#pragma warning disable 168
            var temp1 = (Integer32)DataFactory.CreateSnmpData(stream); // 0
            var temp2 = (Integer32)DataFactory.CreateSnmpData(stream); // 0
#pragma warning restore 168
            _varbindSection = (Sequence)DataFactory.CreateSnmpData(stream);
            Variables = Variable.Transform(_varbindSection);
            _time = (TimeTicks)Variables[0].Data;
            Variables.RemoveAt(0);
            Enterprise = (ObjectIdentifier)Variables[0].Data;
            Variables.RemoveAt(0);
            _length = length.Item2;
        }

        /// <summary>
        /// Gets the request ID.
        /// </summary>
        /// <value>The request ID.</value>
        public Integer32 RequestId { get; private set; }

        /// <summary>
        /// Gets the error status.
        /// </summary>
        /// <value>The error status.</value>
        public Integer32 ErrorStatus
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
        /// Variables.
        /// </summary>
        public IList<Variable> Variables { get; private set; }

        #region ISnmpData Members
        /// <summary>
        /// Type code.
        /// </summary>
        public SnmpType TypeCode
        {
            get { return SnmpType.InformRequestPdu; }
        }

        /// <summary>
        /// Gets the enterprise.
        /// </summary>
        /// <value>The enterprise.</value>
        public ObjectIdentifier Enterprise { get; private set; }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        /// <value>The time stamp.</value>
        [CLSCompliant(false)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TimeStamp")]
        public uint TimeStamp
        {
            get { return _time.ToUInt32(); }
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
                _raw = ByteTool.ParseItems(RequestId, Integer32.Zero, Integer32.Zero, _varbindSection);
            }

            stream.AppendBytes(TypeCode, _length, _raw);            
        }

        #endregion
        
        /// <summary>
        /// Returns a <see cref="string"/> that represents this <see cref="InformRequestPdu"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "INFORM request PDU: seq: {0}; enterprise: {1}; time stamp: {2}; variable count: {3}",
                RequestId,
                Enterprise,
                _time,
                Variables.Count.ToString(CultureInfo.InvariantCulture));
        }
    }
}
