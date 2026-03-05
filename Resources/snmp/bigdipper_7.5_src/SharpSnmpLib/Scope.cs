// Scope type.
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

namespace Lextm.SharpSnmpLib
{
    /// <summary>
    /// Scope segment.
    /// </summary>
    public sealed class Scope : ISegment
    {
        private readonly Sequence _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public Scope(Sequence data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            _container = data;
            ContextEngineId = (OctetString)data[0];
            ContextName = (OctetString)data[1];
            Pdu = (ISnmpPdu)data[2];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="contextEngineId">The context engine ID.</param>
        /// <param name="contextName">Name of the context.</param>
        /// <param name="pdu">The PDU.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "pdu", Justification = "definition")]
        public Scope(OctetString contextEngineId, OctetString contextName, ISnmpPdu pdu)
        {
            if (contextEngineId == null)
            {
                throw new ArgumentNullException("contextEngineId");
            }

            if (contextName == null)
            {
                throw new ArgumentNullException("contextName");
            }

            if (pdu == null)
            {
                throw new ArgumentNullException("pdu");
            }

            ContextEngineId = contextEngineId;
            ContextName = contextName;
            Pdu = pdu;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Scope"/> class.
        /// </summary>
        /// <param name="pdu">The pdu.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "pdu", Justification = "definition")]
        public Scope(ISnmpPdu pdu)
        {
            if (pdu == null)
            {
                throw new ArgumentNullException("pdu");
            }

            Pdu = pdu;
        }

        /// <summary>
        /// Gets the PDU.
        /// </summary>
        /// <value>The PDU.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Pdu", Justification = "definition")]
        public ISnmpPdu Pdu { get; private set; }

        #region ISegment Members

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns></returns>
        public ISnmpData GetData(VersionCode version)
        {
            if (version == VersionCode.V3)
            {
                return ToSequence();
            }

            return Pdu;
        }

        /// <summary>
        /// Gets or sets the name of the context.
        /// </summary>
        /// <value>The name of the context.</value>
        public OctetString ContextName { get; private set; }

        /// <summary>
        /// Gets or sets the context engine id.
        /// </summary>
        /// <value>The context engine id.</value>
        public OctetString ContextEngineId { get; private set; }

        /// <summary>
        /// Converts to <see cref="Sequence"/> object.
        /// </summary>
        /// <returns></returns>
        public Sequence ToSequence()
        {
            return _container ?? new Sequence(null, ContextEngineId, ContextName, Pdu);
        }

        #endregion
    }
}
